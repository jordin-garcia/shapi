import { afterEach, beforeEach, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import Cuentas from '../paginas/A6-5-Cuentas';

const API = 'http://localhost/api/admin/cuentas';
let cache: QueryClient;
let cuentas: { id: string; nombre: string; correo: string; rol: string; estado: string }[];
beforeEach(() => {
  cache = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  cuentas = [
    { id: 'admin', nombre: 'Rodrigo Alvarado', correo: 'rodrigo.alvarado@shapi.localhost', rol: 'administrador', estado: 'activa' },
    { id: 'lucia', nombre: 'Lucía Ramírez Pineda', correo: 'lucia.ramirez@shapi.localhost', rol: 'administrador', estado: 'activa' },
    { id: 'sofia', nombre: 'Sofía Menchú Cojtí', correo: 'sofia.menchu@shapi.localhost', rol: 'soporte', estado: 'activa' },
    { id: 'julio', nombre: 'Julio Estrada Ixcot', correo: 'julio.estrada@shapi.localhost', rol: 'soporte', estado: 'desactivada' },
  ];
  server.use(
    http.get('http://localhost/api/auth/sesion', () => HttpResponse.json({ usuario: { nombre: 'Rodrigo Alvarado', correo: cuentas[0].correo }, organizacion: { id: 'org', nombre: 'Shapi' }, rol: 'administrador', correoVerificado: true, destino: '/admin/organizaciones' })),
    http.get(API, () => HttpResponse.json(cuentas)),
  );
});
afterEach(() => { cleanup(); cache.clear(); });
function abrir() {
  render(<QueryClientProvider client={cache}><RouterProvider router={createMemoryRouter([{ path: '/', element: <Cuentas /> }])} /></QueryClientProvider>);
}

it('RF-42 reproduce A6.5 y oculta la desactivación propia', async () => {
  abrir();
  expect(await screen.findByText('Lucía Ramírez Pineda')).toBeDefined();
  expect(screen.getByText('Sistema')).toBeDefined();
  expect(screen.getByText('Sofía Menchú Cojtí')).toBeDefined();
  expect(screen.getByText('Julio Estrada Ixcot')).toBeDefined();
  expect(await screen.findByText('(usted)')).toBeDefined();
  expect(screen.queryByRole('button', { name: 'Desactivar Rodrigo Alvarado' })).toBeNull();
  expect(screen.getByRole('button', { name: 'Activar Julio Estrada Ixcot' })).toBeDefined();
  expect(screen.getByText('Personal de Shapi con acceso a la plataforma y el rol de cada persona.')).toBeDefined();
});

it('RF-42 muestra la variante vacía cuando solo está la cuenta inicial', async () => {
  cuentas = cuentas.slice(0, 1);
  abrir();
  expect(await screen.findByText('Todavía no ha creado ninguna cuenta')).toBeDefined();
  expect(screen.getByText('Su cuenta se creó en la siembra inicial del sistema. Las cuentas de administración y de soporte que cree aparecerán aquí con su rol.')).toBeDefined();
});

it('RF-42 crea la cuenta con CSRF y refresca la lista', async () => {
  let cuerpo: unknown;
  server.use(http.post(API, async ({ request }) => {
    cuerpo = await request.json();
    expect(request.headers.get('X-Requested-With')).toBe('shapi');
    cuentas.push({ id: 'nueva', nombre: 'Nueva Persona', correo: 'nueva@test.com', rol: 'soporte', estado: 'activa' });
    return HttpResponse.json(cuentas.at(-1), { status: 201 });
  }));
  abrir();
  await userEvent.type(await screen.findByLabelText('Nombre'), 'Nueva Persona');
  await userEvent.type(screen.getByLabelText('Correo'), 'nueva@test.com');
  await userEvent.click(screen.getByRole('button', { name: 'Crear cuenta' }));
  expect(await screen.findByText('Nueva Persona')).toBeDefined();
  expect(cuerpo).toEqual({ nombre: 'Nueva Persona', correo: 'nueva@test.com', rol: 'soporte' });
});

it('RF-42 desactiva y activa mediante sus endpoints', async () => {
  const acciones: string[] = [];
  server.use(http.post(`${API}/:id/:accion`, ({ params, request }) => {
    expect(request.headers.get('X-Requested-With')).toBe('shapi');
    acciones.push(`${params.id}/${params.accion}`);
    cuentas.find(c => c.id === params.id)!.estado = params.accion === 'activar' ? 'activa' : 'desactivada';
    return new HttpResponse(null, { status: 204 });
  }));
  abrir();
  await userEvent.click(await screen.findByRole('button', { name: 'Desactivar Lucía Ramírez Pineda' }));
  await userEvent.click(await screen.findByRole('button', { name: 'Activar Lucía Ramírez Pineda' }));
  await waitFor(() => expect(acciones).toEqual(['lucia/desactivar', 'lucia/activar']));
});

it('RF-42 presenta errores por campo y permite reintentar la consulta', async () => {
  server.use(http.get(API, () => new HttpResponse(null, { status: 503 })));
  abrir();
  const boton = await screen.findByRole('button', { name: 'Reintentar' });
  server.use(http.get(API, () => HttpResponse.json(cuentas)), http.post(API, () => HttpResponse.json({ title: 'Revise los datos.', status: 400, codigo: 'datos_invalidos', errores: { correo: ['El correo ya está registrado.'] } }, { status: 400, headers: { 'Content-Type': 'application/problem+json' } })));
  await userEvent.click(boton);
  await screen.findByText('Lucía Ramírez Pineda');
  await userEvent.type(screen.getByLabelText('Nombre'), 'Otra');
  await userEvent.type(screen.getByLabelText('Correo'), 'otra@test.com');
  await userEvent.click(screen.getByRole('button', { name: 'Crear cuenta' }));
  expect(await screen.findByText('El correo ya está registrado.')).toBeDefined();
});
