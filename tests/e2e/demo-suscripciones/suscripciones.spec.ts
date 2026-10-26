import { test, expect, type Page } from '@playwright/test';

const panel = 'https://shapi.localhost';
const portal = 'https://envios.shapi.localhost';

async function entrar(page: Page, origen: string, correo: string) {
  await page.goto(`${origen}/entrar`);
  await page.getByLabel('Correo electrónico').fill(correo);
  await page.getByLabel('Contraseña', { exact: true }).fill('Shapi2026!demo');
  await page.getByRole('button', { name: 'Entrar', exact: true }).click();
  await expect(page).not.toHaveURL(/\/entrar$/);
}

// RF-24, RF-25, RF-44: ejecutar solo en un ambiente demo aislado; avanza su reloj global.
test('JG-23 · reversión del guion 3, bajada cancelada y aviso de la Prueba desde la interfaz', async ({ browser }, info) => {
  const contextos = await Promise.all(Array.from({ length: 5 }, () => browser.newContext({
    ignoreHTTPSErrors: true, viewport: { width: 1440, height: 900 },
  })));
  const [admin, proveedor, consumidor, prueba, rechazada] = await Promise.all(contextos.map(contexto => contexto.newPage()));
  try {
    await entrar(consumidor, portal, 'mariajose@mercaditoantigua.com');
    await consumidor.goto(`${portal}/cuenta/suscripcion`);
    await consumidor.getByRole('link', { name: 'Cambiar de plan', exact: true }).click();
    await consumidor.getByRole('button', { name: 'Elegir Básico', exact: true }).click();
    await consumidor.getByRole('button', { name: 'Programar cambio de plan', exact: true }).click();
    await expect(consumidor).toHaveURL(`${portal}/cuenta/suscripcion`);
    await expect(consumidor.getByText(/A partir del .* su plan será Básico\./)).toBeVisible();
    await consumidor.screenshot({ path: info.outputPath('B2-3-bajada-programada.png'), fullPage: true });
    await consumidor.getByRole('button', { name: 'Cancelar cambio programado' }).click();
    await expect(consumidor.getByText(/su plan será Básico/)).toHaveCount(0);
    await consumidor.screenshot({ path: info.outputPath('B2-3-bajada-cancelada.png'), fullPage: true });

    await entrar(proveedor, panel, 'ana.morales@enviosxelaju.com');
    await proveedor.goto(`${panel}/panel/suscripcion`);
    await expect(proveedor.getByText('Periodo activo', { exact: true })).toBeVisible();
    await proveedor.screenshot({ path: info.outputPath('B1-4-vigente.png'), fullPage: true });
    await entrar(admin, panel, 'rodrigo.alvarado@shapi.localhost');
    await admin.goto(`${panel}/admin/pagos`);
    const fila = admin.getByRole('row').filter({ hasText: 'Envíos Xelajú' }).filter({ has: admin.getByRole('button', { name: /Revertir pago de/ }) }).first();
    const monto = await fila.getByRole('cell').nth(3).innerText();
    await fila.getByRole('button', { name: /Revertir pago de/ }).click();
    await admin.getByRole('button', { name: 'Revertir pago', exact: true }).click();
    await expect(admin.getByText('Pago revertido.', { exact: true })).toBeVisible();
    await proveedor.reload();
    await expect(proveedor.getByText(`El pago de ${monto} fue revertido`, { exact: false })).toBeVisible();
    await expect(proveedor.getByText('Periodo activo', { exact: true })).toBeVisible();
    await expect(proveedor.getByText('Pago revertido', { exact: true })).toBeVisible();
    await expect(proveedor.getByText('Cobro rechazado', { exact: true })).toHaveCount(0);
    await proveedor.screenshot({ path: info.outputPath('B1-4-gracia-reversion.png'), fullPage: true });
    await proveedor.setViewportSize({ width: 1366, height: 650 });
    await proveedor.screenshot({ path: info.outputPath('B1-4-reversion-1366.png'), fullPage: true });

    await entrar(rechazada, panel, 'mario.pop@transportespeten.com');
    await rechazada.goto(`${panel}/panel/suscripcion`);
    await expect(rechazada.getByText('Cobro rechazado', { exact: true })).toBeVisible();
    await expect(rechazada.getByText('Periodo vencido', { exact: true })).toBeVisible();
    await rechazada.screenshot({ path: info.outputPath('B1-4-gracia-rechazo.png'), fullPage: true });

    await entrar(prueba, panel, 'lucia.cotom@cafetaleraaltiplano.com');
    await prueba.goto(`${panel}/panel/apis`);
    await expect(prueba.getByText(/Su Prueba vence el/)).toHaveCount(0);
    await admin.goto(`${panel}/admin/estado`);
    await admin.getByLabel('Días que desea adelantar').fill('7');
    await admin.getByRole('button', { name: 'Adelantar días', exact: true }).click();
    await expect(admin.getByRole('button', { name: 'Adelantar días', exact: true })).toBeEnabled();
    await prueba.reload();
    await expect(prueba.getByText(/Su Prueba vence el/)).toBeVisible();
    await expect(prueba.getByRole('link', { name: 'Ver planes de pago' })).toHaveAttribute('href', '/panel/suscripcion/planes');
    await prueba.screenshot({ path: info.outputPath('panel-aviso-prueba.png'), fullPage: true });
    await prueba.goto(`${panel}/panel/suscripcion`);
    await expect(prueba.getByText(/Su Prueba vence el/)).toBeVisible();
    await prueba.screenshot({ path: info.outputPath('B1-4-aviso-prueba.png'), fullPage: true });
    await expect(async () => {
      await proveedor.reload();
      await expect(proveedor.getByRole('heading', { name: 'Su tráfico está detenido', exact: true })).toBeVisible();
    }).toPass({ timeout: 10_000, intervals: [200, 400] });
    await proveedor.screenshot({ path: info.outputPath('B1-4-suspendida-por-reversion.png'), fullPage: true });
  } finally {
    await Promise.all(contextos.map(contexto => contexto.close()));
  }
});
