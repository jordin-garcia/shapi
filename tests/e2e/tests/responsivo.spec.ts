import { test, expect, type Page } from '@playwright/test';
import { autenticarPorInterfaz, contrasenaDemo } from '../soporte/flujos';

// JG-32 · RNF-12: la interfaz se usa en celulares y tabletas sin desplazamiento horizontal de la página (ADR-37).

const panel = 'https://shapi.localhost';
const portal = 'https://envios.shapi.localhost';
const anchos = [360, 390, 768];

async function entrar(page: Page, host: string, correo: string) {
  await page.goto(`${host}/entrar`);
  await page.getByLabel('Correo electrónico').fill(correo);
  await page.getByLabel('Contraseña', { exact: true }).fill(contrasenaDemo);
  const ruta = host === panel ? '/api/auth/entrar' : '/api/portal/auth/entrar';
  await autenticarPorInterfaz(page, ruta, () => page.getByRole('button', { name: 'Entrar', exact: true }).click());
  await expect(page).not.toHaveURL(/\/entrar(?:\?.*)?$/, { timeout: 30_000 });
}

async function consultar<T>(page: Page, ruta: string): Promise<T> {
  return page.evaluate(async r => (await fetch(r, { headers: { 'X-Requested-With': 'shapi' } })).json(), ruta);
}

/** Espera a que la página termine de cargar sus datos y devuelve lo que se sale a lo ancho, si algo se sale. */
async function desborde(page: Page, url: string) {
  await page.goto(url);
  await page.waitForLoadState('networkidle');
  await expect(page.getByRole('status', { name: 'Cargando', exact: true })).toHaveCount(0, { timeout: 30_000 });
  return page.evaluate(() => {
    // La página no se desplaza a lo ancho, ni el documento ni el contenido (`main`), que es lo que se desplaza en el
    // panel y en el portal. Las tablas anchas sí pueden desplazarse dentro de su propio contenedor.
    const contenedores = [document.documentElement, ...document.querySelectorAll<HTMLElement>('main')];
    const desbordado = contenedores.find(e => e.scrollWidth > e.clientWidth + 1);
    if (!desbordado) return null;
    const limite = desbordado === document.documentElement ? desbordado.clientWidth : desbordado.getBoundingClientRect().right;
    const fuera = [...desbordado.querySelectorAll<HTMLElement>('*')]
      .filter(e => e.getBoundingClientRect().right > limite + 1 && e.getBoundingClientRect().width > 0)
      .filter(e => !e.parentElement || e.parentElement.getBoundingClientRect().right <= limite + 1)
      .slice(0, 3)
      .map(e => `${e.tagName.toLowerCase()}.${String(e.className).slice(0, 60)}`);
    return `${desbordado.tagName.toLowerCase()} ${desbordado.scrollWidth} > ${desbordado.clientWidth}: ${fuera.join(' | ')}`;
  });
}

const sinSesion: [string, string][] = [
  [panel, '/'], [panel, '/registro'], [panel, '/entrar'], [panel, '/recuperar'], [panel, '/verificar-correo?correo=ana@ejemplo.com'],
  [panel, '/restablecer?token=x'], [panel, '/definir-contrasena?token=x'], [panel, '/invitacion?token=x'], [panel, '/no-existe'],
  [portal, '/'], [portal, '/registro'], [portal, '/entrar'], [portal, '/recuperar'], [portal, '/verificar-correo?correo=a@b.com'],
  [portal, '/restablecer?token=x'], [portal, '/invitacion?token=x'], [portal, '/documentacion'], [portal, '/consola'], [portal, '/planes'],
  [portal, '/no-existe'],
];

for (const ancho of anchos) {
  test.describe(`RNF-12 · ${ancho} px`, () => {
    test.use({ viewport: { width: ancho, height: 800 }, isMobile: ancho < 768, hasTouch: true });

    test('sin sesión: el sitio público, el acceso y el portal no se desbordan', async ({ page }) => {
      test.setTimeout(180_000);
      const errores: string[] = [];
      for (const [host, ruta] of sinSesion) {
        const fuera = await desborde(page, host + ruta);
        if (fuera) errores.push(`${host}${ruta} → ${fuera}`);
      }
      expect(errores).toEqual([]);
    });

    test('proveedor: el panel no se desborda', async ({ page }) => {
      test.setTimeout(240_000);
      await entrar(page, panel, 'ana.morales@enviosxelaju.com');
      const apis = (await consultar<{ elementos: { id: string; nombre: string }[] }>(page, '/api/apis')).elementos;
      const id = apis.find(a => a.nombre === 'API de Cotización de Envíos')!.id;
      const casos = (await consultar<{ elementos: { numero: number }[] }>(page, '/api/casos')).elementos;
      const rutas = ['/panel/apis', '/panel/apis/nueva', '/panel/miembros', '/panel/suscripcion', '/panel/suscripcion/planes',
        '/panel/pagos', '/panel/soporte', '/panel/perfil', '/panel/no-existe',
        ...['especificacion', 'rutas', 'configuracion-rutas', 'dominios', 'portal', 'planes', 'claves', 'consumo', 'consumidores',
          'consumidores/invitar'].map(r => `/panel/apis/${id}/${r}`)];
      if (casos[0]) rutas.push(`/panel/soporte/${casos[0].numero}`);
      const errores: string[] = [];
      for (const ruta of rutas) {
        const fuera = await desborde(page, panel + ruta);
        if (fuera) errores.push(`${ruta} → ${fuera}`);
      }
      expect(errores).toEqual([]);
    });

    test('administración: las pantallas no se desbordan', async ({ page }) => {
      test.setTimeout(180_000);
      await entrar(page, panel, 'rodrigo.alvarado@shapi.localhost');
      const casos = (await consultar<{ elementos: { numero: number }[] }>(page, '/api/admin/casos')).elementos;
      const rutas = ['/admin/organizaciones', '/admin/planes', '/admin/pagos', '/admin/casos', '/admin/cuentas', '/admin/estado',
        '/admin/bitacora', '/admin/perfil'];
      if (casos[0]) rutas.push(`/admin/casos/${casos[0].numero}`);
      const errores: string[] = [];
      for (const ruta of rutas) {
        const fuera = await desborde(page, panel + ruta);
        if (fuera) errores.push(`${ruta} → ${fuera}`);
      }
      expect(errores).toEqual([]);
    });

    test('consumidor: el portal con sesión no se desborda', async ({ page }) => {
      test.setTimeout(180_000);
      await entrar(page, portal, 'mariajose@mercaditoantigua.com');
      const errores: string[] = [];
      for (const ruta of ['/', '/documentacion', '/consola', '/planes', '/cuenta/suscripcion', '/cuenta/consumo', '/cuenta/pagos']) {
        const fuera = await desborde(page, portal + ruta);
        if (fuera) errores.push(`${ruta} → ${fuera}`);
      }
      expect(errores).toEqual([]);
    });
  });
}

test.describe('RNF-12 · menú en pantallas angostas', () => {
  test.use({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true });

  for (const [area, host, correo, destino] of [
    ['panel', panel, 'ana.morales@enviosxelaju.com', 'Historial de pagos'],
    ['portal', portal, 'mariajose@mercaditoantigua.com', 'Pagos'],
  ] as const) {
    test(`${area}: la barra lateral se abre como menú y se cierra`, async ({ page }) => {
      test.setTimeout(90_000);
      await entrar(page, host, correo);
      const barra = page.getByRole('navigation').filter({ has: page.getByRole('button', { name: 'Cerrar sesión', exact: true }) });
      await expect(barra).toBeHidden();
      await page.getByRole('button', { name: 'Abrir menú' }).click();
      await expect(barra).toBeVisible();
      await expect(barra.getByRole('button', { name: 'Cerrar sesión' })).toBeInViewport();
      await page.keyboard.press('Escape');
      await expect(barra).toBeHidden();
      await page.getByRole('button', { name: 'Abrir menú' }).click();
      await page.getByRole('button', { name: 'Cerrar menú' }).click();
      await expect(barra).toBeHidden();
      await page.getByRole('button', { name: 'Abrir menú' }).click();
      await barra.getByRole('link', { name: destino, exact: true }).click();
      await expect(barra).toBeHidden();
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    });
  }

  test('desde 1024 px la barra queda fija y no hay botón de menú', async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 768 });
    await entrar(page, panel, 'ana.morales@enviosxelaju.com');
    await expect(page.getByRole('button', { name: 'Abrir menú' })).toBeHidden();
    await expect(page.getByRole('navigation').filter({ has: page.getByRole('button', { name: 'Cerrar sesión', exact: true }) })).toBeVisible();
  });
});
