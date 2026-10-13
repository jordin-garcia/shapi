import { beforeEach, afterEach, expect, it } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import { crearRutas } from '../rutas';

let cache: QueryClient;
let peticiones: unknown[];
beforeEach(() => {
  cache = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  peticiones = [];
  server.use(http.post('http://localhost/api/auth/definir-contrasena', async ({ request }) => {
    expect(request.headers.get('X-Requested-With')).toBe('shapi');
    peticiones.push(await request.json());
    return new HttpResponse(null, { status: 200 });
  }));
});
afterEach(() => { cleanup(); cache.clear(); });
function abrir(ruta = '/definir-contrasena?token=abc') {
  const router = createMemoryRouter([...crearRutas({ desarrollo: false }), { path: '/destino-prueba', element: <h1>Sesión iniciada</h1> }], { initialEntries: [ruta] });
  render(<QueryClientProvider client={cache}><RouterProvider router={router} /></QueryClientProvider>);
}

it('RF-42 abre la ruta pública, muestra siete días y navega al destino de la sesión', async () => {
  server.use(http.get('http://localhost/api/auth/sesion', () => HttpResponse.json({ usuario: { nombre: 'Lucía', correo: 'lucia@shapi.test' }, organizacion: { id: 'org', nombre: 'Shapi' }, rol: 'soporte', correoVerificado: true, destino: '/destino-prueba' })));
  abrir();
  expect(await screen.findByText('7 días')).toBeDefined();
  await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'NuevaContrasena123!');
  await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));
  expect(await screen.findByRole('heading', { name: 'Sesión iniciada' })).toBeDefined();
  expect(peticiones).toEqual([{ token: 'abc', contrasena: 'NuevaContrasena123!' }]);
}, 15000);

it('RF-42 reintenta la consulta de sesión sin consumir otra vez el token', async () => {
  server.use(http.get('http://localhost/api/auth/sesion', () => new HttpResponse(null, { status: 503 })));
  abrir();
  await userEvent.type(await screen.findByLabelText('Contraseña nueva'), 'NuevaContrasena123!');
  await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));
  const reintentar = await screen.findByRole('button', { name: 'Reintentar' });
  server.use(http.get('http://localhost/api/auth/sesion', () => HttpResponse.json({ usuario: { nombre: 'Lucía', correo: 'lucia@shapi.test' }, organizacion: { id: 'org', nombre: 'Shapi' }, rol: 'soporte', correoVerificado: true, destino: '/destino-prueba' })));
  await userEvent.click(reintentar);
  expect(await screen.findByText('Sesión iniciada')).toBeDefined();
  expect(peticiones).toHaveLength(1);
});

it('RF-42 un enlace inválido pide contactar al administrador', async () => {
  server.use(http.post('http://localhost/api/auth/definir-contrasena', () => HttpResponse.json({ title: 'El enlace venció o ya se usó.', status: 422, codigo: 'token_invalido' }, { status: 422, headers: { 'Content-Type': 'application/problem+json' } })));
  abrir();
  await userEvent.type(await screen.findByLabelText('Contraseña nueva'), 'NuevaContrasena123!');
  await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));
  expect(await screen.findByText('El enlace ya no sirve')).toBeDefined();
  expect(screen.getByText(/Comuníquese con el administrador de Shapi/)).toBeDefined();
});

it('RF-42 una contraseña rechazada muestra el error junto al campo', async () => {
  server.use(http.post('http://localhost/api/auth/definir-contrasena', () => HttpResponse.json({ title: 'Revise la contraseña.', status: 400, codigo: 'datos_invalidos', errores: { contrasena: ['Debe tener entre 10 y 128 caracteres.'] } }, { status: 400, headers: { 'Content-Type': 'application/problem+json' } })));
  abrir();
  await userEvent.type(await screen.findByLabelText('Contraseña nueva'), 'corta');
  await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));
  expect(await screen.findByText('Debe tener entre 10 y 128 caracteres.')).toBeDefined();
});
