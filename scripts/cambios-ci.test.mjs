// Pruebas de la decisión de qué verificaciones necesita un PR (JG-18). Se ejecutan con: node --test "scripts/*.test.mjs"
import { test } from "node:test";
import assert from "node:assert/strict";
import { afectaA, decidir } from "./cambios-ci.mjs";

test("JG-18: un PR solo de frontend no ejecuta el backend, pero sí el frontend", () => {
  const archivos = ["frontend/apps/portal/src/paginas/A5-3-Registro.tsx", "docs/plan/tareas/DC-08-x.md", "docs/plan/bitacora/dominique.md"];
  assert.equal(afectaA("backend", archivos), false);
  assert.equal(afectaA("frontend", archivos), true);
});

test("JG-18: un PR solo de backend no ejecuta el frontend, pero sí el backend", () => {
  const archivos = ["src/Shapi.Api/Planes/Endpoints.cs", "tests/Shapi.Api.Tests/Planes/PlanesTests.cs", "docs/plan/bitacora/emilio.md"];
  assert.equal(afectaA("backend", archivos), true);
  assert.equal(afectaA("frontend", archivos), false);
});

test("JG-18: un PR solo del plan (bloqueada, convergencia) no ejecuta ninguno de los dos", () => {
  const archivos = ["docs/plan/tareas/JZ-12-x.md", "docs/plan/bitacora/jose-pablo.md", "docs/plan/calendario.md", "scripts/tablero.mjs"];
  assert.equal(afectaA("backend", archivos), false);
  assert.equal(afectaA("frontend", archivos), false);
});

test("JG-18: lo que leen las pruebas de .NET ejecuta el backend", () => {
  // Especificaciones (códigos de error, bitácora), mockups (orígenes de demo), compose, contratos y la solución.
  for (const archivo of ["docs/specs/08-compuerta.md", "mockups/A5/A5-1.dc.html", "infra/compose.yml", "contratos/openapi/planes.yaml",
    "origenes-demo/agro-precios/openapi.yaml", "Directory.Packages.props", "Shapi.slnx", ".editorconfig"]) {
    assert.equal(afectaA("backend", [archivo]), true, archivo);
  }
});

test("JG-18: lo que leen las pruebas de Vitest ejecuta el frontend", () => {
  for (const archivo of ["contratos/openapi/identidad.yaml", "docs/specs/11-interfaz.md", "frontend/pnpm-lock.yaml", ".editorconfig"]) {
    assert.equal(afectaA("frontend", [archivo]), true, archivo);
  }
});

test("JG-18: ci.yml y cualquier ruta desconocida ejecutan las dos áreas", () => {
  for (const archivo of [".github/workflows/ci.yml", "carpeta-nueva/algo.txt", "global.json", ".github/workflows/nuevo.yml",
    ".gitattributes", ".editorconfig"]) {
    assert.equal(afectaA("backend", [archivo]), true, archivo);
    assert.equal(afectaA("frontend", [archivo]), true, archivo);
  }
});

test("JG-18: de las especificaciones, solo 11-interfaz.md (y una nueva) ejecuta el frontend", () => {
  assert.equal(afectaA("frontend", ["docs/specs/08-compuerta.md", "docs/specs/12-decisiones.md"]), false);
  assert.equal(afectaA("frontend", ["docs/specs/11-interfaz.md"]), true);
  assert.equal(afectaA("frontend", ["docs/specs/13-nueva.md"]), true);
  // El backend sí lee otras especificaciones (08 y 10): cualquier cambio en docs/specs/ lo ejecuta.
  assert.equal(afectaA("backend", ["docs/specs/12-decisiones.md"]), true);
});

test("JG-18: los scripts que deciden qué se verifica ejecutan las dos áreas; los demás scripts, ninguna", () => {
  for (const archivo of ["scripts/cambios-ci.mjs", "scripts/tareas.mjs"]) {
    assert.equal(afectaA("backend", [archivo]), true, archivo);
    assert.equal(afectaA("frontend", [archivo]), true, archivo);
  }
  assert.equal(afectaA("backend", ["scripts/tablero.mjs", "scripts/tareas.test.mjs"]), false);
});

test("JG-18: un prefijo de archivo no cubre otro archivo con el mismo inicio", () => {
  assert.equal(afectaA("backend", ["README.md.bak"]), true);
  assert.equal(afectaA("backend", ["docs/planes.md"]), true);
});

test("JG-18: fuera de un PR, o sin diff, se verifica todo", () => {
  assert.equal(decidir("backend", "push", []).ejecutar, true);
  assert.equal(decidir("backend", "workflow_dispatch", []).ejecutar, true);
  assert.equal(decidir("backend", "pull_request", null).ejecutar, true);
  assert.equal(decidir("backend", "pull_request", ["frontend/package.json"]).ejecutar, false);
});

test("JG-18: un PR vacío no ejecuta nada", () => {
  assert.equal(decidir("frontend", "pull_request", []).ejecutar, false);
});

test("JG-18: un área desconocida es un error", () => {
  assert.throws(() => afectaA("compuerta", []), /Área desconocida/);
});

test("auditoría 2026-10-03, H-36: mover una especificación que leen las pruebas a una carpeta libre sí verifica", () => {
  // Con --no-renames, archivosDelPr da la ruta de origen y la de destino.
  const movida = ["docs/specs/08-compuerta.md", "docs/plan/08-compuerta.md"];
  assert.equal(decidir("backend", "pull_request", movida).ejecutar, true);
  const movidaInterfaz = ["docs/specs/11-interfaz.md", "docs/plan/11-interfaz.md"];
  assert.equal(decidir("frontend", "pull_request", movidaInterfaz).ejecutar, true);
  // Solo con la ruta de destino (lo que daba el diff con detección de renombres) no se verificaba.
  assert.equal(decidir("backend", "pull_request", ["docs/plan/08-compuerta.md"]).ejecutar, false);
});
