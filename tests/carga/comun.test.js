import { test } from 'node:test';
import assert from 'node:assert/strict';
import { cuerpo, opciones, resumen } from './comun.js';

test('RNF-03: modelo abierto de 200/s durante 5 minutos con 60000 solicitudes', () => {
  const datos = opciones({});
  assert.equal(datos.scenarios.carga.executor, 'constant-arrival-rate');
  assert.equal(datos.scenarios.carga.rate, 200);
  assert.equal(datos.scenarios.carga.duration, '300s');
  assert.deepEqual(datos.thresholds.http_reqs, ['count==60000']);
  assert.deepEqual(datos.thresholds.dropped_iterations, ['count==0']);
  assert.deepEqual(datos.summaryTrendStats, ['med', 'p(95)', 'p(99)']);
});

test('RNF-01: el cuerpo compartido corresponde a la cotización del origen', () => {
  assert.deepEqual(JSON.parse(cuerpo), { origen: '0901', destino: '0101', peso_kg: 2.5, tipo_servicio: 'normal' });
  assert.deepEqual(opciones({ CARGA_BORDE: '127.0.0.1:18443' }).hosts, { 'envios.api.shapi.localhost:443': '127.0.0.1:18443' });
});

test('RNF-03: resume los contadores y percentiles realmente medidos por k6', () => {
  const datos = resumen({ metrics: {
    http_reqs: { values: { count: 59000, rate: 196.65 } }, iterations: { values: { count: 59000 } },
    dropped_iterations: { values: { count: 1000 } }, errores: { values: { passes: 70 } },
    respuestas_invalidas: { values: { count: 2 } },
    http_req_duration: { values: { med: 3, 'p(95)': 8, 'p(99)': 12 } },
  } }, 'A', {});
  assert.equal(datos.errores, 70);
  assert.equal(datos.omitidas, 1000);
  assert.equal(datos.p95, 8);
  assert.equal(datos.peticiones, 59000);
  assert.equal(datos.peticionesPorSegundo, 196.65);
});
