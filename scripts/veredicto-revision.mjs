#!/usr/bin/env node
// Veredicto del check obligatorio `revision-claude` (JG-03). Lee los comentarios del PR y busca la revisión más
// reciente del bot para el commit revisado:
//   - "VEREDICTO: LISTO" sin hallazgos de corrección → el check pasa;
//   - "VEREDICTO: CORREGIR" (o hallazgos de corrección) → falla; solo el coordinador puede integrar con --admin;
//   - ninguna revisión de ese commit (cuota agotada, caída del servicio o tiempo agotado) → falla hasta reintentarla.
// Uso en la CI: node scripts/veredicto-revision.mjs <comentarios.jsonl> <sha>
import { readFileSync, appendFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const BOT = "github-actions[bot]";
const MARCA = "🤖 Revisión automática con Claude";

/** Convierte la salida de `gh api --paginate --jq '.[] | {usuario, body, fecha}'` (un JSON por línea) en un arreglo. */
export function leerComentarios(texto) {
  return texto.split(/\r?\n/).filter((linea) => linea.trim()).map((linea) => JSON.parse(linea));
}

/** Hallazgos de corrección: líneas numeradas entre "CORRECCIÓN" y "OPCIONAL" (o el veredicto). */
function tieneCorrecciones(cuerpo) {
  const seccion = cuerpo.match(/CORRECCI[ÓO]N[^\n]*\n([\s\S]*?)(?:\n\s*OPCIONAL|\n\s*VEREDICTO:|$)/i);
  return Boolean(seccion && /^\s*\d+\./m.test(seccion[1]));
}

export function evaluarVeredicto(comentarios, sha) {
  const revisiones = comentarios
    .filter((c) => c.usuario === BOT && c.body?.trimStart().startsWith(MARCA) && c.body.includes(sha))
    .sort((a, b) => String(a.fecha).localeCompare(String(b.fecha)));
  const ultima = revisiones.at(-1);

  if (!ultima) {
    return {
      estado: "sin_revision",
      mensaje:
        `No hay una revisión completa de Claude para el commit ${sha}: pudo agotarse la cuota, fallar el servicio ` +
        "o pasarse el tiempo. Vuelve a ejecutarla con `gh run rerun <id del run> --failed` o con un commit nuevo.",
    };
  }
  const veredictos = [...ultima.body.matchAll(/VEREDICTO:\s*(LISTO|CORREGIR)/gi)];
  if (veredictos.length === 0) {
    return {
      estado: "sin_revision",
      mensaje: `La revisión del commit ${sha} no terminó con un veredicto. Vuelve a ejecutarla con \`gh run rerun <id del run> --failed\`.`,
    };
  }
  const veredicto = veredictos.at(-1)[1].toUpperCase();
  if (veredicto === "LISTO" && !tieneCorrecciones(ultima.body)) {
    return { estado: "aprobada", mensaje: `La revisión de Claude del commit ${sha} no tiene hallazgos de corrección.` };
  }
  return {
    estado: "corregir",
    mensaje:
      `La revisión de Claude del commit ${sha} tiene hallazgos de corrección (ver el comentario en el PR): corrígelos y ` +
      "haz push. Si es un falso positivo, solo el coordinador puede integrar con `gh pr merge <n> --admin --squash`.",
  };
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  const [archivo, sha] = process.argv.slice(2);
  if (!archivo || !/^[0-9a-f]{40}$/.test(sha ?? "")) {
    console.error("Uso: node scripts/veredicto-revision.mjs <comentarios.jsonl> <sha de 40 caracteres>");
    process.exit(2);
  }
  const resultado = evaluarVeredicto(leerComentarios(readFileSync(archivo, "utf8")), sha);
  console.log(resultado.mensaje);
  if (process.env.GITHUB_STEP_SUMMARY) {
    appendFileSync(process.env.GITHUB_STEP_SUMMARY, `### Revisión con Claude: ${resultado.estado}\n\n${resultado.mensaje}\n`);
  }
  process.exit(resultado.estado === "aprobada" ? 0 : 1);
}
