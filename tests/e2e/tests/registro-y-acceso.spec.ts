import { test, expect } from '@playwright/test';
import { esperarEnlaceCorreo } from '../soporte/mailpit';

test('RNF-15: registro del proveedor, verificacion por correo y acceso al panel', async ({ page }) => {
  test.setTimeout(60_000);
  const sufijo = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  const correo = `e2e-${sufijo}@ejemplo.com`;

  await page.goto('/registro');
  await page.getByLabel('Nombre', { exact: true }).fill('Proveedor E2E');
  await page.getByLabel(/^Correo electr.nico$/u).fill(correo);
  await page.getByLabel(/^Nombre de la organizaci.n$/u).fill(`Organizacion E2E ${sufijo}`);
  await page.getByLabel(/^Contrase.a$/u).fill('PruebaE2E2026!');
  await page.getByRole('button', { name: 'Crear cuenta' }).click();

  await expect(page.getByRole('heading', { name: 'Revise su correo' })).toBeVisible();
  const enlace = await esperarEnlaceCorreo(correo, {
    contiene: '/verificar-correo?token=',
    tiempoLimiteMs: 30_000,
  });

  await page.goto(enlace);
  await expect(page).toHaveURL(/\/panel\/apis$/);
  await expect(page.getByRole('navigation').getByRole('link', { name: 'APIs', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: /^APIs de la organizaci.n$/u })).toBeVisible();
});
