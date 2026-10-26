import { test, expect, type Page } from '@playwright/test';
import { esperarEnlaceCorreo } from '../soporte/mailpit';

const panel = 'https://shapi.localhost';
const portal = 'https://envios.shapi.localhost';

async function entrar(page: Page, correo: string) {
  await page.goto(`${panel}/entrar`);
  await page.getByLabel('Correo electrónico').fill(correo);
  await page.getByLabel('Contraseña', { exact: true }).fill('Shapi2026!demo');
  await page.getByRole('button', { name: 'Entrar', exact: true }).click();
  await expect(page).not.toHaveURL(/\/entrar$/);
}

async function comprobarCompuerta(page: Page, clave: string, estado: number) {
  if (new URL(page.url()).pathname !== '/consola') await page.goto(`${portal}/consola`);
  await page.getByLabel('Ruta', { exact: true }).selectOption({ label: 'GET /cobertura' });
  await page.getByLabel('Su clave de pruebas').fill(clave);
  const respuesta = page.waitForResponse(r => new URL(r.url()).hostname === 'envios.api.shapi.localhost' && new URL(r.url()).pathname === '/cobertura');
  await page.getByRole('button', { name: 'Enviar petición', exact: true }).click();
  expect((await respuesta).status()).toBe(estado);
  await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: 'Respuesta', exact: true }) }).last().getByText(String(estado), { exact: true })).toBeVisible();
}

test('RF-04, RF-22 y RF-23 · guion 3 paso 4 solo con la interfaz', async ({ browser }, info) => {
  const contexto = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 900 } });
  const admin = await contexto.newPage();
  const consumidor = await contexto.newPage();
  const consola = await contexto.newPage();
  for (const pagina of [admin, consumidor, consola]) pagina.setDefaultTimeout(15_000);
  try {
    await entrar(admin, 'rodrigo.alvarado@shapi.localhost');
    await admin.goto(`${panel}/admin/estado`);
    await expect(admin.getByRole('heading', { name: 'Modo demostración' })).toBeVisible();
    await admin.screenshot({ path: info.outputPath('B3-1-administrador.png'), fullPage: true });
    await admin.getByRole('heading', { name: 'Modo demostración' }).scrollIntoViewIfNeeded();
    await admin.screenshot({ path: info.outputPath('B3-1-tarjeta-reloj.png'), fullPage: true });

    // RF-04: otra sesión del personal abierta antes de adelantar la demostración.
    const contextoProveedor = await browser.newContext({ ignoreHTTPSErrors: true });
    const proveedorReal = await contextoProveedor.newPage();
    proveedorReal.setDefaultTimeout(15_000);
    await entrar(proveedorReal, 'ana.morales@enviosxelaju.com');
    await proveedorReal.goto(`${panel}/panel/suscripcion`);
    await expect(proveedorReal.getByRole('heading', { name: 'Suscripción de plataforma', exact: true })).toBeVisible();
    const renovacionAnterior = await proveedorReal.getByText('Renovación automática', { exact: true }).locator('..').innerText();

    const correo = `reloj-${Date.now()}@ejemplo.com`;
    await consumidor.goto(`${portal}/registro`);
    await consumidor.getByLabel('Nombre', { exact: true }).fill('Consumidor Reloj');
    await consumidor.getByLabel('Correo electrónico').fill(correo);
    await consumidor.getByLabel('Nombre de la empresa').fill('Demostración Reloj');
    await consumidor.getByLabel('Contraseña', { exact: true }).fill('PruebaReloj2026!');
    await consumidor.getByRole('button', { name: 'Crear cuenta', exact: true }).click();
    await expect(consumidor).toHaveURL(/verificar-correo/);
    await consumidor.goto(await esperarEnlaceCorreo(correo, { contiene: '/verificar-correo?token=' }));
    await expect(consumidor).toHaveURL(`${portal}/planes`);
    await expect(consumidor.getByRole('heading', { name: 'Planes de la API', exact: true })).toBeVisible();
    await consumidor.getByRole('article').filter({ has: consumidor.getByRole('heading', { name: 'Básico', exact: true }) }).getByRole('button', { name: 'Contratar', exact: true }).click();
    await expect(consumidor.getByRole('heading', { name: 'Contratación de un plan', exact: true })).toBeVisible();
    await consumidor.screenshot({ path: info.outputPath('A5-6-fecha-servidor.png'), fullPage: true });
    await consumidor.getByLabel('Número de tarjeta').fill('4000000000000341');
    await consumidor.getByLabel('Nombre del titular').fill('Consumidor Reloj');
    await consumidor.getByLabel('Vencimiento', { exact: true }).fill('12 / 30');
    await consumidor.getByLabel('Código de seguridad').fill('123');
    await consumidor.getByRole('button', { name: /Pagar .* y contratar el plan/ }).click();
    await expect(consumidor.getByRole('heading', { name: 'Plan contratado', exact: true })).toBeVisible();
    const clave = await consumidor.locator('.font-mono').filter({ hasText: /^shp_prod_/ }).innerText();
    await consumidor.goto(`${portal}/cuenta/suscripcion`);
    await consumidor.getByTestId('clave-produccion-activa').getByRole('button', { name: 'Rotar', exact: true }).click();
    await expect(consumidor.getByText('Consultando fecha…')).toHaveCount(0);
    await consumidor.screenshot({ path: info.outputPath('B2-4-fecha-servidor.png'), fullPage: true });
    await consumidor.getByRole('button', { name: 'Cancelar', exact: true }).click();
    await consumidor.getByRole('link', { name: 'Cambiar de plan', exact: true }).click();
    await consumidor.getByRole('button', { name: 'Elegir Comercio', exact: true }).click();
    await expect(consumidor.getByRole('heading', { name: 'Diferencia prorrateada', exact: true })).toBeVisible();
    await consumidor.screenshot({ path: info.outputPath('B2-7-fecha-servidor.png'), fullPage: true });
    await comprobarCompuerta(consola, clave, 200);

    // Desde el POST del reloj hasta que la interfaz ve la transición: menos de 10 s.
    const iniciado = Date.now();
    await admin.getByRole('button', { name: 'Adelantar 30 días', exact: true }).click();
    await expect(admin.getByRole('button', { name: 'Adelantar 30 días', exact: true })).toBeEnabled();
    await expect(async () => {
      await consumidor.goto(`${portal}/cuenta/suscripcion`);
      await expect(consumidor.getByRole('heading', { name: 'Suscripción y claves', exact: true })).toBeVisible();
      expect(await consumidor.getByRole('heading', { name: /Quedan .* días antes/ }).isVisible()).toBe(true);
    }).toPass({ timeout: 10_000, intervals: [200, 400] });
    await proveedorReal.reload();
    await expect(proveedorReal.getByText('Renovación automática', { exact: true }).locator('..')).not.toHaveText(renovacionAnterior);
    await comprobarCompuerta(consola, clave, 200);
    const tiempoGracia = Date.now() - iniciado;
    expect(tiempoGracia).toBeLessThan(10_000);
    console.info(`Renovación de plataforma, gracia y compuerta visibles en ${tiempoGracia} ms.`);
    await consumidor.screenshot({ path: info.outputPath('B2-3-gracia.png'), fullPage: true });

    const suspension = Date.now();
    await admin.getByRole('button', { name: 'Adelantar 7 días', exact: true }).click();
    await expect(admin.getByRole('button', { name: 'Adelantar 7 días', exact: true })).toBeEnabled();
    await expect(async () => {
      await consumidor.goto(`${portal}/cuenta/suscripcion`);
      await expect(consumidor.getByRole('heading', { name: 'Suscripción y claves', exact: true })).toBeVisible();
      expect(await consumidor.getByText('Suspendida', { exact: true }).first().isVisible()).toBe(true);
      expect(await consumidor.getByRole('button', { name: 'Pagar con otra tarjeta', exact: true }).isVisible()).toBe(true);
    }).toPass({ timeout: 10_000, intervals: [200, 400] });
    await comprobarCompuerta(consola, clave, 403);
    const tiempoSuspension = Date.now() - suspension;
    expect(tiempoSuspension).toBeLessThan(10_000);
    console.info(`Suspensión y compuerta con 403 visibles en ${tiempoSuspension} ms.`);
    await consumidor.screenshot({ path: info.outputPath('B2-3-suspendida.png'), fullPage: true });

    await consumidor.goto(`${portal}/cuenta/suscripcion`);
    await consumidor.getByRole('button', { name: 'Pagar con otra tarjeta', exact: true }).click();
    await consumidor.getByLabel('Número de tarjeta').fill('4242424242424242');
    await consumidor.getByLabel('Nombre del titular').fill('Consumidor Reloj');
    await consumidor.getByLabel('Vencimiento', { exact: true }).fill('12 / 30');
    await consumidor.getByLabel('Código de seguridad').fill('123');
    const pago = consumidor.waitForResponse(r => new URL(r.url()).pathname === '/api/portal/suscripcion/pagar' && r.request().method() === 'POST');
    await consumidor.getByRole('button', { name: /Pagar .* y reactivar/ }).click();
    expect((await pago).status()).toBe(200);
    await expect(consumidor.getByRole('heading', { name: 'Su suscripción está suspendida', exact: true })).toHaveCount(0);
    await expect(consumidor.getByRole('button', { name: /Pagar .* y reactivar/ })).toHaveCount(0);
    await comprobarCompuerta(consola, clave, 200);
    await proveedorReal.reload();
    await expect(proveedorReal.getByRole('heading', { name: 'Suscripción de plataforma', exact: true })).toBeVisible();
    await proveedorReal.screenshot({ path: info.outputPath('B1-4-renovada.png'), fullPage: true });
    await admin.goto(`${panel}/admin/bitacora`);
    await expect(admin.getByRole('table')).toBeVisible();
    await expect(admin.getByRole('table').locator('tbody tr').first()).toBeVisible();
    await admin.screenshot({ path: info.outputPath('B3-2-fecha-demostracion.png'), fullPage: true });
    await admin.getByRole('button', { name: 'Seleccionar periodo', exact: true }).click();
    await admin.getByLabel('Desde', { exact: true }).fill('2020-01-01');
    await admin.getByLabel('Hasta', { exact: true }).fill('2020-01-07');
    await admin.getByRole('button', { name: 'Aplicar', exact: true }).click();
    await expect(admin.getByRole('heading', { name: 'Todavía no hay acciones registradas', exact: true })).toBeVisible();
    await admin.screenshot({ path: info.outputPath('B3-2-vacia.png'), fullPage: true });
    await admin.getByRole('button', { name: 'Cerrar sesión', exact: true }).click();
    await expect(admin).toHaveURL(/\/entrar$/);
    await entrar(admin, 'sofia.menchu@shapi.localhost');
    await admin.goto(`${panel}/admin/estado`);
    await expect(admin.getByRole('heading', { name: 'Estado de los componentes', exact: true })).toBeVisible();
    await expect(admin.getByRole('heading', { name: 'Modo demostración', exact: true })).toHaveCount(0);
    await admin.screenshot({ path: info.outputPath('B3-1-soporte.png'), fullPage: true });
    await contextoProveedor.close();
  } finally {
    if (contexto.pages().length > 0) await contexto.close();
  }
});
