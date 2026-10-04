// Pruebas de la validación del título de los PR (JG-01) y de las fechas programadas del calendario (JG-03).
// Se ejecutan con: node --test "scripts/*.test.mjs"
import { readFileSync } from "node:fs";
import { test } from "node:test";
import assert from "node:assert/strict";
import { archivosDelPr, cargar, clasificar, estadoEnMain, fechaCorta, reemplazarCalendario, resumenHoy, tablaCalendario, validar, validarCierrePr, validarTituloPr } from "./tareas.mjs";

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
  // Con CRLF, el bloque generado también usa CRLF.
  const crlf = texto.replaceAll("\n", "\r\n");
  assert.equal(reemplazarCalendario(crlf, "| a |\n| b |"), `# Calendario\r\n\r\n${inicio}\r\n\r\n| a |\r\n| b |\r\n\r\n<!-- calendario:fin -->\r\n\r\nfin\r\n`);
  // Una marca de fin citada antes de la de inicio no cuenta.
  assert.equal(reemplazarCalendario(`<!-- calendario:fin -->\n${inicio}\n`, "| a |"), null);
});

test("JG-03: el plan real tiene fechas válidas y calendario.md coincide con ellas", () => {
  const { tareas, errores } = cargar();
  assert.deepEqual(validar(tareas, errores), []);
  const actual = readFileSync(new URL("../docs/plan/calendario.md", import.meta.url), "utf8");
  assert.equal(reemplazarCalendario(actual, tablaCalendario(tareas)), actual);
});

// ---------- JG-18: cierre de la tarea en el PR (protocolo B10) ----------

const HECHA = { id: "EM-07", estado: "hecha", archivo: "docs/plan/tareas/EM-07-planes.md" };
const CON_RESULTADO = "---\nestado: hecha\n---\n# EM-07\n\n## Resultado\n- Hecho.\n";
const CON_BITACORA = ["src/a.cs", "docs/plan/tareas/EM-07-planes.md", "docs/plan/bitacora/emilio.md"];

test("JG-18: un PR que cierra su tarea con Resultado y bitácora es válido", () => {
  assert.deepEqual(validarCierrePr("[EM-07] Planes de API", HECHA, CON_RESULTADO, CON_BITACORA), []);
});

test("JG-18: la tarea tiene que quedar hecha y con ## Resultado", () => {
  const errores = validarCierrePr("[EM-07] Planes de API", { ...HECHA, estado: "pendiente" }, "# EM-07\n## Resultados\n", CON_BITACORA);
  assert.equal(errores.length, 2);
  assert.match(errores[0], /estado: hecha/);
  assert.match(errores[1], /## Resultado/);
  // Con saltos de línea de Windows también se reconoce la sección.
  assert.deepEqual(validarCierrePr("[EM-07] Planes", HECHA, "# EM-07\r\n\r\n## Resultado\r\n- Hecho.\r\n", CON_BITACORA), []);
});

test("JG-18: el PR tiene que agregar una entrada en alguna bitácora", () => {
  const errores = validarCierrePr("[EM-07] Planes de API", HECHA, CON_RESULTADO, ["src/a.cs", "docs/plan/bitacora/README/otro.md"]);
  assert.equal(errores.length, 1);
  assert.match(errores[0], /bitacora/);
  // Si no se conocen los archivos del PR (fuera de la CI), la bitácora no se revisa.
  assert.deepEqual(validarCierrePr("[EM-07] Planes de API", HECHA, CON_RESULTADO, null), []);
});

test("JG-18: un PR «Bloqueada:» deja la tarea bloqueada, sin Resultado", () => {
  const bloqueada = { ...HECHA, estado: "bloqueada" };
  assert.deepEqual(validarCierrePr("[EM-07] Bloqueada: falta la cuenta", bloqueada, "# EM-07\n", ["docs/plan/tareas/EM-07-planes.md", "docs/plan/bitacora/emilio.md"]), []);
  assert.match(validarCierrePr("[EM-07] Bloqueada: falta la cuenta", { ...HECHA, estado: "pendiente" }, "# EM-07\n", CON_BITACORA)[0], /estado: bloqueada/);
});

test("JG-18: una corrección de auditoría de una tarea ya hecha necesita su subsección", () => {
  const titulo = "[EM-07] Correcciones de la auditoría: planes";
  assert.match(validarCierrePr(titulo, HECHA, CON_RESULTADO, CON_BITACORA, "hecha")[0], /### Correcciones de la auditoría/);
  const conSubseccion = CON_RESULTADO + "\n### Correcciones de la auditoría (2026-10-03)\n- H-01.\n";
  assert.deepEqual(validarCierrePr(titulo, HECHA, conSubseccion, CON_BITACORA, "hecha"), []);
  // Una tarea nueva que el mismo PR crea y cierra (como EM-17 en la auditoría del 25 sep) no la necesita.
  assert.deepEqual(validarCierrePr(titulo, HECHA, CON_RESULTADO, CON_BITACORA, null), []);
});

test("JG-18: el plan de la auditoría y los títulos sin tarea no se revisan aquí", () => {
  assert.deepEqual(validarCierrePr("[JG-01] Plan de la auditoría 2026-10-03", HECHA, "", ["docs/plan/auditoria-2026-10-03.md"]), []);
  assert.deepEqual(validarCierrePr("sin ID", HECHA, "", []), []);
  assert.deepEqual(validarCierrePr("[EM-99] No existe", undefined, "", []), []);
});

const EN_LA_CI = { GITHUB_EVENT_NAME: "pull_request" };

test("JG-18: en la CI, los archivos del PR son el diff del commit de integración contra la punta de main", () => {
  const llamadas = [];
  const git = (args) => {
    llamadas.push(args.join(" "));
    return args[0] === "diff" ? "src/a.cs\r\ndocs/plan/bitacora/emilio.md\n" : "abc\n";
  };
  assert.deepEqual(archivosDelPr(git, EN_LA_CI), ["src/a.cs", "docs/plan/bitacora/emilio.md"]);
  // Auditoría 2026-10-03, H-36: --no-renames, para que un archivo movido aparezca también con su ruta de origen.
  assert.deepEqual(llamadas, ["rev-parse --verify --quiet HEAD^2", "diff --name-only --no-renames HEAD^1 HEAD"]);
  assert.equal(archivosDelPr(() => { throw new Error("sin HEAD^2"); }, EN_LA_CI), null);
});

test("JG-18: en local, el PR se compara contra origin/main, aunque el último commit sea un merge", () => {
  const llamadas = [];
  const git = (args) => {
    llamadas.push(args.join(" "));
    if (args[0] === "diff") return "docs/plan/tareas/EM-07-planes.md\ndocs/plan/bitacora/emilio.md\n";
    if (args[0] === "show") return "---\nestado: pendiente\n---\n";
    return "abc\n";
  };
  assert.deepEqual(archivosDelPr(git, {}), ["docs/plan/tareas/EM-07-planes.md", "docs/plan/bitacora/emilio.md"]);
  assert.equal(estadoEnMain("docs/plan/tareas/EM-07-planes.md", git, {}), "pendiente");
  assert.deepEqual(llamadas, [
    "rev-parse --verify --quiet origin/main", "diff --name-only --no-renames origin/main...HEAD",
    "rev-parse --verify --quiet origin/main", "show origin/main:docs/plan/tareas/EM-07-planes.md",
  ]);
  // Sin origin/main no hay contra qué comparar.
  assert.equal(archivosDelPr(() => { throw new Error("sin origin/main"); }, {}), null);
});

test("JG-18: en local, una corrección de auditoría sin su subsección también se rechaza", () => {
  const git = (args) => (args[0] === "show" ? "---\nestado: hecha\n---\n" : args[0] === "diff" ? "docs/plan/bitacora/jordin.md\n" : "abc\n");
  const tarea = HECHA;
  const errores = validarCierrePr("[EM-07] Correcciones de la auditoría: planes", tarea, CON_RESULTADO,
    archivosDelPr(git, {}), estadoEnMain(tarea.archivo, git, {}));
  assert.equal(errores.length, 1);
  assert.match(errores[0], /### Correcciones de la auditoría/);
});

test("JG-18: el estado anterior de la tarea se lee de la punta de main", () => {
  const git = (args) => (args[0] === "show" ? "---\nid: EM-07\nestado: hecha\n---\n" : "abc\n");
  assert.equal(estadoEnMain("docs/plan/tareas/EM-07-planes.md", git, EN_LA_CI), "hecha");
  // La tarea no existía en main (la crea el PR) o no hay commit de integración.
  assert.equal(estadoEnMain("docs/plan/tareas/EM-17-x.md", (args) => { if (args[0] === "show") throw new Error("no existe"); return "abc"; }, EN_LA_CI), null);
  assert.equal(estadoEnMain("docs/plan/tareas/EM-07-planes.md", () => { throw new Error("sin HEAD^2"); }, EN_LA_CI), null);
});

test("auditoría 2026-10-03, H-40: en la CI de un PR, si no se puede leer el diff, el cierre falla", () => {
  const errores = validarCierrePr("[EM-07] Planes de API", HECHA, CON_RESULTADO, null, null, true);
  assert.equal(errores.length, 1);
  assert.match(errores[0], /No se pudo leer el diff del PR en la CI/);
  // Fuera de la CI (sin origin/main en local) sigue sin exigirse.
  assert.deepEqual(validarCierrePr("[EM-07] Planes de API", HECHA, CON_RESULTADO, null, null, false), []);
  assert.deepEqual(validarCierrePr("[EM-07] Planes de API", HECHA, CON_RESULTADO, CON_BITACORA, null, true), []);
});

test("JG-03 criterio 13 (auditoría 2026-10-03, H-41): --hoy da lo de hoy, lo atrasado, quién lo espera y la siguiente", () => {
  const tareas = [
    { id: "EM-01", persona: "emilio", estado: "pendiente", programada: "2026-10-01", atrasada: true, titulo: "Atrasada", depende_de: [] },
    { id: "EM-02", persona: "emilio", estado: "pendiente", programada: "2026-10-04", atrasada: false, titulo: "De hoy", depende_de: [] },
    { id: "EM-03", persona: "emilio", estado: "pendiente", programada: "2026-10-06", atrasada: false, titulo: "Siguiente", depende_de: [] },
    { id: "EM-04", persona: "emilio", estado: "hecha", programada: "2026-10-04", atrasada: false, titulo: "Ya hecha", depende_de: [] },
    { id: "DC-01", persona: "dominique", estado: "pendiente", programada: "2026-10-08", atrasada: false, titulo: "Espera", depende_de: ["EM-02"] },
  ];

  const [emilio] = resumenHoy(tareas, "emilio", "2026-10-04");

  assert.equal(emilio.nombre, "Emilio Méndez");
  assert.deepEqual(emilio.deHoy.map((x) => x.tarea.id), ["EM-02"]);
  assert.match(emilio.deHoy[0].laEsperan, /Dominique Contreras \(DC-01\)/);
  assert.deepEqual(emilio.atrasadas.map((x) => x.tarea.id), ["EM-01"]);
  assert.equal(emilio.atrasadas[0].laEsperan, "");
  assert.equal(emilio.proxima.id, "EM-03");
  // Sin persona, una entrada por cada una; sin nada programado, listas vacías y sin siguiente.
  const todas = resumenHoy(tareas, undefined, "2026-10-04");
  assert.equal(todas.length, 4);
  const jose = todas.find((x) => x.persona === "jose-pablo");
  assert.deepEqual([jose.deHoy, jose.atrasadas, jose.proxima], [[], [], null]);
});
