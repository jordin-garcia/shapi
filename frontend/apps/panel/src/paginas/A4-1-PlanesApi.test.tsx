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

    // H-62: los mockups muestran «Q 0.00» también para el plan gratuito. Criterio 5: «llamadas» y «peticiones».
    expect(screen.getByText('Q 0.00')).toBeDefined();
    expect(screen.queryByText('Gratis')).toBeNull();
    expect(screen.getByText('1,000 llamadas')).toBeDefined();
    expect(screen.getByText('10 peticiones')).toBeDefined();
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

    // El plan nuevo empieza como gratuito (mockup NuevoPlan): para cobrar hay que desmarcarlo.
    await usuario.click(screen.getByLabelText(/plan gratuito/i));
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

    // Decidido (3 oct, EM-07): como en el mockup, «Plan gratuito» ya viene marcado.
    expect((cbGratuito as HTMLInputElement).checked).toBe(true);
    expect((inputPrecio as HTMLInputElement).disabled).toBe(true);
    expect((inputPrecio as HTMLInputElement).value).toBe('0.00');
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

  // H-62 y H-66: el precio se edita con 2 decimales («149.00») y el PUT lleva el cuerpo completo.
  it('RF-18 edita un plan de pago y envía el cuerpo del PUT', async () => {
    let cuerpoPut: unknown;
    servidor.use(
      http.get('http://localhost/api/apis/:apiId/planes', () => HttpResponse.json([{
        id: '7', apiId: 'api-1', nombre: 'Comercio', descripcion: 'Para comercios', precio: 149, moneda: 'GTQ',
        esGratuito: false, vigenciaDias: 30, cuotaLlamadas: 5000, limiteMinuto: 60, activo: true,
      }])),
      http.put('http://localhost/api/apis/:apiId/planes/:planId', async ({ request, params }) => {
        cuerpoPut = { planId: params.planId, ...(await request.json() as Record<string, unknown>) };
        return HttpResponse.json({ id: '7', apiId: 'api-1', moneda: 'GTQ', activo: true, ...(cuerpoPut as Record<string, unknown>) });
      }),
    );
    const usuario = userEvent.setup();
    renderizarPagina();
    expect(await screen.findByText('Q 149.00')).toBeDefined();
    await usuario.click(screen.getByText('Editar'));

    const inputPrecio = screen.getByLabelText(/precio/i) as HTMLInputElement;
    expect(inputPrecio.value).toBe('149.00');
    await usuario.clear(inputPrecio);
    await usuario.type(inputPrecio, '175.50');
    const cuota = screen.getByLabelText(/cuota mensual/i);
    await usuario.clear(cuota);
    await usuario.type(cuota, '8000');
    await usuario.click(screen.getByRole('button', { name: 'Guardar plan' }));

    await screen.findByRole('heading', { name: /Planes de la API/i });
    expect(cuerpoPut).toEqual({
      planId: '7',
      nombre: 'Comercio',
      descripcion: 'Para comercios',
      precio: 175.5,
      esGratuito: false,
      vigenciaDias: 30,
      cuotaLlamadas: 8000,
      limiteMinuto: 60,
    });
  });

  // H-66: el 409 plan_duplicado se muestra en el formulario.
  it('RF-18 muestra el error de un nombre repetido', async () => {
    servidor.use(http.post('http://localhost/api/apis/:apiId/planes', () => HttpResponse.json({
      type: 'about:blank', title: 'Ya existe un plan con ese nombre en esta API.', status: 409, codigo: 'plan_duplicado',
    }, { status: 409, headers: { 'Content-Type': 'application/problem+json' } })));
    const usuario = userEvent.setup();
    renderizarPagina();
    await usuario.click(await screen.findByText('Crear un plan'));
    await usuario.type(screen.getByLabelText(/nombre/i), 'Básico');
    await usuario.type(screen.getByLabelText(/descripción/i), 'Repetido');
    await usuario.type(screen.getByLabelText(/cuota mensual/i), '100');
    await usuario.type(screen.getByLabelText(/límite por minuto/i), '5');
    await usuario.click(screen.getByRole('button', { name: 'Crear plan' }));

    expect((await screen.findByRole('alert')).textContent).toContain('Ya existe un plan con ese nombre en esta API.');
    expect(screen.getByRole('heading', { name: 'Crear un plan' })).toBeDefined();
  });

  // H-64: los errores por campo del 400 llegan a la pantalla.
  it('RF-18 muestra los errores por campo de un 400', async () => {
    servidor.use(http.post('http://localhost/api/apis/:apiId/planes', () => HttpResponse.json({
      type: 'about:blank', title: 'Revise los datos del plan.', status: 400, codigo: 'datos_invalidos',
      errores: { precio: ['Un plan de pago debe tener un precio mayor que 0. Si no cobra, márquelo como gratuito.'] },
    }, { status: 400, headers: { 'Content-Type': 'application/problem+json' } })));
    const usuario = userEvent.setup();
    renderizarPagina();
    await usuario.click(await screen.findByText('Crear un plan'));
    await usuario.type(screen.getByLabelText(/nombre/i), 'Cero');
    await usuario.type(screen.getByLabelText(/descripción/i), 'Sin precio');
    await usuario.type(screen.getByLabelText(/cuota mensual/i), '100');
    await usuario.type(screen.getByLabelText(/límite por minuto/i), '5');
    await usuario.click(screen.getByRole('button', { name: 'Crear plan' }));

    const aviso = await screen.findByRole('alert');
    expect(aviso.textContent).toContain('Revise los datos del plan.');
    expect(aviso.textContent).toContain('Un plan de pago debe tener un precio mayor que 0.');
  });
});
