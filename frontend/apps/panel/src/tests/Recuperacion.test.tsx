import { StrictMode } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RouterProvider } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { router } from '../rutas';
import { server } from '../../../../test/servidor';

const API = 'http://localhost/api/auth';

let cliente: QueryClient;
let conSesion = false;
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
  conSesion = false;
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  server.use(
    http.get(`${API}/sesion`, () => conSesion ? HttpResponse.json({
      usuario: { nombre: 'Ana', correo: 'ana@enviosxelaju.com' },
      organizacion: { id: 'org-1', nombre: 'Envíos Xelajú, S.A.' },
      rol: 'propietario', correoVerificado: true, destino: '/panel/apis'
    }) : new HttpResponse(null, { status: 401 })),
  );
});
afterEach(() => { cleanup(); cliente.clear(); });

describe('RF-03 · A1.4a Recuperación', () => {
  it('muestra el formulario inicial', async () => {
    await abrir('/recuperar');
    expect(await screen.findByRole('heading', { name: 'Recuperar la contraseña' })).toBeDefined();
    expect(screen.getByLabelText('Correo electrónico')).toBeDefined();
    expect(screen.getByRole('button', { name: 'Enviar el enlace' })).toBeDefined();
  });

  it('envía la petición con CSRF y muestra estado enviado', async () => {
    server.use(http.post(`${API}/recuperar`, async ({ request }) => { await registrar(request, 'recuperar'); return new HttpResponse(null, { status: 200 }); }));
    await abrir('/recuperar');
    await userEvent.type(await screen.findByLabelText('Correo electrónico'), 'ana@enviosxelaju.com');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar el enlace' }));
    expect(await screen.findByRole('heading', { name: 'Revise su correo' })).toBeDefined();
    expect(screen.getByText(/^Si el correo existe, le enviamos un enlace para definir una contraseña nueva\./)).toBeDefined();
    expect(screen.getByText('Recuperación · revise su correo')).toBeDefined();
    expect(peticiones).toEqual([{ ruta: 'recuperar', csrf: 'shapi', cuerpo: { correo: 'ana@enviosxelaju.com' } }]);
  });
});

describe('RF-03 · A1.4b Nueva contraseña', () => {
  it('reintenta la sesión tras un fallo sin consumir otra vez el token', async () => {
    server.use(http.post(`${API}/restablecer`, async ({ request }) => {
      await registrar(request, 'restablecer');
      server.use(http.get(`${API}/sesion`, () => new HttpResponse(null, { status: 503 })));
      return new HttpResponse(null, { status: 200 });
    }));
    await abrir('/restablecer?token=abc');
    await userEvent.type(await screen.findByLabelText('Contraseña nueva'), 'NuevaContra123');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));
    const reintentar = await screen.findByRole('button', { name: 'Reintentar' });
    server.use(http.get(`${API}/sesion`, () => HttpResponse.json({
      usuario: { nombre: 'Ana', correo: 'ana@enviosxelaju.com' },
      organizacion: { id: 'org-1', nombre: 'Envíos Xelajú, S.A.' },
      rol: 'propietario', correoVerificado: true, destino: '/panel/apis'
    })));
    await userEvent.click(reintentar);
    await waitFor(() => expect(router.state.location.pathname).toBe('/panel/apis'));
    expect(peticiones).toEqual([{ ruta: 'restablecer', csrf: 'shapi', cuerpo: { token: 'abc', contrasena: 'NuevaContra123' } }]);
  });

  it('ofrece solicitar otro enlace si el token venció o ya se usó', async () => {
    server.use(http.post(`${API}/restablecer`, () => HttpResponse.json({
      status: 422, codigo: 'token_invalido', title: 'El enlace no es válido.'
    }, { status: 422, headers: { 'Content-Type': 'application/problem+json' } })));
    await abrir('/restablecer?token=vencido');
    await userEvent.type(await screen.findByLabelText('Contraseña nueva'), 'NuevaContra123');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));
    expect(await screen.findByRole('heading', { name: 'El enlace ya no sirve' })).toBeDefined();
    await userEvent.click(screen.getByRole('link', { name: 'Solicitar un enlace nuevo' }));
    expect(await screen.findByRole('heading', { name: 'Recuperar la contraseña' })).toBeDefined();
    expect(router.state.location.pathname).toBe('/recuperar');
  });

  it('muestra el error de contraseña y permite corregirla sin perder el token', async () => {
    server.use(http.post(`${API}/restablecer`, async ({ request }) => {
      await registrar(request, 'restablecer');
      return HttpResponse.json({
        status: 400, codigo: 'datos_invalidos', title: 'Revise los datos del formulario.',
        errores: { contrasena: ['La contraseña debe tener al menos 10 caracteres.'] }
      }, { status: 400, headers: { 'Content-Type': 'application/problem+json' } });
    }));
    await abrir('/restablecer?token=abc');
    await userEvent.type(await screen.findByLabelText('Contraseña nueva'), 'corta');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));
    expect(await screen.findByText('La contraseña debe tener al menos 10 caracteres.')).toBeDefined();
    expect(router.state.location.search).toBe('?token=abc');
    server.use(http.post(`${API}/restablecer`, async ({ request }) => {
      await registrar(request, 'restablecer');
      conSesion = true;
      return new HttpResponse(null, { status: 200 });
    }));
    await userEvent.clear(screen.getByLabelText('Contraseña nueva'));
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'NuevaContra123');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/panel/apis'));
    expect(peticiones.at(-1)?.cuerpo).toEqual({ token: 'abc', contrasena: 'NuevaContra123' });
  });

  it('muestra el formulario', async () => {
    await abrir('/restablecer?token=abc');
    expect(await screen.findByRole('heading', { name: 'Definir la contraseña' })).toBeDefined();
    expect(screen.getByLabelText('Contraseña nueva')).toBeDefined();
  });

  it('envía token y contraseña y redirige a entrar', async () => {
    server.use(http.post(`${API}/restablecer`, async ({ request }) => { 
      await registrar(request, 'restablecer'); 
      conSesion = true;
      return new HttpResponse(null, { status: 200 }); 
    }));
    await abrir('/restablecer?token=abc');
    await userEvent.type(await screen.findByLabelText('Contraseña nueva'), 'NuevaContra123');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/panel/apis'));
    expect(peticiones).toEqual([{ ruta: 'restablecer', csrf: 'shapi', cuerpo: { token: 'abc', contrasena: 'NuevaContra123' } }]);
  });
});
