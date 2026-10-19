import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { setupServer } from 'msw/node';
import { MemoryRouter, Route, Routes } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import PaginaB15InvitarConsumidores from './B1-5-InvitarConsumidores';

let invitaciones = [
  { id: 'inv-1', correo: 'compras@xelaju.com', enviadaEn: '2026-10-18T12:00:00Z', venceEn: '2026-10-25T12:00:00Z' },
];
let cuerpoEnviado: unknown;
let tokenRenovado = false;

const servidor = setupServer(
  http.get('http://localhost/api/apis/api-1/invitaciones', () => HttpResponse.json({ nombreApi: 'API de Cotización de Envíos', nombrePortal: 'Envíos Xelajú', elementos: invitaciones })),
  http.post('http://localhost/api/apis/api-1/invitaciones', async ({ request }) => {
    cuerpoEnviado = await request.json();
    invitaciones = [{ id: 'inv-2', correo: 'ana@ejemplo.com', enviadaEn: '2026-10-18T12:00:00Z', venceEn: '2026-10-25T12:00:00Z' }, ...invitaciones];
    return new HttpResponse(null, { status: 202 });
  }),
  http.post('http://localhost/api/apis/api-1/invitaciones/:id/reenviar', () => {
    tokenRenovado = true;
    return new HttpResponse(null, { status: 202 });
  }),
  http.delete('http://localhost/api/apis/api-1/invitaciones/:id', ({ params }) => {
    invitaciones = invitaciones.filter(invitacion => invitacion.id !== params.id);
    return new HttpResponse(null, { status: 204 });
  }),
);

beforeAll(() => servidor.listen());
afterEach(() => { cleanup(); servidor.resetHandlers(); invitaciones = [{ id: 'inv-1', correo: 'compras@xelaju.com', enviadaEn: '2026-10-18T12:00:00Z', venceEn: '2026-10-25T12:00:00Z' }]; cuerpoEnviado = undefined; tokenRenovado = false; });
afterAll(() => servidor.close());

function mostrar() {
  const cache = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={cache}><MemoryRouter initialEntries={['/panel/apis/api-1/consumidores/invitar']}>
    <Routes><Route path="/panel/apis/:id/consumidores/invitar" element={<PaginaB15InvitarConsumidores />} /></Routes>
  </MemoryRouter></QueryClientProvider>);
}

describe('B1.5 Invitar consumidores', () => {
  it('RF-05 muestra las invitaciones pendientes y el formulario del mockup', async () => {
    mostrar();
    expect(await screen.findByRole('heading', { name: 'Invitar consumidores' })).toBeDefined();
    expect(screen.getByText('Invitaciones pendientes')).toBeDefined();
    expect(screen.getByText('API de Cotización de Envíos')).toBeDefined();
    expect(screen.getByText('compras@xelaju.com')).toBeDefined();
    expect(screen.getByRole('columnheader', { name: 'Correo' })).toBeDefined();
    expect(screen.getByRole('columnheader', { name: 'Enviada' })).toBeDefined();
    expect(screen.getByRole('columnheader', { name: 'Vence' })).toBeDefined();
    expect(screen.getByRole('button', { name: 'Enviar invitación' })).toBeDefined();
    expect(screen.getByText('Invite a un cliente a crear su cuenta en su portal. La cuenta creada por invitación queda con el correo ya verificado.')).toBeDefined();
    expect(screen.getByText('Le enviaremos un enlace al portal de Envíos Xelajú para crear su cuenta. El enlace vence en 7 días.')).toBeDefined();
  });

  it('RF-05 envía la invitación y permite reenviar y cancelar pendientes', async () => {
    const usuario = userEvent.setup();
    mostrar();
    await screen.findByText('compras@xelaju.com');
    await usuario.type(screen.getByLabelText('Correo'), 'ana@ejemplo.com');
    await usuario.click(screen.getByRole('button', { name: 'Enviar invitación' }));
    await waitFor(() => expect(cuerpoEnviado).toEqual({ correo: 'ana@ejemplo.com' }));
    await screen.findByText('ana@ejemplo.com');
    await usuario.click(screen.getByRole('button', { name: 'Reenviar invitación a compras@xelaju.com' }));
    await waitFor(() => expect(tokenRenovado).toBe(true));
    await usuario.click(screen.getByRole('button', { name: 'Cancelar invitación a compras@xelaju.com' }));
    await waitFor(() => expect(screen.queryByText('compras@xelaju.com')).toBeNull());
  });
});
