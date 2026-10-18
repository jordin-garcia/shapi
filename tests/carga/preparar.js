import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { createHash, randomInt } from 'node:crypto';

// RNF-01/RNF-03: datos y volúmenes exclusivos; nunca modifica el proyecto shapi.
const raiz = fileURLToPath(new URL('../../', import.meta.url));
const carpeta = resolve(raiz, '.shapi/carga');
mkdirSync(carpeta, { recursive: true });
const docker = process.env.CARGA_DOCKER || 'docker';
const sobreescritura = resolve(carpeta, 'compose.yml');
writeFileSync(sobreescritura, `services:
  borde:
    ports: !override
      - "127.0.0.1:18080:80"
      - "127.0.0.1:18443:443"
  origen-envios:
    ports: !override
      - "127.0.0.1:15101:8080"
  api:
    environment:
      SHAPI_REDES_BORDE: 172.31.14.0/24
networks:
  default:
    name: shapi-jz14
    ipam:
      config: !override
        - subnet: 172.31.14.0/24
`);
const compose = ['compose', '--project-name', 'shapi-jz14', '--env-file', '.env', '-f', 'infra/compose.yml', '-f', 'infra/compose.prod.yml', '-f', sobreescritura];
function ejecutar(args, input) {
  const resultado = spawnSync(docker, args, { cwd: raiz, input, stdio: input ? ['pipe', 'inherit', 'inherit'] : 'inherit' });
  if (resultado.error) throw resultado.error;
  if (resultado.status !== 0) throw new Error(`Docker terminó con código ${resultado.status}.`);
}
if (process.argv.includes('--apagar')) {
  ejecutar([...compose, 'down']); // Se conservan los volúmenes y las evidencias.
} else {
  ejecutar([...compose, 'up', '-d', ...(process.argv.includes('--sin-compilar') ? [] : ['--build']), '--wait', '--wait-timeout', '300']);
  ejecutar([...compose, 'exec', '-T', '-e', 'Logging__LogLevel__Default=Warning', 'trabajador', 'dotnet', 'Shapi.Trabajador.dll', 'sembrar-demo']);
  const alfabeto = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz';
  const clave = `shp_prod_${Array.from({ length: 26 }, () => alfabeto[randomInt(62)]).join('')}`;
  const hash = createHash('sha256').update(clave).digest('hex');
  // Fixture SQL solo en el PostgreSQL aislado: conserva validación, Lua y medición reales.
  ejecutar([...compose, 'exec', '-T', 'postgres', 'sh', '-c', 'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"'], `BEGIN;
UPDATE plan_api SET limite_minuto = 24000, cuota_llamadas = 1000000, actualizado_en = now()
WHERE api_id IN (SELECT id FROM api WHERE subdominio = 'envios');
UPDATE ruta SET limite_minuto = 24000, cache_segundos = 0, actualizado_en = now()
WHERE api_id IN (SELECT id FROM api WHERE subdominio = 'envios') AND patron = '/cotizaciones';
UPDATE plan_plataforma SET cuota_peticiones = 10000000, actualizado_en = now();
UPDATE clave SET hash_sha256 = '${hash}', ultimos4 = '${clave.slice(-4)}', actualizado_en = now()
WHERE tipo = 'produccion' AND estado = 'activa' AND suscripcion_id IN (
  SELECT s.id FROM suscripcion_api s JOIN consumidor c ON c.id = s.consumidor_id
  WHERE c.correo = 'oscar@ferreteriazona11.com');
COMMIT;
`);
  ejecutar([...compose, 'exec', '-T', '-e', 'Logging__LogLevel__Default=Warning', 'trabajador', 'dotnet', 'Shapi.Trabajador.dll', 'sembrar-demo']);
  writeFileSync(resolve(carpeta, '.env.carga'), `CARGA_CLAVE=${clave}\n`, { mode: 0o600 });
  console.log('Ambiente shapi-jz14 listo. Resultados: .shapi/carga. Puertos: borde 18443, origen 15101.');
}
