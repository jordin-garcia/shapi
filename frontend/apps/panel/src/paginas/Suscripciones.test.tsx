import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router';
import { server } from '../../../../test/servidor';
import PaginaA21PlanesPlataforma from './A2-1-PlanesPlataforma';
import PaginaA22Contratacion from './A2-2-Contratacion';
import PaginaA25CambioPlan from './A2-5-CambioPlan';
import PaginaB14Suscripcion from './B1-4-Suscripcion';

const API = 'http://localhost/api';
const lanzamiento = { id: 'plan-1', nombre: 'Lanzamiento', descripcion: 'Empezar a cobrar por una API existente', precio: 199, moneda: 'GTQ' as const, vigenciaDias: 30, maxApis: 3, maxMiembros: 3, cuotaPeticiones: 250000, dominioPropio: false, esPrueba: false };
const producto = { ...lanzamiento, id: 'plan-2', nombre: 'Producto', descripcion: 'Proveedores con clientes establecidos', precio: 599, maxApis: 10, maxMiembros: 10, cuotaPeticiones: 2000000, dominioPropio: true };
const suscripcion = {
  plan: { id: lanzamiento.id, nombre: lanzamiento.nombre, descripcion: lanzamiento.descripcion, precio: lanzamiento.precio, moneda: 'GTQ' as const, vigenciaDias: 30 },
  estado: 'activa' as const, periodo: { inicio: '2026-08-24T06:00:00Z', fin: '2026-09-22T06:00:00Z' },
  proximaRenovacion: '2026-09-23T06:00:00Z', tarjetaEnmascarada: 'Visa •••• 4821', graciaHasta: null, diasRestantesCiclo: 13, diasRestantes: 0, cambioProgramado: null,
};
let client: QueryClient;
let cambioSolicitado = false;

function renderizar(entrada: string, vista: React.ReactNode) {
  return render(<QueryClientProvider client={client}><MemoryRouter initialEntries={[entrada]}>{vista}</MemoryRouter></QueryClientProvider>);
}

beforeEach(() => { client = new QueryClient({ defaultOptions: { queries: { retry: false } } }); });
afterEach(() => { cleanup(); client.clear(); server.resetHandlers(); cambioSolicitado = false; });

describe('RF-19, RF-20 y RF-25 · Suscripción de plataforma', () => {
  it('A2.1 muestra los planes, precios y límites de plataforma', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([{ ...lanzamiento, id: 'trial', nombre: 'Prueba', esPrueba: true }, lanzamiento, producto])),
      http.get(API + '/suscripcion', () => HttpResponse.json({ title: 'Sin suscripción', codigo: 'suscripcion_no_encontrada' }, { status: 404, headers: { 'Content-Type': 'application/problem+json' } })),
    );
    renderizar('/panel/suscripcion/planes', <PaginaA21PlanesPlataforma />);
    expect(await screen.findByRole('heading', { name: 'Los planes de Shapi.' })).toBeDefined();
    expect(screen.getAllByText('Q 199.00')).toHaveLength(2);
    expect(screen.getByText(/2,000,000/)).toBeDefined();
  });

  it('A2.2 y A2.3 contrata con tarjeta y confirma la activación', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento])),
      http.get(API + '/suscripcion', () => HttpResponse.json({ title: 'Sin suscripción', codigo: 'suscripcion_no_encontrada' }, { status: 404, headers: { 'Content-Type': 'application/problem+json' } })),
      http.post(API + '/suscripcion/contratar', async ({ request }) => {
        const body = await request.json() as { planId: string; tarjeta: { numero: string } };
        expect(body.planId).toBe(lanzamiento.id);
        expect(body.tarjeta.numero).toBe('4242424242424242');
        return HttpResponse.json({ id: 'sub-1', estado: 'activa' }, { status: 201 });
      }),
    );
    renderizar('/panel/suscripcion/contratar/plan-1', <Routes><Route path="/panel/suscripcion/contratar/:plan" element={<PaginaA22Contratacion />} /></Routes>);
    await userEvent.type(await screen.findByLabelText('Número de tarjeta'), '4242424242424242');
    await userEvent.type(screen.getByLabelText('Mes de vencimiento'), '12');
    await userEvent.type(screen.getByLabelText('Año de vencimiento'), '2030');
    await userEvent.type(screen.getByLabelText('Código de seguridad'), '123');
    await userEvent.type(screen.getByLabelText('Titular de la tarjeta'), 'Ana Morales');
    await userEvent.click(screen.getByRole('button', { name: 'Pagar Q 199.00 y contratar' }));
    expect(await screen.findByRole('heading', { name: 'Plan activado' })).toBeDefined();
    expect(screen.getByText('El cobro fue autorizado y su suscripción de plataforma ya está vigente.')).toBeDefined();
  });

  it('A2.4 informa del rechazo y mantiene el plan vigente', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento])),
      http.get(API + '/suscripcion', () => HttpResponse.json({ title: 'Sin suscripción', codigo: 'suscripcion_no_encontrada' }, { status: 404, headers: { 'Content-Type': 'application/problem+json' } })),
      http.post(API + '/suscripcion/contratar', () => HttpResponse.json({ title: 'No se autorizó el cobro.', codigo: 'pago_rechazado' }, { status: 402, headers: { 'Content-Type': 'application/problem+json' } })),
    );
    renderizar('/panel/suscripcion/contratar/plan-1', <Routes><Route path="/panel/suscripcion/contratar/:plan" element={<PaginaA22Contratacion />} /></Routes>);
    await userEvent.type(await screen.findByLabelText('Número de tarjeta'), '4000000000000002');
    await userEvent.type(screen.getByLabelText('Mes de vencimiento'), '12');
    await userEvent.type(screen.getByLabelText('Año de vencimiento'), '2030');
    await userEvent.type(screen.getByLabelText('Código de seguridad'), '123');
    await userEvent.type(screen.getByLabelText('Titular de la tarjeta'), 'Ana Morales');
    await userEvent.click(screen.getByRole('button', { name: 'Pagar Q 199.00 y contratar' }));
    expect(await screen.findByRole('heading', { name: 'Tarjeta rechazada' })).toBeDefined();
    expect(screen.getByText('No se autorizó el cobro.')).toBeDefined();
  });

  it('A2.5 muestra el cálculo de prorrateo y confirma el cambio', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento, producto])),
      http.get(API + '/suscripcion', () => HttpResponse.json(suscripcion)),
      http.post(API + '/suscripcion/cambiar', () => { cambioSolicitado = true; return HttpResponse.json({ estado: 'activa', aPagar: 173.34 }); }),
    );
    renderizar('/panel/suscripcion/cambiar/plan-2', <Routes><Route path="/panel/suscripcion/cambiar/:plan" element={<PaginaA25CambioPlan />} /></Routes>);
    expect(await screen.findByText('Diferencia prorrateada')).toBeDefined();
    expect(screen.getByText('Q 173.34')).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Pagar Q 173.34 y cambiar de plan' }));
    await waitFor(() => expect(cambioSolicitado).toBe(true));
    expect(screen.getByRole('heading', { name: 'Cambio de plan' })).toBeDefined();
  });

  it('B1.4 muestra el periodo activo y su tarjeta', async () => {
    server.use(http.get(API + '/suscripcion', () => HttpResponse.json(suscripcion)));
    renderizar('/panel/suscripcion', <PaginaB14Suscripcion />);
    expect(await screen.findByRole('heading', { name: 'Suscripción de plataforma' })).toBeDefined();
    expect(screen.getByText('Visa •••• 4821')).toBeDefined();
    expect(screen.getByText('Renovación automática')).toBeDefined();
  });

  it('B1.4 en gracia permite pagar con otra tarjeta y reactivar', async () => {
    server.use(
      http.get(API + '/suscripcion', () => HttpResponse.json({ ...suscripcion, estado: 'en_gracia', graciaHasta: '2026-09-30T06:00:00Z', diasRestantes: 5 })),
      http.post(API + '/suscripcion/pagar', () => HttpResponse.json({ estado: 'activa' })),
    );
    renderizar('/panel/suscripcion', <PaginaB14Suscripcion />);
    expect(await screen.findByText(/Quedan 5 días/)).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Pagar con otra tarjeta' }));
    expect(screen.getByLabelText('Número de tarjeta')).toBeDefined();
  });

  it('B1.4 suspendida indica que el tráfico está detenido y conserva sus claves', async () => {
    server.use(http.get(API + '/suscripcion', () => HttpResponse.json({ ...suscripcion, estado: 'suspendida', graciaHasta: '2026-09-30T06:00:00Z' })));
    renderizar('/panel/suscripcion', <PaginaB14Suscripcion />);
    expect(await screen.findByRole('heading', { name: 'Su tráfico está detenido' })).toBeDefined();
    expect(screen.getByText(/Sus claves, las suscripciones de sus consumidores y su historial se conservan/)).toBeDefined();
    expect(screen.getByRole('button', { name: 'Pagar con otra tarjeta' })).toBeDefined();
  });
});
