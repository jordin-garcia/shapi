#!/usr/bin/env node
// Check obligatorio `revision-claude` (JG-03). Lee los comentarios del PR y busca la revisión más reciente del bot
// para el commit:
//   - "VEREDICTO: LISTO" sin hallazgos de corrección → el check pasa;
//   - "VEREDICTO: CORREGIR" (o hallazgos de corrección) → falla; solo el coordinador puede integrar con --admin;
//   - ninguna revisión completa de ese commit y de la tarea del título (cuota agotada, caída del servicio o tiempo
//     agotado) → falla hasta reintentarla.
// Una revisión completa de un commit no se repite con rerun, reabriendo el PR ni editándolo: solo se revisa otra vez
// si cambia la tarea del título. Un commit nuevo (aunque sea vacío) sí produce otra revisión; eso queda en el
// historial del PR y lo revisa la auditoría del coordinador.
// Uso en la CI:
//   node scripts/veredicto-revision.mjs --decidir <comentarios.jsonl> <sha> <id>   → imprime true o false
//   node scripts/veredicto-revision.mjs <comentarios.jsonl> <sha> <id>              → veredicto; sale con 0 o 1
import { readFileSync, appendFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const BOT = "github-actions[bot]";
const MARCA = "🤖 Revisión automática con Claude";
// Las palabras clave pueden venir con formato Markdown: **CORRECCIÓN**, ### OPCIONAL, > VEREDICTO… El prefijo no
// acepta saltos de línea: si los aceptara, el encabezado empezaría en la línea vacía anterior y la propia línea
// "CORRECCIÓN (…)" contaría como un hallazgo.
const PREFIJO = String.raw`^[ \t>*#_\-]*`;
// Un encabezado de sección no se puede confundir con el texto libre ("Corrección de auditoría…", "Opcionalmente…"):
//   - en MAYÚSCULAS (el formato de revision.md), después de la palabra viene "(", ":", un guion o el fin de la línea;
//   - con mayúscula inicial ("Corrección"), solo "(", ":" o el fin de la línea.
function encabezado(mayusculas, inicial) {
  return PREFIJO + String.raw`(?:${mayusculas}\**[ \t]*(?:[(:—–\-]|$)|${inicial}\**[ \t]*(?:[(:]|$))`;
}
const PALABRA_CORRECCION = String.raw`(?:CORRECCI[ÓO]N(?:ES)?|Correcci[óo]n(?:es)?)`;
const RE_CORRECCION = new RegExp(encabezado(String.raw`CORRECCI[ÓO]N(?:ES)?`, String.raw`Correcci[óo]n(?:es)?`), "m");
const RE_OPCIONAL = new RegExp(encabezado(String.raw`OPCIONAL(?:ES)?`, String.raw`Opcional(?:es)?`), "m");
// El veredicto también se acepta como "Veredicto:" o "**Veredicto:** LISTO"; LISTO y CORREGIR van en mayúsculas.
const RE_VEREDICTO = new RegExp(PREFIJO + String.raw`(?:VEREDICTO|Veredicto)\**[ \t]*:[ \t]*\**[ \t]*(LISTO|CORREGIR)`, "m");
// Se quita de la línea del encabezado su descripción ("(obligatorio corregir):", "— obligatorio corregir"); lo que
// quede después también es un hallazgo.
const RE_RESTO_CABECERA = new RegExp(PREFIJO + PALABRA_CORRECCION +
  String.raw`\**[ \t]*(?:[—–\-][ \t]*)?(?:\([^)]*\)|obligatorio corregir)?[ \t]*:?\**`);
const RE_NINGUNO = /^\(?(?:ningun[oa]|no hay(?: hallazgos)?)\.?\)?\.?$/i;

/** Convierte la salida de `gh api --paginate --jq '.[] | {usuario, body, fecha, editado}'` (un JSON por línea) en un arreglo. */
export function leerComentarios(texto) {
  return texto.split(/\r?\n/).filter((linea) => linea.trim()).map((linea) => JSON.parse(linea));
}

/**
 * El veredicto es el primero que aparece después de la sección de corrección (las notas posteriores no cuentan).
 * Sin una sección de corrección reconocida no se sabe si hay hallazgos: la revisión cuenta como incompleta.
 */
function analizar(cuerpo) {
  const inicio = cuerpo.search(RE_CORRECCION);
  if (inicio < 0) return null;
  const resto = cuerpo.slice(inicio);
  const veredicto = resto.match(RE_VEREDICTO);
  if (!veredicto) return null;
  // La sección de corrección llega hasta OPCIONAL o hasta el veredicto, lo que venga primero.
  const fin = [resto.slice(1).search(RE_OPCIONAL), resto.slice(1).search(RE_VEREDICTO)]
    .filter((i) => i >= 0).map((i) => i + 1);
  const [cabecera, ...siguientes] = resto.slice(0, fin.length ? Math.min(...fin) : undefined).split(/\r?\n/);
  const lineas = [cabecera.replace(RE_RESTO_CABECERA, ""), ...siguientes];
  // Cualquier contenido que no sea "Ninguno" es un hallazgo, venga numerado o con viñetas. Se ignoran las líneas
  // vacías y las cercas de código.
  const correcciones = lineas.some((l) => {
    const texto = l.replace(/[*_`>]/g, "").trim();
    return texto !== "" && !RE_NINGUNO.test(texto);
  });
  return { veredicto: veredicto[1].toUpperCase(), correcciones, tarea: cuerpo.match(/REVISI[ÓO]N\s+([A-Z]{2}-\d{2,})/)?.[1] ?? "" };
}

/** ¿El comentario es una revisión de este commit? El SHA tiene que estar en la línea "Commit revisado". */
function esRevisionDe(c, sha) {
  return c.usuario === BOT && c.body?.trimStart().startsWith(MARCA) &&
    new RegExp(String.raw`commit revisado:?\s*${sha}\b`, "i").test(c.body);
}

/**
 * Revisiones completas (con veredicto) del bot para el commit, de la más antigua a la más reciente. Una revisión
 * editada después de publicarse queda marcada como alterada: quien tiene permiso de escritura puede editar
 * comentarios ajenos, y así podría cambiar CORREGIR por LISTO.
 */
function revisionesCompletas(comentarios, sha) {
  return comentarios
    .filter((c) => esRevisionDe(c, sha))
    .sort((a, b) => String(a.fecha).localeCompare(String(b.fecha)))
    .map((c) => {
      const analisis = analizar(c.body);
      return analisis && { ...analisis, alterada: Boolean(c.editado) };
    })
    .filter(Boolean);
}

/** ¿Hace falta revisar este commit? Solo si no tiene una revisión completa o si cambió la tarea del título. */
export function decidirRevision(comentarios, sha, tarea) {
  const ultima = revisionesCompletas(comentarios, sha).at(-1);
  if (ultima?.alterada) return false;
  return !ultima || ultima.tarea !== (tarea ?? "");
}

export function evaluarVeredicto(comentarios, sha, tarea = "") {
  const ultima = revisionesCompletas(comentarios, sha).at(-1);
  // Una revisión de otra tarea (el título cambió después) no cuenta: la del título actual no se completó.
  if (!ultima || (!ultima.alterada && ultima.tarea !== tarea)) {
    return {
      estado: "sin_revision",
      mensaje:
        `No hay una revisión completa de Claude para el commit ${sha}: pudo agotarse la cuota, fallar el servicio ` +
        "o pasarse el tiempo. Vuelve a ejecutarla con `gh run rerun <id del run> --failed` o con un commit nuevo.",
    };
  }
  if (ultima.alterada) {
    return {
      estado: "corregir",
      mensaje:
        `El comentario de la revisión del commit ${sha} se editó después de publicarse, así que su veredicto no ` +
        "cuenta. Solo el coordinador puede integrar este PR, con `gh pr merge <n> --admin --squash`, o se revisa de " +
        "nuevo con un commit nuevo.",
    };
  }
  if (ultima.veredicto === "LISTO" && !ultima.correcciones) {
    return { estado: "aprobada", mensaje: `La revisión de Claude del commit ${sha} no tiene hallazgos de corrección.` };
  }
  return {
    estado: "corregir",
    mensaje:
      `La revisión de Claude del commit ${sha} tiene hallazgos de corrección (ver el comentario en el PR): corrígelos y ` +
      "haz push; volver a ejecutarla sobre el mismo commit no la repite. Si es un falso positivo, solo el coordinador " +
      "puede integrar con `gh pr merge <n> --admin --squash`.",
  };
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  const args = process.argv.slice(2);
  const decidir = args[0] === "--decidir";
  const [archivo, sha, tarea = ""] = decidir ? args.slice(1) : args;
  if (!archivo || !/^[0-9a-f]{40}$/.test(sha ?? "")) {
    console.error("Uso: node scripts/veredicto-revision.mjs [--decidir] <comentarios.jsonl> <sha de 40 caracteres> [id]");
    process.exit(2);
  }
  const comentarios = leerComentarios(readFileSync(archivo, "utf8"));
  if (decidir) {
    console.log(String(decidirRevision(comentarios, sha, tarea)));
    process.exit(0);
  }
  const resultado = evaluarVeredicto(comentarios, sha, tarea);
  console.log(resultado.mensaje);
  if (process.env.GITHUB_STEP_SUMMARY) {
    appendFileSync(process.env.GITHUB_STEP_SUMMARY, `### Revisión con Claude: ${resultado.estado}\n\n${resultado.mensaje}\n`);
  }
  process.exit(resultado.estado === "aprobada" ? 0 : 1);
}
