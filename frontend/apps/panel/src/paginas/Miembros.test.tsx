import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { setupServer } from 'msw/node';
import { MemoryRouter, Route, Routes } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import PaginaA42Miembros from './A4-2-Miembros';
import PaginaA82Invitacion from './A8-2-Invitacion';

const propietario = { id: 'm-1', usuarioId: 'u-1', nombre: 'Ana Lucía Morales', correo: 'ana@ejemplo.com', rol: 'propietario', esActual: true };
const editor = { id: 'm-2', usuarioId: 'u-2', nombre: 'Diego Us Pérez', correo: 'diego@ejemplo.com', rol: 'editor', esActual: false };
const miembro = { id: 'm-3', usuarioId: 'u-3', nombre: 'Karla Batres', correo: 'karla@ejemplo.com', rol: 'lector', esActual: false };
let datos = {
  elementos: [propietario, editor, miembro], total: 3, invitacionesPendientes: 0,
  organizacion: 'Envíos Xelajú, S.A.', usuarioActualId: 'u-1',
  plan: { nombre: 'Producto', maxMiembros: 10 },
};
let invitacion = {
  organizacion: 'Envíos Xelajú, S.A.', nombrePropietario: 'Ana Lucía Morales', correo: 'diego.us@enviosxelaju.com',
  rol: 'editor', expiraEn: '2026-09-15T06:00:00Z',
};
let cuerpoInvitacion: unknown;
let cuerpoAceptacion: unknown;
let rolActualizado: string | undefined;
let miembroQuitado: string | undefined;
let invitacionNoExiste = false;

const servidor = setupServer(
  http.get('http://localhost/api/miembros', () => HttpResponse.json(datos)),
  http.post('http://localhost/api/miembros/invitaciones', async ({ request }) => {
    cuerpoInvitacion = await request.json();
    datos = { ...datos, total: datos.total + 1, invitacionesPendientes: datos.invitacionesPendientes + 1 };
    return new HttpResponse(null, { status: 202 });
  }),
  http.put('http://localhost/api/miembros/:id/rol', async ({ params, request }) => {
    rolActualizado = `${params.id}:${((await request.json()) as { rol: string }).rol}`;
    return HttpResponse.json({});
  }),
  http.delete('http://localhost/api/miembros/:id', ({ params }) => {
    miembroQuitado = String(params.id);
    datos = { ...datos, elementos: datos.elementos.filter(fila => fila.id !== miembroQuitado), total: datos.total - 1 };
    return new HttpResponse(null, { status: 204 });
  }),
  http.get('http://localhost/api/invitaciones/:token', () => invitacionNoExiste
    ? HttpResponse.json({ type: 'about:blank', title: 'No existe', status: 404, codigo: 'invitacion_no_encontrada' }, { status: 404, headers: { 'content-type': 'application/problem+json' } })
    : HttpResponse.json(invitacion)),
  http.post('http://localhost/api/invitaciones/:token/aceptar', async ({ request }) => {
    cuerpoAceptacion = await request.json();
    return new HttpResponse(null, { status: 201 });
  }),
);

beforeAll(() => servidor.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
  servidor.resetHandlers();
  datos = { elementos: [propietario, editor, miembro], total: 3, invitacionesPendientes: 0, organizacion: 'Envíos Xelajú, S.A.', usuarioActualId: 'u-1', plan: { nombre: 'Producto', maxMiembros: 10 } };
  invitacion = { organizacion: 'Envíos Xelajú, S.A.', nombrePropietario: 'Ana Lucía Morales', correo: 'diego.us@enviosxelaju.com', rol: 'editor', expiraEn: '2026-09-15T06:00:00Z' };
  cuerpoInvitacion = undefined;
  cuerpoAceptacion = undefined;
  rolActualizado = undefined;
  miembroQuitado = undefined;
  invitacionNoExiste = false;
});
afterAll(() => servidor.close());

function renderMiembros() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(<QueryClientProvider client={queryClient}><MemoryRouter initialEntries={['/panel/miembros']}>
    <Routes><Route path="/panel/miembros" element={<PaginaA42Miembros />} /></Routes>
  </MemoryRouter></QueryClientProvider>);
}

function renderInvitacion() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(<QueryClientProvider client={queryClient}><MemoryRouter initialEntries={['/invitacion?token=token-demo']}>
    <Routes><Route path="/invitacion" element={<PaginaA82Invitacion />} /></Routes>
  </MemoryRouter></QueryClientProvider>);
}

describe('A4.2 · Miembros y roles (RF-06, RF-43)', () => {
  it('muestra el estado vacío y envía la invitación con correo y rol', async () => {
    datos = { ...datos, elementos: [propietario], total: 1 };
    const user = userEvent.setup();
    renderMiembros();
    expect(await screen.findByText('Todavía no ha invitado a nadie')).toBeDefined();
    expect(screen.getByText(/Su plan Producto permite 10 miembros; tiene 1\./)).toBeDefined();
    await user.type(screen.getByLabelText('Correo'), 'nueva@ejemplo.com');
    await user.selectOptions(screen.getByLabelText('Rol'), 'lector');
    await user.click(screen.getByRole('button', { name: 'Enviar invitación' }));
    await waitFor(() => expect(cuerpoInvitacion).toEqual({ correo: 'nueva@ejemplo.com', rol: 'lector' }));
    await waitFor(() => expect(screen.getByText(/Su plan Producto permite 10 miembros; tiene 2\./)).toBeDefined());
  });

  it('cambia el rol y pide confirmación antes de quitar al miembro', async () => {
    const user = userEvent.setup();
    renderMiembros();
    await user.selectOptions(await screen.findByLabelText('Rol de Diego Us Pérez'), 'lector');
    await waitFor(() => expect(rolActualizado).toBe('m-2:lector'));
    await user.click(screen.getAllByRole('button', { name: 'Quitar' })[0]);
    expect(screen.getByRole('heading', { name: 'Quitar a Diego Us Pérez' })).toBeDefined();
    expect(screen.getByText('Diego Us Pérez dejará de tener acceso a Envíos Xelajú, S.A. en Shapi.')).toBeDefined();
    await user.click(screen.getByRole('button', { name: 'Quitar miembro' }));
    await waitFor(() => expect(miembroQuitado).toBe('m-2'));
    expect(await screen.findByText('Miembros y roles')).toBeDefined();
  });
});

describe('A8.2 · Aceptar la invitación (RF-06)', () => {
  it('muestra los datos de la invitación y crea la cuenta verificada', async () => {
    const user = userEvent.setup();
    renderInvitacion();
    expect(await screen.findByRole('heading', { name: 'Unirse a Envíos Xelajú, S.A.' })).toBeDefined();
    expect(screen.getByText(/Ana Lucía Morales lo invitó/)).toBeDefined();
    expect(screen.getByLabelText('Correo electrónico')).toHaveProperty('value', invitacion.correo);
    await user.type(screen.getByLabelText('Nombre'), 'Diego Us Pérez');
    await user.type(screen.getByLabelText('Contraseña'), 'UnaContrasenaSegura123!');
    await user.click(screen.getByRole('button', { name: 'Aceptar la invitación' }));
    await waitFor(() => expect(cuerpoAceptacion).toEqual({ nombre: 'Diego Us Pérez', contrasena: 'UnaContrasenaSegura123!' }));
    expect(await screen.findByRole('heading', { name: 'Ya es parte de la organización' })).toBeDefined();
    expect(screen.getByRole('link', { name: 'Entrar a Shapi' }).getAttribute('href')).toBe('/entrar');
  });

  it('explica cuando la invitación ya venció o se usó', async () => {
    invitacionNoExiste = true;
    renderInvitacion();
    expect(await screen.findByRole('heading', { name: 'El enlace ya no sirve' })).toBeDefined();
    expect(screen.getByText('La invitación venció, ya se usó o no pertenece a este portal.')).toBeDefined();
  });
});
