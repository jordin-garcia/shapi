import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, within, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route, Link } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import type { components } from '@shapi/api/claves';
import { server } from '../../../../test/servidor';
import Claves from '../paginas/A4-3-Claves';
import { claveSesion, type SesionActual } from '../modulos/sesion/useSesion';

const URL_LISTA = 'http://localhost/api/apis/:apiId/claves';
const URL_REVOCAR = `${URL_LISTA}/:claveId/revocar`;
type Pagina = components['schemas']['PaginaClavesDeApi'];
const clave = (id: string, tipo: 'produccion' | 'pruebas', estado: 'activa' | 'rotada' | 'revocada' = 'activa') => ({
  id, tipo, estado, claveEnmascarada: `shp_${tipo === 'produccion' ? 'prod' : 'prueba'}_••••${id}`,
  expiraEn: estado === 'rotada' ? '2026-09-11T15:15:00Z' : null,
  revocadaEn: estado === 'revocada' ? '2026-09-10T14:40:00Z' : null,
});
const datos: Pagina = { elementos: [
  { consumidorId: 'c1', consumidor: 'Mercadito Antigua', plan: 'Comercio', claves: [clave('7c2e', 'produccion'), clave('a19d', 'pruebas')] },
  { consumidorId: 'c2', consumidor: 'Boutique Cayalá', plan: 'Básico', claves: [clave('4b80', 'produccion'), clave('e513', 'produccion', 'rotada'), clave('31c7', 'pruebas')] },
  { consumidorId: 'c3', consumidor: 'Ferretería Zona 11', plan: 'Volumen', claves: [clave('90fb', 'produccion'), clave('5e28', 'pruebas')] },
  { consumidorId: 'c4', consumidor: 'Tienda Sololá', plan: 'Básico', claves: [clave('b3a4', 'produccion'), clave('d6f1', 'pruebas', 'revocada')] },
], total: 4 };
let cache: QueryClient;
function mostrar(rol: SesionActual['rol'] = 'propietario') {
  cache.setQueryData(claveSesion, { nombre: 'Ana Lucía Morales', correo: 'ana@example.test', rol, nombreOrganizacion: 'Envíos Xelajú, S.A.', organizacionId: 'org-1', correoVerificado: true, destino: '/panel/apis' } satisfies SesionActual);
  render(<QueryClientProvider client={cache}><MemoryRouter initialEntries={['/panel/apis/api-1/claves']}>
    <Link to="/panel/apis/api-2/claves">Otra API</Link>
    <Routes><Route path="/panel/apis/:id/claves" element={<Claves />} /></Routes>
  </MemoryRouter></QueryClientProvider>);
}
beforeEach(() => {
  cache = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  server.use(http.get('http://localhost/api/apis', () => HttpResponse.json({ elementos: [{ id: 'api-1', nombre: 'API de Cotización de Envíos' }, { id: 'api-2', nombre: 'API de Precios Agrícolas' }], total: 2 })), http.get(URL_LISTA, () => HttpResponse.json(datos)));
});
afterEach(() => { cleanup(); cache.clear(); });

describe('RF-28 · A4.3 Claves del proveedor', () => {
  it('reproduce consumidores, orden, columnas, estados y fechas del mockup en Guatemala', async () => {
    mostrar();
    expect(await screen.findByRole('heading', { name: 'Claves de sus consumidores' })).toBeDefined();
    expect(screen.getByText('API de Cotización de Envíos')).toBeDefined();
    const tabla = screen.getByRole('table');
    expect(within(tabla).getAllByRole('columnheader').map(x => x.textContent)).toEqual(['Consumidor', 'Plan', 'Tipo', 'Clave', 'Estado', 'Acciones']);
    expect(within(tabla).getAllByRole('row').slice(1).map(x => within(x).getAllByRole('cell')[3].textContent)).toEqual(datos.elementos.flatMap(c => c.claves.map(k => k.claveEnmascarada)));
    for (const c of datos.elementos) expect(within(tabla).getByText(c.consumidor)).toBeDefined();
    expect(screen.getByText('Vigente hasta 11 sep 2026, 09:15')).toBeDefined();
    expect(screen.getByText('El 10 sep 2026, 08:40')).toBeDefined();
    expect(screen.getAllByRole('button', { name: 'Revocar' })).toHaveLength(8);
    expect(screen.queryByRole('button', { name: /Rotar/ })).toBeNull();
    const revocada = screen.getByText('shp_prueba_••••d6f1').closest('tr')!;
    expect(within(revocada).queryByRole('button')).toBeNull();
    expect(screen.getByText(/el consumidor emitió una clave nueva desde su portal/)).toBeDefined();
  });

  it('muestra la variante vacía con sus textos y sin acciones', async () => {
    server.use(http.get(URL_LISTA, () => HttpResponse.json({ elementos: [], total: 0 })));
    mostrar();
    expect(await screen.findByRole('heading', { name: 'Todavía no hay claves emitidas' })).toBeDefined();
    expect(screen.getByText('Cuando un consumidor contrate uno de sus planes en su portal, Shapi emitirá su clave de producción y su clave de pruebas.')).toBeDefined();
    expect(screen.getAllByRole('columnheader')).toHaveLength(6);
    expect(screen.queryByRole('button', { name: 'Revocar' })).toBeNull();
  });

  it('permite al lector consultar, sin columna ni acciones de revocación', async () => {
    mostrar('lector');
    await screen.findByText('Mercadito Antigua');
    expect(screen.queryByRole('columnheader', { name: 'Acciones' })).toBeNull();
    expect(screen.queryByRole('button', { name: /Revocar/ })).toBeNull();
  });

  it.each(['propietario', 'editor'] as const)('revoca como %s con CSRF, avisa y vuelve a consultar la lista', async rol => {
    let revocada = false; let lecturas = 0;
    server.use(http.get(URL_LISTA, () => { lecturas++; return HttpResponse.json(revocada ? { ...datos, elementos: datos.elementos.map(c => ({ ...c, claves: c.claves.map(k => k.id === '7c2e' ? { ...k, estado: 'revocada', revocadaEn: '2026-09-10T14:40:00Z' } : k) })) } : datos); }),
      http.post(URL_REVOCAR, ({ request, params }) => { expect(params.apiId).toBe('api-1'); expect(params.claveId).toBe('7c2e'); expect(request.headers.get('X-Requested-With')).toBe('shapi'); revocada = true; return HttpResponse.json(clave('7c2e', 'produccion', 'revocada')); }));
    mostrar(rol);
    await userEvent.click((await screen.findAllByRole('button', { name: 'Revocar' }))[0]);
    expect(screen.getByRole('heading', { name: 'Revocar clave' })).toBeDefined();
    for (const texto of ['Mercadito Antigua', 'Comercio', 'Producción', 'shp_prod_••••7c2e', 'deja de funcionar en menos de 10 segundos']) expect(screen.getByText(texto)).toBeDefined();
    expect(revocada).toBe(false);
    await userEvent.click(screen.getByRole('button', { name: 'Revocar clave' }));
    expect(await screen.findByText('Clave revocada.')).toBeDefined();
    await waitFor(() => expect(lecturas).toBe(2));
    expect(within(screen.getByText('shp_prod_••••7c2e').closest('tr')!).getByText('Revocada')).toBeDefined();
  });

  it('cancelar la confirmación conserva la lista y no revoca', async () => {
    let llamadas = 0;
    server.use(http.post(URL_REVOCAR, () => { llamadas++; return HttpResponse.json({}); }));
    mostrar();
    await userEvent.click((await screen.findAllByRole('button', { name: 'Revocar' }))[0]);
    await userEvent.click(screen.getByRole('button', { name: 'Cancelar' }));
    expect(screen.getByRole('heading', { name: 'Claves de sus consumidores' })).toBeDefined();
    expect(llamadas).toBe(0);
  });

  it('conserva la confirmación al fallar y permite reintentar', async () => {
    let intentos = 0;
    server.use(http.post(URL_REVOCAR, () => ++intentos === 1 ? new HttpResponse(null, { status: 503 }) : HttpResponse.json(clave('7c2e', 'produccion', 'revocada'))));
    mostrar();
    await userEvent.click((await screen.findAllByRole('button', { name: 'Revocar' }))[0]);
    await userEvent.click(screen.getByRole('button', { name: 'Revocar clave' }));
    expect(await screen.findByRole('alert')).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Revocar clave' })).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }));
    expect(await screen.findByText('Clave revocada.')).toBeDefined();
    expect(intentos).toBe(2);
  });

  it('no permite enviar dos revocaciones mientras la primera sigue pendiente', async () => {
    let terminar!: () => void; let llamadas = 0;
    server.use(http.post(URL_REVOCAR, async () => { llamadas++; await new Promise<void>(resolve => { terminar = resolve; }); return HttpResponse.json(clave('7c2e', 'produccion', 'revocada')); }));
    mostrar();
    await userEvent.click((await screen.findAllByRole('button', { name: 'Revocar' }))[0]);
    await userEvent.click(screen.getByRole('button', { name: 'Revocar clave' }));
    await waitFor(() => expect(llamadas).toBe(1));
    expect((screen.getByRole('button', { name: 'Revocando...' }) as HTMLButtonElement).disabled).toBe(true);
    expect((screen.getByRole('button', { name: 'Cancelar' }) as HTMLButtonElement).disabled).toBe(true);
    terminar();
    await screen.findByText('Clave revocada.');
  });

  it('pagina por consumidor y permite regresar', async () => {
    const paginas: string[] = [];
    server.use(http.get(URL_LISTA, ({ request }) => { const p = new URL(request.url).searchParams.get('pagina')!; paginas.push(p); return HttpResponse.json({ total: 21, elementos: p === '2' ? [{ ...datos.elementos[3], consumidor: 'Último consumidor' }] : datos.elementos }); }));
    mostrar();
    await userEvent.click(await screen.findByRole('button', { name: 'Siguiente' }));
    expect(await screen.findByText('Último consumidor')).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Anterior' }));
    expect(await screen.findByText('Mercadito Antigua')).toBeDefined();
    expect(paginas).toEqual(['1', '2', '1']);
  });

  it('al cambiar de API cierra la confirmación y no conserva sus claves', async () => {
    server.use(http.get(URL_LISTA, ({ params }) => HttpResponse.json(params.apiId === 'api-2' ? { elementos: [], total: 0 } : datos)));
    mostrar();
    await userEvent.click((await screen.findAllByRole('button', { name: 'Revocar' }))[0]);
    await userEvent.click(screen.getByRole('link', { name: 'Otra API' }));
    expect(await screen.findByText('Todavía no hay claves emitidas')).toBeDefined();
    expect(screen.queryByText('shp_prod_••••7c2e')).toBeNull();
    expect(screen.getByText('API de Precios Agrícolas')).toBeDefined();
  });

  it('muestra el error de consulta y vuelve a pedir los datos al reintentar', async () => {
    let intentos = 0;
    server.use(http.get(URL_LISTA, () => ++intentos === 1 ? new HttpResponse(null, { status: 503 }) : HttpResponse.json(datos)));
    mostrar();
    await userEvent.click(await screen.findByRole('button', { name: 'Reintentar' }));
    expect(await screen.findByText('Mercadito Antigua')).toBeDefined();
  });

  it('un 403 muestra el estado sin permiso', async () => {
    server.use(http.get(URL_LISTA, () => HttpResponse.json({ title: 'Sin permiso', status: 403, codigo: 'error' }, { status: 403, headers: { 'Content-Type': 'application/problem+json' } })));
    mostrar();
    expect(await screen.findByText('No tiene permiso para ver esta página')).toBeDefined();
  });
});
