import { afterEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import type { components } from '@shapi/api/pagos';
import { server } from '../../../../test/servidor';
import PaginaB13Pagos from './B1-3-Pagos';

const API = 'http://localhost/api';
const pago: components['schemas']['Pago'] = {
  id: 'pago-1', fecha: '2026-09-10T06:00:00Z', concepto: 'cambioPlan',
  descripcion: 'Lanzamiento → Producto · diferencia prorrateada',
  periodoInicio: '2026-09-10T06:00:00Z', periodoFin: '2026-10-10T06:00:00Z',
  monto: 173.34, moneda: 'GTQ', medioPago: { marca: 'visa', ultimos4: '4821' }, estado: 'autorizado',
};

function renderizar() {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={cliente}><PaginaB13Pagos /></QueryClientProvider>);
}

afterEach(() => { cleanup(); server.resetHandlers(); });

describe('B1.3 · Historial de pagos (RF-21)', () => {
  it('muestra el historial de plataforma con concepto, fecha, tarjeta, estado y monto', async () => {
    server.use(http.get(API + '/pagos', ({ request }) => {
      const url = new URL(request.url);
      expect(url.searchParams.get('pagina')).toBe('1');
      expect(url.searchParams.get('tamano')).toBe('20');
      return HttpResponse.json({ elementos: [pago], total: 1 });
    }));

    renderizar();

    expect(await screen.findByRole('heading', { name: 'Historial de pagos' })).toBeDefined();
    expect(screen.getByText('Organización')).toBeDefined();
    expect(screen.getByText('Cobros de su suscripción de plataforma. De su tarjeta solo se conservan la marca y los últimos cuatro dígitos.')).toBeDefined();
    expect(screen.getByRole('columnheader', { name: 'Fecha' })).toBeDefined();
    expect(screen.getByRole('columnheader', { name: 'Concepto' })).toBeDefined();
    expect(screen.getByRole('columnheader', { name: 'Monto' })).toBeDefined();
    expect(screen.getByRole('columnheader', { name: 'Medio de pago' })).toBeDefined();
    expect(screen.getByRole('columnheader', { name: 'Estado' })).toBeDefined();
    expect(screen.getByText('10 sep 2026')).toBeDefined();
    expect(screen.getByText('Cambio de plan de plataforma')).toBeDefined();
    expect(screen.getByText(pago.descripcion)).toBeDefined();
    expect(screen.getByText('Q 173.34')).toBeDefined();
    expect(screen.getByText('Visa •••• 4821')).toBeDefined();
    expect(screen.getByText('Pagado')).toBeDefined();
  });

  it('RF-21 · muestra el último día incluido del periodo de contratación', async () => {
    server.use(http.get(API + '/pagos', () => HttpResponse.json({
      elementos: [{
        ...pago,
        concepto: 'contratacion',
        descripcion: 'Contratación del plan Lanzamiento',
        periodoInicio: '2026-08-24T06:00:00Z',
        periodoFin: '2026-09-23T06:00:00Z',
      }],
      total: 1,
    })));

    renderizar();

    expect(await screen.findByText('Lanzamiento · 24 ago 2026 – 22 sep 2026')).toBeDefined();
  });

  it('RF-21 · presenta el estado vacío del mockup cuando la organización está en Prueba', async () => {
    server.use(http.get(API + '/pagos', () => HttpResponse.json({ elementos: [], total: 0 })));

    renderizar();

    expect(await screen.findByRole('heading', { name: 'Todavía no tiene pagos' })).toBeDefined();
    expect(screen.getByText('Su organización está en el plan Prueba, que no se cobra. Cuando contrate un plan de pago, aquí aparecerá cada cobro con su concepto, su monto, su tarjeta y su estado.')).toBeDefined();
    expect(screen.getByRole('columnheader', { name: 'Fecha' })).toBeDefined();
  });
});
