import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createMemoryRouter, type RouterProviderProps } from 'react-router';
import type { components } from '@shapi/api/claves';
import App from '../App';
import { crearRutas } from '../rutas';
import { server } from '../../../../test/servidor';

const API = 'http://localhost/api/portal';
const configuracion = {
  nombrePortal: 'Envíos Xelajú', colorPrincipal: '#B8322A', urlLogo: null,
  bienvenida: 'Cotice y genere guías.', nombreApi: 'API de Cotización de Envíos',
  descripcionApi: 'Cotice envíos.', hostPortal: 'envios.shapi.localhost',
  hostApi: 'envios.api.shapi.localhost', nombreOrganizacion: 'Envíos Xelajú, S.A.',
};
const sesion = {
  consumidor: { nombre: 'Ana Pérez', nombreEmpresa: 'Mercadito Antigua' },
  correoVerificado: true, destino: '/cuenta/suscripcion',
};
const suscripcion = {
  plan: { id: 'plan-comercio', nombre: 'Comercio', precio: 450, moneda: 'GTQ' },
  estado: 'activa',
  periodo: { inicio: '2026-09-01T06:00:00Z', fin: '2026-10-01T06:00:00Z' },
  proximaRenovacion: '2026-10-01T06:00:00Z', tarjetaEnmascarada: 'Mastercard •••• 3057',
  cuotaLlamadas: 50_000, limiteMinuto: 120,
};
let claves: components['schemas']['Clave'][] = [
  { id: 'prod-1', tipo: 'produccion', claveEnmascarada: 'shp_prod_••••7c2e', estado: 'activa', expiraEn: null, revocadaEn: null },
  { id: 'prueba-1', tipo: 'pruebas', claveEnmascarada: 'shp_prueba_••••a19d', estado: 'activa', expiraEn: null, revocadaEn: null },
];
let cliente: QueryClient;

function montar() {
  const enrutador = createMemoryRouter(crearRutas(), { initialEntries: ['/cuenta/suscripcion'] });
  const vista = render(
    <QueryClientProvider client={cliente}>
      <App enrutador={enrutador as RouterProviderProps['router']} />
    </QueryClientProvider>,
  );
  return { ...vista, enrutador };
}

function responderBase() {
  server.use(
    http.get(`${API}/configuracion`, () => HttpResponse.json(configuracion)),
    http.get(`${API}/auth/sesion`, () => HttpResponse.json(sesion)),
    http.get(`${API}/suscripcion`, () => HttpResponse.json(suscripcion)),
    http.get(`${API}/claves`, () => HttpResponse.json({ elementos: claves })),
  );
}

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  claves = [
    { id: 'prod-1', tipo: 'produccion', claveEnmascarada: 'shp_prod_••••7c2e', estado: 'activa', expiraEn: null, revocadaEn: null },
    { id: 'prueba-1', tipo: 'pruebas', claveEnmascarada: 'shp_prueba_••••a19d', estado: 'activa', expiraEn: null, revocadaEn: null },
  ];
  responderBase();
});

afterEach(() => {
  cleanup();
  cliente.clear();
  vi.restoreAllMocks();
});

describe('DC-11 · RF-22, RF-23 y RF-26 a RF-28', () => {
  it('B2.3 muestra la suscripción vigente, la tarjeta y solamente las claves enmascaradas', async () => {
    montar();

    expect(await screen.findByRole('heading', { name: 'Suscripción y claves' })).toBeDefined();
    expect(screen.getByText('Comercio')).toBeDefined();
    expect(screen.getAllByText('Activa').length).toBeGreaterThan(0);
    expect(screen.getByText('1 sep 2026 – 30 sep 2026')).toBeDefined();
    expect(screen.getByText('1 de octubre de 2026')).toBeDefined();
    expect(screen.getByText('Mastercard •••• 3057')).toBeDefined();
    expect(screen.getByText('shp_prod_••••7c2e')).toBeDefined();
    expect(screen.queryByText(/9Hk2wQ7r/)).toBeNull();
    expect(screen.getByRole('link', { name: 'Cambiar de plan' }).getAttribute('href')).toBe('/cuenta/cambiar-plan/plan-comercio');
  });

  it('B2.3 muestra la gracia con los días restantes y conserva activas las claves', async () => {
    vi.spyOn(Date, 'now').mockReturnValue(new Date('2026-10-03T12:00:00Z').getTime());
    server.use(http.get(`${API}/suscripcion`, () => HttpResponse.json({ ...suscripcion, estado: 'en_gracia' })));

    montar();

    expect(await screen.findByRole('heading', { name: 'Quedan 5 días antes de que sus claves dejen de responder' })).toBeDefined();
    expect(screen.getByText(/cobro de la renovación fue rechazado el 1 de octubre de 2026/i)).toBeDefined();
    expect(screen.getByRole('button', { name: 'Pagar con otra tarjeta' })).toBeDefined();
    expect(screen.getAllByText('Activa')).toHaveLength(2);
  });

  it('B2.3 muestra que la API no está disponible ante api_no_disponible', async () => {
    server.use(http.get(`${API}/suscripcion`, () => HttpResponse.json(
      { title: 'API no disponible', status: 403, codigo: 'api_no_disponible' },
      { status: 403, headers: { 'Content-Type': 'application/problem+json' } },
    )));

    montar();

    expect(await screen.findByRole('heading', { name: 'La API no está disponible temporalmente' })).toBeDefined();
    expect(screen.getByText(/no es un problema de sus claves ni de su suscripción/i)).toBeDefined();
  });

  it('B2.3 muestra el estado vacío cuando no hay suscripción', async () => {
    server.use(http.get(`${API}/suscripcion`, () => new HttpResponse(null, { status: 404 })));

    montar();

    expect(await screen.findByRole('heading', { name: 'Todavía no tiene una suscripción' })).toBeDefined();
    expect(screen.getByRole('link', { name: 'Ver los planes' }).getAttribute('href')).toBe('/planes');
    expect(screen.queryByText('shp_prod_••••7c2e')).toBeNull();
  });

  it('RF-27 rota una clave y muestra la nueva completa solamente hasta que se confirma que fue guardada', async () => {
    server.use(http.post(`${API}/claves/prod-1/rotar`, () => HttpResponse.json({
      id: 'prod-2', tipo: 'produccion', clave: 'shp_prod_9Hk2wQ7rTz4Lb8Nc1Vx6Pm3Ds0',
      claveEnmascarada: 'shp_prod_••••Ds0', estado: 'activa',
      anterior: { ...claves[0], estado: 'rotada', expiraEn: '2026-10-12T16:42:00Z' },
    })));
    montar();
    const produccion = await screen.findByTestId('clave-produccion-activa');

    await userEvent.click(within(produccion).getByRole('button', { name: 'Rotar' }));
    expect(screen.getByRole('heading', { name: 'Rotar clave' })).toBeDefined();
    expect(screen.getByText(/sigue funcionando 24 horas/i)).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Rotar clave' }));

    expect(await screen.findByRole('heading', { name: 'Su clave nueva de producción' })).toBeDefined();
    expect(screen.getByText('shp_prod_9Hk2wQ7rTz4Lb8Nc1Vx6Pm3Ds0')).toBeDefined();
    expect(screen.getByText(/no se volverá a mostrar/i)).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Ya la guardé' }));
    await waitFor(() => expect(screen.queryByText('shp_prod_9Hk2wQ7rTz4Lb8Nc1Vx6Pm3Ds0')).toBeNull());
  });

  it('RF-28 revoca una clave y permite emitir otra del mismo tipo', async () => {
    server.use(
      http.post(`${API}/claves/prueba-1/revocar`, () => {
        claves = [{ ...claves[0] }, { ...claves[1], estado: 'revocada', revocadaEn: '2026-10-11T16:42:00Z' }];
        return HttpResponse.json(claves[1]);
      }),
      http.post(`${API}/claves/emitir`, async ({ request }) => {
        expect(await request.json()).toEqual({ tipo: 'pruebas' });
        return HttpResponse.json({
          id: 'prueba-2', tipo: 'pruebas', clave: 'shp_prueba_Kp7mR2xV9qL4nT6cB1zF8sA0dE',
          claveEnmascarada: 'shp_prueba_••••A0dE', estado: 'activa',
        }, { status: 201 });
      }),
    );
    montar();
    const pruebas = await screen.findByTestId('clave-pruebas-activa');

    await userEvent.click(within(pruebas).getByRole('button', { name: 'Revocar' }));
    expect(screen.getByRole('heading', { name: 'Revocar clave' })).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Revocar clave' }));

    const revocada = await screen.findByTestId('clave-pruebas-revocada');
    await userEvent.click(within(revocada).getByRole('button', { name: 'Emitir una clave nueva' }));
    expect(await screen.findByRole('heading', { name: 'Su clave nueva de pruebas' })).toBeDefined();
    expect(screen.getByText('shp_prueba_Kp7mR2xV9qL4nT6cB1zF8sA0dE')).toBeDefined();
  });
});
