import { test, expect } from '@playwright/test';
import {
  consultar, contratarPlataforma, contrasena, contrasenaDemo, entrar, especificacion,
  esperarEstadoCompuerta, llenarTarjeta, llamarCompuerta, panel, registrarConsumidor,
  registrarProveedor, type PlanPlataforma, type SuscripcionPlataforma,
} from '../soporte/flujos';

// RNF-15, RF-22, RF-23, RF-25 y RF-44. Este proyecto depende de todos los demás:
// el reloj de negocio es global; la vigencia de las sesiones usa hora real (JG-22).
test('RNF-15 · prorrateo, tarjeta 0341, gracia, suspensión, 403 y pago que reactiva', async ({ browser, page }) => {
  test.setTimeout(300_000);
  const proveedor = await registrarProveedor(page);
  const subdominio = `reloj-${proveedor.id}`;
  const host = `https://${subdominio}.shapi.localhost`;
  const compuerta = `https://${subdominio}.api.shapi.localhost`;
  const adminContexto = await browser.newContext({ ignoreHTTPSErrors: true });
  const consumidorContexto = await browser.newContext({ ignoreHTTPSErrors: true });
  const admin = await adminContexto.newPage();
  const consumidor = await consumidorContexto.newPage();
  const suscripcion = () => consultar<SuscripcionPlataforma>(page, '/api/suscripcion');
  try {
    await test.step('Preparar una API propia y un consumidor cuyo plan dure todo el escenario', async () => {
      await contratarPlataforma(page, '4000000000000341');
      const api = await consultar<{ id: string }>(page, '/api/apis', 'POST', {
        nombre: `API del reloj ${proveedor.id}`, urlOrigen: 'http://origen-envios:8080', subdominio,
      });
      // Preparación por los endpoints reales: sin SQL ni escritura directa en Redis.
      const estadoCarga = await page.evaluate(async ({ apiId, especificacion }) => {
        const datos = new FormData();
        datos.set('archivo', new File([especificacion], 'reloj.json', { type: 'application/json' }));
        return (await fetch(`/api/apis/${apiId}/especificacion`, {
          method: 'PUT', headers: { 'X-Requested-With': 'shapi' }, body: datos,
        })).status;
      }, { apiId: api.id, especificacion });
      expect(estadoCarga).toBe(200);
      const rutas = await consultar<{ elementos: { id: string; patron: string }[] }>(page, `/api/apis/${api.id}/rutas`);
      await consultar(page, `/api/apis/${api.id}/rutas/exposicion`, 'PUT', rutas.elementos.map(r => ({ rutaId: r.id, expuesta: r.patron === '/tarifas' })));
      const plan = await consultar<{ id: string }>(page, `/api/apis/${api.id}/planes`, 'POST', {
        nombre: 'Anual E2E', descripcion: 'Consumidor activo durante la prueba del reloj.', precio: 0,
        esGratuito: true, vigenciaDias: 365, cuotaLlamadas: 1000, limiteMinuto: 100,
      });
      expect(plan.id).toBeTruthy();
      await consultar(page, `/api/apis/${api.id}/publicar`, 'POST');
      await registrarConsumidor(consumidor, host);
      await consumidor.getByRole('button', { name: 'Contratar', exact: true }).click();
      await expect(consumidor.getByRole('heading', { name: 'Plan contratado', exact: true })).toBeVisible({ timeout: 30_000 });
    });
    const clave = (await consumidor.locator('.font-mono').allTextContents()).find(c => c.startsWith('shp_prod_')) ?? '';
    expect(Boolean(clave)).toBe(true);
    await esperarEstadoCompuerta(compuerta, clave, 200);
    await entrar(admin, panel, 'rodrigo.alvarado@shapi.localhost');

    async function adelantar(dias: number) {
      // Acceso por la API pública para mantener explícitos los actores de cada avance.
      await consultar(admin, '/api/auth/entrar', 'POST', { correo: 'rodrigo.alvarado@shapi.localhost', contrasena: contrasenaDemo });
      await consultar(admin, '/api/admin/demo/reloj', 'POST', { adelantarDias: dias });
      await consultar(page, '/api/auth/entrar', 'POST', { correo: proveedor.correo, contrasena });
    }

    await test.step('RF-25 · subir Lanzamiento a Producto con el importe prorrateado de un ciclo parcial', async () => {
      await adelantar(5);
      const antes = await suscripcion();
      const planes = await consultar<PlanPlataforma[]>(page, '/api/planes-plataforma');
      const destino = planes.find(p => p.nombre === 'Producto')!;
      expect(destino).toBeTruthy();
      const dias = antes.diasRestantesCiclo;
      expect(dias).toBeGreaterThan(0);
      expect(dias).toBeLessThan(antes.plan.vigenciaDias);
      const redondear = (valor: number) => Math.round((valor + Number.EPSILON) * 100) / 100;
      const credito = redondear(antes.plan.precio * dias / antes.plan.vigenciaDias);
      const cargo = redondear(destino.precio * dias / destino.vigenciaDias);
      const aPagar = redondear(cargo - credito);
      await page.goto(`${panel}/panel/suscripcion/cambiar/${destino.id}`);
      await expect(page.getByRole('heading', { name: 'Diferencia prorrateada' })).toBeVisible();
      const respuestaCambio = page.waitForResponse(r => r.url().endsWith('/api/suscripcion/cambiar') && r.request().method() === 'POST');
      await page.getByRole('button', { name: /Pagar .*cambiar de plan/ }).click();
      const respuesta = await respuestaCambio;
      expect(respuesta.status()).toBe(200);
      const resultado = await respuesta.json();
      expect(resultado.credito).toBe(credito);
      expect(resultado.cargo).toBe(cargo);
      expect(resultado.aPagar).toBe(aPagar);
      await expect(page).toHaveURL(/\/panel\/suscripcion$/);
      const despues = await suscripcion();
      expect(despues.plan.nombre).toBe('Producto');
      expect(despues.proximaRenovacion).toBe(antes.proximaRenovacion);
      expect(despues.tarjetaEnmascarada).toContain('0341');
    });

    await test.step('RF-22 · completar 30 días y rechazar la renovación con 0341 deja siete días de gracia', async () => {
      await adelantar(25);
      await expect.poll(async () => (await suscripcion()).estado, { timeout: 60_000 }).toBe('en_gracia');
      await page.goto(`${panel}/panel/suscripcion`);
      await expect(page.getByText('En gracia', { exact: true })).toBeVisible();
      await esperarEstadoCompuerta(compuerta, clave, 200);
    });

    await test.step('RF-23 · finalizar la gracia suspende la plataforma y detiene la compuerta con 403', async () => {
      await adelantar(7);
      await expect.poll(async () => (await suscripcion()).estado, { timeout: 60_000 }).toBe('suspendida');
      await page.goto(`${panel}/panel/suscripcion`);
      await expect(page.getByRole('heading', { name: 'Su tráfico está detenido', exact: true })).toBeVisible();
      await esperarEstadoCompuerta(compuerta, clave, 403);
      expect((await llamarCompuerta(compuerta, clave)).cuerpo.error?.codigo).toBe('api_no_disponible');
    });

    await test.step('RF-19, RF-23 · pagar con otra tarjeta reactiva la misma clave sin volver a emitirla', async () => {
      await page.getByRole('button', { name: 'Pagar con otra tarjeta', exact: true }).click();
      await llenarTarjeta(page);
      await page.getByRole('button', { name: 'Pagar y reactivar', exact: true }).click();
      await expect(page.getByText('Activa', { exact: true })).toBeVisible({ timeout: 30_000 });
      expect((await suscripcion()).estado).toBe('activa');
      await esperarEstadoCompuerta(compuerta, clave, 200);
    });
  } finally {
    await adminContexto.close();
    await consumidorContexto.close();
  }
});
