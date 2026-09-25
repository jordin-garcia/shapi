import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { RouterProvider } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { router } from '../rutas';
import * as sesionHook from '../modulos/sesion/useSesion';

vi.mock('../modulos/sesion/useSesion');

const renderWithRouter = () => {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  );
};

describe('Router y Guardias', () => {
  it('Redirige a /entrar si no hay sesion en /panel', async () => {
    // RF-07
    vi.mocked(sesionHook.useSesion).mockReturnValue({
      data: null,
      isLoading: false,
      error: new Error('No auth'),
    } as any);

    router.navigate('/panel/apis');
    renderWithRouter();

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/entrar');
    });
  });

  it('Muestra 403 si el rol no es adecuado (ej. admin en panel)', async () => {
    vi.mocked(sesionHook.useSesion).mockReturnValue({
      data: { nombre: 'Admin', rol: 'administrador', organizacionId: '123' },
      isLoading: false,
      error: null,
    } as any);

    router.navigate('/panel/apis');
    renderWithRouter();

    expect(await screen.findByText(/No tiene permiso para ver esta página/i)).toBeDefined();
  });

  it('Permite al propietario entrar a /panel/apis', async () => {
    vi.mocked(sesionHook.useSesion).mockReturnValue({
      data: { nombre: 'User', rol: 'propietario', organizacionId: '123' },
      isLoading: false,
      error: null,
    } as any);

    router.navigate('/panel/apis');
    renderWithRouter();

    expect(await screen.findByText(/Pantalla pendiente/i)).toBeDefined();
    expect(await screen.findByText(/A3-1/i)).toBeDefined();
  });

  it('Carga una ruta publica sin requerir sesion', async () => {
    vi.mocked(sesionHook.useSesion).mockReturnValue({
      data: null,
      isLoading: false,
      error: null,
    } as any);

    router.navigate('/registro');
    renderWithRouter();

    expect(await screen.findByText(/Pantalla pendiente/i)).toBeDefined();
    expect(await screen.findByText(/A1-1/i)).toBeDefined();
  });
});
