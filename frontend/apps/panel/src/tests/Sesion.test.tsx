import { afterEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import type { components } from '@shapi/api/identidad';
import { server } from '../../../../test/servidor';
import { claveSesion, consultarSesion } from '../modulos/sesion/useSesion';
import { CerrarSesion } from '../modulos/sesion/CerrarSesion';

// Las respuestas simuladas usan el esquema generado de contratos/openapi/identidad.yaml: si el contrato cambia,
// el typecheck falla aquí (EM-17).
type Sesion = components['schemas']['Sesion'];

const sesion: Sesion = {
  usuario: { nombre: 'Ana', correo: 'ana@enviosxelaju.com' },
  organizacion: { id: '0b8f7c3e-6d1a-4f2b-9c5e-1a2b3c4d5e6f', nombre: 'Envíos Xelajú, S.A.' },
  rol: 'editor',
  correoVerificado: false,
  destino: '/panel/apis',
};

afterEach(cleanup);

describe('RF-04 · sesión con el contrato generado de identidad', () => {
  it('RF-04 con sesión activa, adapta usuario, organización, rol, correoVerificado y destino', async () => {
    server.use(http.get('http://localhost/api/auth/sesion', () => HttpResponse.json<Sesion>(sesion)));
    await expect(consultarSesion()).resolves.toEqual({
      nombre: 'Ana',
      correo: 'ana@enviosxelaju.com',
      rol: 'editor',
      nombreOrganizacion: 'Envíos Xelajú, S.A.',
      organizacionId: '0b8f7c3e-6d1a-4f2b-9c5e-1a2b3c4d5e6f',
      correoVerificado: false,
      destino: '/panel/apis',
    });
  });

  it('RF-04 sin sesión, el 401 del contrato (ProblemDetails sin codigo) devuelve null', async () => {
    server.use(http.get('http://localhost/api/auth/sesion', () => HttpResponse.json(
      { title: 'Unauthorized', status: 401 },
      { status: 401, headers: { 'Content-Type': 'application/problem+json' } })));
    await expect(consultarSesion()).resolves.toBeNull();
  });

  it('RF-04 un error del servidor no se confunde con la falta de sesión', async () => {
    server.use(http.get('http://localhost/api/auth/sesion', () => new HttpResponse(null, { status: 500 })));
    await expect(consultarSesion()).rejects.toThrow();
  });
});

describe('RF-07 · cierre de sesión con el contrato generado de identidad', () => {
  it('RF-07 envía la cabecera CSRF, lleva a /entrar y borra los datos de la sesión', async () => {
    let cabecera: string | null = null;
    server.use(http.post('http://localhost/api/auth/salir', ({ request }) => {
      cabecera = request.headers.get('X-Requested-With');
      return new HttpResponse(null, { status: 200 });
    }));
    const cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
    cliente.setQueryData(claveSesion, sesion);
    render(<QueryClientProvider client={cliente}>
      <MemoryRouter initialEntries={['/panel/apis']}>
        <Routes>
          <Route path="/panel/apis" element={<CerrarSesion />} />
          <Route path="/entrar" element={<h1>Entrar a Shapi</h1>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>);

    await userEvent.click(screen.getByRole('button', { name: 'Cerrar sesión' }));

    expect(await screen.findByRole('heading', { name: 'Entrar a Shapi' })).toBeDefined();
    expect(cabecera).toBe('shapi');
    await waitFor(() => expect(cliente.getQueryData(claveSesion)).toBeUndefined());
  });
});

describe('H-84 · el panel no conserva un contrato de sesión provisional', () => {
  const fuentes = import.meta.glob<string>('../modulos/sesion/*.{ts,tsx}', { query: '?raw', import: 'default', eager: true });

  it('no existe contratoSesion.ts y el módulo de sesión toma sus tipos de @shapi/api/identidad', () => {
    expect(Object.keys(fuentes)).not.toContain('../modulos/sesion/contratoSesion.ts');
    for (const archivo of ['../modulos/sesion/useSesion.ts', '../modulos/sesion/CerrarSesion.tsx']) {
      expect(fuentes[archivo]).toContain("from '@shapi/api/identidad'");
    }
  });
});
