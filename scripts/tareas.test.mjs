// Pruebas de la validación del título de los PR (JG-01) y de las fechas programadas del calendario (JG-03).
// Se ejecutan con: node --test "scripts/*.test.mjs"
import { readFileSync } from "node:fs";
import { test } from "node:test";
import assert from "node:assert/strict";
import { cargar, clasificar, fechaCorta, reemplazarCalendario, tablaCalendario, validar, validarTituloPr } from "./tareas.mjs";

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

// --- Calendario diario (JG-03, 28 sep): campo programada ---

// Tarea tal como la lee cargar().
function tarea(id, persona, extra = {}) {
  return {
    id, titulo: `Título ${id}`, persona, responsable: "X", avance: "2", prioridad: "P1",
    estado: "pendiente", depende_de: [], archivo: `docs/plan/tareas/${id}-x.md`, ...extra,
  };
}

test("JG-03: toda tarea no hecha necesita programada con formato AAAA-MM-DD", () => {
  assert.deepEqual(validar([tarea("JG-01", "jordin", { estado: "hecha" })]), []);
  assert.deepEqual(validar([tarea("JG-04", "jordin", { programada: "2026-09-28" })]), []);
  assert.match(validar([tarea("JG-04", "jordin")]).join(), /falta el campo "programada"/);
  assert.match(validar([tarea("JG-05", "jordin", { estado: "bloqueada", bloqueo: "x" })]).join(), /falta el campo "programada"/);
  assert.match(validar([tarea("JG-04", "jordin", { programada: "28/09/2026" })]).join(), /programada debe tener el formato AAAA-MM-DD/);
});

test("JG-03: programada no puede ser antes de no_antes_de", () => {
  assert.match(validar([tarea("JG-08", "jordin", { programada: "2026-10-07", no_antes_de: "2026-10-08" })]).join(), /es antes de no_antes_de/);
  assert.deepEqual(validar([tarea("JG-08", "jordin", { programada: "2026-10-08", no_antes_de: "2026-10-08" })]), []);
});

test("JG-03: una tarea se programa después de sus dependencias pendientes, no el mismo día", () => {
  const dep = tarea("DC-04", "dominique", { programada: "2026-09-29" });
  const mismoDia = validar([dep, tarea("JG-05", "jordin", { programada: "2026-09-29", depende_de: ["DC-04"] })]);
  assert.match(mismoDia.join(), /debe ser después de la de su dependencia DC-04 \(2026-09-29\)/);
  assert.deepEqual(validar([dep, tarea("JG-05", "jordin", { programada: "2026-09-30", depende_de: ["DC-04"] })]), []);
  // Una dependencia ya hecha no restringe la fecha.
  const hecha = tarea("DC-04", "dominique", { estado: "hecha", programada: "2026-10-05" });
  assert.deepEqual(validar([hecha, tarea("JG-05", "jordin", { programada: "2026-09-30", depende_de: ["DC-04"] })]), []);
});

test("JG-03: clasificar ordena por fecha programada y marca las atrasadas", () => {
  const tareas = clasificar([
    tarea("JG-05", "jordin", { programada: "2999-01-02" }),
    tarea("JG-07", "jordin", { programada: "2999-01-01" }),
    tarea("JG-04", "jordin", { programada: "2000-01-01" }),
    tarea("JG-01", "jordin", { estado: "hecha" }),
    tarea("JG-02", "jordin", { estado: "hecha", programada: "2000-01-01" }),
  ]);
  assert.deepEqual(tareas.map((t) => t.id), ["JG-02", "JG-04", "JG-07", "JG-05", "JG-01"]);
  assert.deepEqual(tareas.filter((t) => t.atrasada).map((t) => t.id), ["JG-04"]);
});

test("JG-03: fechaCorta da el día de la semana en español", () => {
  assert.equal(fechaCorta("2026-09-28"), "lun 28 sep");
  assert.equal(fechaCorta("2026-10-03"), "sáb 3 oct");
  assert.equal(fechaCorta("2026-11-01"), "dom 1 nov");
});

test("JG-03: la tabla del calendario tiene una fila por día y una columna por persona", () => {
  const tabla = tablaCalendario([
    tarea("JG-04", "jordin", { programada: "2026-09-28" }),
    tarea("EM-04", "emilio", { programada: "2026-09-28" }),
    tarea("JZ-11", "jose-pablo", { programada: "2026-09-29" }),
    tarea("JZ-06", "jose-pablo", { programada: "2026-09-29" }),
    tarea("JG-01", "jordin", { estado: "hecha" }),
  ]);
  assert.equal(
    tabla,
    [
      "| Día | Jordin | Emilio | Dominique | José Pablo |",
      "|---|---|---|---|---|",
      "| Lun 28 sep | **JG-04** Título JG-04 | **EM-04** Título EM-04 | — | — |",
      "| Mar 29 sep | — | — | — | **JZ-06** Título JZ-06<br>**JZ-11** Título JZ-11 |",
    ].join("\n"),
  );
});

test("JG-03: reemplazarCalendario cambia solo el bloque entre las marcas", () => {
  const inicio = "<!-- calendario:inicio (lo genera node scripts/tareas.mjs --calendario --escribir; no lo edites a mano) -->";
  const texto = `# Calendario\n\n${inicio}\nviejo\n<!-- calendario:fin -->\n\nfin\n`;
  assert.equal(reemplazarCalendario(texto, "| a |"), `# Calendario\n\n${inicio}\n\n| a |\n\n<!-- calendario:fin -->\n\nfin\n`);
  assert.equal(reemplazarCalendario("sin marcas", "| a |"), null);
});

test("JG-03: el plan real tiene fechas válidas y calendario.md coincide con ellas", () => {
  const { tareas, errores } = cargar();
  assert.deepEqual(validar(tareas, errores), []);
  const actual = readFileSync(new URL("../docs/plan/calendario.md", import.meta.url), "utf8");
  assert.equal(reemplazarCalendario(actual, tablaCalendario(tareas)), actual);
});
