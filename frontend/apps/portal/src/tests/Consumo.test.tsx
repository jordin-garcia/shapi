import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, cleanup, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createMemoryRouter, type RouterProviderProps } from 'react-router';
import type { components } from '@shapi/api/consumo';
import App from '../App';
import { crearRutas } from '../rutas';
import { server } from '../../../../test/servidor';

const API = 'http://localhost/api/portal';
const consumo: components['schemas']['ConsumoCiclo'] = {
  suscripcionId: 'suscripcion-1', inicio: '2026-09-01', fin: '2026-09-30',
  llamadas: 31_480, cuota: 50_000, restante: 18_520, porcentaje: 62.96,
  peticiones: 27_912, llamadasConsolidadas: 31_480,
  rutas: [
    { rutaId: '1', metodo: 'POST', patron: '/cotizaciones', pesoLlamadas: 1, peticiones: 15_120, llamadas: 15_120, porcentaje: 48.0305 },
    { rutaId: '2', metodo: 'GET', patron: '/rastreo', pesoLlamadas: 1, peticiones: 11_340, llamadas: 11_340, porcentaje: 36.0229 },
    { rutaId: '3', metodo: 'POST', patron: '/guias', pesoLlamadas: 5, peticiones: 892, llamadas: 4_460, porcentaje: 14.1677 },
    { rutaId: '4', metodo: 'GET', patron: '/cobertura', pesoLlamadas: 1, peticiones: 560, llamadas: 560, porcentaje: 1.7789 },
  ],
};
let cliente: QueryClient;

function montar() {
  const enrutador = createMemoryRouter(crearRutas(), { initialEntries: ['/cuenta/consumo'] });
  render(<QueryClientProvider client={cliente}><App enrutador={enrutador as RouterProviderProps['router']} /></QueryClientProvider>);
  return enrutador;
}

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  server.use(
    http.get(`${API}/configuracion`, () => HttpResponse.json({
      nombrePortal: 'Envíos Xelajú', colorPrincipal: '#B8322A', urlLogo: null,
      bienvenida: 'Cotice envíos.', nombreApi: 'API de Cotización de Envíos', descripcionApi: '',
      hostPortal: 'envios.shapi.localhost', hostApi: 'envios.api.shapi.localhost', nombreOrganizacion: 'Envíos Xelajú, S.A.',
    })),
    http.get(`${API}/auth/sesion`, () => HttpResponse.json({
      consumidor: { nombre: 'María José Quiñónez', nombreEmpresa: 'Mercadito Antigua' }, correoVerificado: true,
    })),
    http.get(`${API}/consumo`, () => HttpResponse.json(consumo)),
  );
});

afterEach(() => { cleanup(); cliente.clear(); vi.useRealTimers(); });

describe('JG-12 · RF-37 · consumo del ciclo', () => {
  it('JG-26 · RF-37 consulta de nuevo cada 10 segundos y actualiza la cuota visible', async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    let consultas = 0;
    server.use(http.get(`${API}/consumo`, () => {
      consultas++;
      return HttpResponse.json({ ...consumo, restante: 18521 - consultas });
    }));
    montar();
    await screen.findByText('18,520 llamadas');
    await act(() => vi.advanceTimersByTimeAsync(9999));
    expect(consultas).toBe(1);
    await act(() => vi.advanceTimersByTimeAsync(1));
    await screen.findByText('18,519 llamadas');
    expect(consultas).toBe(2);
    await act(() => vi.advanceTimersByTimeAsync(10000));
    await screen.findByText('18,518 llamadas');
    expect(consultas).toBe(3);
  });

  it('reproduce los textos, cifras, rutas y totales de B2.1', async () => {
    montar();
    expect(await screen.findByRole('heading', { name: 'Consumo del ciclo' })).toBeDefined();
    expect(screen.getByText('Datos consolidados por suscripción, ruta y día.')).toBeDefined();
    expect(screen.getByText('1 sep 2026 – 30 sep 2026')).toBeDefined();
    expect(screen.getByText('de 50,000 llamadas de su cuota')).toBeDefined();
    expect(screen.getByText('63 % de su cuota mensual')).toBeDefined();
    expect(screen.getByText('18,520 llamadas')).toBeDefined();
    const tabla = screen.getByRole('table', { name: 'Consumo por ruta' });
    expect(within(tabla).getAllByRole('columnheader').map(x => x.textContent)).toEqual([
      'Ruta', 'Peticiones', 'Descuenta por petición', 'Llamadas descontadas', '% del consumo',
    ]);
    const filas = within(tabla).getAllByRole('row');
    expect(filas.slice(1).map(f => within(f).getAllByRole('cell').map(x => x.textContent))).toEqual([
      ['POST/cotizaciones', '15,120', '1 llamada', '15,120', '48.0 %'],
      ['GET/rastreo', '11,340', '1 llamada', '11,340', '36.0 %'],
      ['POST/guias', '892', '5 llamadas', '4,460', '14.2 %'],
      ['GET/cobertura', '560', '1 llamada', '560', '1.8 %'],
      ['Total del ciclo', '27,912', '', '31,480', '100 %'],
    ]);
    const barra = screen.getByRole('progressbar');
    expect(barra.getAttribute('aria-valuenow')).toBe('62.96');
    expect(screen.getByRole('link', { name: 'Consumo' }).getAttribute('aria-current')).toBe('page');
  });

  it('sin suscripción muestra ConsumoVacia y el enlace real a planes', async () => {
    server.use(http.get(`${API}/consumo`, () => HttpResponse.json({
      title: 'No tiene un plan contratado.', codigo: 'suscripcion_no_encontrada', status: 404,
    }, { status: 404, headers: { 'Content-Type': 'application/problem+json' } })));
    montar();
    expect(await screen.findByRole('heading', { name: 'Todavía no tiene un plan contratado' })).toBeDefined();
    expect(screen.getByText('En cuanto contrate un plan de esta API verá aquí el consumo de cada ciclo, desglosado por ruta: las peticiones que hizo y las llamadas que descontaron de su cuota.')).toBeDefined();
    expect(screen.getByRole('link', { name: 'Ver los planes' }).getAttribute('href')).toBe('/planes');
    expect(screen.queryByRole('table')).toBeNull();
  });

  it('una suscripción sin uso conserva el ciclo, la cuota y los totales en cero', async () => {
    server.use(http.get(`${API}/consumo`, () => HttpResponse.json({
      ...consumo, llamadas: 0, restante: 50_000, porcentaje: 0, peticiones: 0, llamadasConsolidadas: 0, rutas: [],
    })));
    montar();
    const tabla = await screen.findByRole('table', { name: 'Consumo por ruta' });
    expect(screen.getByText('50,000 llamadas')).toBeDefined();
    const total = within(tabla).getByText('Total del ciclo').closest('tr')!;
    expect(within(total).getAllByRole('cell').map(x => x.textContent)).toEqual(['Total del ciclo', '0', '', '0', '0 %']);
    expect(screen.queryByText('Todavía no tiene un plan contratado')).toBeNull();
  });

  it('conserva los totales consolidados cuando Redis ya tiene más llamadas y representa rutas retiradas', async () => {
    server.use(http.get(`${API}/consumo`, () => HttpResponse.json({
      ...consumo, llamadas: 31_500,
      rutas: [{ rutaId: null, metodo: null, patron: 'Sin ruta identificada', pesoLlamadas: null, peticiones: 27_912, llamadas: 31_480, porcentaje: 100 }],
    })));
    montar();
    const tabla = await screen.findByRole('table', { name: 'Consumo por ruta' });
    expect(screen.getByText('31,500')).toBeDefined();
    expect(within(tabla).getAllByText('31,480')).toHaveLength(2);
    expect(within(tabla).getByText('Sin ruta identificada')).toBeDefined();
    expect(within(tabla).getByText('—')).toBeDefined();
  });

  it.each([404, 503])('un fallo %s muestra un error recuperable y no un plan ausente', async estado => {
    server.use(http.get(`${API}/consumo`, () => HttpResponse.json({
      title: 'No se pudo cargar el consumo.', codigo: estado === 404 ? 'api_no_encontrada' : 'servicio_no_disponible', status: estado,
    }, { status: estado, headers: { 'Content-Type': 'application/problem+json' } })));
    montar();
    await screen.findByRole('button', { name: 'Reintentar' });
    expect(screen.queryByText('Todavía no tiene un plan contratado')).toBeNull();
    server.use(http.get(`${API}/consumo`, () => HttpResponse.json(consumo)));
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }));
    expect(await screen.findByRole('table', { name: 'Consumo por ruta' })).toBeDefined();
  });

  it('muestra el estado de carga antes de recibir el consumo', async () => {
    let resolver!: (respuesta: Response) => void;
    server.use(http.get(`${API}/consumo`, () => new Promise<Response>(resolve => { resolver = resolve; })));
    montar();
    await waitFor(() => expect(resolver).toBeDefined());
    expect(screen.getByRole('status', { name: 'Cargando' })).toBeDefined();
    resolver(HttpResponse.json(consumo));
    expect(await screen.findByRole('table', { name: 'Consumo por ruta' })).toBeDefined();
  });

  it('una cuota excedida limita la barra sin ocultar el porcentaje real', async () => {
    server.use(http.get(`${API}/consumo`, () => HttpResponse.json({ ...consumo, llamadas: 60_000, restante: 0, porcentaje: 120 })));
    montar();
    const barra = await screen.findByRole('progressbar');
    expect(barra.getAttribute('aria-valuenow')).toBe('100');
    expect(screen.getByText('120 % de su cuota mensual')).toBeDefined();
    expect(screen.getByText('0 llamadas')).toBeDefined();
  });
});
