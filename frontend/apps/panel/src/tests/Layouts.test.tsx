import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { LayoutPanel } from '../layouts/LayoutPanel';
import { LayoutAdmin } from '../layouts/LayoutAdmin';
import * as sesionHook from '../modulos/sesion/useSesion';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../modulos/sesion/useSesion');

const renderWithLayout = (Layout: any) => {
  const queryClient = new QueryClient();
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route path="/" element={<Layout />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
};

describe('Layouts - Barras Laterales', () => {
  it('LayoutPanel muestra los grupos correctos', () => {
    vi.mocked(sesionHook.useSesion).mockReturnValue({
      data: { nombre: 'User', rol: 'propietario' },
      isLoading: false,
      error: null,
    } as any);

    renderWithLayout(LayoutPanel);
    
    // N.1
    expect(screen.getByText('Publicación')).toBeDefined();
    expect(screen.getByText('APIs')).toBeDefined();
    expect(screen.getByText('API')).toBeDefined();
    expect(screen.getByText('Especificación')).toBeDefined();
    expect(screen.getByText('Organización')).toBeDefined();
    expect(screen.getByText('Casos de soporte')).toBeDefined();
  });

  it('LayoutAdmin muestra los grupos correctos para administrador', () => {
    vi.mocked(sesionHook.useSesion).mockReturnValue({
      data: { nombre: 'Admin', rol: 'administrador' },
      isLoading: false,
      error: null,
    } as any);

    renderWithLayout(LayoutAdmin);
    
    // A6
    expect(screen.getByText('Plataforma')).toBeDefined();
    expect(screen.getByText('Planes de plataforma')).toBeDefined();
    expect(screen.getByText('Soporte')).toBeDefined();
    expect(screen.getByText('Sistema')).toBeDefined();
    expect(screen.getByText('Cuentas de plataforma')).toBeDefined();
  });

  it('LayoutAdmin oculta Plataforma y Cuentas para el rol soporte', () => {
    vi.mocked(sesionHook.useSesion).mockReturnValue({
      data: { nombre: 'Soporte User', rol: 'soporte' },
      isLoading: false,
      error: null,
    } as any);

    renderWithLayout(LayoutAdmin);
    
    expect(screen.queryByText('Plataforma')).toBeNull();
    expect(screen.queryByText('Cuentas de plataforma')).toBeNull();
    expect(screen.getByText('Soporte')).toBeDefined();
    expect(screen.getByText('Casos')).toBeDefined();
    expect(screen.getByText('Sistema')).toBeDefined();
  });
});
