import { expect, type Page } from '@playwright/test';
import https from 'node:https';
import type { IncomingHttpHeaders } from 'node:http';
import { setTimeout as esperar } from 'node:timers/promises';
import { esperarEnlaceCorreo } from './mailpit';

export const panel = 'https://shapi.localhost';
export const contrasena = 'PruebaE2E2026!';
export const contrasenaDemo = 'Shapi2026!demo';
export const sufijo = () => `${Date.now().toString(36)}${Math.random().toString(36).slice(2, 8)}`;

export interface PlanPlataforma { id: string; nombre: string; precio: number; vigenciaDias: number }
export interface SuscripcionPlataforma {
  estado: string; plan: PlanPlataforma; proximaRenovacion: string;
  periodo: { inicio: string; fin: string }; diasRestantesCiclo: number;
  tarjetaEnmascarada: string | null;
}

/** Usa el navegador para resolver *.localhost y conserva la cookie real del actor. */
export async function consultar<T>(page: Page, ruta: string, metodo = 'GET', datos?: unknown): Promise<T> {
  const ejecutar = () => page.evaluate(async ({ ruta, metodo, datos }) => {
    const respuesta = await fetch(ruta, {
      method: metodo,
      headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'shapi' },
      body: datos === undefined ? undefined : JSON.stringify(datos),
    });
    return { estado: respuesta.status, texto: await respuesta.text(), espera: respuesta.headers.get('Retry-After') };
  }, { ruta, metodo, datos });
  let resultado = await ejecutar();
  if (resultado.estado === 429 && ruta.startsWith('/api/auth/')) {
    await esperar((Number(resultado.espera) || 60) * 1000 + 100);
    resultado = await ejecutar();
  }
  // No incluir cuerpos: algunos endpoints devuelven claves completas o tokens de sesión.
  expect(resultado.estado, `${metodo} ${ruta}`).toBeGreaterThanOrEqual(200);
  expect(resultado.estado, `${metodo} ${ruta}`).toBeLessThan(300);
  return (resultado.texto ? JSON.parse(resultado.texto) : undefined) as T;
}

/** Respeta el límite real de 10 credenciales/min/IP compartido por toda la suite. */
export async function autenticarPorInterfaz(page: Page, ruta: string, accion: () => Promise<unknown>) {
  for (let intento = 0; intento < 2; intento++) {
    const respuestaPendiente = page.waitForResponse(r => new URL(r.url()).pathname === ruta && r.request().method() === 'POST');
    await accion();
    const respuesta = await respuestaPendiente;
    if (respuesta.status() !== 429) return;
    expect(intento, `Se agotó el reintento de ${ruta}`).toBe(0);
    await esperar((Number(respuesta.headers()['retry-after']) || 60) * 1000 + 100);
  }
}

export async function entrar(page: Page, host: string, correo: string, clave = contrasenaDemo) {
  await page.goto(`${host}/entrar`);
  await page.getByLabel('Correo electrónico').fill(correo);
  await page.getByLabel('Contraseña', { exact: true }).fill(clave);
  await autenticarPorInterfaz(page, '/api/auth/entrar', () => page.getByRole('button', { name: 'Entrar', exact: true }).click());
  await expect(page).not.toHaveURL(/\/entrar(?:\?.*)?$/, { timeout: 30_000 });
}

export async function registrarProveedor(page: Page) {
  const id = sufijo();
  const correo = `proveedor-${id}@ejemplo.com`;
  const organizacion = `Organización E2E ${id}`;
  await page.goto(`${panel}/registro`);
  await page.getByLabel('Nombre', { exact: true }).fill('Proveedor E2E');
  await page.getByLabel('Correo electrónico').fill(correo);
  await page.getByLabel('Nombre de la organización').fill(organizacion);
  await page.getByLabel('Contraseña', { exact: true }).fill(contrasena);
  await autenticarPorInterfaz(page, '/api/auth/registro', () => page.getByRole('button', { name: 'Crear cuenta' }).click());
  await expect(page.getByRole('heading', { name: 'Revise su correo' })).toBeVisible({ timeout: 30_000 });
  const enlace = await esperarEnlaceCorreo(correo, { contiene: '/verificar-correo?token=' });
  await autenticarPorInterfaz(page, '/api/auth/verificar-correo', () => page.goto(enlace));
  await expect(page).toHaveURL(/\/panel\/apis$/, { timeout: 30_000 });
  return { correo, organizacion, id };
}

export async function registrarConsumidor(page: Page, host: string) {
  const correo = `consumidor-${sufijo()}@ejemplo.com`;
  await page.goto(`${host}/registro`);
  await page.getByLabel('Nombre', { exact: true }).fill('Consumidora E2E');
  await page.getByLabel('Correo electrónico').fill(correo);
  await page.getByLabel('Nombre de la empresa').fill('Comercio E2E');
  await page.getByLabel('Contraseña', { exact: true }).fill(contrasena);
  await autenticarPorInterfaz(page, '/api/portal/auth/registro', () => page.getByRole('button', { name: 'Crear cuenta' }).click());
  await expect(page).toHaveURL(/\/verificar-correo\?correo=/, { timeout: 30_000 });
  const enlace = await esperarEnlaceCorreo(correo, { contiene: '/verificar-correo?token=' });
  await autenticarPorInterfaz(page, '/api/portal/auth/verificar-correo', () => page.goto(enlace));
  await expect(page).toHaveURL(/\/planes$/, { timeout: 30_000 });
  return correo;
}

export async function llenarTarjeta(page: Page, numero = '4242424242424242', portal = false) {
  await page.getByLabel('Número de tarjeta', { exact: true }).fill(numero);
  await page.getByLabel(portal ? 'Nombre del titular' : 'Titular de la tarjeta', { exact: true }).fill('Persona E2E');
  if (portal) await page.getByLabel('Vencimiento', { exact: true }).fill('12 / 35');
  else {
    await page.getByLabel('Mes de vencimiento', { exact: true }).fill('12');
    await page.getByLabel('Año de vencimiento', { exact: true }).fill('2035');
  }
  await page.getByLabel('Código de seguridad', { exact: true }).fill('123');
}

export async function contratarPlataforma(page: Page, numero = '4242424242424242') {
  const planes = await consultar<PlanPlataforma[]>(page, '/api/planes-plataforma');
  const lanzamiento = planes.find(p => p.nombre === 'Lanzamiento')!;
  expect(lanzamiento).toBeTruthy();
  await page.goto(`${panel}/panel/suscripcion/contratar/${lanzamiento.id}`);
  await llenarTarjeta(page, numero);
  await page.getByRole('button', { name: /Pagar .*activar el plan/ }).click();
  await expect(page.getByRole('heading', { name: 'Plan activado' })).toBeVisible({ timeout: 30_000 });
  return lanzamiento;
}

/** Petición HTTP real a la compuerta, sin depender de CORS ni del DNS de Node. */
export async function llamarCompuerta(host: string, clave: string, ruta = '/tarifas') {
  return new Promise<{ estado: number; cabeceras: IncomingHttpHeaders; cuerpo: { error?: { codigo: string }; [campo: string]: unknown } }>((resolve, reject) => {
    const peticion = https.get(new URL(ruta, host), {
      rejectUnauthorized: false,
      maxVersion: 'TLSv1.2',
      lookup: (_nombre, opciones, callback) => {
        if (opciones.all) callback(null, [{ address: '127.0.0.1', family: 4 }]);
        else callback(null, '127.0.0.1', 4);
      },
      headers: { 'X-API-Key': clave },
      timeout: 10_000,
    }, respuesta => {
      const partes: Buffer[] = [];
      respuesta.on('data', (parte: Buffer) => partes.push(parte));
      respuesta.on('end', () => {
        try { resolve({ estado: respuesta.statusCode ?? 0, cabeceras: respuesta.headers, cuerpo: JSON.parse(Buffer.concat(partes).toString()) }); }
        catch { reject(new Error('La compuerta no devolvió JSON.')); }
      });
    });
    peticion.on('timeout', () => peticion.destroy(new Error('La compuerta no respondió en 10 s.')));
    peticion.on('error', reject);
  });
}

export async function esperarEstadoCompuerta(host: string, clave: string, estado: number, ruta = '/tarifas') {
  await expect.poll(async () => {
    const respuesta = await llamarCompuerta(host, clave, ruta);
    return respuesta.estado === estado ? String(estado) : `${respuesta.estado} ${respuesta.cuerpo.error?.codigo ?? ''}`;
  }, { timeout: 10_000 }).toBe(String(estado));
}

export const especificacion = JSON.stringify({
  openapi: '3.0.3', info: { title: 'Envíos E2E', version: '1.0.0' },
  paths: Object.fromEntries(['/tarifas', '/rastreo'].map(ruta => [ruta, {
    get: { summary: ruta, responses: { '200': { description: 'Respuesta del origen' } } },
  }])),
});
