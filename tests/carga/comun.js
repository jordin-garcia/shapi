export const cuerpo = JSON.stringify({ origen: '0901', destino: '0101', peso_kg: 2.5, tipo_servicio: 'normal' });

// RNF-03: el modelo abierto mantiene la tasa aunque aumente la latencia.
export function opciones(entorno) {
  const tasa = Number(entorno.CARGA_TASA || 200);
  const segundos = Number(entorno.CARGA_SEGUNDOS || 300);
  if (!Number.isInteger(tasa) || tasa < 1 || !Number.isInteger(segundos) || segundos < 1) {
    throw new Error('CARGA_TASA y CARGA_SEGUNDOS deben ser enteros positivos.');
  }
  const hosts = {};
  if (entorno.CARGA_BORDE) hosts['envios.api.shapi.localhost:443'] = entorno.CARGA_BORDE;
  if (entorno.CARGA_ORIGEN) hosts['localhost:5101'] = entorno.CARGA_ORIGEN;
  return {
    hosts,
    // Solo la CA local de Caddy; nunca se usa este escenario contra producción real.
    insecureSkipTLSVerify: true,
    discardResponseBodies: false,
    summaryTrendStats: ['med', 'p(95)', 'p(99)'],
    scenarios: {
      carga: { executor: 'constant-arrival-rate', rate: tasa, timeUnit: '1s', duration: `${segundos}s`, preAllocatedVUs: 100, maxVUs: 500, gracefulStop: '15s' },
    },
    thresholds: {
      errores: ['rate<0.01'],
      respuestas_invalidas: ['count==0'],
      dropped_iterations: ['count==0'],
      iterations: [`count==${tasa * segundos}`],
      http_reqs: [`count==${tasa * segundos}`],
    },
    systemTags: ['status', 'method', 'name', 'scenario', 'error_code'],
    // No registrar cuerpos ni cabeceras: contienen claves en las solicitudes.
    maxRedirects: 0,
  };
}

export function resumen(datos, escenario, entorno) {
  const valor = (nombre, campo) => datos.metrics[nombre]?.values?.[campo];
  const tasa = Number(entorno.CARGA_TASA || 200);
  const duracionSegundos = Number(entorno.CARGA_SEGUNDOS || 300);
  return {
    escenario, tasa, duracionSegundos,
    peticiones: valor('http_reqs', 'count'), iteraciones: valor('iterations', 'count'),
    omitidas: valor('dropped_iterations', 'count') ?? 0,
    errores: valor('errores', 'passes'), respuestasInvalidas: valor('respuestas_invalidas', 'count'),
    p50: valor('http_req_duration', 'med'), p95: valor('http_req_duration', 'p(95)'), p99: valor('http_req_duration', 'p(99)'),
    peticionesPorSegundo: valor('http_reqs', 'rate'),
    metricas: datos.metrics,
  };
}
