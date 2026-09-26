// Pruebas del veredicto de la revisión con Claude (JG-03, check obligatorio). Se ejecutan con: node --test "scripts/*.test.mjs"
import { test } from "node:test";
import assert from "node:assert/strict";
import { decidirRevision, evaluarVeredicto, leerComentarios } from "./veredicto-revision.mjs";

const SHA = "2a8538104b56392157a385811d04449a8cdf6df6";
const OTRO_SHA = "1238c51eb2b8be7c23df68d290fa5315b60d4d3a";
const BOT = "github-actions[bot]";

const revision = (sha, cuerpo, { formato = "linea" } = {}) =>
  (formato === "linea"
    ? `🤖 Revisión automática con Claude\nCommit revisado: ${sha}\n\n`
    : `🤖 Revisión automática con Claude — commit revisado: ${sha}\n\n`) + "```\n" + cuerpo + "\n```";

const LISTO = "REVISIÓN EM-03\nCORRECCIÓN (obligatorio corregir):\nNinguno\n\nOPCIONAL (no bloquea):\n1. Un detalle.\n\nVEREDICTO: LISTO";
const CORREGIR = "REVISIÓN EM-06\nCORRECCIÓN (obligatorio corregir):\n1. [a.cs:4] Las pruebas no compilan → moverlas.\nOPCIONAL (no bloquea):\nNinguno\nVEREDICTO: CORREGIR";

const comentario = (body, usuario = BOT, fecha = "2026-09-26T10:00:00Z") => ({ usuario, body, fecha });

test("aprueba cuando la última revisión de ese commit dice LISTO sin correcciones", () => {
  const r = evaluarVeredicto([comentario(revision(SHA, LISTO))], SHA, "EM-03");
  assert.equal(r.estado, "aprobada");
});

test("también reconoce el commit en la línea del título", () => {
  const r = evaluarVeredicto([comentario(revision(SHA, LISTO, { formato: "titulo" }))], SHA, "EM-03");
  assert.equal(r.estado, "aprobada");
});

test("falla cuando la revisión pide corregir", () => {
  const r = evaluarVeredicto([comentario(revision(SHA, CORREGIR))], SHA, "EM-06");
  assert.equal(r.estado, "corregir");
  assert.match(r.mensaje, /--admin/);
});

test("falla si dice LISTO pero enumera hallazgos de corrección", () => {
  const contradictorio = CORREGIR.replace("VEREDICTO: CORREGIR", "VEREDICTO: LISTO");
  assert.equal(evaluarVeredicto([comentario(revision(SHA, contradictorio))], SHA, "EM-06").estado, "corregir");
});

test("usa la revisión más reciente del mismo commit", () => {
  const comentarios = [
    comentario(revision(SHA, CORREGIR), BOT, "2026-09-26T10:00:00Z"),
    comentario(revision(SHA, LISTO), BOT, "2026-09-26T11:00:00Z"),
  ];
  assert.equal(evaluarVeredicto(comentarios, SHA, "EM-03").estado, "aprobada");
  assert.equal(evaluarVeredicto([...comentarios].reverse(), SHA, "EM-03").estado, "aprobada");
});

test("no usa la revisión de otro commit: sin revisión del commit actual, bloquea", () => {
  const r = evaluarVeredicto([comentario(revision(OTRO_SHA, LISTO))], SHA);
  assert.equal(r.estado, "sin_revision");
  assert.match(r.mensaje, /gh run rerun/);
});

test("ignora comentarios que imitan la revisión pero no son del bot", () => {
  const r = evaluarVeredicto([comentario(revision(SHA, LISTO), "MiloDou")], SHA);
  assert.equal(r.estado, "sin_revision");
});

test("ignora comentarios del bot que no son una revisión", () => {
  const r = evaluarVeredicto([comentario(`Otro aviso del bot. Commit revisado: ${SHA}\nVEREDICTO: LISTO`)], SHA);
  assert.equal(r.estado, "sin_revision");
});

test("una revisión sin veredicto cuenta como no completada", () => {
  const incompleta = revision(SHA, "REVISIÓN EM-03\nCORRECCIÓN (obligatorio corregir):\nNinguno");
  assert.equal(evaluarVeredicto([comentario(incompleta)], SHA).estado, "sin_revision");
});

test("sin ningún comentario, la revisión no se completó (cuota, caída o tiempo)", () => {
  assert.equal(evaluarVeredicto([], SHA).estado, "sin_revision");
});

test("lee los comentarios en JSON por línea, como los entrega gh api --paginate --jq", () => {
  const texto = [
    JSON.stringify({ usuario: BOT, body: revision(SHA, LISTO), fecha: "2026-09-26T10:00:00Z" }),
    "",
    JSON.stringify({ usuario: "MiloDou", body: "Listo, lo reviso", fecha: "2026-09-26T10:05:00Z" }),
  ].join("\n");
  const comentarios = leerComentarios(texto);
  assert.equal(comentarios.length, 2);
  assert.equal(evaluarVeredicto(comentarios, SHA, "EM-03").estado, "aprobada");
});

test("acepta las palabras clave con formato Markdown sin contar los hallazgos opcionales", () => {
  const markdown = [
    "## REVISIÓN EM-03", "**CORRECCIÓN (obligatorio corregir):**", "Ninguno", "",
    "### OPCIONAL (no bloquea):", "1. Un detalle.", "2. Otro.", "", "**VEREDICTO: LISTO**",
  ].join("\n");
  assert.equal(evaluarVeredicto([comentario(revision(SHA, markdown))], SHA, "EM-03").estado, "aprobada");
  const conHallazgo = markdown.replace("Ninguno", "1. [a.ts:3] Falta la prueba.").replace("LISTO", "CORREGIR");
  assert.equal(evaluarVeredicto([comentario(revision(SHA, conHallazgo))], SHA, "EM-03").estado, "corregir");
});

test("los hallazgos con viñetas también cuentan como corrección", () => {
  const conVinetas = "REVISIÓN EM-03\nCORRECCIÓN (obligatorio corregir):\n- [a.ts:3] Falta la prueba.\n\nOPCIONAL (no bloquea):\nNinguno\nVEREDICTO: LISTO";
  assert.equal(evaluarVeredicto([comentario(revision(SHA, conVinetas))], SHA, "EM-03").estado, "corregir");
  // "Ninguno" en negrita o con punto, y la cerca de código, no son hallazgos.
  const sinHallazgos = "REVISIÓN EM-03\nCORRECCIÓN (obligatorio corregir):\n**Ninguno.**\n```\n\nVEREDICTO: LISTO";
  assert.equal(evaluarVeredicto([comentario(`🤖 Revisión automática con Claude\nCommit revisado: ${SHA}\n\n\`\`\`\n${sinHallazgos}`)], SHA, "EM-03").estado, "aprobada");
});

test("las notas posteriores al veredicto no lo cambian", () => {
  const conNotas = revision(SHA, LISTO) + "\n\nNotas de la revisión: la ronda anterior decía\nVEREDICTO: CORREGIR";
  assert.equal(evaluarVeredicto([comentario(conNotas)], SHA, "EM-03").estado, "aprobada");
});

test("no vuelve a revisar un commit que ya tiene una revisión completa de la misma tarea (rerun o reabrir)", () => {
  assert.equal(decidirRevision([comentario(revision(SHA, CORREGIR))], SHA, "EM-06"), false);
  assert.equal(decidirRevision([comentario(revision(SHA, LISTO))], SHA, "EM-03"), false);
});

test("revisa si el commit no tiene una revisión completa", () => {
  assert.equal(decidirRevision([], SHA, "EM-03"), true);
  assert.equal(decidirRevision([comentario(revision(OTRO_SHA, LISTO))], SHA, "EM-03"), true);
  const incompleta = revision(SHA, "REVISIÓN EM-03\nCORRECCIÓN (obligatorio corregir):\nNinguno");
  assert.equal(decidirRevision([comentario(incompleta)], SHA, "EM-03"), true);
});

test("una revisión editada después de publicarse no cuenta y no se repite", () => {
  const editada = { ...comentario(revision(SHA, CORREGIR.replace("VEREDICTO: CORREGIR", "VEREDICTO: LISTO").replace(/1\. \[a\.cs:4\][^\n]*/, "Ninguno"))), editado: true };
  const r = evaluarVeredicto([editada], SHA);
  assert.equal(r.estado, "corregir");
  assert.match(r.mensaje, /se editó/);
  assert.equal(decidirRevision([editada], SHA, "EM-06"), false);
});

test("el commit tiene que estar en la línea \"Commit revisado\", no citado en otra parte", () => {
  const citaOtro = revision(OTRO_SHA, LISTO) + `\n\nNota: el commit anterior era ${SHA}.`;
  assert.equal(evaluarVeredicto([comentario(citaOtro)], SHA).estado, "sin_revision");
});

test("una revisión de otra tarea no cuenta: si la del título actual no se completó, bloquea", () => {
  // La revisión de EM-03 aprobó, pero el título ahora dice EM-04 y su revisión nueva no llegó.
  assert.equal(evaluarVeredicto([comentario(revision(SHA, LISTO))], SHA, "EM-04").estado, "sin_revision");
  // La revisión reducida (título sin ID) no cuenta cuando el ID ya se corrigió.
  const sinId = revision(SHA, "REVISIÓN (título sin ID)\nCORRECCIÓN (obligatorio corregir):\nNinguno\nVEREDICTO: LISTO");
  assert.equal(evaluarVeredicto([comentario(sinId)], SHA, "EM-03").estado, "sin_revision");
  assert.equal(evaluarVeredicto([comentario(sinId)], SHA, "").estado, "aprobada");
});

test("revisa otra vez el mismo commit solo si cambió la tarea del título", () => {
  const sinId = revision(SHA, "REVISIÓN (título sin ID)\nCORRECCIÓN (obligatorio corregir):\nNinguno\nVEREDICTO: LISTO");
  assert.equal(decidirRevision([comentario(sinId)], SHA, "EM-03"), true);
  assert.equal(decidirRevision([comentario(sinId)], SHA, ""), false);
  assert.equal(decidirRevision([comentario(revision(SHA, CORREGIR))], SHA, "EM-07"), true);
});

