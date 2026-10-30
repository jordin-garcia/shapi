import { test, expect } from '@playwright/test';
import { autenticarPorInterfaz } from '../soporte/flujos';

test('JZ-20 · RNF-12 cuentas completas sin desplazamiento horizontal a 1280 px', async ({ page }, info) => {
  test.setTimeout(90_000);
  await page.setViewportSize({ width: 1280, height: 800 });
  await page.goto('/entrar');
  await page.getByLabel('Correo electrónico').fill('rodrigo.alvarado@shapi.localhost');
  await page.getByLabel('Contraseña', { exact: true }).fill('Shapi2026!demo');
  await autenticarPorInterfaz(page, '/api/auth/entrar', () => page.getByRole('button', { name: 'Entrar', exact: true }).click());
  await expect(page).not.toHaveURL(/\/entrar$/);
  await page.goto('/admin/cuentas');
  const cuentas = page.getByRole('region', { name: 'Cuentas de plataforma' });
  await expect(cuentas.getByText('Julio Estrada Ixcot', { exact: true })).toBeVisible();
  expect(await cuentas.locator('table').evaluate(tabla => {
    const contenedor = tabla.parentElement!;
    return contenedor.scrollWidth <= contenedor.clientWidth;
  })).toBe(true);
  for (const nombre of ['Estado', 'Acción']) {
    await expect(cuentas.getByRole('columnheader', { name: nombre, exact: true })).toBeInViewport({ ratio: 1 });
  }
  for (const accion of await cuentas.getByRole('button').all()) await expect(accion).toBeInViewport({ ratio: 1 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath('A6.5-1280.png'), fullPage: true });
});
