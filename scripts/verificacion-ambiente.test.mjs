import { test } from "node:test";
import assert from "node:assert/strict";
import { exigirPersistenciaRedis, verificarDominioNoVerificado, calentarCompuerta } from "../infra/verificacion-ambiente.mjs";

// RNF-05: la presencia de las opciones no basta; se exigen sus valores efectivos.
test("JG-27: Redis exige AOF yes y fsync everysec", () => {
  exigirPersistenciaRedis(["redis-server", "--appendonly", "yes", "--appendfsync", "everysec"]);
  for (const comando of [
    ["redis-server", "--appendonly", "no", "--appendfsync", "everysec"],
    ["redis-server", "--appendonly", "yes", "--appendfsync", "no"],
    ["redis-server", "--appendonly", "yes"],
  ]) assert.throws(() => exigirPersistenciaRedis(comando));
});

// RNF-09: un certificado almacenado no puede abrir los endpoints internos.
test("JG-27: el dominio no verificado admite rechazo TLS o 404, nunca acceso interno", async () => {
  await verificarDominioNoVerificado(async () => { throw new Error("TLS rechazado"); });
  await verificarDominioNoVerificado(async () => ({ statusCode: 404 }));
  for (const statusCode of [200, 302, 401, 500]) {
    await assert.rejects(verificarDominioNoVerificado(async () => ({ statusCode })));
  }
});

// RF-39, RF-45: se calienta tráfico real de pruebas; no se alteran las cuotas de producción ni el histograma.
test("JG-27: calentamiento con tres claves de pruebas, diez peticiones por clave", async () => {
  const llamadas = [];
  await calentarCompuerta(async (clave) => { llamadas.push(clave); return { statusCode: 200 }; });
  assert.equal(llamadas.length, 30);
  const claves = [...new Set(llamadas)];
  assert.equal(claves.length, 3);
  for (const clave of claves) {
    assert.ok(clave.startsWith("shp_prueba_"));
    assert.equal(llamadas.filter((c) => c === clave).length, 10);
  }
});

test("JG-27: calentamiento respeta Retry-After y no acepta fallos del origen", async () => {
  const esperas = [];
  let llamadas = 0;
  await calentarCompuerta(async () => ++llamadas === 1
    ? { statusCode: 429, headers: { "retry-after": "12" } }
    : { statusCode: 200 }, async (ms) => esperas.push(ms));
  assert.equal(llamadas, 31);
  assert.deepEqual(esperas, [12000]);
  await assert.rejects(calentarCompuerta(async () => ({ statusCode: 502 })));
  await assert.rejects(calentarCompuerta(async () => ({ statusCode: 429, headers: { "retry-after": "1" } }), async () => {}));
});
