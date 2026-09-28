// Pruebas de las reglas del repositorio: CI, workflows y permisos de los agentes (auditoría del 25 sep, paso 15).
// Leen los archivos como texto, sin dependencias. Se ejecutan con: node --test "scripts/*.test.mjs"
import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, readdirSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const RAIZ = join(dirname(fileURLToPath(import.meta.url)), "..");
const WORKFLOWS = join(RAIZ, ".github", "workflows");
const leer = (...ruta) => readFileSync(join(RAIZ, ...ruta), "utf8");

// Devuelve { nombreDelJob: textoDelJob } de un workflow. Los jobs van con 2 espacios bajo `jobs:`.
function jobs(workflow) {
  const lineas = workflow.split(/\r?\n/);
  const inicio = lineas.findIndex((l) => l === "jobs:");
  const resultado = {};
  let actual = null;
  for (const linea of lineas.slice(inicio + 1)) {
    const nombre = linea.match(/^  ([\w-]+):\s*$/);
    if (nombre) {
      actual = nombre[1];
      resultado[actual] = "";
    } else if (/^\S/.test(linea)) {
      break;
    } else if (actual) {
      resultado[actual] += linea + "\n";
    }
  }
  return resultado;
}

const eventosPullRequest = (workflow) => workflow.match(/pull_request:\s*\n(?:\s*#.*\n)*\s*types:\s*\[([^\]]*)\]/)?.[1].split(",").map((e) => e.trim()) ?? [];

// Aproxima cómo Claude Code compara una regla `Bash(<patrón>)`: `*` es cualquier texto.
const coincide = (patron, comando) => new RegExp("^" + patron.replace(/[.+?^${}()|[\]\\]/g, "\\$&").replace(/\*/g, ".*") + "$").test(comando);
const reglas = (tipo) => JSON.parse(leer(".claude", "settings.json")).permissions[tipo]
  .map((r) => r.match(/^Bash\((.*)\)$/)?.[1]).filter(Boolean);
const negado = (comando) => reglas("deny").some((p) => coincide(p, comando));

const ci = leer(".github", "workflows", "ci.yml");
const titulo = (() => {
  try {
    return leer(".github", "workflows", "titulo-pr.yml");
  } catch {
    return "";
  }
})();

test("H-106: el job backend falla si el modelo tiene cambios sin migración", () => {
  assert.match(jobs(ci).backend, /dotnet ef migrations has-pending-model-changes -p src\/Shapi\.Infraestructura -s src\/Shapi\.Api/);
});

test("H-106: dotnet-ef se instala con la versión de EF Core de Directory.Packages.props", () => {
  assert.match(jobs(ci).backend, /dotnet tool install --global dotnet-ef --version "\$\(.*Directory\.Packages\.props.*\)"/);
});

test("H-107: el job frontend falla si los tipos generados no coinciden con los contratos", () => {
  const frontend = jobs(ci).frontend;
  assert.match(frontend, /pnpm generar:api/);
  // git status detecta también los archivos nuevos, que git diff no ve.
  assert.match(frontend, /git status --porcelain -- packages\/api\/src\/generado/);
  assert.ok(frontend.indexOf("pnpm generar:api") < frontend.indexOf("git status --porcelain"));
});

test("H-108: todos los jobs de todos los workflows tienen timeout-minutes", () => {
  for (const archivo of readdirSync(WORKFLOWS).filter((a) => a.endsWith(".yml"))) {
    for (const [nombre, texto] of Object.entries(jobs(readFileSync(join(WORKFLOWS, archivo), "utf8")))) {
      assert.match(texto, /^ {4}timeout-minutes: \d+$/m, `${archivo}: el job ${nombre} no tiene timeout-minutes`);
    }
  }
});

test("H-108: la CI no se repite al editar el PR; el título lo valida titulo-pr.yml, que sí escucha edited", () => {
  assert.ok(!eventosPullRequest(ci).includes("edited"), "ci.yml no debe escuchar edited");
  assert.doesNotMatch(ci, /--validar-titulo/);
  assert.ok(titulo, "falta .github/workflows/titulo-pr.yml");
  assert.deepEqual(eventosPullRequest(titulo), ["opened", "synchronize", "reopened", "edited"]);
  assert.match(jobs(titulo).titulo, /name: titulo/);
  assert.match(jobs(titulo).titulo, /node scripts\/tareas\.mjs --validar-titulo/);
  // Un job omitido cuenta como aprobado: el check obligatorio no puede tener `if`.
  assert.doesNotMatch(jobs(titulo).titulo, /^ {4}if:/m);
});

test("H-109: las acciones de terceros que reciben secretos van fijadas por SHA", () => {
  for (const archivo of readdirSync(WORKFLOWS).filter((a) => a.endsWith(".yml"))) {
    const texto = readFileSync(join(WORKFLOWS, archivo), "utf8");
    // Cada paso con `uses:` y `secrets.` en su bloque (hasta el paso siguiente).
    for (const paso of texto.split(/\n(?= {6}- )/)) {
      const accion = paso.match(/uses:\s*([^\s#]+)/)?.[1];
      if (!accion || !paso.includes("secrets.") || accion.startsWith("actions/")) continue;
      assert.match(accion, /@[0-9a-f]{40}$/, `${archivo}: ${accion} recibe secretos y no está fijada por SHA`);
    }
  }
});

test("H-110: los agentes no pueden hacer push a main con ninguna variante", () => {
  const prohibidos = [
    "git push origin main",
    "git push -u origin main",
    "git push --force-with-lease origin main",
    "git push origin main --force",
    "git push origin HEAD:main",
    "git push origin HEAD:refs/heads/main",
    "git push origin jordin/JG-01-algo:main",
    "git push -f origin HEAD:main",
    "git push origin +HEAD:main",
    "git push origin +main",
    "git push upstream main",
    "git push --all origin",
    "git push --mirror origin",
  ];
  for (const comando of prohibidos) assert.ok(negado(comando), `no está negado: ${comando}`);
});

test("H-110: las ramas de trabajo se pueden subir", () => {
  const permitidos = [
    "git push -u origin jordin/JG-01-auditoria-ci-y-reglas",
    "git push origin emilio/EM-06-pasarela-de-pagos",
    "git push origin dominique/DC-03-dominio-mainstream",
  ];
  for (const comando of permitidos) {
    assert.ok(!negado(comando), `se niega una rama de trabajo: ${comando}`);
    assert.ok(reglas("allow").some((p) => coincide(p, comando)), `no está permitido: ${comando}`);
  }
  // Protocolo B11: el push forzado con --force-with-lease sobre la rama propia no se niega (pide confirmación).
  assert.ok(!negado("git push --force-with-lease origin jordin/EM-06-pasarela-de-pagos"));
});
