import { StrictMode } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RouterProvider } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { router } from '../rutas';
import { server } from '../../../../test/servidor';

const API = 'http://localhost/api';

let cliente: QueryClient;
const peticiones: { ruta: string; cuerpo: unknown; csrf: string | null }[] = [];

async function registrar(request: Request, ruta: string) {
  peticiones.push({ ruta, cuerpo: await request.clone().json().catch(() => null), csrf: request.headers.get('X-Requested-With') });
}

async function abrir(ruta: string) {
  await router.navigate(ruta);
  render(<StrictMode><QueryClientProvider client={cliente}><RouterProvider router={router} /></QueryClientProvider></StrictMode>);
}

beforeEach(() => {
  peticiones.length = 0;
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  server.use(
    http.get(`${API}/auth/sesion`, () => HttpResponse.json({
        usuario: { nombre: 'Ana', correo: 'ana@enviosxelaju.com' },
        organizacion: { id: 'org-1', nombre: 'Envíos Xelajú, S.A.' },
        rol: 'propietario', correoVerificado: true, destino: '/panel/apis'
    })),
    http.get(`${API}/perfil`, () => HttpResponse.json({
      nombre: 'Ana', correo: 'ana@enviosxelaju.com'
    }))
  );
});
afterEach(() => { cleanup(); cliente.clear(); });

describe('RF-04 · A8.1 Perfil', () => {
  it('muestra el formulario con el perfil actual', async () => {
    await abrir('/panel/perfil');
    expect(await screen.findByRole('heading', { name: 'Mi perfil' })).toBeDefined();
    expect(screen.getByDisplayValue('Ana')).toBeDefined();
    expect(screen.getByDisplayValue('ana@enviosxelaju.com')).toBeDefined();
    expect(screen.getByRole('button', { name: 'Actualizar nombre' })).toBeDefined();
  });

  it('permite cambiar el nombre', async () => {
    server.use(http.put(`${API}/perfil`, async ({ request }) => { await registrar(request, 'perfil'); return new HttpResponse(null, { status: 200 }); }));
    await abrir('/panel/perfil');
    const input = await screen.findByLabelText('Nombre');
    await userEvent.clear(input);
    await userEvent.type(input, 'Ana Lucía');
    await userEvent.click(screen.getByRole('button', { name: 'Actualizar nombre' }));
    await waitFor(() => expect(peticiones).toEqual([{ ruta: 'perfil', csrf: 'shapi', cuerpo: { nombre: 'Ana Lucía' } }]));
  });

  it('permite cambiar la contraseña', async () => {
    server.use(http.post(`${API}/perfil/contrasena`, async ({ request }) => { await registrar(request, 'contrasena'); return new HttpResponse(null, { status: 200 }); }));
    await abrir('/panel/perfil');
    await userEvent.type(await screen.findByLabelText('Contraseña actual'), 'ContraActual123');
    await userEvent.type(screen.getByLabelText('Nueva contraseña'), 'NuevaContra123');
    await userEvent.click(screen.getByRole('button', { name: 'Actualizar contraseña' }));
    await waitFor(() => expect(peticiones).toEqual([{ ruta: 'contrasena', csrf: 'shapi', cuerpo: { actual: 'ContraActual123', nueva: 'NuevaContra123' } }]));
  });
});
