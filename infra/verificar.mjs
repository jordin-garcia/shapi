import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, mkdtempSync, readdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import http from "node:http";
import https from "node:https";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const raiz = join(dirname(fileURLToPath(import.meta.url)), "..");
const rutaCompose = join(raiz, "infra", "compose.yml");
const rutaComposeProduccion = join(raiz, "infra", "compose.prod.yml");
const rutaCaddyfile = join(raiz, "infra", "caddy", "Caddyfile.dev");
const rutaCaddyfileProduccion = join(raiz, "infra", "caddy", "Caddyfile.prod");
const rutaEntorno = join(raiz, ".env.example");
const rutaEntornoLocal = join(raiz, ".env");
const rutaDockerignore = join(raiz, ".dockerignore");
const rutaGitignore = join(raiz, ".gitignore");
const rutaManual = join(raiz, "docs", "manual-tecnico.md");
const rutaInstalacion = join(raiz, "docs", "plan", "instalacion.md");
const rutaPublicacion = join(raiz, ".github", "workflows", "publicar-imagenes.yml");
const dockerDesktop = join(
  process.env.LOCALAPPDATA ?? "",
  "Programs",
  "DockerDesktop",
  "resources",
  "bin",
  "docker.exe",
);
const ejecutableDocker = process.env.SHAPI_DOCKER_BIN
  ?? (process.platform === "win32" && existsSync(dockerDesktop) ? dockerDesktop : "docker");

// Compose lee el .env de la carpeta del archivo (infra/), no el de la raíz: se pasa con --env-file (H-68).
const rutaEntornoActivo = existsSync(rutaEntornoLocal) ? rutaEntornoLocal : rutaEntorno;
const argumentosComposeDesarrollo = ["compose", "--env-file", rutaEntornoActivo, "-f", rutaCompose];
const argumentosComposeProduccion = [
  ...argumentosComposeDesarrollo,
  "-f",
  rutaComposeProduccion,
];
let argumentosCompose = argumentosComposeDesarrollo;

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

function ejecutarDocker(argumentos, entorno = process.env) {
  try {
    return execFileSync(ejecutableDocker, argumentos, {
      cwd: raiz,
      env: entorno,
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

function esperar(milisegundos) {
  return new Promise((resolve) => setTimeout(resolve, milisegundos));
}

function resolverLocal(_host, opciones, callback) {
  if (typeof opciones === "object" && opciones.all) {
    callback(null, [{ address: "127.0.0.1", family: 4 }]);
    return;
  }

  callback(null, "127.0.0.1", 4);
}

async function esperarServiciosSanos(nombres) {
  const limite = Date.now() + 90_000;
  let servicios = [];

  while (Date.now() < limite) {
    servicios = interpretarServicios(
      ejecutarDocker([...argumentosCompose, "ps", "--format", "json"]),
    );

    const todosSanos = nombres.every((nombre) => {
      const servicio = servicios.find((actual) => actual.Service === nombre);
      return servicio?.State === "running" && servicio.Health === "healthy";
    });

    if (todosSanos) return servicios;
    await esperar(1_000);
  }

  return servicios;
}

function solicitar(url, opciones = {}) {
  const cliente = url.startsWith("https:") ? https : http;
  return new Promise((resolve, reject) => {
    const peticion = cliente.get(
      url,
      {
        rejectUnauthorized: false,
        lookup: resolverLocal,
        timeout: 5_000,
        // Node 25 en Windows y Caddy 2.10 pueden negociar de forma incompatible
        // el grupo híbrido de TLS 1.3; TLS 1.2 sigue verificando HTTPS sin depender de ese grupo.
        maxVersion: "TLSv1.2",
        ...opciones,
      },
      (respuesta) => {
        respuesta.resume();
        respuesta.on("end", () => resolve(respuesta));
      },
    );

    peticion.on("timeout", () => peticion.destroy(new Error(`Tiempo agotado: ${url}`)));
    peticion.on("error", (error) => reject(new Error(`${url}: ${error.message}`, { cause: error })));
  });
}

function iniciarServidor(puerto, destino) {
  const servidor = http.createServer((peticion, respuesta) => {
    if (
      puerto === 5080 &&
      peticion.url?.startsWith("/interno/tls/autorizar?domain=")
    ) {
      respuesta.writeHead(200).end();
      return;
    }

    respuesta.writeHead(200, {
      "Content-Type": "text/plain",
      "X-Shapi-Prueba-Destino": destino,
      "X-Shapi-Prueba-Host": peticion.headers.host ?? "",
    });
    respuesta.end(destino);
  });

  servidor.on("upgrade", (peticion, socket) => {
    socket.end(
      "HTTP/1.1 101 Switching Protocols\r\n" +
        "Connection: Upgrade\r\n" +
        "Upgrade: websocket\r\n" +
        `X-Shapi-Prueba-Destino: ${destino}\r\n` +
        `X-Shapi-Prueba-Host: ${peticion.headers.host ?? ""}\r\n` +
        "\r\n",
    );
  });

  return new Promise((resolve, reject) => {
    servidor.once("error", reject);
    servidor.listen(puerto, "0.0.0.0", () => resolve(servidor));
  });
}

function cerrarServidor(servidor) {
  return new Promise((resolve, reject) => {
    servidor.close((error) => (error ? reject(error) : resolve()));
  });
}

function solicitarUpgrade(url) {
  return new Promise((resolve, reject) => {
    const peticion = https.request(url, {
      rejectUnauthorized: false,
      lookup: resolverLocal,
      maxVersion: "TLSv1.2",
      headers: {
        Connection: "Upgrade",
        Upgrade: "websocket",
        "Sec-WebSocket-Key": "c2hhcGktanotMDE=",
        "Sec-WebSocket-Version": "13",
      },
      timeout: 5_000,
    });

    peticion.on("upgrade", (respuesta, socket) => {
      socket.destroy();
      resolve(respuesta);
    });
    peticion.on("response", (respuesta) => {
      respuesta.resume();
      reject(new Error(`No hubo upgrade WebSocket: ${respuesta.statusCode}`));
    });
    peticion.on("timeout", () =>
      peticion.destroy(new Error(`Tiempo agotado: ${url}`)),
    );
    peticion.on("error", (error) => reject(new Error(`${url}: ${error.message}`, { cause: error })));
    peticion.end();
  });
}

// RNF-09, RNF-14: configuración reproducible y verificable del entorno local.
const compose = leer(rutaCompose);
const composeProduccion = leer(rutaComposeProduccion);
const caddyfile = leer(rutaCaddyfile);
const caddyfileProduccion = leer(rutaCaddyfileProduccion);
const entorno = leer(rutaEntorno);
const manual = leer(rutaManual);
const instalacion = leer(rutaInstalacion);
const publicacion = leer(rutaPublicacion);

exigirTexto(
  compose,
  [
    "postgres:16-alpine",
    "redis:7.4-alpine",
    "--appendonly",
    "--appendfsync",
    "axllent/mailpit:v1.27",
    "${SHAPI_POSTGRES_PUERTO:-5432}",
    "caddy:2",
    "host.docker.internal:host-gateway",
    "pgdata:",
    "redisdata:",
    "caddydata:",
  ],
  "infra/compose.yml",
);
assert.ok(!compose.includes(":latest"), "infra/compose.yml no debe usar imágenes :latest");

// RNF-09, RNF-14: el ambiente productivo simulado se construye y opera solo con contenedores.
for (const ruta of [
  "src/Shapi.Api/Dockerfile",
  "src/Shapi.Compuerta/Dockerfile",
  "src/Shapi.Trabajador/Dockerfile",
  "infra/borde/Dockerfile",
]) {
  assert.ok(existsSync(join(raiz, ruta)), `Falta ${ruta}`);
}
exigirTexto(
  composeProduccion,
  [
    "ghcr.io/jordin-garcia/shapi-api",
    "ghcr.io/jordin-garcia/shapi-compuerta",
    "ghcr.io/jordin-garcia/shapi-trabajador",
    "ghcr.io/jordin-garcia/shapi-borde",
    "SHAPI_APLICAR_MIGRACIONES: \"true\"",
    "SHAPI_DPKEYS_DIR: /var/lib/shapi/dpkeys",
    "dpkeys:/var/lib/shapi/dpkeys",
    "ports: !reset []",
    "restart: unless-stopped",
  ],
  "infra/compose.prod.yml",
);
exigirTexto(
  caddyfileProduccion,
  [
    "ask http://api:8080/interno/tls/autorizar",
    "reverse_proxy api:8080",
    "reverse_proxy compuerta:8080",
    "root * /srv/panel",
    "root * /srv/portal",
    "try_files {path} /index.html",
    "Content-Security-Policy",
  ],
  "infra/caddy/Caddyfile.prod",
);
exigirTexto(
  publicacion,
  [
    "branches: [main]",
    "packages: write",
    "src/Shapi.Api/Dockerfile",
    "src/Shapi.Compuerta/Dockerfile",
    "src/Shapi.Trabajador/Dockerfile",
    "infra/borde/Dockerfile",
    "ghcr.io/jordin-garcia/shapi-${{ matrix.nombre }}:latest",
    "ghcr.io/jordin-garcia/shapi-${{ matrix.nombre }}:${{ github.sha }}",
  ],
  ".github/workflows/publicar-imagenes.yml",
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
    "http:// {",
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
    "SHAPI_POSTGRES_PUERTO=5432",
    "SHAPI_SECRETO_ORIGEN_ENVIOS=",
    "SHAPI_SECRETO_ORIGEN_AGRO=",
  ],
  ".env.example",
);

exigirTexto(
  manual,
  [
    "# Manual técnico",
    "## Entorno de desarrollo",
    "docker compose --env-file .env -f infra/compose.yml up -d",
    "docker compose --env-file .env -f infra/compose.yml down -v",
    "Import-Certificate",
    "update-ca-certificates",
    "https://correo.shapi.localhost",
    "## Ambiente productivo simulado",
    "docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml up -d --build",
    "exec trabajador dotnet Shapi.Trabajador.dll sembrar-demo",
    "logs -f",
  ],
  "docs/manual-tecnico.md",
);

// Los documentos con comandos para copiar (H-68). Las bitácoras no se revisan: son historial.
const rutaTareas = join(raiz, "docs", "plan", "tareas");
const documentosConComandos = [
  [manual, "docs/manual-tecnico.md"],
  [instalacion, "docs/plan/instalacion.md"],
  [leer(join(raiz, "AGENTS.md")), "AGENTS.md"],
  [leer(join(raiz, "docs", "plan", "calendario.md")), "docs/plan/calendario.md"],
  ...readdirSync(rutaTareas)
    .filter((archivo) => archivo.endsWith(".md"))
    .map((archivo) => [leer(join(rutaTareas, archivo)), `docs/plan/tareas/${archivo}`]),
];
for (const [contenido, contexto] of documentosConComandos) {
  assert.ok(
    !contenido.includes("docker compose -f infra/compose.yml"),
    `${contexto} debe pasar --env-file .env a todos los comandos de Compose`,
  );
}

assert.ok(existsSync(rutaDockerignore), "Falta .dockerignore en la raíz");
exigirTexto(
  leer(rutaDockerignore),
  ["**/bin/", "**/obj/", "**/node_modules/", ".git/", ".env", ".shapi/"],
  ".dockerignore",
);
exigirTexto(leer(rutaGitignore), [".shapi/"], ".gitignore");

// Configuración resuelta por Compose, con un .env de prueba: el puerto de PostgreSQL es configurable (H-69),
// todos los puertos se publican solo en 127.0.0.1 (H-71) y todos los servicios tienen healthcheck (H-72).
const carpetaTemporal = mkdtempSync(join(tmpdir(), "shapi-verificar-"));
try {
  const entornoPrueba = join(carpetaTemporal, ".env");
  // Como el .env de ejemplo: los orígenes de desarrollo en localhost, que producción no debe tomar (auditoría H-27).
  writeFileSync(
    entornoPrueba,
    "SHAPI_POSTGRES_PUERTO=5999\nSHAPI_URL_ORIGEN_ENVIOS=http://localhost:5101\nSHAPI_URL_ORIGEN_AGRO=http://localhost:5102\n",
  );
  // Una variable de la terminal le gana al archivo: se quita para que valga la del .env de prueba.
  const { SHAPI_POSTGRES_PUERTO: _, ...entornoSinPuerto } = process.env;
  const configuracionDesarrollo = JSON.parse(
    ejecutarDocker([
      "compose",
      "--env-file",
      entornoPrueba,
      "-f",
      rutaCompose,
      "config",
      "--format",
      "json",
    ], entornoSinPuerto),
  );
  const puertoPostgres = configuracionDesarrollo.services.postgres.ports?.[0];
  assert.equal(
    String(puertoPostgres?.published),
    "5999",
    "El puerto de PostgreSQL debe salir de SHAPI_POSTGRES_PUERTO",
  );
  for (const [nombre, servicio] of Object.entries(configuracionDesarrollo.services)) {
    for (const puerto of servicio.ports ?? []) {
      assert.equal(
        puerto.host_ip,
        "127.0.0.1",
        `${nombre} publica ${puerto.published} fuera de 127.0.0.1`,
      );
    }
    assert.ok(servicio.healthcheck?.test, `${nombre} no tiene healthcheck`);
  }

  const configuracionProduccion = JSON.parse(
    ejecutarDocker([
      "compose",
      "--env-file",
      entornoPrueba,
      "-f",
      rutaCompose,
      "-f",
      rutaComposeProduccion,
      "config",
      "--format",
      "json",
    ], entornoSinPuerto),
  );

  const entornoTrabajador = configuracionProduccion.services.trabajador.environment;
  assert.equal(
    entornoTrabajador.SHAPI_URL_ORIGEN_ENVIOS,
    "http://origen-envios:8080",
    "En producción, la siembra debe usar el nombre de servicio del origen de envíos, no el del .env",
  );
  assert.equal(
    entornoTrabajador.SHAPI_URL_ORIGEN_AGRO,
    "http://origen-agro:8080",
    "En producción, la siembra debe usar el nombre de servicio del origen de agro, no el del .env",
  );

  for (const [nombre, servicio] of Object.entries(configuracionProduccion.services)) {
    for (const puerto of servicio.ports ?? []) {
      assert.equal(
        puerto.host_ip,
        "127.0.0.1",
        `${nombre} publica ${puerto.published} fuera de 127.0.0.1`,
      );
    }

    if (nombre !== "trabajador") {
      assert.ok(servicio.healthcheck?.test, `${nombre} no tiene healthcheck`);
    }
    assert.equal(servicio.restart, "unless-stopped", `${nombre} no reinicia automáticamente`);
  }

  const puertosPublicados = Object.entries(configuracionProduccion.services)
    .flatMap(([nombre, servicio]) => (servicio.ports ?? []).map((puerto) => [nombre, String(puerto.published)]))
    .sort((a, b) => Number(a[1]) - Number(b[1]));
  assert.deepEqual(
    puertosPublicados,
    [["borde", "80"], ["borde", "443"]],
    "En producción solo el borde debe publicar los puertos 80 y 443",
  );
} finally {
  rmSync(carpetaTemporal, { recursive: true, force: true });
}

const serviciosProyecto = interpretarServicios(
  ejecutarDocker([...argumentosComposeProduccion, "ps", "--format", "json"]),
);
const produccionActiva = serviciosProyecto.some(
  (servicio) => servicio.Service === "api" && servicio.State === "running",
);
argumentosCompose = produccionActiva
  ? argumentosComposeProduccion
  : argumentosComposeDesarrollo;

ejecutarDocker([
  ...argumentosCompose,
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

const nombresServicios = [
  "postgres",
  "redis",
  "mailpit",
  "borde",
  "origen-envios",
  "origen-agro",
  ...(produccionActiva ? ["api", "compuerta"] : []),
];
const servicios = await esperarServiciosSanos(nombresServicios);

for (const nombre of nombresServicios) {
  const servicio = servicios.find((actual) => actual.Service === nombre);
  assert.ok(servicio, `No se encontró el servicio ${nombre}`);
  assert.equal(servicio.State, "running", `${nombre} no está en ejecución`);
  assert.equal(servicio.Health, "healthy", `${nombre} no está sano`);
  for (const publicado of servicio.Publishers ?? []) {
    if (!publicado.PublishedPort) continue;
    assert.equal(
      publicado.URL,
      "127.0.0.1",
      `${nombre} publica el puerto ${publicado.PublishedPort} en ${publicado.URL}`,
    );
  }
}

if (produccionActiva) {
  const trabajador = servicios.find((actual) => actual.Service === "trabajador");
  assert.ok(trabajador, "No se encontró el servicio trabajador");
  assert.equal(trabajador.State, "running", "trabajador no está en ejecución");
  assert.ok(!trabajador.Health, "trabajador no debe tener healthcheck HTTP");

  const panel = await solicitar("https://shapi.localhost/");
  assert.equal(panel.statusCode, 200, `El panel respondió ${panel.statusCode}`);
  assert.match(panel.headers["content-security-policy"] ?? "", /default-src 'self'/);

  const portal = await solicitar("https://envios.shapi.localhost/");
  assert.equal(portal.statusCode, 200, `El portal respondió ${portal.statusCode}`);
  assert.match(portal.headers["content-security-policy"] ?? "", /default-src 'self'/);

  const apiPorBorde = await solicitar("https://shapi.localhost/api/auth/sesion");
  assert.ok(
    [200, 401].includes(apiPorBorde.statusCode),
    `La API por el borde respondió ${apiPorBorde.statusCode}`,
  );
  assert.ok(
    !(apiPorBorde.headers["content-security-policy"] ?? "").includes("default-src 'self'"),
    "Caddy no debe reemplazar la CSP propia de las respuestas de la API",
  );

  const apiSinSiembra = await solicitar("https://envios.api.shapi.localhost/");
  assert.equal(apiSinSiembra.statusCode, 404, "Una API sin siembra debe responder 404");

  await assert.rejects(
    solicitar("http://127.0.0.1:5080/salud"),
    "La API de control no debe publicar el puerto 5080 en el host",
  );
  await assert.rejects(
    solicitar("https://api.enviosxelaju.localhost/interno/tls/autorizar?domain=x.localhost"),
    "Un dominio propio no verificado no debe obtener certificado ni acceder a /interno/*",
  );
} else {
  const servidores = [];
  try {
    for (const [puerto, destino] of [
      [5080, "api"],
      [5090, "compuerta"],
      [5173, "panel"],
      [5174, "portal"],
    ]) {
      servidores.push(await iniciarServidor(puerto, destino));
    }

    for (const [url, destino] of [
      ["https://shapi.localhost/api/prueba", "api"],
      ["https://shapi.localhost/", "panel"],
      ["https://envios.shapi.localhost/api/portal/prueba", "api"],
      ["https://envios.shapi.localhost/", "portal"],
      ["https://envios.api.shapi.localhost/rastreo", "compuerta"],
      ["https://api.enviosxelaju.localhost/rastreo", "compuerta"],
    ]) {
      const respuesta = await solicitar(url);
      assert.equal(
        respuesta.headers["x-shapi-prueba-destino"],
        destino,
        `${url} no llegó a ${destino}`,
      );
      if (url === "https://envios.shapi.localhost/api/portal/prueba") {
        assert.equal(
          respuesta.headers["x-shapi-prueba-host"],
          "envios.shapi.localhost",
          "La API del portal debe recibir el Host original",
        );
      }
    }

    const internoDominioPropio = await solicitar(
      "https://api.enviosxelaju.localhost/interno/tls/autorizar?domain=x.localhost",
    );
    assert.equal(
      internoDominioPropio.statusCode,
      404,
      "/interno/* debe permanecer privado en dominios propios",
    );

    const websocket = await solicitarUpgrade("https://shapi.localhost/@vite/client");
    assert.equal(websocket.statusCode, 101);
    assert.equal(websocket.headers["x-shapi-prueba-destino"], "panel");
  } finally {
    await Promise.all(servidores.map(cerrarServidor));
  }
}

const correo = await solicitar("https://correo.shapi.localhost");
assert.ok(
  correo.statusCode >= 200 && correo.statusCode < 400,
  `Mailpit respondió ${correo.statusCode}`,
);
assert.match(correo.headers["strict-transport-security"] ?? "", /max-age=/);
assert.equal(correo.headers["x-content-type-options"], "nosniff");
assert.equal(correo.headers["referrer-policy"], "strict-origin-when-cross-origin");

const redireccion = await solicitar(
  "http://api.enviosxelaju.localhost/rastreo?id=GT-1001",
);
assert.ok(
  [301, 302, 307, 308].includes(redireccion.statusCode),
  `HTTP no redirigió: respondió ${redireccion.statusCode}`,
);
assert.equal(
  redireccion.headers.location,
  "https://api.enviosxelaju.localhost/rastreo?id=GT-1001",
);

for (const host of [
  "shapi.localhost",
  "envios.shapi.localhost",
  "envios.api.shapi.localhost",
]) {
  const interno = await solicitar(`https://${host}/interno/tls/autorizar?domain=x.localhost`);
  assert.equal(
    interno.statusCode,
    404,
    `/interno/* debe permanecer privado en ${host}`,
  );
}

console.log("✓ Configuración declarativa de desarrollo y producción completa");
if (produccionActiva) {
  console.log("✓ Los contenedores con healthcheck están sanos y el trabajador está en ejecución");
  console.log("✓ Solo el borde publica los puertos 80 y 443 en 127.0.0.1");
  console.log("✓ Caddy sirve los frontends y enruta la API y la compuerta por nombres de servicio");
} else {
  console.log("✓ PostgreSQL, Redis, Mailpit, borde y los orígenes están sanos y solo publican en 127.0.0.1");
  console.log("✓ Caddy enruta cada host, conserva Host y admite WebSocket");
}
console.log("✓ Caddy usa HTTPS, agrega cabeceras y no publica /interno/*");
console.log("✓ https://correo.shapi.localhost responde correctamente");
