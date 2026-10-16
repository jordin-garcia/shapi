import { test, expect, type Page } from '@playwright/test';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

// RNF-12: usa cuentas reales de la siembra, también cuando el workflow de main parte sin datos.
test.beforeAll(() => {
  test.setTimeout(120_000);
  execFileSync('docker', ['compose', '--env-file', '.env', '-f', 'infra/compose.yml', '-f', 'infra/compose.prod.yml', 'exec', '-T', 'trabajador', 'dotnet', 'Shapi.Trabajador.dll', 'sembrar-demo'], {
    cwd: fileURLToPath(new URL('../../..', import.meta.url)), timeout: 120_000, stdio: 'pipe',
  });
});

async function entrar(page: Page, host: string, correo: string) {
  await page.goto(`${host}/entrar`);
  await page.getByLabel('Correo electrónico').fill(correo);
  await page.getByLabel('Contraseña', { exact: true }).fill('Shapi2026!demo');
  const ojo = page.getByRole('button', { name: 'Mostrar contraseña' });
  await ojo.click();
  await expect(page.getByLabel('Contraseña', { exact: true })).toHaveAttribute('type', 'text');
  await page.getByRole('button', { name: 'Ocultar contraseña' }).click();
  await page.getByRole('button', { name: 'Entrar', exact: true }).click();
  await expect(page).not.toHaveURL(/\/entrar$/);
}

for (const [area, host, correo, ruta] of [
  ['proveedor', 'https://shapi.localhost', 'ana.morales@enviosxelaju.com', '/panel/apis'],
  ['administración', 'https://shapi.localhost', 'rodrigo.alvarado@shapi.localhost', '/admin/organizaciones'],
  ['soporte', 'https://shapi.localhost', 'sofia.menchu@shapi.localhost', '/admin/casos'],
  ['consumidor', 'https://envios.shapi.localhost', 'mariajose@mercaditoantigua.com', '/cuenta/consumo'],
  ['documentación', 'https://envios.shapi.localhost', 'mariajose@mercaditoantigua.com', '/documentacion'],
] as const) {
  test(`RNF-12 · barra de ${area} completa y fija a 1366 × 650`, async ({ page }, info) => {
    await page.setViewportSize({ width: 1366, height: 650 });
    await entrar(page, host, correo);
    await page.goto(host + ruta);
    if (area === 'proveedor') {
      // Selecciona la API real sin acoplarse a su UUID de siembra.
      await page.getByRole('combobox', { name: /API/i }).selectOption({ label: 'API de Cotización de Envíos' });
      await expect(page.getByRole('link', { name: 'Configuración por ruta', exact: true })).toBeVisible();
    }
    const barra = page.getByRole('navigation').filter({ has: page.getByRole('button', { name: 'Cerrar sesión', exact: true }) });
    await expect(barra).toBeVisible();
    await expect(barra.getByRole('button', { name: 'Cerrar sesión' })).toBeInViewport({ ratio: 1 });
    for (const enlace of await barra.getByRole('link').all()) await expect(enlace).toBeInViewport({ ratio: 1 });
    expect(await barra.evaluate(e => e.scrollHeight <= e.clientHeight)).toBe(true);
    expect(await barra.evaluate(e => getComputedStyle(e).overflowY)).not.toMatch(/auto|scroll/);
    await expect(barra.getByRole('button', { name: 'Cerrar sesión' })).toHaveCSS('cursor', 'pointer');
    await expect(barra.getByRole('link').first()).toHaveCSS('cursor', 'pointer');
    if (['proveedor', 'administración', 'documentación'].includes(area)) {
      await page.screenshot({ path: info.outputPath(`${area}-1366x650.png`) });
      await page.setViewportSize({ width: 1440, height: 900 });
      await page.screenshot({ path: info.outputPath(`${area}-1440x900.png`) });
      await page.setViewportSize({ width: 1366, height: 650 });
    }
    const antes = await barra.boundingBox();
    // Hace larga la página aun si los datos de esta vista son breves; comprueba el contenedor de desplazamiento.
    const contenido = page.locator('main').first();
    await contenido.evaluate(e => { const espacio = document.createElement('div'); espacio.style.height = '2000px'; e.append(espacio); e.scrollTop = 1000; });
    expect(await contenido.evaluate(e => e.scrollTop)).toBeGreaterThan(0);
    expect(await barra.boundingBox()).toEqual(antes);
    expect(await page.evaluate(() => window.scrollY)).toBe(0);
    await expect(barra.getByRole('button', { name: 'Cerrar sesión' })).toBeInViewport({ ratio: 1 });
  });
}

test('RNF-12 · solo la lista de rutas se desplaza cuando excede la barra', async ({ page }) => {
  await page.setViewportSize({ width: 1366, height: 650 });
  await entrar(page, 'https://envios.shapi.localhost', 'mariajose@mercaditoantigua.com');
  await page.route('**/api/portal/documentacion', async ruta => {
    const respuesta = await ruta.fetch();
    const datos = await respuesta.json();
    datos.rutas = Array.from({ length: 35 }, (_, i) => ({ ...datos.rutas[0], patron: `/ruta-${i}` }));
    await ruta.fulfill({ response: respuesta, json: datos });
  });
  await page.goto('https://envios.shapi.localhost/documentacion');
  const lista = page.getByLabel('Rutas documentadas');
  await expect(lista.getByRole('link')).toHaveCount(35);
  expect(await lista.evaluate(e => e.scrollHeight > e.clientHeight)).toBe(true);
  await lista.evaluate(e => { e.scrollTop = e.scrollHeight; });
  await expect(lista.getByRole('link').last()).toBeInViewport({ ratio: 1 });
  await expect(page.getByRole('link', { name: 'Planes', exact: true })).toBeInViewport({ ratio: 1 });
  await expect(page.getByRole('button', { name: 'Cerrar sesión' })).toBeInViewport({ ratio: 1 });
});

test('RNF-12 · las acciones usan mano y las deshabilitadas conservan not-allowed', async ({ page }) => {
  await page.goto('/entrar');
  await page.evaluate(() => {
    const prueba = document.createElement('section');
    prueba.id = 'cursores';
    prueba.innerHTML = '<button>Acción</button><button disabled>Inactiva</button><a href="#">Enlace</a><a role="button" href="#">Botón enlace</a><select aria-label="Selector"><option>Uno</option></select><label>Casilla<input type="checkbox"></label><input type="radio" aria-label="Opción"><button aria-disabled="true">Inactiva ARIA</button>';
    document.body.append(prueba);
  });
  const muestra = page.locator('#cursores');
  for (const selector of ['button:not(:disabled):not([aria-disabled])', 'a', 'select', 'input', 'label']) {
    for (const accion of await muestra.locator(selector).all()) await expect(accion).toHaveCSS('cursor', 'pointer');
  }
  for (const accion of await muestra.locator(':disabled, [aria-disabled="true"]').all()) await expect(accion).toHaveCSS('cursor', 'not-allowed');
});
