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
    } else if (/^\S/.test(linea) && !linea.startsWith("#")) {
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
    const todos = Object.entries(jobs(readFileSync(join(WORKFLOWS, archivo), "utf8")));
    assert.ok(todos.length > 0, `${archivo}: no se encontró ningún job`);
    for (const [nombre, texto] of todos) {
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
    "git push origin refs/heads/main",
    "git push origin \"main\"",
    "git push origin 'HEAD:main'",
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

test("H-09: el check obligatorio revision-claude no se omite y siempre decide el veredicto", () => {
  const revision = jobs(leer(".github", "workflows", "revision-claude.yml")).revision;
  assert.match(revision, /name: revision-claude/);
  // Un job omitido cuenta como aprobado: sin `if` a nivel de job.
  assert.doesNotMatch(revision, /^ {4}if:/m);
  const pasos = revision.split(/\n(?= {6}- )/);
  const accion = pasos.find((paso) => paso.includes("uses: anthropics/claude-code-action@"));
  const veredicto = pasos.find((paso) => paso.includes("name: Veredicto de la revisión"));
  assert.ok(accion && veredicto, "faltan los pasos de la revisión o del veredicto");
  // Si la acción falla, el veredicto igual se ejecuta y el check falla porque no encuentra la revisión.
  assert.match(accion, /^ {8}continue-on-error: true$/m);
  assert.match(veredicto, /^ {8}if: \$\{\{ !cancelled\(\) \}\}$/m);
  assert.doesNotMatch(veredicto, /continue-on-error/);
  assert.match(veredicto, /node scripts\/veredicto-revision\.mjs "\$RUNNER_TEMP\/comentarios\.jsonl" "\$SHA" "\$TAREA"/);
});

test("H-151: ningún agente integra con --admin; solo Jordin, a mano, salta un falso positivo (B11)", () => {
  for (const comando of ["gh pr merge 40 --admin --squash", "gh pr merge --admin 40", "gh pr merge 40 --squash --admin --delete-branch"]) {
    assert.ok(negado(comando), `no está negado: ${comando}`);
  }
  // El auto-merge normal de B11 sigue permitido.
  const autoMerge = "gh pr merge 40 --auto --squash --delete-branch";
  assert.ok(!negado(autoMerge), "se niega el auto-merge normal");
  assert.ok(reglas("allow").some((p) => coincide(p, autoMerge)), "el auto-merge normal no está permitido");
});

test("JG-18: el check titulo valida el cierre de la tarea con el diff del PR", () => {
  const job = jobs(titulo).titulo;
  // El diff del PR es el commit de integración contra su primer padre: hace falta fetch-depth 2.
  assert.match(job, /uses: actions\/checkout@v\d+\n {8}with:\n {10}fetch-depth: 2\n/);
  const pasos = job.split(/\n(?= {6}- )/);
  const indice = (texto) => pasos.findIndex((paso) => paso.includes(texto));
  const cierre = pasos[indice("--validar-cierre")];
  assert.ok(cierre, "falta el paso de --validar-cierre");
  assert.ok(indice("--validar-titulo") < indice("--validar-cierre"), "el cierre se valida después del título");
  // Como el título, va en una variable de entorno y el paso no se puede omitir.
  assert.match(cierre, /TITULO_PR: \$\{\{ github\.event\.pull_request\.title \}\}/);
  assert.doesNotMatch(cierre, /^ {8}(if|continue-on-error):/m);
});

test("JG-18: el PR ejecuta las pruebas E2E en el ambiente productivo simulado, antes de integrar", () => {
  const ambiente = jobs(leer(".github", "workflows", "publicar-imagenes.yml"))["verificar-ambiente"];
  assert.ok(ambiente, "falta el job verificar-ambiente");
  // Es un check obligatorio (auditoría 2026-10-03, paso 5): su único if es por el evento, así que en un PR nunca se omite.
  assert.match(ambiente, /^ {4}name: ambiente-productivo$/m);
  assert.match(ambiente, /^ {4}if: github\.event_name == 'pull_request'$/m);
  for (const doc of ["protocolo.md", "convenciones.md"]) {
    assert.match(leer("docs", "plan", doc), /`revision-claude` y `ambiente-productivo`/, `${doc} debe nombrar el check ambiente-productivo`);
  }
  const pasos = ambiente.split(/\n(?= {6}- )/);
  const indice = (texto) => pasos.findIndex((paso) => paso.includes(texto));
  assert.ok(indice("node infra/verificar.mjs") >= 0 && indice("node infra/verificar.mjs") < indice("run: pnpm test"),
    "las pruebas E2E corren después de verificar el ambiente");
  assert.match(pasos[indice("run: pnpm test")], /working-directory: tests\/e2e/);
  assert.doesNotMatch(pasos[indice("run: pnpm test")], /continue-on-error/);
  // El ambiente se apaga aunque fallen las pruebas.
  assert.ok(indice("down -v") > indice("run: pnpm test"));
});

test("JG-18: backend y frontend omiten sus pasos, nunca el job, si el PR no los afecta", () => {
  for (const area of ["backend", "frontend"]) {
    const job = jobs(ci)[area];
    // Un check obligatorio omitido cuenta como aprobado: el job no puede tener `if`.
    assert.doesNotMatch(job, /^ {4}if:/m, `${area}: el job no puede tener if`);
    assert.match(job, /uses: actions\/checkout@v\d+\n {8}with:\n {10}fetch-depth: 2\n/, `${area}: falta fetch-depth 2`);
    const pasos = job.split(/\n(?= {6}- )/).filter((paso) => /^ {6}- /.test(paso));
    const decision = pasos.findIndex((paso) => paso.includes(`node scripts/cambios-ci.mjs ${area} "\${{ github.event_name }}"`));
    assert.ok(decision > 0, `${area}: falta el paso que decide con scripts/cambios-ci.mjs`);
    assert.match(pasos[decision], /^ {8}id: cambios$/m);
    assert.doesNotMatch(pasos[decision], /^ {8}(if|continue-on-error):/m, `${area}: la decisión no se puede omitir`);
    // Todos los pasos posteriores dependen de la decisión, y ninguno puede fallar en silencio.
    for (const paso of pasos.slice(decision + 1)) {
      // != 'false': si la decisión no escribe su salida, se verifica todo.
      assert.match(paso, /^(?: {6}- | {8})if: steps\.cambios\.outputs\.ejecutar != 'false'$/m, `${area}: paso sin la condición:\n${paso}`);
      assert.doesNotMatch(paso, /continue-on-error/, `${area}: paso con continue-on-error:\n${paso}`);
    }
  }
});
