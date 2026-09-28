// Pruebas de la validación del título de los PR (JG-01). Se ejecutan con: node --test "scripts/*.test.mjs"
import { test } from "node:test";
import assert from "node:assert/strict";
import { validarTituloPr } from "./tareas.mjs";

const IDS = ["JG-01", "EM-03", "DC-04", "JZ-12"];

test("acepta [ID] título con una tarea del plan", () => {
  assert.deepEqual(validarTituloPr("[EM-03] Pantallas de registro, verificación y acceso", IDS), []);
  assert.deepEqual(validarTituloPr("[JZ-12] Bloqueada: falta la cuenta de correo", IDS), []);
  assert.deepEqual(validarTituloPr("[JG-01] Validar el título de los PR  ", IDS), []);
});

test("rechaza títulos sin el ID entre corchetes al inicio", () => {
  for (const titulo of [
    "EM-02: Registro e inicio de sesión del personal",
    "fix(ci): quitar el límite de turnos (JG-03)",
    "Pantallas de registro [EM-03]",
    "[em-03] Pantallas de registro",
    "(EM-03) Pantallas de registro",
    "  [EM-03] Pantallas de registro", // la revisión con Claude no encontraría el ID
  ]) {
    const errores = validarTituloPr(titulo, IDS);
    assert.equal(errores.length, 1, titulo);
    assert.match(errores[0], /no sigue el formato "\[<ID>\] <título>"/);
  }
});

test("rechaza el ID sin título, sin espacio o vacío", () => {
  for (const titulo of ["[EM-03]", "[EM-03]   ", "[EM-03]Pantallas", "", undefined]) {
    assert.equal(validarTituloPr(titulo, IDS).length, 1, String(titulo));
  }
});

test("rechaza un ID que no existe en el plan", () => {
  const errores = validarTituloPr("[EM-99] Tarea inventada", IDS);
  assert.deepEqual(errores, ["La tarea EM-99 del título no existe en docs/plan/tareas/."]);
});
