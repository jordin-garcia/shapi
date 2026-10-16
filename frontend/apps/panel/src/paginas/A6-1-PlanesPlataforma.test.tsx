import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { setupServer } from 'msw/node';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import PaginaA61PlanesPlataforma from './A6-1-PlanesPlataforma';

const planes = [{ id: 'plan-1', nombre: 'Lanzamiento', descripcion: 'Para empezar', precio: 199, moneda: 'GTQ' as const,
  vigenciaDias: 30, maxApis: 5, maxMiembros: 3, cuotaPeticiones: 50000, dominioPropio: false, esPrueba: false, activo: true, orden: 1 },
{ id: 'plan-2', nombre: 'Prueba', descripcion: 'Primera API', precio: 0, moneda: 'GTQ' as const,
  vigenciaDias: 30, maxApis: 1, maxMiembros: 1, cuotaPeticiones: 10000, dominioPropio: false, esPrueba: true, activo: true, orden: 2 },
{ id: 'plan-3', nombre: 'Escala', descripcion: 'Sin límites de recursos', precio: 1500, moneda: 'GTQ' as const,
  vigenciaDias: 30, maxApis: null, maxMiembros: null, cuotaPeticiones: 10000000, dominioPropio: true, esPrueba: false, activo: true, orden: 3 }];
const servidor = setupServer(
  http.get('http://localhost/api/admin/planes-plataforma', () => HttpResponse.json(planes)),
  http.post('http://localhost/api/admin/planes-plataforma', async ({ request }) => HttpResponse.json({ id: 'plan-2', ...(await request.json() as object), moneda: 'GTQ', esPrueba: false, activo: true, orden: 2 }, { status: 201 })),
  http.put('http://localhost/api/admin/planes-plataforma/:id', async ({ request }) => HttpResponse.json({ id: 'plan-1', ...(await request.json() as object), moneda: 'GTQ', esPrueba: false, activo: true, orden: 1 })),
  http.post('http://localhost/api/admin/planes-plataforma/:id/desactivar', () => new HttpResponse(null, { status: 204 })),
);
beforeAll(() => servidor.listen());
afterEach(() => servidor.resetHandlers());
afterAll(() => servidor.close());

function renderizar() {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={cliente}><PaginaA61PlanesPlataforma /></QueryClientProvider>);
}

describe('PaginaA61PlanesPlataforma', () => {
  it('RF-17 muestra los planes y permite abrir el formulario de creación', async () => {
    renderizar();
    expect(await screen.findByRole('heading', { name: 'Planes de plataforma' })).toBeDefined();
    expect(screen.getByText('Lanzamiento')).toBeDefined();
    await userEvent.setup().click(screen.getByRole('button', { name: 'Crear un plan' }));
    expect(screen.getByRole('heading', { name: 'Crear plan de plataforma' })).toBeDefined();
  });

  it('RF-17 reproduce singular y plural de límites y el texto de límites ilimitados', async () => {
    renderizar();
    expect(await screen.findByText('1 API · 10,000 peticiones · 1 miembro')).toBeDefined();
    expect(screen.getByText('5 APIs · 50,000 peticiones · 3 miembros')).toBeDefined();
    expect(screen.getByText('APIs y miembros ilimitados · 10,000,000 peticiones · dominio propio')).toBeDefined();
  });

  it('RF-17 crea un plan con sus límites y precio', async () => {
    const usuario = userEvent.setup();
    let creado = false;
    servidor.use(http.post('http://localhost/api/admin/planes-plataforma', async ({ request }) => {
      const cuerpo = await request.json() as Record<string, unknown>;
      creado = cuerpo.nombre === 'Profesional' && cuerpo.precio === 299 && cuerpo.cuotaPeticiones === 90000;
      return HttpResponse.json({ id: 'plan-2', nombre: 'Profesional', descripcion: 'Plan avanzado', precio: 299, moneda: 'GTQ', vigenciaDias: 30, maxApis: 10, maxMiembros: 5, cuotaPeticiones: 90000, dominioPropio: true, esPrueba: false, activo: true, orden: 2 }, { status: 201 });
    }));
    renderizar();
    await usuario.click(await screen.findByRole('button', { name: 'Crear un plan' }));
    await usuario.type(screen.getByLabelText('Nombre'), 'Profesional');
    await usuario.type(screen.getByLabelText('Descripción'), 'Plan avanzado');
    await usuario.type(screen.getByLabelText('APIs'), '10');
    await usuario.type(screen.getByLabelText('Peticiones por ciclo'), '90000');
    await usuario.type(screen.getByLabelText('Miembros'), '5');
    await usuario.type(screen.getByRole('spinbutton', { name: 'Precio' }), '299');
    await usuario.click(screen.getByRole('button', { name: 'Crear plan' }));
    await waitFor(() => expect(creado).toBe(true));
  });

  it('RF-17 edita los datos de un plan existente', async () => {
    const usuario = userEvent.setup();
    let actualizado = false;
    servidor.use(http.put('http://localhost/api/admin/planes-plataforma/:id', async ({ request }) => {
      const cuerpo = await request.json() as Record<string, unknown>;
      actualizado = cuerpo.descripcion === 'Para equipos en crecimiento';
      return HttpResponse.json({ ...planes[0], descripcion: cuerpo.descripcion });
    }));
    renderizar();
    await usuario.click((await screen.findAllByRole('button', { name: 'Editar' }))[0]);
    expect(screen.getByRole('heading', { name: 'Editar plan de plataforma' })).toBeDefined();
    const descripcion = screen.getByRole('textbox', { name: 'Descripción' });
    await usuario.clear(descripcion);
    await usuario.type(descripcion, 'Para equipos en crecimiento');
    await usuario.click(screen.getByRole('button', { name: 'Guardar plan' }));
    await waitFor(() => expect(actualizado).toBe(true));
  });
});
