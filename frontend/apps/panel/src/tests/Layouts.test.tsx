import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import { describe, it, expect, vi, afterEach, beforeEach } from 'vitest';
import { render, screen, cleanup } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { LayoutPublico } from '../layouts/LayoutPublico';
import { LayoutPanel } from '../layouts/LayoutPanel';
import { LayoutAdmin } from '../layouts/LayoutAdmin';
import * as sesionHook from '../modulos/sesion/useSesion';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../modulos/sesion/useSesion');

beforeEach(() => server.use(http.get('http://localhost/api/apis', () => HttpResponse.json({ elementos: [{ id: 'api-1', nombre: 'Envíos Xelajú' }], total: 1 }))));
afterEach(cleanup);

const renderWithLayout = (Layout: React.ComponentType) => {
  const queryClient = new QueryClient();
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/panel/apis/api-1/planes']}>
        <Routes>
          <Route path="/panel/apis/:id/*" element={<Layout />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
};

describe('RNF-12 / RF-07 · Layouts - Barras Laterales', () => {
  it('encabezado público conserva marca y superficie oscura de A1', () => {
    renderWithLayout(LayoutPublico);
    expect(screen.getByText('Shapi')).toBeDefined();
    expect(screen.getByRole('banner').className).toContain('bg-[#060910]');
    expect(screen.getByRole('banner').querySelector('svg')).not.toBeNull();
  });
  it('LayoutPanel muestra los grupos correctos', () => {
    vi.mocked(sesionHook.useSesion).mockReturnValue({
      data: { nombre: 'User', rol: 'propietario', nombreOrganizacion: 'Envíos Xelajú, S.A.' },
      isLoading: false,
      error: null,
    } as ReturnType<typeof sesionHook.useSesion>);

    renderWithLayout(LayoutPanel);

    // N.1
    expect(screen.getByText('Envíos Xelajú, S.A.')).toBeDefined();
    expect(screen.getByRole('link', { name: 'User' }).getAttribute('href')).toBe('/panel/perfil');
    expect(screen.getByText('Propietario')).toBeDefined();
    expect(screen.getByText('Publicación')).toBeDefined();
    expect(screen.getByText('APIs')).toBeDefined();
    expect(screen.getByText('API')).toBeDefined();
    expect(screen.getByText('Especificación')).toBeDefined();
    expect(screen.getByText('Organización')).toBeDefined();
    expect(screen.getByText('Casos de soporte')).toBeDefined();
    expect(screen.getByRole('link', { name: 'Planes' }).className).toContain('bg-[#0E1830]');
    expect(screen.getByRole('link', { name: 'APIs' }).className).not.toContain('bg-[#0E1830]');
  });

  it('LayoutAdmin muestra los grupos correctos para administrador', () => {
    vi.mocked(sesionHook.useSesion).mockReturnValue({
      data: { nombre: 'Admin', rol: 'administrador' },
      isLoading: false,
      error: null,
    } as ReturnType<typeof sesionHook.useSesion>);

    renderWithLayout(LayoutAdmin);

    // A6
    expect(screen.getByText('Plataforma')).toBeDefined();
    expect(screen.getByText('Planes de plataforma')).toBeDefined();
    expect(screen.getAllByText('Soporte').length).toBeGreaterThan(0);
    expect(screen.getByText('Sistema')).toBeDefined();
    expect(screen.getByText('Cuentas de plataforma')).toBeDefined();
  });

  it('LayoutAdmin oculta Plataforma y Cuentas para el rol soporte', () => {
    vi.mocked(sesionHook.useSesion).mockReturnValue({
      data: { nombre: 'Soporte User', rol: 'soporte' },
      isLoading: false,
      error: null,
    } as ReturnType<typeof sesionHook.useSesion>);

    renderWithLayout(LayoutAdmin);

    expect(screen.queryByText('Plataforma')).toBeNull();
    expect(screen.queryByText('Cuentas de plataforma')).toBeNull();
    expect(screen.getAllByText('Soporte').length).toBeGreaterThan(0);
    expect(screen.getByText('Casos')).toBeDefined();
    expect(screen.getByText('Sistema')).toBeDefined();
  });
});
