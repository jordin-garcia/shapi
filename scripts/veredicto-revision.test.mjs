// Pruebas del veredicto de la revisión con Claude (JG-03, check obligatorio). Se ejecutan con: node --test "scripts/*.test.mjs"
import { test } from "node:test";
import assert from "node:assert/strict";
import { evaluarVeredicto, leerComentarios } from "./veredicto-revision.mjs";

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
  const r = evaluarVeredicto([comentario(revision(SHA, LISTO))], SHA);
  assert.equal(r.estado, "aprobada");
});

test("también reconoce el commit en la línea del título", () => {
  const r = evaluarVeredicto([comentario(revision(SHA, LISTO, { formato: "titulo" }))], SHA);
  assert.equal(r.estado, "aprobada");
});

test("falla cuando la revisión pide corregir", () => {
  const r = evaluarVeredicto([comentario(revision(SHA, CORREGIR))], SHA);
  assert.equal(r.estado, "corregir");
  assert.match(r.mensaje, /--admin/);
});

test("falla si dice LISTO pero enumera hallazgos de corrección", () => {
  const contradictorio = CORREGIR.replace("VEREDICTO: CORREGIR", "VEREDICTO: LISTO");
  assert.equal(evaluarVeredicto([comentario(revision(SHA, contradictorio))], SHA).estado, "corregir");
});

test("usa la revisión más reciente del mismo commit", () => {
  const comentarios = [
    comentario(revision(SHA, CORREGIR), BOT, "2026-09-26T10:00:00Z"),
    comentario(revision(SHA, LISTO), BOT, "2026-09-26T11:00:00Z"),
  ];
  assert.equal(evaluarVeredicto(comentarios, SHA).estado, "aprobada");
  assert.equal(evaluarVeredicto([...comentarios].reverse(), SHA).estado, "aprobada");
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
  assert.equal(evaluarVeredicto(comentarios, SHA).estado, "aprobada");
});
