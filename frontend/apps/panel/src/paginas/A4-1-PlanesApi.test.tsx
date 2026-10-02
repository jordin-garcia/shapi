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

let ultimoCuerpoPOST: Record<string, unknown> | null = null;

const servidor = setupServer(
  
  http.get('http://localhost/api/apis', () => {
    return HttpResponse.json({
      elementos: [{ id: 'api-1', nombre: 'API de Cotización de Envíos', subdominio: 'envios', estado: 'publicada' }],
      total: 1,
      planNombre: 'Básico',
      maxApis: 10
    });
  }),
  http.get('http://localhost/api/apis/:apiId/planes', () => {
    return HttpResponse.json(planesMock);
  }),
  http.post('http://localhost/api/apis/:apiId/planes', async ({ request }) => {
    const body = await request.clone().json();
    ultimoCuerpoPOST = body as Record<string, unknown>;
    const bodyObj = body as Record<string, unknown>;
    if (!bodyObj.nombre || bodyObj.precio === undefined) {
      return HttpResponse.json({ codigo: 'datos_invalidos' }, { status: 400 });
    }
    return HttpResponse.json({ id: '2', ...(body as Record<string, unknown>), apiId: 'api-1', activo: true });
  }),
  http.put('http://localhost/api/apis/:apiId/planes/:planId', async ({ request }) => {
    const body = await request.clone().json();
    return HttpResponse.json({ id: '1', ...(body as Record<string, unknown>), apiId: 'api-1', activo: true });
  })
);

beforeAll(() => servidor.listen());
afterEach(() => {
  servidor.resetHandlers();
  ultimoCuerpoPOST = null;
});
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
  it('RF-18 Muestra la variante vacía', async () => {
    servidor.use(
      
  http.get('http://localhost/api/apis', () => {
    return HttpResponse.json({
      elementos: [{ id: 'api-1', nombre: 'API de Cotización de Envíos', subdominio: 'envios', estado: 'publicada' }],
      total: 1,
      planNombre: 'Básico',
      maxApis: 10
    });
  }),
  http.get('http://localhost/api/apis/:apiId/planes', () => {
        return HttpResponse.json([]);
      })
    );
    renderizarPagina();
    await waitFor(() => {
      expect(screen.getByText('Todavía no tiene planes para esta API')).toBeDefined();
    });
  });
  it('RF-18 carga la lista de planes', async () => {
    renderizarPagina();
    expect(screen.getByRole('status', { name: 'Cargando' })).toBeDefined();
    
    await waitFor(() => {
      expect(screen.getByText('Básico')).toBeDefined();
    });
    
    expect(screen.getByText('Gratis')).toBeDefined();
    expect(screen.getByText('1,000 llamadas')).toBeDefined();
  });

  it('RF-18 permite crear un plan enviando el cuerpo correcto', async () => {
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
    
    // Verificamos que se manejó la mutación
    // El interceptor devuelve el mismo body

    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Planes de la API/i })).toBeDefined();
    });

    expect(ultimoCuerpoPOST).toEqual({
      nombre: 'Pro',
      descripcion: 'Plan pro',
      cuotaLlamadas: 5000,
      limiteMinuto: 30,
      precio: 150,
      vigenciaDias: 30,
      esGratuito: false
    });
  });

  it('RF-18 permite crear un plan gratuito enviando el cuerpo correcto', async () => {
    const usuario = userEvent.setup();
    renderizarPagina();
    
    await waitFor(() => {
      expect(screen.getByText('Crear un plan')).toBeDefined();
    });
    
    await usuario.click(screen.getByText('Crear un plan'));
    
    await usuario.type(screen.getByLabelText(/nombre/i), 'Gratis');
    await usuario.type(screen.getByLabelText(/descripción/i), 'Para iniciar');
    await usuario.type(screen.getByLabelText(/cuota mensual/i), '100');
    await usuario.type(screen.getByLabelText(/límite por minuto/i), '5');
    
    const inputPrecio = screen.getByLabelText(/precio/i);
    const cbGratuito = screen.getByLabelText(/plan gratuito/i);
    
    await usuario.click(cbGratuito);
    
    expect((inputPrecio as HTMLInputElement).disabled).toBe(true);
    expect(screen.getByText('Un plan gratuito queda en Q 0.00.')).toBeDefined();

    await usuario.click(screen.getByRole('button', { name: 'Crear plan' }));
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Planes de la API/i })).toBeDefined();
    });

    expect(ultimoCuerpoPOST).toEqual({
      nombre: 'Gratis',
      descripcion: 'Para iniciar',
      cuotaLlamadas: 100,
      limiteMinuto: 5,
      precio: 0,
      vigenciaDias: 30,
      esGratuito: true
    });
  });

  it('RF-19 permite editar un plan', async () => {
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
