import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, type RouterProviderProps } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import App from '../App';
import { crearRutas } from '../rutas';
import { server } from '../../../../test/servidor';

const API = 'http://localhost/api/portal';
const configuracion = {
  nombrePortal: 'Envíos Xelajú', colorPrincipal: '#B8322A', urlLogo: null,
  bienvenida: 'Cotice y genere guías.', nombreApi: 'API de Cotización de Envíos',
  descripcionApi: 'Cotice envíos.', hostPortal: 'envios.shapi.localhost', hostApi: 'envios.api.shapi.localhost',
  nombreOrganizacion: 'Envíos Xelajú, S.A.',
};
const planes = [
  { id: 'basico', apiId: 'api-1', nombre: 'Básico', descripcion: 'Para tiendas que empiezan a vender en línea.', precio: 149, moneda: 'GTQ', esGratuito: false, vigenciaDias: 30, cuotaLlamadas: 5000, limiteMinuto: 30, activo: true },
  { id: 'comercio', apiId: 'api-1', nombre: 'Comercio', descripcion: 'Para tiendas con envíos todos los días.', precio: 450, moneda: 'GTQ', esGratuito: false, vigenciaDias: 30, cuotaLlamadas: 50000, limiteMinuto: 120, activo: true },
  { id: 'gratis', apiId: 'api-1', nombre: 'Prueba gratuita', descripcion: 'Para probar la API.', precio: 0, moneda: 'GTQ', esGratuito: true, vigenciaDias: 30, cuotaLlamadas: 1000, limiteMinuto: 10, activo: true },
];
const contratacion = {
  suscripcion: { id: 'susc-1', apiId: 'api-1', planId: 'comercio', nombrePlan: 'Comercio', estado: 'activa', inicio: '2026-10-07T06:00:00Z', fin: '2026-11-06T06:00:00Z', proximaRenovacion: '2026-11-06T06:00:00Z' },
  claves: [
    { id: 'clave-1', tipo: 'produccion', clave: 'shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e', claveEnmascarada: 'shp_prod_••••7c2e', estado: 'activa' },
    { id: 'clave-2', tipo: 'pruebas', clave: 'shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d', claveEnmascarada: 'shp_prueba_••••0a19d', estado: 'activa' },
  ],
};

let cliente: QueryClient;

function montar(ruta: string) {
  const enrutador = createMemoryRouter(crearRutas(), { initialEntries: [ruta] });
  const vista = render(<QueryClientProvider client={cliente}><App enrutador={enrutador as RouterProviderProps['router']} /></QueryClientProvider>);
  return { ...vista, enrutador };
}

function responderPlanes(datos = planes) {
  server.use(
    http.get(`${API}/configuracion`, () => HttpResponse.json(configuracion)),
    http.get(`${API}/documentacion`, () => HttpResponse.json({ rutas: [] })),
    http.get(`${API}/planes`, () => HttpResponse.json(datos)),
    http.get(`${API}/auth/sesion`, () => new HttpResponse(null, { status: 401 })),
  );
}

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  responderPlanes();
});

afterEach(() => {
  cleanup();
  cliente.clear();
});

describe('RF-19, RF-20 y RF-26 · planes y contratación del portal', () => {
  it('A5.4 lista los planes activos y A5.0 reutiliza la sección', async () => {
    montar('/planes');
    expect(await screen.findByRole('heading', { name: 'Planes de la API' })).toBeDefined();
    expect(screen.getByText('Básico')).toBeDefined();
    expect(screen.getByText('Q 149.00')).toBeDefined();
    expect(screen.getByText('50,000 llamadas al mes')).toBeDefined();
    cleanup();
    montar('/');
    expect(await screen.findByRole('heading', { name: 'Planes de la API' })).toBeDefined();
    expect(screen.getByText('Comercio')).toBeDefined();
  });

  it('A5.4 muestra el estado vacío cuando no hay planes activos', async () => {
    responderPlanes([]);
    montar('/planes');
    expect(await screen.findByRole('heading', { name: 'Todavía no hay planes para esta API' })).toBeDefined();
    expect(screen.getByText('Cuando Envíos Xelajú publique sus planes, podrá contratarlos desde aquí.')).toBeDefined();
  });

  it('A5.4 lleva a entrar sin sesión y avisa si el correo no está verificado', async () => {
    server.use(http.get(`${API}/auth/sesion`, () => new HttpResponse(null, { status: 401 })));
    const sinSesion = montar('/planes');
    await userEvent.click((await screen.findAllByRole('button', { name: 'Contratar' }))[0]);
    await waitFor(() => expect(sinSesion.enrutador.state.location.pathname).toBe('/entrar'));

    cleanup();
    cliente.clear();
    responderPlanes();
    server.use(http.get(`${API}/auth/sesion`, () => HttpResponse.json({ consumidor: { nombre: 'Ana', nombreEmpresa: 'Tienda' }, correoVerificado: false, destino: '/planes' })));
    montar('/planes');
    await userEvent.click((await screen.findAllByRole('button', { name: 'Contratar' }))[0]);
    expect(await screen.findByText(/verifique su correo/i)).toBeDefined();
  });

  it('A5.6 procesa el pago y A5.4b muestra las claves completas una sola vez', async () => {
    server.use(
      http.get(`${API}/auth/sesion`, () => HttpResponse.json({ consumidor: { nombre: 'Ana', nombreEmpresa: 'Tienda' }, correoVerificado: true, destino: '/planes' })),
      http.post(`${API}/suscripciones`, async ({ request }) => {
        const cuerpo = await request.json() as { planId: string; tarjeta?: { numero: string } };
        expect(cuerpo.planId).toBe('comercio');
        expect(cuerpo.tarjeta?.numero).toContain('4242');
        return HttpResponse.json(contratacion, { status: 201 });
      }),
    );
    montar('/contratar/comercio');
    expect(await screen.findByRole('heading', { name: 'Contratación de un plan' })).toBeDefined();
    await userEvent.type(screen.getByLabelText('Número de tarjeta'), '4242 4242 4242 4242');
    await userEvent.type(screen.getByLabelText('Nombre del titular'), 'ANA PÉREZ');
    await userEvent.type(screen.getByLabelText('Vencimiento'), '11 / 28');
    await userEvent.type(screen.getByLabelText('Código de seguridad'), '123');
    await userEvent.click(screen.getByRole('button', { name: /pagar q 450\.00/i }));
    expect(await screen.findByRole('heading', { name: 'Plan contratado' })).toBeDefined();
    expect(screen.getByText(contratacion.claves[0].clave)).toBeDefined();
    expect(screen.getByText('Cópielas ahora.')).toBeDefined();
    await userEvent.click(screen.getByRole('link', { name: 'Ir a la documentación' }));
    expect(screen.queryByText(contratacion.claves[0].clave)).toBeNull();
  });

  it('A5.6 muestra el rechazo en el mismo formulario', async () => {
    server.use(
      http.get(`${API}/auth/sesion`, () => HttpResponse.json({ consumidor: { nombre: 'Ana', nombreEmpresa: 'Tienda' }, correoVerificado: true, destino: '/planes' })),
      http.post(`${API}/suscripciones`, () => HttpResponse.json({ title: 'El pago fue rechazado', status: 402, codigo: 'pago_rechazado' }, { status: 402, headers: { 'Content-Type': 'application/problem+json' } })),
    );
    montar('/contratar/comercio');
    await screen.findByRole('heading', { name: 'Contratación de un plan' });
    await userEvent.type(screen.getByLabelText('Número de tarjeta'), '4000 0000 0000 0002');
    await userEvent.type(screen.getByLabelText('Nombre del titular'), 'ANA PÉREZ');
    await userEvent.type(screen.getByLabelText('Vencimiento'), '11 / 28');
    await userEvent.type(screen.getByLabelText('Código de seguridad'), '123');
    await userEvent.click(screen.getByRole('button', { name: /pagar q 450\.00/i }));
    expect(await screen.findByText(/pago fue rechazado/i)).toBeDefined();
    expect(screen.getByRole('button', { name: /pagar/i })).toBeDefined();
  });

  it('RF-20 activa un plan gratuito directamente desde A5.4', async () => {
    server.use(
      http.get(`${API}/auth/sesion`, () => HttpResponse.json({ consumidor: { nombre: 'Ana', nombreEmpresa: 'Tienda' }, correoVerificado: true, destino: '/planes' })),
      http.post(`${API}/suscripciones`, async ({ request }) => {
        const cuerpo = await request.json() as Record<string, unknown>;
        expect(cuerpo).toEqual({ planId: 'gratis' });
        return HttpResponse.json({ ...contratacion, suscripcion: { ...contratacion.suscripcion, planId: 'gratis', nombrePlan: 'Prueba gratuita' } }, { status: 201 });
      }),
    );
    montar('/planes');
    const gratis = await screen.findByText('Prueba gratuita');
    await userEvent.click(gratis.closest('article')?.querySelector('button') as HTMLElement);
    expect(await screen.findByRole('heading', { name: 'Plan contratado' })).toBeDefined();
  });
});
