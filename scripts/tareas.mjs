#!/usr/bin/env node
// Lista y valida las tareas del plan (docs/plan/tareas/*.md).
// Funciona igual en Windows y Linux; solo requiere Node 20+ y no usa dependencias.
//
// Uso:
//   node scripts/tareas.mjs                     resumen de todo el equipo
//   node scripts/tareas.mjs --persona emilio    tareas de una persona (disponibles, bloqueadas, hechas)
//   node scripts/tareas.mjs --siguiente emilio  solo el ID de la siguiente tarea disponible
//   node scripts/tareas.mjs --ver EM-02         verifica si una tarea se puede empezar
//   node scripts/tareas.mjs --validar           valida el formato de todas las tareas (se usa en la CI)
//   node scripts/tareas.mjs --json              todas las tareas con su situación, en JSON (lo usa scripts/tablero.mjs)
//   node scripts/tareas.mjs --validar-titulo    valida el título de un PR (argumento o variable TITULO_PR; se usa en la CI)
//
// Personas válidas: jordin, emilio, dominique, jose-pablo

import { readdirSync, readFileSync, realpathSync } from "node:fs";
import { join, dirname, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const RAIZ = join(dirname(fileURLToPath(import.meta.url)), "..");
const DIR = join(RAIZ, "docs", "plan", "tareas");

export const PERSONAS = {
  jordin: { nombre: "Jordin García", prefijo: "JG", github: "jordin-garcia" },
  emilio: { nombre: "Emilio Méndez", prefijo: "EM", github: "MiloDou" },
  dominique: { nombre: "Dominique Contreras", prefijo: "DC", github: "Dom-cs13" },
  "jose-pablo": { nombre: "José Pablo Zúñiga", prefijo: "JZ", github: "PabloZ7-425" },
};
const ESTADOS = ["pendiente", "hecha", "bloqueada"];
export const AVANCES = ["1", "2", "3", "final"];
const PRIORIDADES = ["P1", "P2", "P3"];
const ORDEN_AVANCE = { 1: 1, 2: 2, 3: 3, final: 4 };
const CAMPOS = ["id", "titulo", "persona", "responsable", "avance", "prioridad", "estado", "depende_de"];

function parsearFrontmatter(texto, archivo) {
  const m = texto.match(/^---\r?\n([\s\S]*?)\r?\n---/);
  if (!m) throw new Error(`${archivo}: falta el bloque --- de metadatos al inicio`);
  const datos = {};
  for (const linea of m[1].split(/\r?\n/)) {
    if (!linea.trim() || linea.trim().startsWith("#")) continue;
    const i = linea.indexOf(":");
    if (i < 0) throw new Error(`${archivo}: línea de metadatos inválida: "${linea}"`);
    const clave = linea.slice(0, i).trim();
    let valor = linea.slice(i + 1).trim();
    if (valor.startsWith("[") && valor.endsWith("]")) {
      valor = valor.slice(1, -1).split(",").map((v) => v.trim().replace(/^["']|["']$/g, "")).filter(Boolean);
    } else {
      valor = valor.replace(/^["']|["']$/g, "");
    }
    datos[clave] = valor;
  }
  return datos;
}

export function cargar() {
  const archivos = readdirSync(DIR).filter((a) => a.endsWith(".md") && !a.startsWith("_") && a !== "README.md");
  const tareas = [];
  const errores = [];
  for (const archivo of archivos) {
    try {
      const t = parsearFrontmatter(readFileSync(join(DIR, archivo), "utf8"), archivo);
      t.archivo = `docs/plan/tareas/${archivo}`;
      tareas.push(t);
    } catch (e) {
      errores.push(e.message);
    }
  }
  return { tareas, errores };
}

function validar(tareas, erroresIniciales) {
  const errores = [...erroresIniciales];
  const ids = new Map();
  for (const t of tareas) {
    for (const c of CAMPOS) if (t[c] === undefined) errores.push(`${t.archivo}: falta el campo "${c}"`);
    if (ids.has(t.id)) errores.push(`${t.archivo}: ID duplicado ${t.id} (también en ${ids.get(t.id)})`);
    ids.set(t.id, t.archivo);
    const p = PERSONAS[t.persona];
    if (!p) errores.push(`${t.archivo}: persona "${t.persona}" no válida (${Object.keys(PERSONAS).join(", ")})`);
    else if (!String(t.id).startsWith(p.prefijo + "-") && t.reasignada !== "si") errores.push(`${t.archivo}: el ID ${t.id} no coincide con el prefijo ${p.prefijo} de ${t.persona} (si se reasignó, agregue reasignada: si)`);
    if (!t.archivo.includes(`/${t.id}-`)) errores.push(`${t.archivo}: el nombre del archivo debe empezar con "${t.id}-"`);
    if (!ESTADOS.includes(t.estado)) errores.push(`${t.archivo}: estado "${t.estado}" no válido (${ESTADOS.join(", ")})`);
    if (!AVANCES.includes(String(t.avance))) errores.push(`${t.archivo}: avance "${t.avance}" no válido (${AVANCES.join(", ")})`);
    if (!PRIORIDADES.includes(t.prioridad)) errores.push(`${t.archivo}: prioridad "${t.prioridad}" no válida (${PRIORIDADES.join(", ")})`);
    if (!Array.isArray(t.depende_de)) errores.push(`${t.archivo}: depende_de debe ser una lista, por ejemplo [JG-01, EM-01] o []`);
    if (t.estado === "bloqueada" && !t.bloqueo) errores.push(`${t.archivo}: una tarea bloqueada debe explicar el motivo en el campo "bloqueo"`);
    if (t.no_antes_de && !/^\d{4}-\d{2}-\d{2}$/.test(t.no_antes_de)) errores.push(`${t.archivo}: no_antes_de debe tener el formato AAAA-MM-DD`);
  }
  for (const t of tareas) {
    for (const d of t.depende_de || []) {
      if (!ids.has(d)) errores.push(`${t.archivo}: depende de ${d}, que no existe`);
      if (d === t.id) errores.push(`${t.archivo}: depende de sí misma`);
    }
  }
  // detectar ciclos
  const porId = Object.fromEntries(tareas.map((t) => [t.id, t]));
  const visitando = new Set(), listo = new Set();
  const dfs = (id, camino) => {
    if (listo.has(id) || !porId[id]) return;
    if (visitando.has(id)) { errores.push(`Dependencia circular: ${[...camino, id].join(" → ")}`); return; }
    visitando.add(id);
    for (const d of porId[id].depende_de || []) dfs(d, [...camino, id]);
    visitando.delete(id); listo.add(id);
  };
  for (const t of tareas) dfs(t.id, []);
  return errores;
}

export function hoy() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

export function clasificar(tareas) {
  const porId = Object.fromEntries(tareas.map((t) => [t.id, t]));
  const fecha = hoy();
  for (const t of tareas) {
    const faltan = (t.depende_de || []).filter((d) => porId[d]?.estado !== "hecha");
    t.faltan = faltan;
    t.esperaFecha = t.no_antes_de && t.no_antes_de > fecha;
    if (t.estado === "hecha") t.situacion = "hecha";
    else if (t.estado === "bloqueada") t.situacion = "bloqueada";
    else if (faltan.length || t.esperaFecha) t.situacion = "en_espera";
    else t.situacion = "disponible";
  }
  const orden = (a, b) =>
    ORDEN_AVANCE[a.avance] - ORDEN_AVANCE[b.avance] ||
    a.prioridad.localeCompare(b.prioridad) ||
    a.id.localeCompare(b.id, undefined, { numeric: true });
  return tareas.sort(orden);
}

function linea(t, porId) {
  const base = `  ${t.id.padEnd(6)} [Avance ${String(t.avance).padEnd(5)} ${t.prioridad}] ${t.titulo}`;
  if (t.situacion === "en_espera") {
    const motivos = [];
    if (t.faltan.length) motivos.push("espera a " + t.faltan.map((d) => `${d} (${PERSONAS[porId[d]?.persona]?.nombre ?? "?"})`).join(", "));
    if (t.esperaFecha) motivos.push(`no antes del ${t.no_antes_de}`);
    return `${base}\n         ↳ ${motivos.join("; ")}`;
  }
  if (t.situacion === "bloqueada") return `${base}\n         ↳ BLOQUEADA: ${t.bloqueo}`;
  return base;
}

function mostrarPersona(persona, tareas) {
  const p = PERSONAS[persona];
  if (!p) { console.error(`Persona desconocida "${persona}". Use: ${Object.keys(PERSONAS).join(", ")}`); process.exit(2); }
  const porId = Object.fromEntries(tareas.map((t) => [t.id, t]));
  const mias = tareas.filter((t) => t.persona === persona);
  const grupos = { disponible: [], en_espera: [], bloqueada: [], hecha: [] };
  for (const t of mias) grupos[t.situacion].push(t);
  console.log(`\nTareas de ${p.nombre} (${persona}) — ${grupos.hecha.length}/${mias.length} hechas\n`);
  console.log(`DISPONIBLES AHORA (en este orden):`);
  console.log(grupos.disponible.length ? grupos.disponible.map((t) => linea(t, porId)).join("\n") : "  (ninguna)");
  console.log(`\nEN ESPERA (dependencias o fecha):`);
  console.log(grupos.en_espera.length ? grupos.en_espera.map((t) => linea(t, porId)).join("\n") : "  (ninguna)");
  if (grupos.bloqueada.length) { console.log(`\nBLOQUEADAS:`); console.log(grupos.bloqueada.map((t) => linea(t, porId)).join("\n")); }
  console.log(`\nHECHAS:`);
  console.log(grupos.hecha.length ? grupos.hecha.map((t) => `  ${t.id} ${t.titulo}`).join("\n") : "  (ninguna)");
  if (grupos.disponible[0]) console.log(`\nSiguiente recomendada: ${grupos.disponible[0].id} → ${grupos.disponible[0].archivo}`);
}

function resumen(tareas) {
  console.log(`\nResumen del plan (${hoy()})\n`);
  for (const [clave, p] of Object.entries(PERSONAS)) {
    const mias = tareas.filter((t) => t.persona === clave);
    const cuenta = (s) => mias.filter((t) => t.situacion === s).length;
    console.log(`  ${p.nombre.padEnd(22)} hechas ${String(cuenta("hecha")).padStart(2)}/${String(mias.length).padEnd(3)} disponibles ${cuenta("disponible")}  en espera ${cuenta("en_espera")}  bloqueadas ${cuenta("bloqueada")}`);
  }
  for (const av of AVANCES) {
    const del = tareas.filter((t) => String(t.avance) === av);
    const hechas = del.filter((t) => t.estado === "hecha").length;
    console.log(`\n  Avance ${av}: ${hechas}/${del.length} hechas`);
    for (const t of del.filter((x) => x.estado !== "hecha")) console.log(`    ${t.id.padEnd(6)} ${t.prioridad} ${PERSONAS[t.persona]?.nombre.padEnd(22)} ${t.situacion.padEnd(10)} ${t.titulo}`);
  }
  console.log(`\nPara ver las tareas de una persona: node scripts/tareas.mjs --persona <jordin|emilio|dominique|jose-pablo>`);
}

// Título de un PR: "[<ID>] <título>", con el ID de una tarea que existe (protocolo §B11).
// La revisión con Claude toma el ID del título: sin él, no revisa criterios ni alcance.
export const FORMATO_TITULO = /^\[([A-Z]{2}-\d{2,})\] +\S/;

// Se aplica al título tal como llega, sin recortar, igual que en revision-claude.yml.
export function validarTituloPr(titulo, ids) {
  const coincidencia = FORMATO_TITULO.exec(titulo ?? "");
  if (!coincidencia) {
    return [`El título "${(titulo ?? "").trim()}" no sigue el formato "[<ID>] <título>", por ejemplo "[EM-03] Pantallas de registro, verificación y acceso".`];
  }
  if (!ids.includes(coincidencia[1])) {
    return [`La tarea ${coincidencia[1]} del título no existe en docs/plan/tareas/.`];
  }
  return [];
}

// Forma pública de una tarea (--json y scripts/tablero.mjs).
export function aJson(t) {
  return {
    id: t.id,
    titulo: t.titulo,
    persona: t.persona,
    github: PERSONAS[t.persona]?.github ?? null,
    avance: String(t.avance),
    prioridad: t.prioridad,
    estado: t.estado,
    depende_de: t.depende_de,
    situacion: t.situacion,
    faltan: t.faltan,
    no_antes_de: t.no_antes_de || null,
    bloqueo: t.bloqueo || null,
  };
}

// Solo se ejecuta como programa, no cuando otro script lo importa.
// import.meta.main existe desde Node 24.2; la comparación de rutas cubre versiones anteriores.
const esPrincipal = import.meta.main ?? (process.argv[1] && pathToFileURL(realpathSync(resolve(process.argv[1]))).href === import.meta.url);

if (esPrincipal) {
  const args = process.argv.slice(2);
  const { tareas, errores: erroresCarga } = cargar();
  const errores = validar(tareas, erroresCarga);

  if (args[0] === "--validar") {
    if (errores.length) { console.error("El plan tiene errores:\n- " + errores.join("\n- ")); process.exit(1); }
    console.log(`Plan válido: ${tareas.length} tareas.`);
    process.exit(0);
  }
  if (args[0] === "--validar-titulo") {
    const titulo = args[1] ?? process.env.TITULO_PR;
    const erroresTitulo = validarTituloPr(titulo, tareas.map((t) => t.id));
    if (erroresTitulo.length) {
      console.error(`${erroresTitulo.join("\n")}\nCorrija el título del PR: la CI se vuelve a ejecutar sola al editarlo.`);
      process.exit(1);
    }
    console.log(`Título válido: ${titulo}`);
    process.exit(0);
  }
  if (errores.length) console.error("Advertencia, el plan tiene errores de formato (corra --validar):\n- " + errores.join("\n- ") + "\n");

  clasificar(tareas);

  if (args[0] === "--json") console.log(JSON.stringify(tareas.map(aJson), null, 2));
  else if (args[0] === "--persona" && args[1]) mostrarPersona(args[1], tareas);
  else if (args[0] === "--siguiente" && args[1]) {
    const t = tareas.find((x) => x.persona === args[1] && x.situacion === "disponible");
    console.log(t ? t.id : "NINGUNA");
  } else if (args[0] === "--ver" && args[1]) {
    const t = tareas.find((x) => x.id === args[1]);
    if (!t) { console.error(`No existe la tarea ${args[1]}`); process.exit(2); }
    const porId = Object.fromEntries(tareas.map((x) => [x.id, x]));
    console.log(linea(t, porId));
    console.log(`  Archivo: ${t.archivo}`);
    console.log(`  Situación: ${t.situacion}${t.situacion === "disponible" ? " (se puede empezar)" : ""}`);
    process.exit(t.situacion === "disponible" ? 0 : 1);
  } else resumen(tareas);
}
