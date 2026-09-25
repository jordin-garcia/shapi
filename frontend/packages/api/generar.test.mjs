import { afterEach, describe, expect, it } from 'vitest';
import { mkdtemp, mkdir, writeFile, readFile, readdir, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { generarContratos } from './generar.mjs';

const temporales = [];
afterEach(async () => { for (const ruta of temporales.splice(0)) await rm(ruta, { recursive: true, force: true }); });
async function entorno() {
  const raiz = await mkdtemp(join(tmpdir(), 'shapi-contratos-'));
  temporales.push(raiz);
  const contratos = join(raiz, 'contratos'); const salida = join(raiz, 'generado');
  await mkdir(contratos);
  return { contratos, salida };
}
const contrato = nombre => `openapi: 3.1.0\ninfo:\n  title: ${nombre}\n  version: 1.0.0\npaths:\n  /api/${nombre}:\n    get:\n      responses:\n        '200':\n          description: OK\n`;

describe('RNF-12 · generación por módulo', () => {
  it('admite la carpeta de contratos vacía', async () => {
    await expect(generarContratos(await entorno())).resolves.toEqual([]);
  });
  it('genera cada YAML e ignora README.md', async () => {
    const rutas = await entorno();
    await writeFile(join(rutas.contratos, 'apis.yaml'), contrato('apis'));
    await writeFile(join(rutas.contratos, 'identidad.yaml'), contrato('identidad'));
    await writeFile(join(rutas.contratos, 'README.md'), 'Instrucciones');
    await generarContratos(rutas);
    expect((await readdir(rutas.salida)).sort()).toEqual(['apis.ts', 'identidad.ts']);
    expect(await readFile(join(rutas.salida, 'apis.ts'), 'utf8')).toContain('/api/apis');
  });
  it.each(['openapi: [sin cerrar', 'openapi: 2.0\npaths: {}'])('rechaza un contrato inválido: %s', async contenido => {
    const rutas = await entorno();
    await writeFile(join(rutas.contratos, 'invalido.yaml'), contenido);
    await expect(generarContratos(rutas)).rejects.toThrow();
  });
});
