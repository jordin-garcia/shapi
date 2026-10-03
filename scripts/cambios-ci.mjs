#!/usr/bin/env node
// Decide si un PR necesita las verificaciones del backend o del frontend (JG-18). Los jobs `backend` y `frontend` de
// ci.yml son checks obligatorios y nunca se omiten (un job omitido cuenta como aprobado y pisaría un fallo): lo que
// se omite son sus pasos, cuando el PR no toca nada que esas verificaciones lean.
//
// La regla es conservadora: cada área tiene una lista de rutas que seguro NO le afectan, y cualquier otra ruta, incluida
// una carpeta nueva que todavía no está en la lista, hace que se verifique. Así, olvidarse de actualizar la lista solo
// cuesta tiempo, nunca deja pasar algo sin verificar. Fuera de un PR (push a main) siempre se verifica todo.
//
// Los archivos del PR los da archivosDelPr de tareas.mjs: en la CI de un pull_request, el commit de integración contra
// su primer padre, la punta de main. Uso en la CI (después de actions/checkout con fetch-depth: 2):
//   node scripts/cambios-ci.mjs <backend|frontend> <evento>   → escribe ejecutar=true|false en $GITHUB_OUTPUT
import { appendFileSync } from "node:fs";
import { realpathSync } from "node:fs";
import { resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { archivosDelPr } from "./tareas.mjs";

// Rutas que no leen ni compilan ninguna de las verificaciones de cada área. Un prefijo que termina en "/" cubre la
// carpeta completa; si no, es un archivo exacto.
const COMUNES = [
  "docs/plan/", "docs/lineamientos.md", "docs/manual-tecnico.md", "scripts/", ".claude/", ".gemini/",
  "AGENTS.md", "CLAUDE.md", "README.md",
  // Los workflows no cambian lo que se verifica, salvo ci.yml, que es donde se verifica.
  ".github/pull_request_template.md", ".github/workflows/claude-interactivo.yml", ".github/workflows/e2e.yml",
  ".github/workflows/publicar-imagenes.yml", ".github/workflows/revision-claude.yml",
  ".github/workflows/tablero-plan.yml", ".github/workflows/titulo-pr.yml",
];

export const NO_AFECTAN = {
  // Las pruebas de .NET leen src/, tests/, origenes-demo/, docs/specs/, mockups/ e infra/ (además de los .props).
  backend: [...COMUNES, "frontend/", "tests/e2e/"],
  // Las pruebas de Vitest leen frontend/, contratos/ (tipos generados) y docs/specs/11-interfaz.md (rutas.test.tsx).
  // Las demás especificaciones se enumeran una por una: una nueva ejecuta el frontend hasta que se agregue aquí.
  frontend: [
    ...COMUNES, "src/", "tests/", "origenes-demo/", "infra/", "mockups/",
    "Directory.Build.props", "Directory.Packages.props", "Shapi.slnx", ".dockerignore",
    ...["01-vision-y-alcance", "02-glosario", "03-requisitos", "04-roles-y-permisos", "05-casos-de-uso", "06-arquitectura",
      "07-modelo-de-datos", "08-compuerta", "09-cobros-y-suscripciones", "10-identidad-y-seguridad", "12-decisiones", "README"]
      .map((especificacion) => `docs/specs/${especificacion}.md`),
  ],
};

// Archivos que deciden qué se verifica, como ci.yml (que no está en ninguna lista): un cambio en ellos ejecuta todo,
// aunque estén dentro de una carpeta de las listas. .gitattributes no está en las listas porque decide los finales de
// línea con que se escriben los archivos en la CI, y eso lo revisa dotnet format.
const SIEMPRE = ["scripts/cambios-ci.mjs", "scripts/tareas.mjs"];

const AREAS = Object.keys(NO_AFECTAN);

/** ¿Algún archivo del PR puede cambiar el resultado de las verificaciones del área? */
export function afectaA(area, archivos) {
  if (!AREAS.includes(area)) throw new Error(`Área desconocida: ${area}. Use ${AREAS.join(" o ")}.`);
  const libres = NO_AFECTAN[area];
  return archivos.some((archivo) => SIEMPRE.includes(archivo) ||
    !libres.some((ruta) => (ruta.endsWith("/") ? archivo.startsWith(ruta) : archivo === ruta)));
}

/** Decide si se ejecutan las verificaciones del área, y por qué. */
export function decidir(area, evento, archivos) {
  if (evento !== "pull_request") return { ejecutar: true, motivo: `el evento es ${evento || "desconocido"}: se verifica todo` };
  if (archivos === null) return { ejecutar: true, motivo: "no se pudo leer el diff del PR: se verifica todo" };
  if (afectaA(area, archivos)) return { ejecutar: true, motivo: `el PR cambia archivos que afectan al ${area}` };
  return { ejecutar: false, motivo: `el PR no cambia nada que lean las verificaciones del ${area} (${archivos.length} archivos)` };
}

// Como en tareas.mjs: solo se ejecuta como programa, no cuando una prueba lo importa.
const esPrincipal = import.meta.main ?? (process.argv[1] && pathToFileURL(realpathSync(resolve(process.argv[1]))).href === import.meta.url);

if (esPrincipal) {
  const [area, evento] = process.argv.slice(2);
  if (!AREAS.includes(area)) {
    console.error(`Uso: node scripts/cambios-ci.mjs <${AREAS.join("|")}> <evento>`);
    process.exit(2);
  }
  const { ejecutar, motivo } = decidir(area, evento, evento === "pull_request" ? archivosDelPr() : []);
  console.log(`${ejecutar ? "Se verifica" : "No se verifica"} el ${area}: ${motivo}.`);
  if (process.env.GITHUB_OUTPUT) appendFileSync(process.env.GITHUB_OUTPUT, `ejecutar=${ejecutar}\n`);
  if (!ejecutar && process.env.GITHUB_STEP_SUMMARY) {
    appendFileSync(process.env.GITHUB_STEP_SUMMARY, `### ${area}: sin cambios que verificar\n\n${motivo}.\n`);
  }
}
