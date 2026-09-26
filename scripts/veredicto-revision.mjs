#!/usr/bin/env node
// Check obligatorio `revision-claude` (JG-03). Lee los comentarios del PR y busca la revisión más reciente del bot
// para el commit:
//   - "VEREDICTO: LISTO" sin hallazgos de corrección → el check pasa;
//   - "VEREDICTO: CORREGIR" (o hallazgos de corrección) → falla; solo el coordinador puede integrar con --admin;
//   - ninguna revisión completa de ese commit (cuota agotada, caída del servicio o tiempo agotado) → falla hasta
//     reintentarla.
// Una revisión completa de un commit no se repite (ni con rerun, ni reabriendo el PR): solo se revisa otra vez si
// cambia la tarea del título. Así nadie puede repetirla hasta que salga LISTO.
// Uso en la CI:
//   node scripts/veredicto-revision.mjs --decidir <comentarios.jsonl> <sha> <id>   → imprime true o false
//   node scripts/veredicto-revision.mjs <comentarios.jsonl> <sha>                   → veredicto; sale con 0 o 1
import { readFileSync, appendFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const BOT = "github-actions[bot]";
const MARCA = "🤖 Revisión automática con Claude";
// Las palabras clave pueden venir con formato Markdown: **CORRECCIÓN**, ### OPCIONAL, > VEREDICTO…
const PREFIJO = String.raw`^[\s>*#_\-]*`;
const RE_CORRECCION = new RegExp(PREFIJO + String.raw`CORRECCI[ÓO]N`, "im");
const RE_OPCIONAL = new RegExp(PREFIJO + String.raw`OPCIONAL`, "im");
const RE_VEREDICTO = new RegExp(PREFIJO + String.raw`VEREDICTO:\s*\**\s*(LISTO|CORREGIR)`, "im");

/** Convierte la salida de `gh api --paginate --jq '.[] | {usuario, body, fecha}'` (un JSON por línea) en un arreglo. */
export function leerComentarios(texto) {
  return texto.split(/\r?\n/).filter((linea) => linea.trim()).map((linea) => JSON.parse(linea));
}

/** El veredicto es el primero que aparece después de la sección de corrección (las notas posteriores no cuentan). */
function analizar(cuerpo) {
  const inicio = cuerpo.search(RE_CORRECCION);
  const desde = inicio >= 0 ? inicio : 0;
  const resto = cuerpo.slice(desde);
  const veredicto = resto.match(RE_VEREDICTO);
  if (!veredicto) return null;
  let correcciones = false;
  if (inicio >= 0) {
    // La sección de corrección llega hasta OPCIONAL o hasta el veredicto, lo que venga primero.
    const fin = [resto.slice(1).search(RE_OPCIONAL), resto.slice(1).search(RE_VEREDICTO)]
      .filter((i) => i >= 0).map((i) => i + 1);
    const seccion = resto.slice(0, fin.length ? Math.min(...fin) : undefined).split(/\r?\n/).slice(1).join("\n");
    correcciones = /^\s*\d+\./m.test(seccion);
  }
  return { veredicto: veredicto[1].toUpperCase(), correcciones, tarea: cuerpo.match(/REVISI[ÓO]N\s+([A-Z]{2}-\d{2,})/)?.[1] ?? "" };
}

/** Revisiones completas (con veredicto) del bot para el commit, de la más antigua a la más reciente. */
function revisionesCompletas(comentarios, sha) {
  return comentarios
    .filter((c) => c.usuario === BOT && c.body?.trimStart().startsWith(MARCA) && c.body.includes(sha))
    .sort((a, b) => String(a.fecha).localeCompare(String(b.fecha)))
    .map((c) => analizar(c.body))
    .filter(Boolean);
}

/** ¿Hace falta revisar este commit? Solo si no tiene una revisión completa o si cambió la tarea del título. */
export function decidirRevision(comentarios, sha, tarea) {
  const ultima = revisionesCompletas(comentarios, sha).at(-1);
  return !ultima || ultima.tarea !== (tarea ?? "");
}

export function evaluarVeredicto(comentarios, sha) {
  const ultima = revisionesCompletas(comentarios, sha).at(-1);
  if (!ultima) {
    return {
      estado: "sin_revision",
      mensaje:
        `No hay una revisión completa de Claude para el commit ${sha}: pudo agotarse la cuota, fallar el servicio ` +
        "o pasarse el tiempo. Vuelve a ejecutarla con `gh run rerun <id del run> --failed` o con un commit nuevo.",
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
  const resultado = evaluarVeredicto(comentarios, sha);
  console.log(resultado.mensaje);
  if (process.env.GITHUB_STEP_SUMMARY) {
    appendFileSync(process.env.GITHUB_STEP_SUMMARY, `### Revisión con Claude: ${resultado.estado}\n\n${resultado.mensaje}\n`);
  }
  process.exit(resultado.estado === "aprobada" ? 0 : 1);
}
