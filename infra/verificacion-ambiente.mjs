import assert from "node:assert/strict";

export function exigirPersistenciaRedis(comando) {
  for (const [opcion, valor] of [["--appendonly", "yes"], ["--appendfsync", "everysec"]]) {
    const indice = comando.indexOf(opcion);
    assert.ok(indice >= 0 && comando[indice + 1] === valor, `Redis debe usar ${opcion} ${valor} (RNF-05)`);
  }
}

export async function verificarDominioNoVerificado(solicitar) {
  let respuesta;
  try {
    respuesta = await solicitar();
  } catch {
    // Caddy puede rechazar el saludo TLS si aún no hay un certificado en el volumen.
    return;
  }
  assert.equal(respuesta.statusCode, 404, "Un certificado previo no debe dar acceso a /interno/*");
}

export async function calentarCompuerta(cotizar, esperar = (ms) => new Promise((resolve) => setTimeout(resolve, ms))) {
  const claves = [
    "shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d",
    "shp_prueba_BoutiqueCayalaDem0000031c7",
    "shp_prueba_FerreteriaZona11De00005e28",
  ];
  for (const clave of claves) {
    for (let i = 0; i < 10; i++) {
      let respuesta;
      for (let intento = 0; intento < 3; intento++) {
        respuesta = await cotizar(clave);
        if (respuesta.statusCode !== 429) break;
        const segundos = Number(respuesta.headers?.["retry-after"]);
        assert.ok(Number.isFinite(segundos) && segundos > 0 && segundos <= 60, "Retry-After debe ser de 1 a 60 segundos");
        if (intento < 2) await esperar(segundos * 1000);
      }
      assert.equal(respuesta.statusCode, 200, `El calentamiento con clave de pruebas respondió ${respuesta.statusCode}`);
    }
  }
}
