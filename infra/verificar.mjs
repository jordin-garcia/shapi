import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import http from "node:http";
import https from "node:https";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const raiz = join(dirname(fileURLToPath(import.meta.url)), "..");
const rutaCompose = join(raiz, "infra", "compose.yml");
const rutaCaddyfile = join(raiz, "infra", "caddy", "Caddyfile.dev");
const rutaEntorno = join(raiz, ".env.example");
const rutaManual = join(raiz, "docs", "manual-tecnico.md");

function leer(ruta) {
  return readFileSync(ruta, "utf8");
}

function exigirTexto(contenido, textos, contexto) {
  for (const texto of textos) {
    assert.ok(
      contenido.includes(texto),
      `${contexto} debe contener: ${texto}`,
    );
  }
}

function ejecutarDocker(argumentos) {
  try {
    return execFileSync("docker", argumentos, {
      cwd: raiz,
      encoding: "utf8",
      stdio: ["ignore", "pipe", "pipe"],
    });
  } catch (error) {
    const detalle = error.stderr?.trim() || error.message;
    throw new Error(`Falló docker ${argumentos.join(" ")}: ${detalle}`);
  }
}

function interpretarServicios(texto) {
  const limpio = texto.trim();
  if (!limpio) return [];

  try {
    const valor = JSON.parse(limpio);
    return Array.isArray(valor) ? valor : [valor];
  } catch {
    return limpio.split(/\r?\n/).map((linea) => JSON.parse(linea));
  }
}

function solicitar(url, opciones = {}) {
  const cliente = url.startsWith("https:") ? https : http;
  return new Promise((resolve, reject) => {
    const peticion = cliente.get(
      url,
      {
        rejectUnauthorized: false,
        lookup: (_host, _opciones, callback) =>
          callback(null, "127.0.0.1", 4),
        timeout: 5_000,
        ...opciones,
      },
      (respuesta) => {
        respuesta.resume();
        respuesta.on("end", () => resolve(respuesta));
      },
    );

    peticion.on("timeout", () => peticion.destroy(new Error(`Tiempo agotado: ${url}`)));
    peticion.on("error", reject);
  });
}

// RNF-09, RNF-14: configuración reproducible y verificable del entorno local.
const compose = leer(rutaCompose);
const caddyfile = leer(rutaCaddyfile);
const entorno = leer(rutaEntorno);
const manual = leer(rutaManual);

exigirTexto(
  compose,
  [
    "postgres:16-alpine",
    "redis:7.4-alpine",
    "--appendonly",
    "--appendfsync",
    "axllent/mailpit",
    "caddy:2",
    "host.docker.internal:host-gateway",
    "pgdata:",
    "redisdata:",
    "caddydata:",
  ],
  "infra/compose.yml",
);

exigirTexto(
  caddyfile,
  [
    "local_certs",
    "on_demand_tls",
    "ask http://host.docker.internal:5080/interno/tls/autorizar",
    "https://shapi.localhost",
    "https://*.shapi.localhost",
    "https://*.api.shapi.localhost",
    "https://correo.shapi.localhost",
    "https:// {",
    "host.docker.internal:5080",
    "host.docker.internal:5090",
    "host.docker.internal:5173",
    "host.docker.internal:5174",
    "mailpit:8025",
    "header_up Host {http.request.host}",
    "\t\ton_demand",
    "Strict-Transport-Security",
    "X-Content-Type-Options",
    "Referrer-Policy",
    "/interno/*",
  ],
  "infra/caddy/Caddyfile.dev",
);

exigirTexto(
  entorno,
  [
    "SHAPI_DOMINIO_BASE=shapi.localhost",
    "SHAPI_MODO_DEMO=true",
    "SHAPI_POSTGRES_USUARIO=shapi",
    "SHAPI_POSTGRES_CONTRASENA=shapi_desarrollo",
    "SHAPI_POSTGRES_BASE_DATOS=shapi",
    "SHAPI_POSTGRES_CADENA=Host=localhost;Port=5432;Database=shapi;Username=shapi;Password=shapi_desarrollo",
    "SHAPI_REDIS=localhost:6379",
    "SHAPI_SMTP_HOST=localhost",
    "SHAPI_SMTP_PUERTO=1025",
    "SHAPI_SMTP_USUARIO=",
    "SHAPI_SMTP_CONTRASENA=",
    "SHAPI_SMTP_TLS=false",
    "SHAPI_DNS_MODO=simulado",
    "SHAPI_ORIGENES_PERMITIDOS=localhost:5101,localhost:5102,origen-envios:8080,origen-agro:8080",
    "SHAPI_ADMIN_CORREO=",
    "SHAPI_ADMIN_NOMBRE=",
    "SHAPI_ADMIN_CONTRASENA=",
    "SHAPI_PASARELA_FALLA=false",
    "SHAPI_DPKEYS_DIR=",
    "SHAPI_APLICAR_MIGRACIONES=false",
    "SHAPI_URL_ORIGEN_ENVIOS=http://localhost:5101",
    "SHAPI_URL_ORIGEN_AGRO=http://localhost:5102",
  ],
  ".env.example",
);

exigirTexto(
  manual,
  [
    "# Manual técnico",
    "## Entorno de desarrollo",
    "docker compose -f infra/compose.yml up -d",
    "docker compose -f infra/compose.yml down -v",
    "Import-Certificate",
    "update-ca-certificates",
    "https://correo.shapi.localhost",
  ],
  "docs/manual-tecnico.md",
);

ejecutarDocker([
  "compose",
  "-f",
  rutaCompose,
  "exec",
  "-T",
  "borde",
  "caddy",
  "validate",
  "--config",
  "/etc/caddy/Caddyfile",
  "--adapter",
  "caddyfile",
]);

const servicios = interpretarServicios(
  ejecutarDocker(["compose", "-f", rutaCompose, "ps", "--format", "json"]),
);

for (const nombre of ["postgres", "redis", "mailpit", "borde"]) {
  const servicio = servicios.find((actual) => actual.Service === nombre);
  assert.ok(servicio, `No se encontró el servicio ${nombre}`);
  assert.equal(servicio.State, "running", `${nombre} no está en ejecución`);
  assert.equal(servicio.Health, "healthy", `${nombre} no está sano`);
}

const correo = await solicitar("https://correo.shapi.localhost");
assert.ok(
  correo.statusCode >= 200 && correo.statusCode < 400,
  `Mailpit respondió ${correo.statusCode}`,
);
assert.match(correo.headers["strict-transport-security"] ?? "", /max-age=/);
assert.equal(correo.headers["x-content-type-options"], "nosniff");
assert.equal(
  correo.headers["referrer-policy"],
  "strict-origin-when-cross-origin",
);

const redireccion = await solicitar("http://correo.shapi.localhost");
assert.ok(
  [301, 302, 307, 308].includes(redireccion.statusCode),
  `HTTP no redirigió: respondió ${redireccion.statusCode}`,
);
assert.match(redireccion.headers.location ?? "", /^https:\/\//);

const interno = await solicitar("https://shapi.localhost/interno/salud");
assert.equal(interno.statusCode, 404, "/interno/* debe permanecer privado");

console.log("✓ Configuración declarativa completa");
console.log("✓ PostgreSQL, Redis, Mailpit y borde están sanos");
console.log("✓ Caddy usa HTTPS, agrega cabeceras y no publica /interno/*");
console.log("✓ https://correo.shapi.localhost responde correctamente");
