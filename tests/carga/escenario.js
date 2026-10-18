import http from 'k6/http';
import { Rate, Counter } from 'k6/metrics';
import { cuerpo, opciones, resumen } from './comun.js';

const escenario = __ENV.CARGA_ESCENARIO || 'A';
if (!['A', 'B'].includes(escenario)) throw new Error('CARGA_ESCENARIO debe ser A o B.');
if (escenario === 'A' && !__ENV.CARGA_CLAVE?.startsWith('shp_prod_')) {
  throw new Error('A requiere CARGA_CLAVE de producción; no use una clave de pruebas.');
}

export const options = opciones(__ENV);
const errores = new Rate('errores');
const invalidas = new Counter('respuestas_invalidas');
const url = escenario === 'A' ? 'https://envios.api.shapi.localhost/cotizaciones' : 'http://localhost:5101/cotizaciones';

// RNF-01/RNF-03: mismo POST y cuerpo en ambas rutas; sin caché ni pausas.
export default function () {
  const headers = { 'Content-Type': 'application/json' };
  if (escenario === 'A') headers['X-Api-Key'] = __ENV.CARGA_CLAVE;
  else if (__ENV.CARGA_SECRETO_ORIGEN) headers['X-Shapi-Secreto'] = __ENV.CARGA_SECRETO_ORIGEN;
  const respuesta = http.post(url, cuerpo, { headers, timeout: '10s', tags: { name: `cotizaciones_${escenario}` } });
  errores.add(respuesta.status !== 200);
  let valida = false;
  if (respuesta.status === 200) {
    try {
      const dato = respuesta.json();
      valida = dato.tarifa === 'Q 38.50' && dato.origen === 'Quetzaltenango' && dato.destino === 'Ciudad de Guatemala' && dato.entrega_estimada === '2 días hábiles';
    } catch { /* El contador identifica una respuesta 200 que no es la cotización. */ }
  }
  invalidas.add(respuesta.status === 200 && !valida ? 1 : 0);
}

export function handleSummary(datos) {
  const salida = JSON.stringify(resumen(datos, escenario, __ENV), null, 2);
  return { [__ENV.CARGA_RESUMEN || `/resultados/${escenario}.json`]: salida, stdout: `${salida}\n` };
}
