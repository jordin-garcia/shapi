import { test, expect, type Page } from '@playwright/test';
import { esperarEnlaceCorreo } from '../soporte/mailpit';

async function entrar(page: Page, host: string, correo: string) {
  await page.goto(`${host}/entrar`);
  await page.getByLabel('Correo electrónico').fill(correo);
  await page.getByLabel('Contraseña', { exact: true }).fill('Shapi2026!demo');
  const ojo = page.getByRole('button', { name: 'Mostrar contraseña' });
  await ojo.click();
  await expect(page.getByLabel('Contraseña', { exact: true })).toHaveAttribute('type', 'text');
  await page.getByRole('button', { name: 'Ocultar contraseña' }).click();
  await page.getByRole('button', { name: 'Entrar', exact: true }).click();
  await expect(page).not.toHaveURL(/\/entrar$/, { timeout: 30_000 });
}

for (const [area, host, correo, ruta] of [
  ['proveedor', 'https://shapi.localhost', 'ana.morales@enviosxelaju.com', '/panel/apis'],
  ['administración', 'https://shapi.localhost', 'rodrigo.alvarado@shapi.localhost', '/admin/organizaciones'],
  ['soporte', 'https://shapi.localhost', 'sofia.menchu@shapi.localhost', '/admin/casos'],
  ['consumidor', 'https://envios.shapi.localhost', 'mariajose@mercaditoantigua.com', '/cuenta/suscripcion'],
  ['documentación', 'https://envios.shapi.localhost', 'mariajose@mercaditoantigua.com', '/documentacion'],
] as const) {
  test(`RNF-12 · barra de ${area} completa y fija a 1366 × 650`, async ({ page }, info) => {
    test.setTimeout(90_000);
    await page.setViewportSize({ width: 1366, height: 650 });
    await entrar(page, host, correo);
    await page.goto(host + ruta);
    if (area === 'proveedor') {
      // Selecciona la API real sin acoplarse a su UUID de siembra.
      await page.getByRole('combobox', { name: /API/i }).selectOption({ label: 'API de Cotización de Envíos' });
      await expect(page.getByRole('link', { name: 'Configuración por ruta', exact: true })).toBeVisible();
      await page.getByRole('link', { name: 'Planes', exact: true }).click();
    }
    await expect(page.locator('main').first().getByRole('heading').first()).toBeVisible({ timeout: 30_000 });
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
    await page.route('**/api/**/salir', ruta => ruta.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ status: 503, title: 'Error de prueba' }) }));
    await barra.getByRole('button', { name: 'Cerrar sesión' }).click();
    await expect(barra.getByRole('button', { name: 'Reintentar', exact: true })).toBeInViewport({ ratio: 1 });
    await expect(barra.getByRole('button', { name: 'Cerrar sesión' })).toBeInViewport({ ratio: 1 });
    expect(await barra.evaluate(e => e.scrollHeight <= e.clientHeight)).toBe(true);
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
    prueba.innerHTML = '<button>Acción</button><button disabled class="cursor-pointer">Inactiva</button><a href="#">Enlace</a><a role="button" href="#">Botón enlace</a><select aria-label="Selector"><option>Uno</option></select><label>Casilla<input type="checkbox"></label><input type="radio" aria-label="Opción"><button aria-disabled="true" class="cursor-pointer">Inactiva ARIA</button>';
    document.body.append(prueba);
  });
  const muestra = page.locator('#cursores');
  for (const selector of ['button:not(:disabled):not([aria-disabled])', 'a', 'select', 'input', 'label']) {
    for (const accion of await muestra.locator(selector).all()) await expect(accion).toHaveCSS('cursor', 'pointer');
  }
  for (const accion of await muestra.locator(':disabled, [aria-disabled="true"]').all()) await expect(accion).toHaveCSS('cursor', 'not-allowed');
});

test('RF-20 y RNF-12 · registro, tarjeta rechazada, reintento vacío y tarjeta aprobada', async ({ page }, info) => {
  test.setTimeout(180_000);
  await page.setViewportSize({ width: 1366, height: 650 });
  const sufijo = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  const correo = `tarjeta-${sufijo}@ejemplo.com`;
  await page.goto('/registro');
  await page.getByLabel('Nombre', { exact: true }).fill('Proveedor de prueba');
  await page.getByLabel('Correo electrónico').fill(correo);
  await page.getByLabel('Nombre de la organización').fill(`Organización ${sufijo}`);
  await page.getByLabel('Contraseña', { exact: true }).fill('PruebaDeTarjeta2026!');
  await page.getByRole('button', { name: 'Mostrar contraseña' }).click();
  await expect(page.getByLabel('Contraseña', { exact: true })).toHaveAttribute('type', 'text');
  await page.getByRole('button', { name: 'Crear cuenta' }).click();
  await expect(page.getByRole('heading', { name: 'Revise su correo' })).toBeVisible({ timeout: 30_000 });
  await page.goto(await esperarEnlaceCorreo(correo, { contiene: '/verificar-correo?token=', tiempoLimiteMs: 30_000 }));
  await expect(page).toHaveURL(/\/panel\/apis$/, { timeout: 30_000 });
  await page.goto('/panel/suscripcion/planes');
  await page.getByRole('link', { name: 'Contratar', exact: true }).first().click();
  const etiquetas = ['Número de tarjeta', 'Mes de vencimiento', 'Año de vencimiento', 'Código de seguridad', 'Titular de la tarjeta'];
  for (const numero of ['4000000000000002', '4242424242424242']) {
    const valores = [numero, '12', '2035', '123', 'Proveedor de prueba'];
    for (let i = 0; i < etiquetas.length; i++) await page.getByLabel(etiquetas[i], { exact: true }).fill(valores[i]);
    await page.getByRole('button', { name: /Pagar .*activar el plan/ }).click();
    if (numero.endsWith('0002')) {
      await expect(page.getByRole('heading', { name: 'Tarjeta rechazada' })).toBeVisible({ timeout: 30_000 });
      await page.screenshot({ path: info.outputPath('tarjeta-rechazada.png') });
      await page.getByRole('button', { name: 'Usar otra tarjeta' }).click();
      for (const etiqueta of etiquetas) await expect(page.getByLabel(etiqueta, { exact: true })).toHaveValue('');
    }
  }
  await expect(page.getByRole('heading', { name: 'Plan activado' })).toBeVisible({ timeout: 30_000 });
  await page.screenshot({ path: info.outputPath('plan-activado.png') });
});
