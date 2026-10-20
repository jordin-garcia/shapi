import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse, delay } from 'msw';
import type { components } from '@shapi/api/consumo';
import { server } from '../../../../test/servidor';
import Consumidores from '../paginas/B1-2-Consumidores';

const URL_API = 'http://localhost/api/apis/:id/consumidores';
const datos: components['schemas']['ConsumoConsumidores'] = {
  apiId: 'api-1', apiNombre: 'API de Cotización de Envíos', total: 4, activas: 4,
  llamadas: 106632, cuota: 560000, facturacion: 1948, moneda: 'GTQ',
  elementos: [
    ['Mercadito Antigua', 'Comercio', '2026-09-01', '2026-09-30', 31480, 50000, 62.96, 450],
    ['Boutique Cayalá', 'Básico', '2026-08-28', '2026-09-26', 4116, 5000, 82.32, 149],
    ['Ferretería Zona 11', 'Volumen', '2026-09-05', '2026-10-04', 69600, 500000, 13.92, 1200],
    ['Tienda Sololá', 'Básico', '2026-09-09', '2026-10-08', 1436, 5000, 28.72, 149],
  ].map(([consumidor, plan, inicio, fin, llamadas, cuota, porcentaje, facturacion], i) => ({
    suscripcionId: `s-${i}`, consumidorId: `c-${i}`, consumidor: String(consumidor), plan: String(plan),
    inicio: String(inicio), fin: String(fin), llamadas: Number(llamadas), cuota: Number(cuota),
    porcentaje: Number(porcentaje), facturacion: Number(facturacion), ultimoCobro: 'autorizado', estadoSuscripcion: 'activa',
  })),
};
let cache: QueryClient;
let rol = 'propietario';
function mostrar() {
  render(<QueryClientProvider client={cache}><MemoryRouter initialEntries={['/panel/apis/api-1/consumidores']}><Routes>
    <Route path="/panel/apis/:id/consumidores" element={<Consumidores />} />
    <Route path="/panel/apis/:id/consumidores/invitar" element={<h1>Invitaciones</h1>} />
    <Route path="/panel/apis/:id/otra" element={<Consumidores />} />
  </Routes></MemoryRouter></QueryClientProvider>);
}
beforeEach(() => {
  rol = 'propietario';
  cache = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  server.use(http.get('http://localhost/api/auth/sesion', () => HttpResponse.json({
    usuario: { nombre: 'Ana', correo: 'ana@envios.test' }, organizacion: { id: 'org-1', nombre: 'Envíos' },
    rol, correoVerificado: true, destino: '/panel/apis',
  })));
});
afterEach(() => { cleanup(); cache.clear(); });

describe('RF-36 · B1.2 Consumo y facturación por consumidor', () => {
  it('reproduce encabezado, columnas, filas, porcentajes y totales del mockup', async () => {
    server.use(http.get(URL_API, () => HttpResponse.json(datos)));
    mostrar();
    expect(await screen.findByRole('heading', { name: 'Consumo y facturación por consumidor' })).toBeDefined();
    expect(screen.getByText(datos.apiNombre)).toBeDefined();
    expect(screen.getByText('Cada consumidor con el consumo de su ciclo en curso y lo que se le facturó por su plan. Consolidado por suscripción, ruta y día.')).toBeDefined();
    const tabla = screen.getByRole('table');
    expect(within(tabla).getAllByRole('columnheader').map(x => x.textContent)).toEqual(['Consumidor', 'Plan', 'Ciclo en curso', 'Consumo del ciclo', 'Uso', 'Facturación', 'Cobro']);
    const filas = within(tabla).getAllByRole('row').slice(1);
    expect(filas).toHaveLength(4);
    const esperadas = [
      ['Mercadito Antigua', 'Comercio', '1 sep – 30 sep 2026', '31,480 de 50,000', '63 %', 'Q 450.00', 'Pagado'],
      ['Boutique Cayalá', 'Básico', '28 ago – 26 sep 2026', '4,116 de 5,000', '82 %', 'Q 149.00', 'Pagado'],
      ['Ferretería Zona 11', 'Volumen', '5 sep – 4 oct 2026', '69,600 de 500,000', '14 %', 'Q 1,200.00', 'Pagado'],
      ['Tienda Sololá', 'Básico', '9 sep – 8 oct 2026', '1,436 de 5,000', '29 %', 'Q 149.00', 'Pagado'],
    ];
    filas.forEach((fila, i) => expect(within(fila).getAllByRole('cell').map(x => x.textContent)).toEqual(esperadas[i]));
    expect(screen.getByText('4 consumidores con suscripción activa')).toBeDefined();
    expect(screen.getByText('Facturación de los ciclos en curso')).toBeDefined();
    expect(screen.getByText('Q 1,948.00')).toBeDefined();
    await userEvent.click(screen.getByRole('link', { name: 'Invitar consumidores' }));
    expect(await screen.findByRole('heading', { name: 'Invitaciones' })).toBeDefined();
  });

  it('muestra la variante vacía con columnas y enlace, sin totales ficticios', async () => {
    server.use(http.get(URL_API, () => HttpResponse.json({ ...datos, elementos: [], total: 0, activas: 0, llamadas: 0, cuota: 0, facturacion: 0 })));
    mostrar();
    expect(await screen.findByRole('heading', { name: 'Todavía no tiene consumidores' })).toBeDefined();
    expect(screen.getByText('Cuando alguien contrate un plan de esta API desde su portal, aquí aparecerá con el consumo de su ciclo y lo que se le facturó.')).toBeDefined();
    expect(screen.getAllByRole('columnheader')).toHaveLength(7);
    expect(screen.queryByText('Facturación de los ciclos en curso')).toBeNull();
    expect(screen.getByRole('link', { name: 'Invitar consumidores' }).getAttribute('href')).toBe('/panel/apis/api-1/consumidores/invitar');
  });

  it('distingue rechazo, ausencia de cobros y suscripciones no activas', async () => {
    server.use(http.get(URL_API, () => HttpResponse.json({ ...datos, total: 2, activas: 0, elementos: [
      { ...datos.elementos[0], ultimoCobro: 'rechazado', estadoSuscripcion: 'en_gracia' },
      { ...datos.elementos[1], ultimoCobro: null, estadoSuscripcion: 'suspendida', facturacion: 0, llamadas: 0, porcentaje: 0 },
    ] })));
    mostrar();
    expect(await screen.findByText('Rechazado')).toBeDefined();
    expect(screen.getByText('Sin cobros')).toBeDefined();
    expect(screen.getByText('2 consumidores con suscripción vigente')).toBeDefined();
    expect(screen.getByText('0 %')).toBeDefined();
  });

  it('oculta la invitación al lector que sí puede consultar el consumo', async () => {
    rol = 'lector';
    server.use(http.get(URL_API, () => HttpResponse.json(datos)));
    mostrar();
    await screen.findByText('Mercadito Antigua');
    expect(screen.queryByRole('link', { name: 'Invitar consumidores' })).toBeNull();
  });

  it('muestra carga y permite reintentar un fallo sin confundirlo con una lista vacía', async () => {
    let falla = true;
    server.use(http.get(URL_API, async () => { await delay(50); return falla ? new HttpResponse(null, { status: 500 }) : HttpResponse.json(datos); }));
    mostrar();
    expect(await screen.findByRole('status', { name: 'Cargando' })).toBeDefined();
    expect(await screen.findByText('No se pudo cargar el consumo de los consumidores.')).toBeDefined();
    expect(screen.queryByText('Todavía no tiene consumidores')).toBeNull();
    falla = false;
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }));
    expect(await screen.findByText('Mercadito Antigua')).toBeDefined();
  });
});
