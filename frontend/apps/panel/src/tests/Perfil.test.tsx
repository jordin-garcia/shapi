import { StrictMode } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor, within } from '@testing-library/react';
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
    http.get(`${API}/perfil`, () => HttpResponse.json({ nombre: 'Ana' })),
    http.get(`${API}/apis`, () => HttpResponse.json({ elementos: [{ id: 'api-1', nombre: 'API de Cotización de Envíos' }], total: 1 })),
    http.get(`${API}/apis/:id/planes`, () => HttpResponse.json([]))
  );
});
afterEach(() => { cleanup(); cliente.clear(); });

const problema = (estado: number, codigo: string, titulo: string, errores?: Record<string, string[]>) =>
  HttpResponse.json({ type: 'about:blank', title: titulo, status: estado, codigo, ...(errores ? { errores } : {}) },
    { status: estado, headers: { 'Content-Type': 'application/problem+json' } });

describe('RF-04 · A8.1 Perfil', () => {
  it('muestra el formulario con el perfil actual', async () => {
    await abrir('/panel/perfil');
    expect(await screen.findByRole('heading', { name: 'Mi perfil' })).toBeDefined();
    expect(screen.getByDisplayValue('Ana')).toBeDefined();
    expect(screen.getByText('ana@enviosxelaju.com')).toBeDefined();
    expect(screen.getByRole('button', { name: 'Guardar cambios' })).toBeDefined();
  });

  it('muestra la organización, el rol y «Cerrar sesión» (auditoría 2026-10-03, H-21)', async () => {
    await abrir('/panel/perfil');
    // El layout también muestra la organización y el rol: se buscan dentro de la tarjeta «Datos personales».
    const tarjeta = within((await screen.findByRole('heading', { name: 'Datos personales' })).parentElement!);
    expect(within(tarjeta.getByText('Organización').parentElement!).getByText('Envíos Xelajú, S.A.')).toBeDefined();
    expect(within(tarjeta.getByText('Rol').parentElement!).getByText('Propietario')).toBeDefined();
    // La barra lateral también tiene «Cerrar sesión»: el de A8.1 está junto al aviso del vencimiento.
    const pie = within(screen.getByText('Su sesión vence tras 8 horas sin actividad.').parentElement!);
    expect(pie.getByRole('button', { name: 'Cerrar sesión' })).toBeDefined();
  });

  it('en la administración también se ve en /admin/perfil (criterio 5)', async () => {
    server.use(http.get(`${API}/auth/sesion`, () => HttpResponse.json({
      usuario: { nombre: 'Rodrigo', correo: 'rodrigo.alvarado@shapi.localhost' },
      organizacion: { id: 'org-p', nombre: 'Plataforma Shapi' },
      rol: 'administrador', correoVerificado: true, destino: '/admin/organizaciones'
    })));
    await abrir('/admin/perfil');
    expect(await screen.findByRole('heading', { name: 'Mi perfil' })).toBeDefined();
    expect(screen.getByDisplayValue('Rodrigo')).toBeDefined();
    const tarjeta = within(screen.getByRole('heading', { name: 'Datos personales' }).parentElement!);
    expect(within(tarjeta.getByText('Rol').parentElement!).getByText('Administrador')).toBeDefined();
  });

  it('al guardar el nombre avisa con un aviso breve (11 §4)', async () => {
    server.use(http.put(`${API}/perfil`, () => new HttpResponse(null, { status: 200 })));
    await abrir('/panel/perfil');
    await userEvent.click(await screen.findByRole('button', { name: 'Guardar cambios' }));
    expect((await screen.findByRole('status')).textContent).toBe('Nombre actualizado.');
  });

  it('muestra el error del nombre debajo del campo', async () => {
    server.use(http.put(`${API}/perfil`, () => problema(400, 'datos_invalidos', 'El nombre es obligatorio.', { nombre: ['Escriba su nombre.'] })));
    await abrir('/panel/perfil');
    await userEvent.clear(await screen.findByLabelText('Nombre'));
    await userEvent.click(screen.getByRole('button', { name: 'Guardar cambios' }));
    expect(await screen.findByText('Escriba su nombre.')).toBeDefined();
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('con la contraseña actual incorrecta muestra el error debajo del campo', async () => {
    server.use(http.post(`${API}/perfil/contrasena`, () =>
      problema(400, 'datos_invalidos', 'Revise los datos del formulario.', { contrasenaActual: ['La contraseña actual no es correcta.'] })));
    await abrir('/panel/perfil');
    await userEvent.type(await screen.findByLabelText('Contraseña actual'), 'Incorrecta123');
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'NuevaContra123');
    await userEvent.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));
    expect(await screen.findByText('La contraseña actual no es correcta.')).toBeDefined();
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('si falla la red ofrece «Reintentar» y vuelve a enviar', async () => {
    let intentos = 0;
    server.use(http.put(`${API}/perfil`, () => {
      intentos++;
      return intentos === 1 ? HttpResponse.error() : new HttpResponse(null, { status: 200 });
    }));
    await abrir('/panel/perfil');
    await userEvent.click(await screen.findByRole('button', { name: 'Guardar cambios' }));
    expect((await screen.findByRole('alert')).textContent).toContain('No se pudo completar la solicitud.');
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }));
    expect((await screen.findByRole('status')).textContent).toBe('Nombre actualizado.');
    expect(intentos).toBe(2);
  });

  it('permite cambiar el nombre', async () => {
    server.use(http.put(`${API}/perfil`, async ({ request }) => { await registrar(request, 'perfil'); return new HttpResponse(null, { status: 200 }); }));
    await abrir('/panel/perfil');
    const input = await screen.findByLabelText('Nombre');
    await userEvent.clear(input);
    await userEvent.type(input, 'Ana Lucía');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar cambios' }));
    await waitFor(() => expect(peticiones).toEqual([{ ruta: 'perfil', csrf: 'shapi', cuerpo: { nombre: 'Ana Lucía' } }]));
  });

  it('permite cambiar la contraseña', async () => {
    server.use(http.post(`${API}/perfil/contrasena`, async ({ request }) => { await registrar(request, 'contrasena'); return new HttpResponse(null, { status: 200 }); }));
    await abrir('/panel/perfil');
    await userEvent.type(await screen.findByLabelText('Contraseña actual'), 'ContraActual123');
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'NuevaContra123');
    await userEvent.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));
    await waitFor(() => expect(peticiones).toEqual([{ ruta: 'contrasena', csrf: 'shapi', cuerpo: { contrasenaActual: 'ContraActual123', contrasenaNueva: 'NuevaContra123' } }]));
    expect((await screen.findByRole('status')).textContent).toBe('Contraseña actualizada. Se cerraron sus sesiones en otros equipos.');
    expect((screen.getByLabelText('Contraseña actual') as HTMLInputElement).value).toBe('');
  });
});
