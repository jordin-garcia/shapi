import { test, expect } from '@playwright/test';
import { esperarEnlaceCorreo } from '../soporte/mailpit';
import {
  autenticarPorInterfaz, consultar, contratarPlataforma, entrar, especificacion, esperarEstadoCompuerta,
  llenarTarjeta, llamarCompuerta, panel, registrarConsumidor, registrarProveedor,
} from '../soporte/flujos';

// RNF-15: guiones 2 y 3. Cada intento crea su organización, API y consumidor,
// sin modificar las cuentas ni las claves de la siembra usadas por las otras E2E.
test('RNF-15 · publicación, contratación, claves, consumo, administración y soporte', async ({ browser, page }) => {
  test.setTimeout(420_000);
  const proveedor = await registrarProveedor(page);
  const subdominio = `e2e-${proveedor.id}`;
  const host = `https://${subdominio}.shapi.localhost`;
  const compuerta = `https://${subdominio}.api.shapi.localhost`;
  const nombreApi = `Envíos E2E ${proveedor.id}`;
  const contextoConsumidor = await browser.newContext({ ignoreHTTPSErrors: true });
  const contextoAdmin = await browser.newContext({ ignoreHTTPSErrors: true });
  const contextoSoporte = await browser.newContext({ ignoreHTTPSErrors: true });
  const consumidor = await contextoConsumidor.newPage();
  const admin = await contextoAdmin.newPage();
  const soporte = await contextoSoporte.newPage();
  let apiId = '';
  let produccion = '';
  let pruebas = '';
  let nuevaProduccion = '';
  let correoConsumidor = '';
  try {
    await test.step('RF-08 a RF-14, RF-18 · registrar, importar, exponer, configurar y publicar', async () => {
      await page.getByRole('link', { name: 'Registrar una API', exact: true }).click();
      await page.getByLabel('Nombre de la API').fill(nombreApi);
      await page.getByLabel('URL del servidor de origen').fill('http://origen-envios:8080');
      await page.getByLabel('Subdominio', { exact: true }).fill(subdominio);
      await page.getByRole('button', { name: 'Registrar y continuar' }).click();
      await expect(page.getByText('Secreto de origen', { exact: true })).toBeVisible({ timeout: 30_000 });
      await page.getByRole('button', { name: 'Continuar a la especificación' }).click();
      await expect(page).toHaveURL(/\/panel\/apis\/[^/]+\/especificacion$/);
      apiId = new URL(page.url()).pathname.split('/')[3];
      await page.getByLabel('Elegir archivo OpenAPI').setInputFiles({
        name: 'envios-e2e.json', mimeType: 'application/json', buffer: Buffer.from(especificacion),
      });
      await page.getByRole('link', { name: 'Continuar a la selección de rutas' }).click();
      await expect(page.getByLabel('Ocultar GET /tarifas', { exact: true })).toBeChecked();
      await expect(page.getByLabel('Ocultar GET /rastreo', { exact: true })).toBeChecked();
      await page.getByLabel('Exponer GET /tarifas', { exact: true }).check();
      await page.getByRole('button', { name: 'Guardar rutas expuestas' }).click();
      await expect(page.getByText('Las rutas expuestas se guardaron.', { exact: true })).toBeVisible();
      await page.goto(`${panel}/panel/apis/${apiId}/configuracion-rutas`);
      await page.getByLabel('Límite por minuto de GET /tarifas').fill('100');
      await page.getByLabel('Caché de GET /tarifas').selectOption('0');
      await page.getByLabel('Peso en llamadas de GET /tarifas').fill('1');
      await page.getByRole('button', { name: 'Guardar configuración' }).click();
      await expect(page.getByText('La configuración por ruta se guardó.', { exact: true })).toBeVisible();
      await page.goto(`${panel}/panel/apis/${apiId}/planes`);
      await page.getByRole('button', { name: 'Crear un plan', exact: true }).click();
      await page.getByLabel('Nombre', { exact: true }).fill('Comercio E2E');
      await page.getByLabel('Descripción', { exact: true }).fill('Plan para comprobar el flujo real de contratación.');
      await page.getByLabel('Cuota mensual', { exact: true }).fill('1000');
      await page.getByLabel('Límite por minuto', { exact: true }).fill('100');
      await page.getByText('Plan gratuito', { exact: true }).click();
      await page.getByLabel('Precio', { exact: true }).fill('25');
      await page.getByRole('button', { name: 'Crear plan', exact: true }).click();
      await expect(page.getByRole('heading', { name: 'Planes de la API', exact: true })).toBeVisible();
      await expect(page.getByRole('row').filter({ hasText: 'Comercio E2E' })).toContainText('Q 25.00');
      await page.goto(`${panel}/panel/apis`);
      const fila = page.getByRole('row').filter({ hasText: nombreApi });
      await fila.getByRole('button', { name: 'Publicar', exact: true }).click();
      await expect(fila).toContainText('Publicada');
      await consumidor.goto(host);
      await expect(consumidor.getByRole('link', { name: /Documentación/ }).first()).toBeVisible({ timeout: 30_000 });
      await consumidor.goto(`${host}/documentacion`);
      await expect(consumidor.getByText('/tarifas', { exact: true }).first()).toBeVisible();
      await expect(consumidor.getByText('/rastreo', { exact: true })).toHaveCount(0);
    });

    await test.step('RF-02, RF-05, RF-19, RF-26 · registro, Mailpit, tarjeta 4242 y claves una sola vez', async () => {
      correoConsumidor = await registrarConsumidor(consumidor, host);
      await consumidor.locator('article').filter({ has: consumidor.getByRole('heading', { name: 'Comercio E2E' }) })
        .getByRole('button', { name: 'Contratar', exact: true }).click();
      await llenarTarjeta(consumidor, '4242424242424242', true);
      await consumidor.getByRole('button', { name: /Pagar .*contratar el plan/ }).click();
      await expect(consumidor.getByRole('heading', { name: 'Plan contratado', exact: true })).toBeVisible({ timeout: 30_000 });
      const claves = await consumidor.locator('.font-mono').allTextContents();
      produccion = claves.find(valor => valor.startsWith('shp_prod_')) ?? '';
      pruebas = claves.find(valor => valor.startsWith('shp_prueba_')) ?? '';
      // Se comprueba el formato sin imprimir los valores completos en fallos o registros.
      expect(Boolean(produccion && pruebas)).toBe(true);
      await esperarEstadoCompuerta(compuerta, produccion, 200);
      const respuesta = await llamarCompuerta(compuerta, produccion);
      expect(respuesta.cuerpo).toHaveProperty('tarifas');
      for (const cabecera of ['x-ratelimit-limit', 'x-ratelimit-remaining', 'x-cuota-limite', 'x-cuota-restante']) {
        expect(respuesta.cabeceras[cabecera], cabecera).toBeDefined();
      }
      const oculta = await llamarCompuerta(compuerta, produccion, '/rastreo?guia=E2E');
      expect(oculta.estado).toBe(403);
      expect(oculta.cuerpo.error?.codigo).toBe('ruta_no_permitida');
      await consumidor.goto(`${host}/cuenta/suscripcion`);
      await expect(consumidor.getByTestId('clave-produccion-activa')).toBeVisible();
      expect((await consumidor.locator('body').innerText()).includes(produccion)).toBe(false);
      expect((await consumidor.locator('body').innerText()).includes(pruebas)).toBe(false);
    });

    await test.step('RF-03, RF-16 · consola real y recuperación de contraseña por correo', async () => {
      await consumidor.goto(`${host}/consola`);
      await consumidor.getByLabel('Su clave de pruebas', { exact: true }).fill(pruebas);
      await consumidor.getByRole('button', { name: 'Enviar petición', exact: true }).click();
      await expect(consumidor.locator('pre')).toContainText('tarifas', { timeout: 30_000 });
      await expect(consumidor.getByText('200', { exact: true })).toBeVisible();
      await consumidor.getByRole('button', { name: 'Cerrar sesión', exact: true }).first().click();
      await expect(consumidor).toHaveURL(/\/entrar$/);
      await consumidor.goto(`${host}/recuperar`);
      await consumidor.getByLabel('Correo electrónico').fill(correoConsumidor);
      await autenticarPorInterfaz(consumidor, '/api/portal/auth/recuperar', () => consumidor.getByRole('button', { name: 'Enviar el enlace', exact: true }).click());
      await expect(consumidor.getByRole('heading', { name: 'Revise su correo' })).toBeVisible();
      await consumidor.goto(await esperarEnlaceCorreo(correoConsumidor, { contiene: '/restablecer?token=' }));
      await consumidor.getByLabel('Contraseña nueva', { exact: true }).fill('NuevaContrasenaE2E2026!');
      await autenticarPorInterfaz(consumidor, '/api/portal/auth/restablecer', () => consumidor.getByRole('button', { name: 'Guardar la contraseña', exact: true }).click());
      await expect(consumidor).toHaveURL(/\/cuenta\/suscripcion$/);
      await expect(consumidor.getByTestId('clave-produccion-activa')).toBeVisible();
    });

    await test.step('RF-27, RF-28 · rotar conserva la anterior y revocar rechaza inmediatamente', async () => {
      await consumidor.getByTestId('clave-produccion-activa').getByRole('button', { name: 'Rotar', exact: true }).click();
      await consumidor.getByRole('button', { name: 'Rotar clave', exact: true }).click();
      await expect(consumidor.getByRole('heading', { name: 'Su clave nueva de producción' })).toBeVisible();
      nuevaProduccion = (await consumidor.locator('.font-mono').last().innerText()).trim();
      expect(nuevaProduccion.startsWith('shp_prod_') && nuevaProduccion !== produccion).toBe(true);
      await esperarEstadoCompuerta(compuerta, nuevaProduccion, 200);
      await esperarEstadoCompuerta(compuerta, produccion, 200);
      await consumidor.getByRole('button', { name: 'Ya la guardé' }).click();
      await expect(consumidor.getByTestId('clave-produccion-rotada')).toBeVisible();
      await consumidor.getByTestId('clave-produccion-activa').getByRole('button', { name: 'Revocar', exact: true }).click();
      await consumidor.getByRole('button', { name: 'Revocar clave', exact: true }).click();
      await expect(consumidor.getByTestId('clave-produccion-revocada')).toBeVisible();
      await esperarEstadoCompuerta(compuerta, nuevaProduccion, 401);
    });

    await test.step('RF-31 · superar el límite por minuto devuelve 429 con Retry-After', async () => {
      // La clave de pruebas tiene 10 peticiones/min; comparte consumidor pero no la cuota de producción.
      const estados: number[] = [];
      let rechazadas = 0;
      for (let i = 0; i < 22; i++) {
        const respuesta = await llamarCompuerta(compuerta, pruebas);
        estados.push(respuesta.estado);
        if (respuesta.estado === 429) {
          rechazadas++;
          expect(Number(respuesta.cabeceras['retry-after'])).toBeGreaterThan(0);
          expect(respuesta.cuerpo.error?.codigo).toBe('limite_por_minuto');
        }
      }
      expect(estados).toContain(200);
      expect(rechazadas).toBeGreaterThan(0);
    });

    await test.step('RF-35 a RF-37 · consumo consolidado, p95, errores y facturación real', async () => {
      interface Consumo { peticiones: number; latenciaTotalP95: { milisegundos: number | null }; rechazosCompuerta: { codigo: string; peticiones: number }[] }
      await expect.poll(async () => {
        const datos = await consultar<Consumo>(page, `/api/apis/${apiId}/consumo`);
        return datos.peticiones > 0 && datos.latenciaTotalP95.milisegundos !== null && datos.rechazosCompuerta.some(e => e.codigo === '429' && e.peticiones > 0);
      }, { timeout: 45_000, intervals: [1000, 2000] }).toBe(true);
      await page.goto(`${panel}/panel/apis/${apiId}/consumo`);
      await expect(page.getByRole('heading', { name: 'Consumo, latencia y errores', exact: true })).toBeVisible();
      await expect(page.getByText('Latencia de la compuerta · percentil 95', { exact: true })).toBeVisible();
      await expect(page.getByRole('cell', { name: '429', exact: true })).toBeVisible();
      await page.goto(`${panel}/panel/apis/${apiId}/consumidores`);
      const fila = page.getByRole('row').filter({ hasText: 'Comercio E2E' });
      await expect(fila).toContainText('Q 25.00');
      const facturacion = await consultar<{ elementos: { llamadas: number; facturacion: number }[] }>(page, `/api/apis/${apiId}/consumidores`);
      expect(facturacion.elementos[0].llamadas).toBeGreaterThan(0);
      expect(facturacion.elementos[0].facturacion).toBe(25);
      await consumidor.goto(`${host}/cuenta/consumo`);
      await expect(consumidor.getByRole('heading', { name: 'Consumo del ciclo', exact: true })).toBeVisible();
      const consumo = await consultar<{ llamadas: number; rutas: { patron: string }[] }>(consumidor, '/api/portal/consumo');
      expect(consumo.llamadas).toBeGreaterThan(0);
      expect(consumo.rutas.some(r => r.patron === '/tarifas')).toBe(true);
    });

    await test.step('RF-40, RF-46 · proveedor abre caso, soporte se asigna, responde y cierra', async () => {
      const asunto = `Caso E2E ${proveedor.id}`;
      await page.goto(`${panel}/panel/soporte`);
      await page.getByLabel('Asunto', { exact: true }).fill(asunto);
      await page.getByLabel('Descripción', { exact: true }).fill('Necesito confirmar la configuración de mi API.');
      await page.getByRole('button', { name: 'Abrir caso', exact: true }).click();
      const fila = page.getByRole('row').filter({ hasText: asunto });
      await expect(fila).toBeVisible();
      const enlace = await fila.getByRole('link', { name: 'Abrir', exact: true }).getAttribute('href');
      const numero = enlace!.split('/').at(-1);
      await entrar(soporte, panel, 'sofia.menchu@shapi.localhost');
      await soporte.goto(`${panel}/admin/casos/${numero}`);
      await expect(soporte.getByRole('heading', { name: asunto })).toBeVisible();
      await soporte.getByRole('button', { name: 'Asignarme el caso', exact: true }).click();
      await expect(soporte.getByText(/Atiende Sofía/)).toBeVisible();
      await soporte.getByLabel('Respuesta', { exact: true }).fill('Su configuración fue revisada y funciona correctamente.');
      await soporte.getByRole('button', { name: 'Enviar respuesta', exact: true }).click();
      await expect(soporte.getByText('Su configuración fue revisada y funciona correctamente.', { exact: true })).toBeVisible();
      await page.goto(`${panel}/panel/soporte/${numero}`);
      await expect(page.getByText('Su configuración fue revisada y funciona correctamente.', { exact: true })).toBeVisible();
      await soporte.getByRole('button', { name: 'Cerrar el caso', exact: true }).click();
      await expect(soporte.getByText('Cerrado', { exact: true })).toBeVisible();
      await page.reload();
      await expect(page.getByText('Cerrado', { exact: true })).toBeVisible();
    });

    await test.step('RF-38 · administrador suspende la organización, verifica 403 y la reactiva', async () => {
      await entrar(admin, panel, 'rodrigo.alvarado@shapi.localhost');
      await admin.goto(`${panel}/admin/organizaciones`);
      await admin.getByRole('button', { name: `Suspender ${proveedor.organizacion}`, exact: true }).click();
      await admin.getByLabel('Motivo administrativo').fill('Comprobación automatizada de suspensión y reactivación.');
      await admin.getByRole('button', { name: 'Suspender organización', exact: true }).click();
      await expect(admin.getByRole('button', { name: `Reactivar ${proveedor.organizacion}`, exact: true })).toBeVisible();
      // La clave rotada anterior sigue emitida y dentro de sus 24 horas.
      await esperarEstadoCompuerta(compuerta, produccion, 403);
      expect((await llamarCompuerta(compuerta, produccion)).cuerpo.error?.codigo).toBe('api_no_disponible');
      await admin.getByRole('button', { name: `Reactivar ${proveedor.organizacion}`, exact: true }).click();
      await esperarEstadoCompuerta(compuerta, produccion, 200);
    });

    await test.step('RF-39, RF-41 · revisar las acciones sensibles y los siete componentes', async () => {
      await admin.goto(`${panel}/admin/bitacora`);
      await expect(admin.getByRole('heading', { name: 'Bitácora de acciones sensibles' })).toBeVisible();
      await expect(admin.getByRole('row').filter({ hasText: proveedor.organizacion }).first()).toBeVisible();
      await soporte.goto(`${panel}/admin/estado`);
      await expect(soporte.getByRole('heading', { name: 'Estado de los componentes' })).toBeVisible();
      await expect(soporte.locator('tbody tr')).toHaveCount(7, { timeout: 30_000 });
    });

    await test.step('RF-19, RF-24 · contratar plataforma y revertir su pago desde administración', async () => {
      await contratarPlataforma(page);
      await admin.goto(`${panel}/admin/pagos`);
      const fila = admin.getByRole('row').filter({ hasText: proveedor.organizacion });
      await fila.getByRole('button', { name: /Revertir/ }).click();
      await expect(admin.getByRole('heading', { name: 'Revertir un pago' })).toBeVisible();
      await admin.getByRole('button', { name: 'Revertir pago', exact: true }).click();
      await expect(admin.getByRole('row').filter({ hasText: proveedor.organizacion })).toContainText('Revertido');
      await page.goto(`${panel}/panel/suscripcion`);
      await expect(page.getByText('En gracia', { exact: true })).toBeVisible();
      await esperarEstadoCompuerta(compuerta, produccion, 200);
    });
  } finally {
    await contextoConsumidor.close();
    await contextoAdmin.close();
    await contextoSoporte.close();
  }
});
