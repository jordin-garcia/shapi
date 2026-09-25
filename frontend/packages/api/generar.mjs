import { readdir, mkdir, writeFile } from 'node:fs/promises';
import { resolve, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import openapiTS, { astToString } from 'openapi-typescript';

export async function generarContratos({
  contratos = fileURLToPath(new URL('../../../contratos/openapi/', import.meta.url)),
  salida = fileURLToPath(new URL('./src/generado/', import.meta.url)),
} = {}) {
  const archivos = (await readdir(contratos, { withFileTypes: true }))
    .filter(archivo => archivo.isFile() && archivo.name.endsWith('.yaml'))
    .map(archivo => archivo.name).sort();
  const generados = [];
  for (const archivo of archivos) {
    const tipos = await openapiTS(pathToFileURL(join(contratos, archivo)));
    generados.push({ nombre: archivo.replace(/\.yaml$/, '.ts'), contenido: astToString(tipos) });
  }
  if (generados.length) await mkdir(salida, { recursive: true });
  for (const archivo of generados) await writeFile(join(salida, archivo.nombre), archivo.contenido);
  return generados.map(archivo => archivo.nombre);
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    const generados = await generarContratos();
    console.log(generados.length ? `Tipos generados: ${generados.join(', ')}` : 'No hay contratos YAML que generar.');
  } catch (error) {
    console.error('No se pudieron generar los contratos:', error.message);
    process.exitCode = 1;
  }
}
