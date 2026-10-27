import { test, expect, type Page } from '@playwright/test';

const panel = 'https://shapi.localhost';
const portal = 'https://envios.shapi.localhost';
const claveDemo = 'shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d';
const dominio = 'api.enviosxelaju.localhost';

async function entrar(page: Page, origen: string, correo: string) {
  await page.goto(`${origen}/entrar`);
  await page.getByLabel('Correo electrónico').fill(correo);
  await page.getByLabel('Contraseña', { exact: true }).fill('Shapi2026!demo');
  await page.getByRole('button', { name: 'Entrar', exact: true }).click();
  await expect(page).not.toHaveURL(/\/entrar$/);
}

// RF-12, RF-15, RF-16, RF-25, RF-43 y RF-47: navegador y servicios reales, sin interceptar respuestas.
test('JG-24 · dominio, documentación, consola y desconexión al renovar la bajada', async ({ browser }, info) => {
  const contextos = await Promise.all(Array.from({ length: 4 }, () => browser.newContext({
    ignoreHTTPSErrors: true, viewport: { width: 1440, height: 900 }, colorScheme: 'light',
  })));
  const [proveedor, agro, consumidor, admin] = await Promise.all(contextos.map(c => c.newPage()));
  try {
    await entrar(proveedor, panel, 'ana.morales@enviosxelaju.com');
    const apis = (await (await proveedor.request.get(`${panel}/api/apis`)).json()).elementos as { id: string; subdominio: string }[];
    const api = apis.find(a => a.subdominio === 'envios')!;
    await proveedor.goto(`${panel}/panel/apis/${api.id}/dominios`);
    await expect(proveedor.getByRole('heading', { name: 'Direcciones y dominio propio' })).toBeVisible();
    await expect(proveedor.getByText(/shps_••••/)).toBeVisible();
    await proveedor.screenshot({ path: info.outputPath('A3-6-envios-pendiente.png'), fullPage: true });

    await entrar(agro, panel, 'carlos.tzul@agroprecios.com');
    const apisAgro = (await (await agro.request.get(`${panel}/api/apis`)).json()).elementos as { id: string; subdominio: string }[];
    await agro.goto(`${panel}/panel/apis/${apisAgro.find(a => a.subdominio === 'agro')!.id}/dominios`);
    await expect(agro.getByRole('heading', { name: 'Direcciones y dominio propio' })).toBeVisible();
    await expect(agro.getByText(/shps_••••/)).toBeVisible();
    await agro.screenshot({ path: info.outputPath('A3-6-agro-sin-dominio.png'), fullPage: true });

    const campo = proveedor.getByLabel('Dominio', { exact: true });
    await campo.fill('api.ejemplo.com');
    await proveedor.getByRole('button', { name: 'Conectar dominio', exact: true }).click();
    await expect(proveedor.getByText('Use un dominio válido que termine en .localhost.')).toBeVisible();
    await proveedor.screenshot({ path: info.outputPath('A3-6-error-validacion.png'), fullPage: true });
    await campo.fill(dominio);
    await proveedor.getByRole('button', { name: 'Conectar dominio', exact: true }).click();
    await expect(proveedor.getByText('El dominio quedó pendiente de verificación.')).toBeVisible();
    await proveedor.getByRole('button', { name: 'Simular la creación del registro' }).click();
    await expect(proveedor.getByText('El registro DNS simulado se creó.')).toBeVisible();
    await proveedor.getByRole('button', { name: 'Verificar registro DNS' }).click();
    await expect(proveedor.getByText('Verificado', { exact: true })).toBeVisible();
    await expect(proveedor.getByText('Verificado', { exact: true })).toHaveCSS('color', 'rgb(20, 101, 66)');
    await expect(proveedor.getByLabel('Certificado TLS del dominio propio')).toHaveText('Se emite con la primera petición');
    await proveedor.screenshot({ path: info.outputPath('A3-6-envios-verificado.png'), fullPage: true });

    await consumidor.goto(`${portal}/documentacion`);
    await expect(consumidor.getByText(`https://${dominio}/cotizaciones`, { exact: true })).toBeVisible();
    await consumidor.screenshot({ path: info.outputPath('A5-1-documentacion-dominio.png'), fullPage: true });
    await consumidor.goto(`${portal}/consola`);
    await consumidor.getByLabel('Su clave de pruebas').fill(claveDemo);
    const peticion = consumidor.waitForRequest(r => r.url() === 'https://envios.api.shapi.localhost/cotizaciones' && r.method() === 'POST');
    await consumidor.getByRole('button', { name: 'Enviar petición' }).click();
    await peticion;
    await expect(consumidor.getByText('200', { exact: true })).toBeVisible();
    await consumidor.screenshot({ path: info.outputPath('A5-2-consola-dominio.png'), fullPage: true });

    await entrar(consumidor, portal, 'mariajose@mercaditoantigua.com');
    const planesPortal = await (await consumidor.request.get(`${portal}/api/portal/planes`)).json() as { id: string; esGratuito: boolean }[];
    await consumidor.goto(`${portal}/contratar/${planesPortal.find(p => !p.esGratuito)!.id}`);
    await expect(consumidor.getByRole('heading', { name: 'Datos de la tarjeta' })).toBeVisible();
    await expect(consumidor.getByText(/No se guardan el número completo ni el código de seguridad:/)).toBeVisible();
    await expect(consumidor.getByText(/\bShapi\b/)).toHaveCount(0);
    await consumidor.screenshot({ path: info.outputPath('A5-6-pago-sin-marca.png'), fullPage: true });

    await proveedor.goto(`${panel}/panel/suscripcion`);
    const suscripcion = await (await proveedor.request.get(`${panel}/api/suscripcion`)).json() as { proximaRenovacion: string };
    const planes = await (await proveedor.request.get(`${panel}/api/planes-plataforma`)).json() as { id: string; nombre: string }[];
    await proveedor.goto(`${panel}/panel/suscripcion/cambiar/${planes.find(p => p.nombre === 'Lanzamiento')!.id}`);
    await expect(proveedor.getByText(/Al aplicar el cambio, se desconectarán los dominios propios/)).toBeVisible();
    await proveedor.screenshot({ path: info.outputPath('A2-5-aviso-desconexion.png'), fullPage: true });
    await proveedor.getByRole('button', { name: 'Programar cambio de plan' }).click();
    await expect(proveedor).toHaveURL(`${panel}/panel/suscripcion`);
    await expect(proveedor.getByRole('heading', { name: 'Suscripción de plataforma', exact: true })).toBeVisible();
    await expect(proveedor.getByText(/A partir del .* su plan será Lanzamiento\./)).toBeVisible();
    await expect(proveedor.getByText(/Al aplicar el cambio, se desconectarán los dominios propios/)).toBeVisible();
    await proveedor.screenshot({ path: info.outputPath('B1-4-aviso-desconexion.png'), fullPage: true });
    const antes = await proveedor.request.post(`https://${dominio}/cotizaciones`, {
      headers: { 'X-Api-Key': claveDemo }, data: { origen: '0901', destino: '0301', peso_kg: 2.5, tipo_servicio: 'normal' },
    });
    expect(antes.status()).toBe(200);
    await entrar(admin, panel, 'rodrigo.alvarado@shapi.localhost');
    await admin.goto(`${panel}/admin/estado`);
    const reloj = await (await admin.request.get(`${panel}/api/admin/demo/reloj`)).json() as { ahora: string };
    const dias = Math.ceil((Date.parse(suscripcion.proximaRenovacion) - Date.parse(reloj.ahora)) / 86_400_000) + 1;
    await admin.getByLabel('Días que desea adelantar').fill(String(dias));
    await admin.getByRole('button', { name: 'Adelantar días', exact: true }).click();
    await expect(async () => {
      const estado = await (await proveedor.request.get(`${panel}/api/apis/${api.id}/dominio`)).json();
      expect(estado.dominio).toBeNull();
      expect(estado.dominioPropioPermitido).toBe(false);
    }).toPass({ timeout: 15_000, intervals: [300, 600] });
    await expect(async () => {
      const despues = await proveedor.request.post(`https://${dominio}/cotizaciones`, { headers: { 'X-Api-Key': claveDemo }, data: {} });
      expect(despues.status()).toBe(404);
      expect((await despues.json()).error.codigo).toBe('api_no_encontrada');
    }).toPass({ timeout: 10_000, intervals: [200, 400] });
    const subdominio = await proveedor.request.post('https://envios.api.shapi.localhost/cotizaciones', {
      headers: { 'X-Api-Key': claveDemo }, data: { origen: '0901', destino: '0301', peso_kg: 2.5, tipo_servicio: 'normal' },
    });
    expect(subdominio.status()).toBe(200);
    await proveedor.goto(`${panel}/panel/apis/${api.id}/dominios`);
    await expect(proveedor.getByText(/Su plan Lanzamiento no incluye dominio propio/)).toBeVisible();
    await proveedor.screenshot({ path: info.outputPath('A3-6-despues-bajada.png'), fullPage: true });
  } finally {
    await Promise.all(contextos.map(c => c.close()));
  }
});
