#!/usr/bin/env node
// Genera el issue "Tablero del plan", el recordatorio del día (tareas programadas y atrasadas)
// y los avisos de tareas recién disponibles o bloqueadas (JG-03).
// Lo ejecuta .github/workflows/tablero-plan.yml; no usa dependencias.
//
// Uso:
//   node scripts/tablero.mjs <cuerpo-anterior.md> <cuerpo-nuevo.md> <avisos.md>
//
// Lee el cuerpo actual del issue (puede no existir), escribe el cuerpo nuevo y el texto
// del comentario de avisos, que queda vacío si no hay nada que avisar.
// El estado de la ejecución anterior se guarda en el propio cuerpo del issue:
//   <!-- estado-tablero: {"disponibles":[...],"hechas":[...],"bloqueadas":[...],"recordatorio":"AAAA-MM-DD"} -->
// "recordatorio" es el último día en que se publicó el recordatorio, para publicarlo una sola vez al día.

import { existsSync, readFileSync, realpathSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { AVANCES, PERSONAS, aJson, cargar, clasificar, fechaCorta, hoy, laEsperan } from "./tareas.mjs";

const COORDINADOR = "jordin";
// El recordatorio sale en la primera ejecución del día a partir de esta hora (la del cron de tablero-plan.yml),
// para no marcar como atrasada, pasada la medianoche, una tarea que alguien está por integrar.
export const HORA_RECORDATORIO = 7;
const MARCA_ESTADO = /<!-- estado-tablero: (\{.*?\}) -->/s;

// Estado guardado en el cuerpo del issue, o null si no hay (primera ejecución).
export function leerEstado(cuerpo) {
  const m = (cuerpo ?? "").match(MARCA_ESTADO);
  if (!m) return null;
  try {
    const e = JSON.parse(m[1]);
    return { disponibles: e.disponibles ?? [], hechas: e.hechas ?? [], bloqueadas: e.bloqueadas ?? [], recordatorio: e.recordatorio ?? null };
  } catch {
    return null;
  }
}

function estadoActual(tareas, recordatorio) {
  const ids = (filtro) => tareas.filter(filtro).map((t) => t.id).sort();
  return {
    disponibles: ids((t) => t.situacion === "disponible"),
    hechas: ids((t) => t.estado === "hecha"),
    bloqueadas: ids((t) => t.situacion === "bloqueada"),
    recordatorio,
  };
}

// Por qué todavía no se puede empezar una tarea: "espera a DC-04 (Dominique Contreras)".
function motivoEspera(t, porId, fecha) {
  const motivos = [];
  if (t.faltan?.length) motivos.push("espera a " + t.faltan.map((d) => `${d} (${PERSONAS[porId[d]?.persona]?.nombre ?? "?"})`).join(", "));
  if (t.no_antes_de && t.no_antes_de > fecha) motivos.push(`no antes del ${t.no_antes_de}`);
  return motivos.join("; ");
}

// Recordatorio del día: a cada persona, su tarea programada para hoy y las atrasadas.
// Solo menciona a quien tiene algo; si nadie tiene nada, devuelve "".
function recordatorio(tareas, fecha) {
  const porId = Object.fromEntries(tareas.map((t) => [t.id, t]));
  const detalle = (t) => {
    const partes = [];
    if (t.situacion === "bloqueada") partes.push(`Está bloqueada: ${sinPunto(t.bloqueo)}.`);
    else if (t.situacion === "en_espera") partes.push(`Todavía ${motivoEspera(t, porId, fecha)}.`);
    const esperan = laEsperan(t, tareas);
    if (esperan) partes.push(`La esperan: ${esperan}.`);
    return partes.length ? " " + partes.join(" ") : "";
  };
  const lineas = [`**Tareas del día** (${fechaCorta(fecha)})`];
  for (const [clave, p] of Object.entries(PERSONAS)) {
    const pendientes = tareas.filter((t) => t.persona === clave && t.estado !== "hecha" && t.programada && t.programada <= fecha);
    if (!pendientes.length) continue;
    const mencion = `@${p.github}`;
    lineas.push("", `**${p.nombre}**`);
    // Primero las atrasadas: son las que están deteniendo a los demás.
    for (const t of pendientes.filter((x) => x.programada < fecha)) {
      lineas.push(`- ${mencion}: ⏰ **${t.id}** (${t.titulo}) está atrasada: estaba programada para el ${fechaCorta(t.programada)}.${detalle(t)}`);
    }
    for (const t of pendientes.filter((x) => x.programada === fecha)) {
      lineas.push(`- ${mencion}: hoy te toca **${t.id}** (${t.titulo}).${detalle(t)}`);
    }
  }
  return lineas.length > 1 ? lineas.join("\n") + "\n" : "";
}

// "JG-02" · "JG-04 y DC-04" · "JG-04, DC-04 y EM-01"
function enumerar(ids) {
  return ids.length === 1 ? ids[0] : `${ids.slice(0, -1).join(", ")} y ${ids.at(-1)}`;
}

function sinPunto(texto) {
  return String(texto ?? "").trim().replace(/\.$/, "");
}

function avisos(tareas, anterior, fecha) {
  if (!anterior) return "";
  const antes = {
    disponibles: new Set(anterior.disponibles),
    hechas: new Set(anterior.hechas),
    bloqueadas: new Set(anterior.bloqueadas),
  };
  const porPersona = new Map();
  const agregar = (persona, texto) => porPersona.set(persona, [...(porPersona.get(persona) ?? []), texto]);

  for (const t of tareas) {
    if (t.situacion === "disponible" && !antes.disponibles.has(t.id)) {
      const mencion = `@${t.github}`;
      const integradas = (t.depende_de ?? []).filter((d) => !antes.hechas.has(d));
      if (integradas.length) {
        const verbo = integradas.length === 1 ? "integrada" : "integradas";
        agregar(t.persona, `${mencion}: con ${enumerar(integradas)} ${verbo}, tu tarea ${t.id} (${t.titulo}) ya está disponible.`);
      } else if (antes.bloqueadas.has(t.id)) {
        agregar(t.persona, `${mencion}: tu tarea ${t.id} (${t.titulo}) ya no está bloqueada y está disponible.`);
      } else if (t.no_antes_de) {
        agregar(t.persona, `${mencion}: tu tarea ${t.id} (${t.titulo}) ya está disponible: llegó su fecha (${t.no_antes_de}).`);
      } else {
        agregar(t.persona, `${mencion}: tu tarea ${t.id} (${t.titulo}) ya está disponible.`);
      }
    }
    if (t.situacion === "bloqueada" && !antes.bloqueadas.has(t.id)) {
      const duenio = PERSONAS[t.persona]?.nombre ?? t.persona;
      agregar(COORDINADOR, `@${PERSONAS[COORDINADOR].github}: la tarea ${t.id} (${t.titulo}), de ${duenio}, quedó bloqueada: ${sinPunto(t.bloqueo)}.`);
    }
  }
  if (!porPersona.size) return "";

  const lineas = [`**Novedades del plan** (${fecha})`];
  for (const [clave, p] of Object.entries(PERSONAS)) {
    if (!porPersona.has(clave)) continue;
    lineas.push("", `**${p.nombre}**`, ...porPersona.get(clave).map((l) => `- ${l}`));
  }
  return lineas.join("\n") + "\n";
}

function cuerpo(tareas, estado, fecha) {
  const porId = Object.fromEntries(tareas.map((t) => [t.id, t]));
  const cuando = (t) => {
    if (!t.programada || t.estado === "hecha") return "";
    return t.programada < fecha ? ` · ⏰ atrasada (${fechaCorta(t.programada)})` : ` · ${fechaCorta(t.programada)}`;
  };
  const item = (t) => `- ${t.id} · ${t.prioridad}${cuando(t)} · ${t.titulo}`;
  const espera = (t) => `${item(t)} — ${motivoEspera(t, porId, fecha)}`;

  // Sin @ en el cuerpo: las menciones van solo en el comentario de avisos.
  const lineas = [
    "# Tablero del plan",
    "",
    "> Se actualiza solo en cada integración a `main` y cada día a las 07:00 (Guatemala). No lo edites a mano: se reemplaza en cada ejecución.",
    `> Última actualización: ${fecha}. Detalle de tus tareas: \`node scripts/tareas.mjs --persona <clave>\`. Qué te toca hoy: \`node scripts/tareas.mjs --hoy <clave>\`.`,
  ];

  lineas.push("", `## Hoy (${fechaCorta(fecha)})`, "");
  for (const [clave, p] of Object.entries(PERSONAS)) {
    const pendientes = tareas.filter((t) => t.persona === clave && t.estado !== "hecha" && t.programada && t.programada <= fecha);
    const ids = pendientes.map((t) => (t.programada < fecha ? `⏰ ${t.id} (atrasada)` : t.id));
    lineas.push(`- ${p.nombre}: ${ids.length ? ids.join(", ") : "nada programado"}`);
  }
  for (const [clave, p] of Object.entries(PERSONAS)) {
    const mias = tareas.filter((t) => t.persona === clave);
    const de = (s) => mias.filter((t) => t.situacion === s);
    lineas.push("", `## ${p.nombre} (${p.github}) · ${de("hecha").length}/${mias.length} hechas`);
    lineas.push("", "**Disponibles**", ...(de("disponible").length ? de("disponible").map(item) : ["- Ninguna"]));
    lineas.push("", "**En espera**", ...(de("en_espera").length ? de("en_espera").map(espera) : ["- Ninguna"]));
    if (de("bloqueada").length) lineas.push("", "**Bloqueadas**", ...de("bloqueada").map((t) => `${item(t)} — ${sinPunto(t.bloqueo)}`));
  }

  lineas.push("", "## Avance por entrega", "", "| Entrega | Hechas | Total | % |", "|---|---:|---:|---:|");
  for (const av of AVANCES) {
    const del = tareas.filter((t) => String(t.avance) === av);
    const hechas = del.filter((t) => t.estado === "hecha").length;
    const pct = del.length ? Math.round((hechas / del.length) * 100) : 0;
    lineas.push(`| ${av === "final" ? "Avance final" : `Avance ${av}`} | ${hechas} | ${del.length} | ${pct} % |`);
  }
  lineas.push("", `<!-- estado-tablero: ${JSON.stringify(estado)} -->`);
  return lineas.join("\n") + "\n";
}

// tareas: arreglo con la forma de `tareas.mjs --json`. Devuelve el cuerpo nuevo del issue y el comentario de avisos.
// El comentario junta el recordatorio del día (una vez al día, desde HORA_RECORDATORIO) y las novedades.
export function generar(tareas, cuerpoAnterior, fecha = hoy(), hora = new Date().getHours()) {
  const anterior = leerEstado(cuerpoAnterior);
  const tocaRecordatorio = hora >= HORA_RECORDATORIO && anterior?.recordatorio !== fecha;
  const estado = estadoActual(tareas, tocaRecordatorio ? fecha : (anterior?.recordatorio ?? null));
  const partes = [tocaRecordatorio ? recordatorio(tareas, fecha) : "", avisos(tareas, anterior, fecha)].filter(Boolean);
  return {
    cuerpo: cuerpo(tareas, estado, fecha),
    avisos: partes.join("\n"),
  };
}

const esPrincipal = import.meta.main ?? (process.argv[1] && pathToFileURL(realpathSync(resolve(process.argv[1]))).href === import.meta.url);

if (esPrincipal) {
  const [entrada, salidaCuerpo, salidaAvisos] = process.argv.slice(2);
  if (!salidaAvisos) {
    console.error("Uso: node scripts/tablero.mjs <cuerpo-anterior.md> <cuerpo-nuevo.md> <avisos.md>");
    process.exit(2);
  }
  const { tareas, errores } = cargar();
  if (errores.length) console.error("Advertencia, el plan tiene errores de formato (corra tareas.mjs --validar):\n- " + errores.join("\n- "));
  clasificar(tareas);
  const anterior = existsSync(entrada) ? readFileSync(entrada, "utf8") : "";
  const r = generar(tareas.map(aJson), anterior);
  writeFileSync(salidaCuerpo, r.cuerpo);
  writeFileSync(salidaAvisos, r.avisos);
  console.log(r.avisos ? `Avisos:\n${r.avisos}` : "Sin novedades: no hay avisos.");
}
