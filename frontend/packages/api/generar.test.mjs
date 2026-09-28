import { afterEach, describe, expect, it } from 'vitest';
import { mkdtemp, mkdir, writeFile, readFile, readdir, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
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
  it('RNF-12 · H-90 admite que la carpeta de contratos no exista', async () => {
    const rutas = await entorno();
    await expect(generarContratos({ contratos: join(rutas.contratos, 'no-existe'), salida: rutas.salida })).resolves.toEqual([]);
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

describe('RNF-12 · exportación de los tipos generados (H-83)', () => {
  const contratos = fileURLToPath(new URL('../../../contratos/openapi/', import.meta.url));
  const generado = fileURLToPath(new URL('./src/generado/', import.meta.url));

  it('exporta cualquier módulo generado como @shapi/api/<modulo>, sin editar package.json', async () => {
    const paquete = JSON.parse(await readFile(new URL('./package.json', import.meta.url), 'utf8'));
    expect(paquete.exports['./*']).toBe('./src/generado/*.ts');
  });
  it('cada contrato de contratos/openapi se importa como @shapi/api/<modulo>', async () => {
    const modulos = (await readdir(contratos)).filter(archivo => archivo.endsWith('.yaml')).map(archivo => archivo.replace(/\.yaml$/, ''));
    expect(modulos).toContain('identidad');
    const requerir = createRequire(import.meta.url);
    for (const modulo of modulos) expect(requerir.resolve(`@shapi/api/${modulo}`)).toBe(join(generado, `${modulo}.ts`));
  });
});
