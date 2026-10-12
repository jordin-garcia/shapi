import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, within, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import type { components } from '@shapi/api/consumo';
import { server } from '../../../../test/servidor';
import Consumo from '../paginas/B1-1-Consumo';

const API_URL = 'http://localhost/api/apis/:id/consumo';
const p95 = (milisegundos: number | null, supera2500 = false) => ({ milisegundos, supera2500 });
const datos: components['schemas']['ConsumoApi'] = {
  apiId: 'api-1', apiNombre: 'API de Cotización de Envíos', desde: '2026-09-01', hasta: '2026-09-10',
  peticiones: 105456, llamadas: 93147, bytesEntrada: 310000000, bytesSalida: 1530000000,
  latenciaTotalP95: p95(236), latenciaCompuertaP95: p95(10),
  dias: [3180, 3640, 3910, 3036, 13720, 11460, 16280, 18700, 15140, 16390].map((peticiones, i) => ({ fecha: `2026-09-${String(i + 1).padStart(2, '0')}`, peticiones })),
  rutas: [
    { rutaId: 'ruta-1', metodo: 'POST', patron: '/cotizaciones', peticiones: 50620, porcentaje: 48, latenciaTotalP95: p95(214), latenciaCompuertaP95: p95(12) },
    { rutaId: 'ruta-2', metodo: 'GET', patron: '/rastreo', peticiones: 37964, porcentaje: 36, latenciaTotalP95: p95(142), latenciaCompuertaP95: p95(9) },
    { rutaId: 'ruta-3', metodo: 'POST', patron: '/guias', peticiones: 9492, porcentaje: 9, latenciaTotalP95: p95(388), latenciaCompuertaP95: p95(11) },
    { rutaId: 'ruta-4', metodo: 'GET', patron: '/cobertura', peticiones: 7380, porcentaje: 7, latenciaTotalP95: p95(96), latenciaCompuertaP95: p95(8) },
  ],
  rechazosCompuerta: [
    { codigo: '401', motivo: 'Clave inválida o revocada', peticiones: 476, porcentaje: .45 },
    { codigo: '403', motivo: 'Acceso no permitido', peticiones: 74, porcentaje: .07 },
    { codigo: '429', motivo: 'Cuota agotada o límite por minuto excedido', peticiones: 1476, porcentaje: 1.4 },
  ],
  erroresOrigen: [
    { codigo: '4xx', motivo: 'Respuestas 4xx de su servidor', peticiones: 250, porcentaje: .24 },
    { codigo: '5xx', motivo: 'Respuestas 5xx de su servidor', peticiones: 66, porcentaje: .06 },
    { codigo: 'fallas', motivo: 'Fallas de conexión o tiempo de espera del origen', peticiones: 0, porcentaje: 0 },
  ],
};
let cache: QueryClient;
function mostrar() {
  render(<QueryClientProvider client={cache}><MemoryRouter initialEntries={['/panel/apis/api-1/consumo']}><Routes><Route path="/panel/apis/:id/consumo" element={<Consumo />} /></Routes></MemoryRouter></QueryClientProvider>);
}
beforeEach(() => { cache = new QueryClient({ defaultOptions: { queries: { retry: false } } }); });
afterEach(() => { cleanup(); cache.clear(); });

describe('RF-35 · B1.1 Consumo, latencia y errores', () => {
  it('reproduce indicadores, barras, rutas y errores del mockup', async () => {
    server.use(http.get(API_URL, () => HttpResponse.json(datos)));
    mostrar();
    expect(await screen.findByRole('heading', { name: 'Consumo, latencia y errores' })).toBeDefined();
    expect(screen.getByText('API de Cotización de Envíos')).toBeDefined();
    expect(screen.getByText('Datos consolidados por suscripción, ruta y día.')).toBeDefined();
    for (const texto of ['105,456', '236 ms', '10 ms', '1.84 GB', '0.31 GB de entrada · 1.53 GB de salida', 'En los 10 días del periodo']) expect(screen.getAllByText(texto).length).toBeGreaterThan(0);
    expect(screen.getByText('93,147 llamadas descontadas')).toBeDefined();
    expect(screen.getByRole('button', { name: 'Del 1 al 10 de septiembre de 2026' })).toBeDefined();
    expect(screen.getAllByRole('img', { name: /sep: .* peticiones/ })).toHaveLength(10);
    const tablas = screen.getAllByRole('table');
    expect(within(tablas[0]).getAllByRole('columnheader').map(x => x.textContent)).toEqual(['Ruta expuesta', 'Peticiones', '% del total', 'Latencia total p95', 'Latencia de la compuerta p95']);
    for (const texto of ['/cotizaciones', '50,620', '48.0 %', '214 ms', '12 ms']) expect(within(tablas[0]).getByText(texto)).toBeDefined();
    for (const texto of ['Rechazos de la compuerta · la petición no llegó a su servidor', 'Errores de su servidor de origen', '401', '403', '429', '4xx', '5xx', 'fallas', '0.45 %', '1.40 %']) expect(within(tablas[1]).getByText(texto)).toBeDefined();
  });

  it('muestra la variante vacía sin indicadores ni tablas', async () => {
    server.use(http.get(API_URL, () => HttpResponse.json({ ...datos, peticiones: 0, llamadas: 0, rutas: [] })));
    mostrar();
    expect(await screen.findByRole('heading', { name: 'Todavía no hay consumo de esta API' })).toBeDefined();
    expect(screen.getByText('Cuando sus consumidores empiecen a llamar a la API, aquí verá las peticiones por ruta y por día, la latencia, los errores y los datos transferidos del periodo.')).toBeDefined();
    expect(screen.queryByRole('table')).toBeNull();
    expect(screen.queryByText('Peticiones a la API')).toBeNull();
  });

  it('consulta el periodo elegido y no reutiliza los datos del anterior', async () => {
    let periodo = '';
    server.use(http.get(API_URL, ({ request }) => {
      const url = new URL(request.url);
      periodo = url.search;
      return HttpResponse.json(url.searchParams.has('desde') ? { ...datos, desde: '2026-08-01', hasta: '2026-08-31', peticiones: 0 } : datos);
    }));
    mostrar();
    await userEvent.click(await screen.findByRole('button', { name: 'Del 1 al 10 de septiembre de 2026' }));
    const desde = screen.getByLabelText('Desde');
    const hasta = screen.getByLabelText('Hasta');
    await userEvent.clear(desde); await userEvent.type(desde, '2026-08-01');
    await userEvent.clear(hasta); await userEvent.type(hasta, '2026-08-31');
    await userEvent.click(screen.getByRole('button', { name: 'Aplicar periodo' }));
    await waitFor(() => expect(periodo).toBe('?desde=2026-08-01&hasta=2026-08-31'));
    expect(await screen.findByRole('heading', { name: 'Todavía no hay consumo de esta API' })).toBeDefined();
  });

  it('muestra el rango abierto y la fila sin ruta con porcentajes finitos', async () => {
    server.use(http.get(API_URL, () => HttpResponse.json({ ...datos, latenciaTotalP95: p95(null, true), rutas: [{ ...datos.rutas[0], rutaId: null, metodo: null, patron: 'Sin ruta identificada', latenciaTotalP95: p95(null), latenciaCompuertaP95: p95(null, true) }] })));
    mostrar();
    await screen.findByText('Sin ruta identificada');
    expect(screen.getAllByText('> 2500 ms')).toHaveLength(2);
    expect(screen.getByText('—')).toBeDefined();
  });

  it('rechaza periodos de más de 366 días sin pedirlos a la API', async () => {
    let solicitudes = 0;
    server.use(http.get(API_URL, () => { solicitudes++; return HttpResponse.json(datos); }));
    mostrar();
    await userEvent.click(await screen.findByRole('button', { name: 'Del 1 al 10 de septiembre de 2026' }));
    const desde = screen.getByLabelText('Desde');
    await userEvent.clear(desde); await userEvent.type(desde, '2020-01-01');
    await userEvent.click(screen.getByRole('button', { name: 'Aplicar periodo' }));
    expect(screen.getByText('Seleccione un periodo de hasta 366 días.')).toBeDefined();
    expect(solicitudes).toBe(1);
  });

  it('redondea solo la presentación del p95 al milisegundo más cercano', async () => {
    server.use(http.get(API_URL, () => HttpResponse.json({ ...datos, latenciaCompuertaP95: p95(10.6) })));
    mostrar();
    const etiqueta = await screen.findByText('Latencia de la compuerta · percentil 95');
    expect(within(etiqueta.parentElement!).getByText('11 ms')).toBeDefined();
  });

  it('permite reintentar un error sin confundirlo con el periodo vacío', async () => {
    let falla = true;
    server.use(http.get(API_URL, () => falla ? new HttpResponse(null, { status: 500 }) : HttpResponse.json(datos)));
    mostrar();
    expect(await screen.findByText('No se pudo cargar el consumo.')).toBeDefined();
    expect(screen.queryByText('Todavía no hay consumo de esta API')).toBeNull();
    falla = false;
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }));
    expect(await screen.findByText('105,456')).toBeDefined();
  });
});
