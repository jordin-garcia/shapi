import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createMemoryRouter, type RouterProviderProps } from 'react-router';
import App from '../App';
import { crearRutas } from '../rutas';
import { server } from '../../../../test/servidor';

const API = 'http://localhost/api/portal';
const descriptorPortapapeles = Object.getOwnPropertyDescriptor(navigator, 'clipboard');
const configuracion = {
  nombrePortal: 'Envíos Xelajú', colorPrincipal: '#B8322A', urlLogo: null,
  bienvenida: 'Cotice envíos.', nombreApi: 'API de Cotización de Envíos', descripcionApi: '',
  hostPortal: 'envios.shapi.localhost', hostApi: 'envios.api.shapi.localhost', nombreOrganizacion: 'Envíos Xelajú, S.A.',
};
const suscripcion = {
  plan: { id: 'plan-comercio', nombre: 'Comercio', precio: 450, moneda: 'GTQ' },
  estado: 'activa', periodo: { inicio: '2026-09-01T06:00:00Z', fin: '2026-09-30T06:00:00Z' },
  proximaRenovacion: '2026-10-01T06:00:00Z', tarjetaEnmascarada: 'Mastercard •••• 3057',
  cuotaLlamadas: 50_000, limiteMinuto: 120,
};
const planes = [
  { id: 'plan-basico', apiId: 'api-1', nombre: 'Básico', descripcion: 'Para comenzar.', precio: 150, moneda: 'GTQ', esGratuito: false, vigenciaDias: 30, cuotaLlamadas: 10_000, limiteMinuto: 60, activo: true },
  { id: 'plan-comercio', apiId: 'api-1', nombre: 'Comercio', descripcion: 'Para tiendas.', precio: 450, moneda: 'GTQ', esGratuito: false, vigenciaDias: 30, cuotaLlamadas: 50_000, limiteMinuto: 120, activo: true },
  { id: 'plan-volumen', apiId: 'api-1', nombre: 'Volumen', descripcion: 'Para alto tráfico.', precio: 1200, moneda: 'GTQ', esGratuito: false, vigenciaDias: 30, cuotaLlamadas: 500_000, limiteMinuto: 600, activo: true },
];
let cliente: QueryClient;

function montar(ruta: string) {
  const enrutador = createMemoryRouter(crearRutas(), { initialEntries: [ruta] });
  render(<QueryClientProvider client={cliente}><App enrutador={enrutador as RouterProviderProps['router']} /></QueryClientProvider>);
  return enrutador;
}

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  server.use(
    http.get(`${API}/reloj`, () => HttpResponse.json({ ahora: '2026-09-18T06:00:00Z', inicioCiclo: '2026-09-18T06:00:00Z', finClaveAnteriorPrevisto: '2026-09-18T06:00:00Z' })),
    http.get(`${API}/configuracion`, () => HttpResponse.json(configuracion)),
    http.get(`${API}/auth/sesion`, () => HttpResponse.json({ consumidor: { nombre: 'María José Quiñónez', nombreEmpresa: 'Mercadito Antigua' }, correoVerificado: true })),
    http.get(`${API}/suscripcion`, () => HttpResponse.json(suscripcion)),
    http.get(`${API}/planes`, () => HttpResponse.json(planes)),
    http.get(`${API}/claves`, () => HttpResponse.json({ elementos: [] })),
  );
});

afterEach(() => {
  cleanup();
  cliente.clear();
  vi.restoreAllMocks();
  if (descriptorPortapapeles) Object.defineProperty(navigator, 'clipboard', descriptorPortapapeles);
  else Reflect.deleteProperty(navigator, 'clipboard');
});

describe('DC-13 · RF-21 · historial de pagos', () => {
  it('B2.2 muestra los datos y encabezados del mockup', async () => {
    server.use(http.get(`${API}/pagos`, ({ request }) => {
      const url = new URL(request.url);
      expect(url.searchParams.get('pagina')).toBe('1');
      expect(url.searchParams.get('tamano')).toBe('20');
      return HttpResponse.json({ elementos: [{
        id: 'pago-1', fecha: '2026-09-01T14:00:00Z', concepto: 'contratacion',
        descripcion: 'Suscripción al plan Comercio', periodoInicio: '2026-09-01T06:00:00Z',
        periodoFin: '2026-10-01T06:00:00Z', monto: 450, moneda: 'GTQ',
        medioPago: { marca: 'mastercard', ultimos4: '3057' }, estado: 'autorizado',
      }], total: 1 });
    }));
    montar('/cuenta/pagos');

    expect(await screen.findByRole('heading', { name: 'Historial de pagos' })).toBeDefined();
    expect(screen.getByText('Los cobros de su suscripción a la API de Cotización de Envíos.')).toBeDefined();
    const tabla = screen.getByRole('table', { name: 'Sus pagos' });
    expect(within(tabla).getAllByRole('columnheader').map(celda => celda.textContent)).toEqual(['Fecha', 'Concepto', 'Monto', 'Medio de pago', 'Estado']);
    expect(within(tabla).getByText('Suscripción al plan Comercio')).toBeDefined();
    expect(within(tabla).getByText('1 sep 2026 – 30 sep 2026')).toBeDefined();
    expect(within(tabla).getByText('Q 450.00')).toBeDefined();
    expect(within(tabla).getByText('Mastercard •••• 3057')).toBeDefined();
    expect(within(tabla).getByText('Pagado')).toBeDefined();
  });

  it('B2.2 muestra la variante vacía y enlaza los planes', async () => {
    server.use(http.get(`${API}/pagos`, () => HttpResponse.json({ elementos: [], total: 0 })));
    montar('/cuenta/pagos');

    expect(await screen.findByRole('heading', { name: 'Todavía no tiene pagos' })).toBeDefined();
    expect(screen.getByText('Cuando contrate un plan de esta API, aquí aparecerá cada cobro con su fecha, su concepto, su monto, su tarjeta y su estado.')).toBeDefined();
    expect(screen.getByRole('link', { name: 'Ver los planes' }).getAttribute('href')).toBe('/planes');
  });
});

describe('DC-13 · RF-25 · cambio de plan', () => {
  it('permite elegir el destino al llegar desde la suscripción vigente', async () => {
    vi.spyOn(Date, 'now').mockReturnValue(new Date('1999-01-01T00:00:00Z').getTime());
    const enrutador = montar('/cuenta/suscripcion');

    await userEvent.click(await screen.findByRole('link', { name: 'Cambiar de plan' }));
    expect(enrutador.state.location.pathname).toBe('/cuenta/cambiar-plan/plan-comercio');
    await userEvent.click(await screen.findByRole('button', { name: 'Elegir Volumen' }));

    expect(enrutador.state.location.pathname).toBe('/cuenta/cambiar-plan/plan-volumen');
    expect(await screen.findByRole('heading', { name: 'Diferencia prorrateada' })).toBeDefined();
    expect(screen.getByRole('button', { name: 'Pagar Q 325.00 y cambiar de plan' })).toBeDefined();
    await userEvent.click(screen.getByRole('link', { name: 'Elegir otro plan' }));
    expect(enrutador.state.location.pathname).toBe('/cuenta/cambiar-plan/plan-comercio');
    expect(await screen.findByRole('button', { name: 'Elegir Volumen' })).toBeDefined();
  });

  it('B2.7 calcula el prorrateo del mockup y confirma una subida con la tarjeta registrada', async () => {
    vi.spyOn(Date, 'now').mockReturnValue(new Date('1999-01-01T00:00:00Z').getTime());
    let cuerpo: unknown;
    server.use(http.post(`${API}/suscripcion/cambiar`, async ({ request }) => {
      cuerpo = await request.json();
      return HttpResponse.json({ estado: 'activa', credito: 195, cargo: 520, aPagar: 325, inicio: '2026-09-01T06:00:00Z', fin: '2026-10-01T06:00:00Z', claves: [] });
    }));
    const enrutador = montar('/cuenta/cambiar-plan/plan-volumen');

    expect(await screen.findByRole('heading', { name: 'Cambio de plan' })).toBeDefined();
    expect(screen.getByText('Plan actual')).toBeDefined();
    expect(screen.getByText('Plan destino')).toBeDefined();
    expect(screen.getByText('13 de 30')).toBeDefined();
    expect(screen.getByText('− Q 195.00')).toBeDefined();
    expect(screen.getByText('Q 520.00')).toBeDefined();
    expect(screen.getByText('Q 325.00')).toBeDefined();
    expect(screen.getByText('Mastercard •••• 3057')).toBeDefined();
    expect(screen.getByText('Desde el 1 de octubre de 2026 el plan Volumen se cobra completo: Q 1,200.00 cada 30 días.')).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Pagar Q 325.00 y cambiar de plan' }));

    await waitFor(() => expect(cuerpo).toEqual({ planId: 'plan-volumen' }));
    await waitFor(() => expect(enrutador.state.location.pathname).toBe('/cuenta/suscripcion'));
  });

  it('B2.7 informa y programa una bajada para la próxima renovación', async () => {
    let cuerpo: unknown;
    server.use(http.post(`${API}/suscripcion/cambiar`, async ({ request }) => {
      cuerpo = await request.json();
      return HttpResponse.json({ estado: 'programado', planSiguienteId: 'plan-basico', efectivoDesde: '2026-10-01T06:00:00Z' });
    }));
    const enrutador = montar('/cuenta/cambiar-plan/plan-basico');

    expect(await screen.findByRole('heading', { name: 'Cambio programado' })).toBeDefined();
    expect(screen.getByText('El plan Básico se aplicará en su próxima renovación, el 1 oct 2026. No se cobrará ni se reembolsará nada hoy.')).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Programar cambio de plan' }));

    await waitFor(() => expect(cuerpo).toEqual({ planId: 'plan-basico' }));
    await waitFor(() => expect(enrutador.state.location.pathname).toBe('/cuenta/suscripcion'));
  });

  it('cobra el ciclo completo al salir de un plan gratuito y revela las claves nuevas una sola vez', async () => {
    server.use(
      http.get(`${API}/suscripcion`, () => HttpResponse.json({
        ...suscripcion,
        plan: { id: 'plan-gratis', nombre: 'Gratis', precio: 0, moneda: 'GTQ' },
        tarjetaEnmascarada: null, cuotaLlamadas: 1_000, limiteMinuto: 20,
      })),
      http.post(`${API}/suscripcion/cambiar`, async ({ request }) => {
        expect(await request.json()).toEqual({
          planId: 'plan-volumen',
          tarjeta: { numero: '4242 4242 4242 4242', titular: 'María José Quiñónez', mesVencimiento: '12', anioVencimiento: '30', cvv: '123' },
        });
        return HttpResponse.json({
          estado: 'activa', credito: 0, cargo: 1200, aPagar: 1200,
          inicio: '2026-09-18T06:00:00Z', fin: '2026-10-18T06:00:00Z',
          claves: [
            { id: 'prod-nueva', tipo: 'produccion', clave: 'shp_prod_completa', claveEnmascarada: 'shp_prod_••••leta', estado: 'activa' },
            { id: 'prueba-nueva', tipo: 'pruebas', clave: 'shp_prueba_completa', claveEnmascarada: 'shp_prueba_••••leta', estado: 'activa' },
          ],
        });
      }),
    );
    const enrutador = montar('/cuenta/cambiar-plan/plan-volumen');

    expect(await screen.findByText('− Q 0.00')).toBeDefined();
    expect(screen.getByText('Cargo por el ciclo completo de Volumen')).toBeDefined();
    expect(screen.getAllByText('Q 1,200.00')).toHaveLength(3);
    await userEvent.type(screen.getByLabelText('Número de tarjeta'), '4242 4242 4242 4242');
    await userEvent.type(screen.getByLabelText('Nombre del titular'), 'María José Quiñónez');
    await userEvent.type(screen.getByLabelText('Mes'), '12');
    await userEvent.type(screen.getByLabelText('Año'), '30');
    await userEvent.type(screen.getByLabelText('Código de seguridad'), '123');
    await userEvent.click(screen.getByRole('button', { name: 'Pagar Q 1,200.00 y cambiar de plan' }));

    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: vi.fn().mockResolvedValue(undefined) } });
    expect(await screen.findByRole('heading', { name: 'Su clave nueva de producción' })).toBeDefined();
    expect(screen.getByText('shp_prod_completa')).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Copiar' }));
    expect(await screen.findByRole('button', { name: 'Copiada' })).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Ya la guardé' }));
    expect(await screen.findByRole('heading', { name: 'Su clave nueva de pruebas' })).toBeDefined();
    expect(screen.getByText('shp_prueba_completa')).toBeDefined();
    expect(screen.queryByText('shp_prod_completa')).toBeNull();
    expect(screen.getByRole('button', { name: 'Copiar' })).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Ya la guardé' }));
    await waitFor(() => expect(enrutador.state.location.pathname).toBe('/cuenta/suscripcion'));
  });
});
