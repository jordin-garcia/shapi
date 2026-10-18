import { test } from 'node:test';
import assert from 'node:assert/strict';
import { comparar } from './comparar.js';

function resumen(escenario, p95 = 20) {
  return {
    escenario, tasa: 200, duracionSegundos: 300,
    peticiones: 60000, iteraciones: 60000, omitidas: 0,
    errores: 0, respuestasInvalidas: 0,
    p50: 10, p95, p99: 30,
  };
}

// RNF-01: se compara el p95, no el promedio ni el p99.
test('RNF-01: acepta 15 ms adicionales y rechaza un exceso', () => {
  assert.equal(comparar(resumen('A', 35), resumen('B')).cumple, true);
  assert.equal(comparar(resumen('A', 35.01), resumen('B')).cumple, false);
});

// RNF-03: el 1 % exacto ya incumple; no se ocultan fallos con percentiles bajos.
test('RNF-03: exige menos del 1 % de errores', () => {
  assert.equal(comparar({ ...resumen('A'), errores: 599 }, resumen('B')).cumple, true);
  assert.equal(comparar({ ...resumen('A'), errores: 600 }, resumen('B')).cumple, false);
});

test('RNF-03: acepta la iteración adicional del límite temporal, sin admitir menos carga', () => {
  const a = { ...resumen('A'), peticiones: 60001, iteraciones: 60001 };
  assert.equal(comparar(a, resumen('B')).cumple, true);
  assert.equal(comparar({ ...a, iteraciones: 60000 }, resumen('B')).cumple, false);
  assert.equal(comparar({ ...a, peticiones: 60002, iteraciones: 60002 }, resumen('B')).cumple, false);
});

test('RNF-03: una carga incompleta, omitida o breve no demuestra el requisito', () => {
  for (const cambios of [
    { peticiones: 59999 }, { iteraciones: 59999 }, { omitidas: 1 },
    { tasa: 199 }, { duracionSegundos: 299 }, { respuestasInvalidas: 1 },
  ]) {
    assert.equal(comparar({ ...resumen('A'), ...cambios }, resumen('B')).cumple, false);
  }
});

test('RNF-01: rechaza una referencia directa inválida o intercambiada', () => {
  assert.equal(comparar(resumen('B'), resumen('A')).cumple, false);
  assert.equal(comparar(resumen('A'), { ...resumen('B'), errores: 1 }).cumple, false);
  assert.equal(comparar(resumen('A'), { ...resumen('B'), p95: null }).cumple, false);
});

test('RNF-01/RNF-03: no acepta datos ausentes, NaN ni contadores negativos', () => {
  for (const cambios of [{ p95: NaN }, { errores: -1 }, { omitidas: undefined }, { p99: Infinity }]) {
    assert.equal(comparar({ ...resumen('A'), ...cambios }, resumen('B')).cumple, false);
  }
});
