import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { comparar } from './comparar.js';

const raiz = fileURLToPath(new URL('../../', import.meta.url));
const carpeta = resolve(raiz, '.shapi/carga');
const docker = process.env.CARGA_DOCKER || 'docker';
const humo = process.argv.includes('--humo');
const clave = readFileSync(resolve(carpeta, '.env.carga'), 'utf8').trim().split('=')[1];
// Recupera el secreto solo en memoria; no aparece en la línea de comandos ni en resultados.
const secreto = spawnSync(docker, ['exec', 'shapi-jz14-origen-envios-1', 'printenv', 'SECRETO_ORIGEN'], { encoding: 'utf8' });
if (secreto.status !== 0) throw new Error('No se pudo consultar el origen aislado. Ejecute preparar.js.');
const entorno = { ...process.env, CARGA_CLAVE: clave, CARGA_SECRETO_ORIGEN: secreto.stdout.trim() };
let fallos = false;
for (const escenario of ['B', 'A']) {
  const archivo = `${escenario}${humo ? '-humo' : ''}.json`;
  const resultado = spawnSync(docker, [
    'run', '--rm', '--network', 'host',
    '--mount', `type=bind,source=${resolve(raiz, 'tests/carga')},target=/pruebas,readonly`,
    '--mount', `type=bind,source=${carpeta},target=/resultados`,
    '-e', 'CARGA_CLAVE', '-e', 'CARGA_SECRETO_ORIGEN',
    '-e', `CARGA_ESCENARIO=${escenario}`, '-e', `CARGA_RESUMEN=/resultados/${archivo}`,
    '-e', 'CARGA_BORDE=127.0.0.1:18443', '-e', 'CARGA_ORIGEN=127.0.0.1:15101',
    ...(humo ? ['-e', 'CARGA_TASA=5', '-e', 'CARGA_SEGUNDOS=10'] : []),
    'grafana/k6:1.6.1', 'run', '--quiet', '/pruebas/escenario.js',
  ], { cwd: raiz, env: entorno, encoding: 'utf8' });
  writeFileSync(resolve(carpeta, `${archivo}.log`), (resultado.stdout || '') + (resultado.stderr || ''));
  if (resultado.error) throw resultado.error;
  const resumen = JSON.parse(readFileSync(resolve(carpeta, archivo), 'utf8'));
  const { metricas, ...visible } = resumen;
  console.log(JSON.stringify(visible, null, 2));
  if (resultado.status !== 0) fallos = true;
}
if (!humo) {
  const leer = nombre => JSON.parse(readFileSync(resolve(carpeta, nombre), 'utf8'));
  const resultado = comparar(leer('A.json'), leer('B.json'));
  writeFileSync(resolve(carpeta, 'comparacion.json'), JSON.stringify(resultado, null, 2));
  console.log(JSON.stringify(resultado, null, 2));
  if (!resultado.cumple) fallos = true;
}
process.exitCode = fallos ? 1 : 0;
