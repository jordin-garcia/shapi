import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import PaginaA31Apis from '../paginas/A3-1-Apis';
import PaginaA32Registro from '../paginas/A3-2-Registro';

const API = 'http://localhost/api/apis';
let cliente: QueryClient;

function envolver(contenido: React.ReactNode, ruta = '/panel/apis') {
  return render(
    <QueryClientProvider client={cliente}>
      <MemoryRouter initialEntries={[ruta]}>{contenido}</MemoryRouter>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
});

afterEach(() => {
  cleanup();
  cliente.clear();
});

describe('RF-14 · A3.1 Lista de APIs', () => {
  it('reproduce el contenido del mockup con APIs', async () => {
    server.use(http.get(API, () => HttpResponse.json({
      total: 2,
      planNombre: 'Producto',
      maxApis: 10,
      elementos: [
        { id: 'api-1', nombre: 'API de Cotización de Envíos', subdominio: 'envios', estado: 'publicada' },
        { id: 'api-2', nombre: 'API de Recolecciones', subdominio: 'recolecciones', estado: 'despublicada' },
      ],
    })));

    envolver(<PaginaA31Apis />);

    expect(await screen.findByRole('heading', { name: 'APIs de la organización' })).toBeDefined();
    expect(screen.getByText('Publicación')).toBeDefined();
    expect(screen.getByText(/Usa 2 de 10 APIs de su plan Producto/)).toBeDefined();
    expect(screen.getByRole('link', { name: 'Registrar una API' }).getAttribute('href')).toBe('/panel/apis/nueva');
    expect(screen.getAllByRole('columnheader').map(celda => celda.textContent)).toEqual(['API', 'Subdominio', 'Estado', 'Acción']);
    expect(screen.getByRole('link', { name: 'API de Cotización de Envíos' }).getAttribute('href')).toBe('/panel/apis/api-1/especificacion');
    expect(screen.getByText('envios')).toBeDefined();
    expect(screen.getByText('Publicada')).toBeDefined();
    expect(screen.getByText('Despublicar')).toBeDefined();
    expect(screen.getByText('Despublicada')).toBeDefined();
    expect(screen.getByText('Publicar')).toBeDefined();
  });

  it('reproduce la variante vacía del mockup', async () => {
    server.use(http.get(API, () => HttpResponse.json({
      total: 0,
      planNombre: 'Prueba',
      maxApis: 1,
      elementos: [],
    })));

    envolver(<PaginaA31Apis />);

    expect(await screen.findByRole('heading', { name: 'Todavía no tiene APIs registradas' })).toBeDefined();
    expect(document.body.textContent).toContain('Su plan Prueba incluye 1 API');
    expect(screen.getByText('Registre su primera API con la URL de su servidor de origen. Después cargará su especificación OpenAPI y elegirá qué rutas exponer.')).toBeDefined();
    expect(screen.getByRole('link', { name: 'Registrar una API' }).getAttribute('href')).toBe('/panel/apis/nueva');
  });

  it('muestra un error recuperable si falla la consulta', async () => {
    server.use(http.get(API, () => HttpResponse.json({ title: 'Fallo' }, { status: 500 })));

    envolver(<PaginaA31Apis />);

    expect(await screen.findByText('No se pudieron cargar las APIs.')).toBeDefined();
    expect(screen.getByRole('button', { name: 'Reintentar' })).toBeDefined();
  });
});

describe('RF-08 y RF-47 · A3.2 Registro de API', () => {
  it('reproduce el formulario y actualiza los hosts con el subdominio', async () => {
    envolver(<PaginaA32Registro />, '/panel/apis/nueva');

    expect(screen.getByText('Nueva API')).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Registrar una API' })).toBeDefined();
    expect(screen.getByLabelText('Nombre de la API')).toBeDefined();
    expect(screen.getByLabelText('URL del servidor de origen')).toBeDefined();
    const subdominio = screen.getByLabelText('Subdominio');
    await userEvent.type(subdominio, 'envios');
    expect(screen.getByText('envios.shapi.localhost')).toBeDefined();
    expect(screen.getByText('envios.api.shapi.localhost')).toBeDefined();
    expect(screen.getByRole('button', { name: 'Registrar y continuar' })).toBeDefined();
    expect(screen.getByRole('link', { name: 'Volver a las APIs' }).getAttribute('href')).toBe('/panel/apis');
  });

  it('envía el formulario con CSRF, muestra conexión y secreto una vez y continúa', async () => {
    let cuerpo: unknown;
    let csrf: string | null = null;
    server.use(http.post(API, async ({ request }) => {
      cuerpo = await request.json();
      csrf = request.headers.get('X-Requested-With');
      return HttpResponse.json({
        id: 'api-123',
        nombre: 'API de Cotización de Envíos',
        subdominio: 'envios',
        estado: 'borrador',
        secretoOrigen: 'shps_0123456789ABCDEFGHIJKLMNOPQRSTUV',
        conexionMilisegundos: 142,
      }, { status: 201 });
    }));

    envolver(
      <Routes>
        <Route path="/panel/apis/nueva" element={<PaginaA32Registro />} />
        <Route path="/panel/apis/:id/especificacion" element={<h1>Especificación</h1>} />
      </Routes>,
      '/panel/apis/nueva',
    );
    await userEvent.type(screen.getByLabelText('Nombre de la API'), 'API de Cotización de Envíos');
    await userEvent.type(screen.getByLabelText('URL del servidor de origen'), 'https://servicios.enviosxelaju.com/v1');
    await userEvent.type(screen.getByLabelText('Subdominio'), 'envios');
    await userEvent.click(screen.getByRole('button', { name: 'Registrar y continuar' }));

    expect((await screen.findByRole('status')).textContent).toContain('Conexión probada: su servidor respondió en 142 ms');
    expect(screen.getByText('shps_0123456789ABCDEFGHIJKLMNOPQRSTUV')).toBeDefined();
    expect(cuerpo).toEqual({
      nombre: 'API de Cotización de Envíos',
      urlOrigen: 'https://servicios.enviosxelaju.com/v1',
      subdominio: 'envios',
    });
    expect(csrf).toBe('shapi');
    await userEvent.click(screen.getByRole('button', { name: 'Continuar a la especificación' }));
    expect(await screen.findByRole('heading', { name: 'Especificación' })).toBeDefined();
  });

  it('muestra los errores de validación del backend junto a cada campo', async () => {
    server.use(http.post(API, () => HttpResponse.json({
      type: 'about:blank',
      title: 'Revise los datos del formulario.',
      status: 400,
      codigo: 'datos_invalidos',
      errores: {
        nombre: ['Escriba el nombre de la API.'],
        subdominio: ['El subdominio no está disponible.'],
      },
    }, { status: 400, headers: { 'Content-Type': 'application/problem+json' } })));

    envolver(<PaginaA32Registro />, '/panel/apis/nueva');
    await userEvent.type(screen.getByLabelText('URL del servidor de origen'), 'https://8.8.8.8');
    await userEvent.click(screen.getByRole('button', { name: 'Registrar y continuar' }));

    expect(await screen.findByText('Escriba el nombre de la API.')).toBeDefined();
    expect(screen.getByText('El subdominio no está disponible.')).toBeDefined();
  });
});
