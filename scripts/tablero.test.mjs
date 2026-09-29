// Pruebas del tablero del plan (JG-03). Se ejecutan con: node --test "scripts/*.test.mjs"
import { test } from "node:test";
import assert from "node:assert/strict";
import { generar, leerEstado } from "./tablero.mjs";
import { aJson, cargar, clasificar } from "./tareas.mjs";

const GITHUB = { jordin: "jordin-garcia", emilio: "MiloDou", dominique: "Dom-cs13", "jose-pablo": "PabloZ7-425" };

// Tarea con la forma de `tareas.mjs --json`.
function tarea(id, persona, situacion, extra = {}) {
  return {
    id,
    titulo: `Título ${id}`,
    persona,
    github: GITHUB[persona],
    avance: "2",
    prioridad: "P1",
    estado: situacion === "hecha" || situacion === "bloqueada" ? situacion : "pendiente",
    depende_de: [],
    situacion,
    faltan: [],
    no_antes_de: null,
    bloqueo: null,
    ...extra,
  };
}

// Estado inicial: JG-02 disponible, EM-03 espera a JG-02, JG-08 espera su fecha.
function planInicial() {
  return [
    tarea("JG-01", "jordin", "hecha"),
    tarea("JG-02", "jordin", "disponible", { depende_de: ["JG-01"] }),
    tarea("EM-03", "emilio", "en_espera", { depende_de: ["JG-02"], faltan: ["JG-02"] }),
    tarea("JG-08", "jordin", "en_espera", { no_antes_de: "2026-10-08" }),
    tarea("DC-04", "dominique", "disponible"),
  ];
}

test("JG-03: la primera ejecución crea el tablero y no menciona a nadie", () => {
  const { cuerpo, avisos } = generar(planInicial(), "", "2026-09-23");
  assert.equal(avisos, "");
  assert.doesNotMatch(cuerpo, /@/);
  assert.match(cuerpo, /^# Tablero del plan/);
  assert.match(cuerpo, /## Emilio Méndez \(MiloDou\) · 0\/1 hechas/);
  assert.match(cuerpo, /- EM-03 · P1 · Título EM-03 — espera a JG-02 \(Jordin García\)/);
  assert.match(cuerpo, /- JG-08 · P1 · Título JG-08 — no antes del 2026-10-08/);
  assert.match(cuerpo, /\| Avance 2 \| 1 \| 5 \| 20 % \|/);
  assert.match(cuerpo, /\| Avance final \| 0 \| 0 \| 0 % \|/);
});

test("JG-03: un cuerpo sin estado (issue recién creado) cuenta como primera ejecución", () => {
  const { avisos } = generar(planInicial(), "Generando el tablero…", "2026-09-23");
  assert.equal(avisos, "");
});

test("JG-03: el cuerpo conserva el estado en un comentario HTML", () => {
  const plan = [...planInicial(), tarea("JZ-02", "jose-pablo", "bloqueada", { bloqueo: "falta la cuenta de correo" })];
  const { cuerpo: primera } = generar(plan, "", "2026-09-23", 7);
  const { cuerpo } = generar(plan, primera, "2026-09-23", 7);
  assert.match(cuerpo, /<!-- estado-tablero: \{.*\} -->\n$/);
  assert.deepEqual(leerEstado(cuerpo), {
    disponibles: ["DC-04", "JG-02"],
    hechas: ["JG-01"],
    bloqueadas: ["JZ-02"],
    recordatorio: "2026-09-23",
  });
  assert.match(cuerpo, /\*\*Bloqueadas\*\*\n- JZ-02 · P1 · Título JZ-02 — falta la cuenta de correo/);
});

test("JG-03: una dependencia que pasa a hecha menciona al dueño de la tarea desbloqueada", () => {
  const { cuerpo: anterior } = generar(planInicial(), "", "2026-09-23");
  const plan = planInicial();
  plan[1] = tarea("JG-02", "jordin", "hecha", { depende_de: ["JG-01"] });
  plan[2] = tarea("EM-03", "emilio", "disponible", { depende_de: ["JG-02"] });

  const { avisos } = generar(plan, anterior, "2026-09-24");
  assert.equal(
    avisos,
    "**Novedades del plan** (2026-09-24)\n\n**Emilio Méndez**\n- @MiloDou: con JG-02 integrada, tu tarea EM-03 (Título EM-03) ya está disponible.\n",
  );
});

test("JG-03: si se integran varias dependencias a la vez, se nombran todas", () => {
  const antes = [
    tarea("JG-04", "jordin", "disponible"),
    tarea("DC-04", "dominique", "disponible"),
    tarea("JG-05", "jordin", "en_espera", { depende_de: ["JG-04", "DC-04"], faltan: ["JG-04", "DC-04"] }),
  ];
  const { cuerpo: anterior } = generar(antes, "", "2026-10-01");
  const despues = [
    tarea("JG-04", "jordin", "hecha"),
    tarea("DC-04", "dominique", "hecha"),
    tarea("JG-05", "jordin", "disponible", { depende_de: ["JG-04", "DC-04"] }),
  ];
  const { avisos } = generar(despues, anterior, "2026-10-02");
  assert.match(avisos, /- @jordin-garcia: con JG-04 y DC-04 integradas, tu tarea JG-05 \(Título JG-05\) ya está disponible\./);
});

test("JG-03: un desbloqueo por fecha menciona al dueño con la fecha", () => {
  const { cuerpo: anterior } = generar(planInicial(), "", "2026-10-07");
  const plan = planInicial();
  plan[3] = tarea("JG-08", "jordin", "disponible", { no_antes_de: "2026-10-08" });

  const { avisos } = generar(plan, anterior, "2026-10-08");
  assert.equal(
    avisos,
    "**Novedades del plan** (2026-10-08)\n\n**Jordin García**\n- @jordin-garcia: tu tarea JG-08 (Título JG-08) ya está disponible: llegó su fecha (2026-10-08).\n",
  );
});

test("JG-03: una tarea que pasa a bloqueada menciona al coordinador con el dueño y el motivo", () => {
  const { cuerpo: anterior } = generar(planInicial(), "", "2026-09-23");
  const plan = planInicial();
  plan[4] = tarea("DC-04", "dominique", "bloqueada", { bloqueo: "falta la decisión del equipo sobre SSRF." });

  const { avisos } = generar(plan, anterior, "2026-09-24");
  assert.equal(
    avisos,
    "**Novedades del plan** (2026-09-24)\n\n**Jordin García**\n- @jordin-garcia: la tarea DC-04 (Título DC-04), de Dominique Contreras, quedó bloqueada: falta la decisión del equipo sobre SSRF.\n",
  );
});

test("JG-03: una tarea que sale de bloqueada no dice que llegó su fecha", () => {
  const antes = [tarea("JG-08", "jordin", "bloqueada", { no_antes_de: "2026-10-08", bloqueo: "falta algo" })];
  const { cuerpo: anterior } = generar(antes, "", "2026-10-20");
  const despues = [tarea("JG-08", "jordin", "disponible", { no_antes_de: "2026-10-08" })];

  const { avisos } = generar(despues, anterior, "2026-10-21");
  assert.match(avisos, /- @jordin-garcia: tu tarea JG-08 \(Título JG-08\) ya no está bloqueada y está disponible\./);
  assert.doesNotMatch(avisos, /llegó su fecha/);
});

test("JG-03: los avisos van en un solo comentario agrupado por persona", () => {
  const { cuerpo: anterior } = generar(planInicial(), "", "2026-10-07");
  const plan = planInicial();
  plan[1] = tarea("JG-02", "jordin", "hecha", { depende_de: ["JG-01"] });
  plan[2] = tarea("EM-03", "emilio", "disponible", { depende_de: ["JG-02"] });
  plan[3] = tarea("JG-08", "jordin", "disponible", { no_antes_de: "2026-10-08" });

  const { avisos } = generar(plan, anterior, "2026-10-08");
  assert.equal(
    avisos,
    [
      "**Novedades del plan** (2026-10-08)",
      "",
      "**Jordin García**",
      "- @jordin-garcia: tu tarea JG-08 (Título JG-08) ya está disponible: llegó su fecha (2026-10-08).",
      "",
      "**Emilio Méndez**",
      "- @MiloDou: con JG-02 integrada, tu tarea EM-03 (Título EM-03) ya está disponible.",
      "",
    ].join("\n"),
  );
});

test("JG-03: dos ejecuciones seguidas sin cambios no repiten avisos", () => {
  const { cuerpo: primera } = generar(planInicial(), "", "2026-09-23");
  const plan = planInicial();
  plan[1] = tarea("JG-02", "jordin", "hecha", { depende_de: ["JG-01"] });
  plan[2] = tarea("EM-03", "emilio", "disponible", { depende_de: ["JG-02"] });
  plan[4] = tarea("DC-04", "dominique", "bloqueada", { bloqueo: "falta algo" });

  const segunda = generar(plan, primera, "2026-09-24");
  assert.notEqual(segunda.avisos, "");
  const tercera = generar(plan, segunda.cuerpo, "2026-09-24");
  assert.equal(tercera.avisos, "");
  assert.equal(tercera.cuerpo, segunda.cuerpo);
});

test("JG-03: tareas.mjs --json trae los campos que usa el tablero", () => {
  const { tareas } = cargar();
  clasificar(tareas);
  const json = tareas.map(aJson);
  assert.ok(json.length > 0);
  for (const t of json) {
    assert.deepEqual(Object.keys(t).sort(), [
      "avance", "bloqueo", "depende_de", "estado", "faltan", "github", "id",
      "no_antes_de", "persona", "prioridad", "programada", "situacion", "titulo",
    ]);
    if (t.estado !== "hecha") assert.match(t.programada ?? "", /^\d{4}-\d{2}-\d{2}$/, t.id);
    assert.ok(["disponible", "en_espera", "bloqueada", "hecha"].includes(t.situacion), t.id);
    assert.ok(Array.isArray(t.faltan) && Array.isArray(t.depende_de), t.id);
    assert.ok(t.github, t.id);
  }
});

// --- Calendario diario (28 sep): recordatorio de las tareas programadas y atrasadas ---

// Martes 29 sep: JG-04 atrasada (del lunes) y la esperan JG-05 y EM-07; DC-04 toca hoy; EM-04 ya está hecha;
// José Pablo tiene su tarea hasta mañana.
function planDelDia() {
  return [
    tarea("JG-04", "jordin", "disponible", { programada: "2026-09-28" }),
    tarea("EM-04", "emilio", "hecha", { programada: "2026-09-28" }),
    tarea("DC-04", "dominique", "disponible", { programada: "2026-09-29" }),
    tarea("JG-05", "jordin", "en_espera", { programada: "2026-09-30", depende_de: ["JG-04", "DC-04"], faltan: ["JG-04", "DC-04"] }),
    tarea("EM-07", "emilio", "en_espera", { programada: "2026-10-01", depende_de: ["JG-04"], faltan: ["JG-04"] }),
    tarea("JZ-06", "jose-pablo", "disponible", { programada: "2026-09-30" }),
  ];
}

test("JG-03: el recordatorio menciona solo a quien tiene una tarea hoy o atrasada", () => {
  const { cuerpo: anterior } = generar(planDelDia(), "", "2026-09-28", 6);
  const { avisos } = generar(planDelDia(), anterior, "2026-09-29", 7);
  assert.equal(
    avisos,
    [
      "**Tareas del día** (mar 29 sep)",
      "",
      "**Jordin García**",
      "- @jordin-garcia: ⏰ **JG-04** (Título JG-04) está atrasada: estaba programada para el lun 28 sep. La esperan: Emilio Méndez (EM-07).",
      "",
      "**Dominique Contreras**",
      "- @Dom-cs13: hoy te toca **DC-04** (Título DC-04). La esperan: Jordin García (JG-05).",
      "",
    ].join("\n"),
  );
  assert.doesNotMatch(avisos, /MiloDou|PabloZ7-425/);
});

test("JG-03: el recordatorio dice qué falta si la tarea del día todavía no se puede empezar", () => {
  const plan = [
    tarea("DC-04", "dominique", "disponible", { programada: "2026-09-29" }),
    tarea("JG-05", "jordin", "en_espera", { programada: "2026-09-30", depende_de: ["DC-04"], faltan: ["DC-04"] }),
    tarea("JZ-02", "jose-pablo", "bloqueada", { programada: "2026-09-30", bloqueo: "falta la cuenta de correo." }),
  ];
  const { cuerpo: anterior } = generar(plan, "", "2026-09-30", 6);
  const { avisos } = generar(plan, anterior, "2026-09-30", 8);
  assert.match(avisos, /- @jordin-garcia: hoy te toca \*\*JG-05\*\* \(Título JG-05\)\. Todavía espera a DC-04 \(Dominique Contreras\)\./);
  assert.match(avisos, /- @PabloZ7-425: hoy te toca \*\*JZ-02\*\* \(Título JZ-02\)\. Está bloqueada: falta la cuenta de correo\./);
  assert.match(avisos, /- @Dom-cs13: ⏰ \*\*DC-04\*\* .* La esperan: Jordin García \(JG-05\)\./);
});

test("JG-03: el recordatorio sale una vez al día, desde las 07:00", () => {
  const { cuerpo: madrugada } = generar(planDelDia(), "", "2026-09-29", 6);
  assert.equal(leerEstado(madrugada).recordatorio, null);

  const primera = generar(planDelDia(), madrugada, "2026-09-29", 7);
  assert.match(primera.avisos, /\*\*Tareas del día\*\*/);
  assert.equal(leerEstado(primera.cuerpo).recordatorio, "2026-09-29");

  const segunda = generar(planDelDia(), primera.cuerpo, "2026-09-29", 15);
  assert.equal(segunda.avisos, "");
  assert.equal(leerEstado(segunda.cuerpo).recordatorio, "2026-09-29");

  const manana = generar(planDelDia(), segunda.cuerpo, "2026-09-30", 7);
  assert.match(manana.avisos, /\*\*Tareas del día\*\* \(mié 30 sep\)/);
});

test("JG-03: la primera ejecución no publica el recordatorio; sale en la siguiente", () => {
  const primera = generar(planDelDia(), "Generando el tablero…", "2026-09-29", 9);
  assert.equal(primera.avisos, "");
  assert.equal(leerEstado(primera.cuerpo).recordatorio, null);

  const segunda = generar(planDelDia(), primera.cuerpo, "2026-09-29", 9);
  assert.match(segunda.avisos, /^\*\*Tareas del día\*\* \(mar 29 sep\)/);
});

test("JG-03: antes de las 07:00 no hay recordatorio, pero sí novedades", () => {
  const { cuerpo: anterior } = generar(planInicial(), "", "2026-09-29", 5);
  const plan = planInicial();
  plan[1] = tarea("JG-02", "jordin", "hecha", { depende_de: ["JG-01"], programada: "2026-09-28" });
  plan[2] = tarea("EM-03", "emilio", "disponible", { depende_de: ["JG-02"], programada: "2026-09-29" });

  const { avisos } = generar(plan, anterior, "2026-09-29", 6);
  assert.doesNotMatch(avisos, /Tareas del día/);
  assert.match(avisos, /^\*\*Novedades del plan\*\*/);
});

test("JG-03: si nadie tiene nada programado no se comenta, y el día cuenta como avisado", () => {
  const plan = [tarea("JG-08", "jordin", "en_espera", { programada: "2026-10-08", no_antes_de: "2026-10-08" })];
  const { cuerpo: anterior } = generar(plan, "", "2026-10-02", 6);
  const { cuerpo, avisos } = generar(plan, anterior, "2026-10-02", 7);
  assert.equal(avisos, "");
  assert.equal(leerEstado(cuerpo).recordatorio, "2026-10-02");
});

test("JG-03: el recordatorio y las novedades van en un solo comentario, primero el recordatorio", () => {
  const { cuerpo: anterior } = generar(planInicial(), "", "2026-09-28", 6);
  const plan = planInicial();
  plan[1] = tarea("JG-02", "jordin", "hecha", { depende_de: ["JG-01"], programada: "2026-09-28" });
  plan[2] = tarea("EM-03", "emilio", "disponible", { depende_de: ["JG-02"], programada: "2026-09-29" });

  const { avisos } = generar(plan, anterior, "2026-09-29", 7);
  assert.equal(
    avisos,
    [
      "**Tareas del día** (mar 29 sep)",
      "",
      "**Emilio Méndez**",
      "- @MiloDou: hoy te toca **EM-03** (Título EM-03).",
      "",
      "**Novedades del plan** (2026-09-29)",
      "",
      "**Emilio Méndez**",
      "- @MiloDou: con JG-02 integrada, tu tarea EM-03 (Título EM-03) ya está disponible.",
      "",
    ].join("\n"),
  );
});

test("JG-03: el cuerpo muestra lo de hoy por persona y la fecha programada de cada tarea, sin menciones", () => {
  const { cuerpo } = generar(planDelDia(), "", "2026-09-29", 7);
  assert.doesNotMatch(cuerpo, /@/);
  assert.match(cuerpo, /## Hoy \(mar 29 sep\)\n\n- Jordin García: ⏰ JG-04 \(atrasada\)\n- Emilio Méndez: nada programado\n- Dominique Contreras: DC-04\n- José Pablo Zúñiga: nada programado\n/);
  assert.match(cuerpo, /- JG-04 · P1 · ⏰ atrasada \(lun 28 sep\) · Título JG-04\n/);
  assert.match(cuerpo, /- JG-05 · P1 · mié 30 sep · Título JG-05 — espera a JG-04 \(Jordin García\), DC-04 \(Dominique Contreras\)/);
});
