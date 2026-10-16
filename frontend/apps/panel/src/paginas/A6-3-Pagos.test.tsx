import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { setupServer } from 'msw/node';
import { MemoryRouter } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import PaginaA63Pagos from './A6-3-Pagos';

const pago = { id: 'pago-1', fecha: '2026-10-15T15:30:00Z', organizacion: 'Agro Precios, S.A.', plan: 'Lanzamiento', monto: 199, moneda: 'GTQ' as const, tarjeta: 'Visa •••• 4821', estado: 'pagado' as const, periodoInicio: '2026-10-01T00:00:00Z', periodoFin: '2026-11-01T00:00:00Z' };
const pagoRechazado = { ...pago, id: 'pago-2', organizacion: 'Transportes Petén, S.A.', tarjeta: 'Mastercard •••• 7744', estado: 'rechazado' as const };
let revertido = false;
const servidor = setupServer(
  http.get('http://localhost/api/admin/pagos', () => HttpResponse.json([pago, pagoRechazado])),
  http.post('http://localhost/api/admin/pagos/:id/revertir', () => { revertido = true; return new HttpResponse(null, { status: 204 }); }),
);
beforeAll(() => servidor.listen());
afterEach(() => { servidor.resetHandlers(); revertido = false; });
afterAll(() => servidor.close());

function renderizar() {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={cliente}><MemoryRouter><PaginaA63Pagos /></MemoryRouter></QueryClientProvider>);
}

describe('PaginaA63Pagos', () => {
  it('RF-24 muestra los cobros de plataforma y no permite revertir un pago rechazado', async () => {
    renderizar();
    expect(await screen.findByText('Agro Precios, S.A.')).toBeDefined();
    expect(screen.getByText('Visa •••• 4821')).toBeDefined();
    expect(screen.getByRole('button', { name: 'Revertir pago de Agro Precios, S.A.' })).toBeDefined();
    expect(screen.queryByRole('button', { name: 'Revertir pago de Transportes Petén, S.A.' })).toBeNull();
  });

  it('RF-24 confirma la reversión y actualiza el estado', async () => {
    const usuario = userEvent.setup();
    renderizar();
    await usuario.click(await screen.findByRole('button', { name: 'Revertir pago de Agro Precios, S.A.' }));
    expect(screen.getByRole('heading', { name: 'Revertir un pago' })).toBeDefined();
    await usuario.click(screen.getByRole('button', { name: 'Revertir pago' }));
    await waitFor(() => expect(revertido).toBe(true));
  });
});
