import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

// RNF-01/RNF-03: una referencia incompleta no puede certificar el rendimiento.
export function comparar(a, b) {
  const problemas = [];
  for (const [dato, escenario] of [[a, 'A'], [b, 'B']]) {
    if (!dato || dato.escenario !== escenario) problemas.push(`Falta el escenario ${escenario}.`);
    for (const campo of ['tasa', 'duracionSegundos', 'peticiones', 'iteraciones', 'omitidas', 'errores', 'respuestasInvalidas', 'p50', 'p95', 'p99']) {
      if (!Number.isFinite(dato?.[campo]) || dato[campo] < 0) problemas.push(`${escenario}: ${campo} inválido.`);
    }
    if (dato?.tasa !== 200 || dato?.duracionSegundos !== 300) problemas.push(`${escenario}: se requieren 200 peticiones/s durante 300 s.`);
    if (![60000, 60001].includes(dato?.peticiones) || dato?.iteraciones !== dato?.peticiones || dato?.omitidas !== 0) problemas.push(`${escenario}: carga incompleta o distinta de la programada.`);
    if (dato?.respuestasInvalidas !== 0) problemas.push(`${escenario}: respuesta de cotización inválida.`);
  }
  const porcentajeErrores = 100 * a?.errores / a?.peticiones;
  const adicionalP95 = a?.p95 - b?.p95;
  if (!(porcentajeErrores < 1)) problemas.push('A: los errores deben ser menores al 1 %.');
  if (b?.errores !== 0) problemas.push('B: el origen de referencia devolvió errores.');
  if (!(adicionalP95 <= 15)) problemas.push('RNF-01: la diferencia de p95 supera 15 ms.');
  return { cumple: problemas.length === 0, adicionalP95, porcentajeErrores, problemas };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const [rutaA, rutaB] = process.argv.slice(2);
    if (!rutaA || !rutaB) throw new Error('Uso: node tests/carga/comparar.js A.json B.json');
    const resultado = comparar(JSON.parse(readFileSync(rutaA, 'utf8')), JSON.parse(readFileSync(rutaB, 'utf8')));
    console.log(JSON.stringify(resultado, null, 2));
    process.exitCode = resultado.cumple ? 0 : 1;
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
