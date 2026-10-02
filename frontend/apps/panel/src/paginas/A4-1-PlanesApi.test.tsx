import { describe, it, expect, beforeAll, afterEach, afterAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { setupServer } from 'msw/node';
import { MemoryRouter, Route, Routes } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import PaginaA41PlanesApi from './A4-1-PlanesApi';

const planesMock = [
  {
    id: '1',
    apiId: 'api-1',
    nombre: 'Básico',
    descripcion: 'Para tiendas nuevas',
    precio: 0,
    esGratuito: true,
    vigenciaDias: 30,
    cuotaLlamadas: 1000,
    limiteMinuto: 10,
    activo: true
  }
];

const servidor = setupServer(
  http.get('http://localhost/api/apis/:apiId/planes', () => {
    return HttpResponse.json(planesMock);
  }),
  http.post('http://localhost/api/apis/:apiId/planes', async ({ request }) => {
    const body = await request.json();
    return HttpResponse.json({ id: '2', ...(body as Record<string, unknown>), apiId: 'api-1', activo: true });
  }),
  http.put('http://localhost/api/apis/:apiId/planes/:planId', async ({ request }) => {
    const body = await request.json();
    return HttpResponse.json({ id: '1', ...(body as Record<string, unknown>), apiId: 'api-1', activo: true });
  })
);

beforeAll(() => servidor.listen());
afterEach(() => servidor.resetHandlers());
afterAll(() => servidor.close());

function renderizarPagina() {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={cliente}>
      <MemoryRouter initialEntries={['/panel/apis/api-1/planes']}>
        <Routes>
          <Route path="/panel/apis/:id/planes" element={<PaginaA41PlanesApi />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('PaginaA41PlanesApi', () => {
  it('carga la lista de planes', async () => {
    renderizarPagina();
    expect(screen.getByRole('status', { name: 'Cargando' })).toBeDefined();
    
    await waitFor(() => {
      expect(screen.getByText('Básico')).toBeDefined();
    });
    
    expect(screen.getByText('Gratis')).toBeDefined();
    expect(screen.getByText('1,000 llamadas')).toBeDefined();
  });

  it('permite crear un plan', async () => {
    const usuario = userEvent.setup();
    renderizarPagina();
    
    await waitFor(() => {
      expect(screen.getByText('Crear un plan')).toBeDefined();
    });
    
    await usuario.click(screen.getByText('Crear un plan'));
    
    expect(screen.getByRole('heading', { name: 'Crear un plan' })).toBeDefined();
    
    await usuario.type(screen.getByLabelText(/nombre/i), 'Pro');
    await usuario.type(screen.getByLabelText(/descripción/i), 'Plan pro');
    await usuario.type(screen.getByLabelText(/cuota mensual/i), '5000');
    await usuario.type(screen.getByLabelText(/límite por minuto/i), '30');
    
    const inputPrecio = screen.getByLabelText(/precio/i);
    await usuario.clear(inputPrecio);
    await usuario.type(inputPrecio, '150');

    await usuario.click(screen.getByRole('button', { name: 'Crear plan' }));
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Planes de la API/i })).toBeDefined();
    });
  });

  it('permite editar un plan', async () => {
    const usuario = userEvent.setup();
    renderizarPagina();
    
    await waitFor(() => {
      expect(screen.getByText('Editar')).toBeDefined();
    });
    
    await usuario.click(screen.getByText('Editar'));
    
    expect(screen.getByRole('heading', { name: 'Editar plan' })).toBeDefined();
    
    const inputNombre = screen.getByLabelText(/nombre/i);
    await usuario.clear(inputNombre);
    await usuario.type(inputNombre, 'Básico Editado');
    
    await usuario.click(screen.getByRole('button', { name: 'Guardar plan' }));
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Planes de la API/i })).toBeDefined();
    });
  });
});
