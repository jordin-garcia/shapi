// Pruebas del tablero del plan (JG-03). Se ejecutan con: node --test scripts/
import { test } from "node:test";
import assert from "node:assert/strict";
import { generar, leerEstado } from "./tablero.mjs";

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
  const { cuerpo } = generar(plan, "", "2026-09-23");
  assert.match(cuerpo, /<!-- estado-tablero: \{.*\} -->\n$/);
  assert.deepEqual(leerEstado(cuerpo), {
    disponibles: ["DC-04", "JG-02"],
    hechas: ["JG-01"],
    bloqueadas: ["JZ-02"],
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
