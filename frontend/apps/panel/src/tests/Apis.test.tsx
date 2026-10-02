import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import PaginaA31Apis from '../paginas/A3-1-Apis';
import PaginaA32Registro from '../paginas/A3-2-Registro';
import PaginaA33Especificacion from '../paginas/A3-3-Especificacion';
import PaginaA34Rutas from '../paginas/A3-4-Rutas';

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
  vi.restoreAllMocks();
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
    expect(document.body.textContent).toContain('Usa 2 de 10 APIs de su plan Producto');
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

    expect((await screen.findByRole('status')).textContent).toContain('Conexión probada: su servidor respondió en 142 ms. Shapi prueba la conexión antes de guardar.');
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

const API_ID = '0199a5b2-7c3d-7e4f-8a9b-0c1d2e3f4a5b';
const RUTAS = [
  { id: '0199a5b2-7c3d-7e4f-8a9b-000000000001', metodo: 'POST', patron: '/cotizaciones', resumen: 'Cotizar un envío', descripcion: null, expuesta: true },
  { id: '0199a5b2-7c3d-7e4f-8a9b-000000000002', metodo: 'POST', patron: '/guias', resumen: 'Crear una guía', descripcion: null, expuesta: true },
  { id: '0199a5b2-7c3d-7e4f-8a9b-000000000003', metodo: 'GET', patron: '/tarifas', resumen: 'Listar tarifas', descripcion: null, expuesta: false },
  { id: '0199a5b2-7c3d-7e4f-8a9b-000000000004', metodo: 'GET', patron: '/rastreo', resumen: 'Rastrear una guía', descripcion: null, expuesta: true },
  { id: '0199a5b2-7c3d-7e4f-8a9b-000000000005', metodo: 'GET', patron: '/cobertura', resumen: 'Consultar cobertura', descripcion: null, expuesta: true },
] as const;

describe('RF-09 · A3.3 Especificación OpenAPI', () => {
  it('carga el archivo y reproduce las rutas encontradas del mockup', async () => {
    server.use(
      http.get(`${API}/${API_ID}/rutas`, () => HttpResponse.json({
        apiId: API_ID, apiNombre: 'API de Cotización de Envíos', elementos: [], totalExpuestas: 0, totalOcultas: 0,
      })),
      http.put(`${API}/${API_ID}/especificacion`, ({ request }) => {
        expect(request.headers.get('X-Requested-With')).toBe('shapi');
        expect(request.headers.get('Content-Type')).toContain('multipart/form-data');
        return HttpResponse.json({
          apiId: API_ID,
          apiNombre: 'API de Cotización de Envíos',
          titulo: 'API de Cotización de Envíos',
          descripcion: 'Cotice envíos.',
          version: '1.0.0',
          versionOpenApi: '3.0',
          formato: 'yaml',
          cargadaEn: '2026-10-01T12:00:00Z',
          totalRutas: 5,
          rutas: RUTAS.map(ruta => ({ ...ruta, expuesta: false })),
        });
      }),
    );

    envolver(<Routes><Route path="/panel/apis/:id/especificacion" element={<PaginaA33Especificacion />} /></Routes>, `/panel/apis/${API_ID}/especificacion`);
    expect(await screen.findByRole('heading', { name: 'Cargar especificación OpenAPI' })).toBeDefined();
    vi.spyOn(FormData.prototype, 'set').mockImplementation(() => undefined);
    const archivo = new File(['openapi: 3.0.3'], 'cotizacion-envios.yaml', { type: 'application/yaml' });
    await userEvent.upload(screen.getByLabelText('Elegir archivo OpenAPI'), archivo);

    expect(await screen.findByText('Cargado')).toBeDefined();
    expect(screen.getByText('cotizacion-envios.yaml')).toBeDefined();
    expect(document.body.textContent).toContain('OpenAPI 3.0 · 14 B');
    expect(document.body.textContent).toContain('Rutas encontradas5');
    expect(screen.getByText('/cotizaciones')).toBeDefined();
    expect(screen.getByRole('link', { name: 'Continuar a la selección de rutas' }).getAttribute('href'))
      .toBe(`/panel/apis/${API_ID}/rutas`);
  });

  it('muestra el error de una especificación inválida', async () => {
    server.use(
      http.get(`${API}/${API_ID}/rutas`, () => HttpResponse.json({
        apiId: API_ID, apiNombre: 'API de Cotización de Envíos', elementos: [], totalExpuestas: 0, totalOcultas: 0,
      })),
      http.put(`${API}/${API_ID}/especificacion`, () => HttpResponse.json({
        type: 'about:blank', title: 'La especificación OpenAPI no es válida.', status: 422,
        codigo: 'especificacion_invalida', detalle: { ubicacion: 'línea 4', mensaje: 'Fin inesperado.' },
      }, { status: 422, headers: { 'Content-Type': 'application/problem+json' } })),
    );
    envolver(<Routes><Route path="/panel/apis/:id/especificacion" element={<PaginaA33Especificacion />} /></Routes>, `/panel/apis/${API_ID}/especificacion`);
    await screen.findByRole('heading', { name: 'Cargar especificación OpenAPI' });
    vi.spyOn(FormData.prototype, 'set').mockImplementation(() => undefined);
    await userEvent.upload(screen.getByLabelText('Elegir archivo OpenAPI'), new File(['x'], 'mala.yaml'));
    const aviso = await screen.findByRole('alert');
    expect(aviso.textContent).toContain('La especificación OpenAPI no es válida.');
    expect(aviso.textContent).toContain('línea 4');
    expect(aviso.textContent).toContain('Fin inesperado.');
  });
});

describe('RF-10 · A3.4 Rutas expuestas', () => {
  it('muestra el resumen, cambia estados y guarda el lote', async () => {
    let cuerpo: unknown;
    server.use(
      http.get(`${API}/${API_ID}/rutas`, () => HttpResponse.json({
        apiId: API_ID, apiNombre: 'API de Cotización de Envíos', elementos: RUTAS, totalExpuestas: 4, totalOcultas: 1,
      })),
      http.put(`${API}/${API_ID}/rutas/exposicion`, async ({ request }) => {
        cuerpo = await request.json();
        return HttpResponse.json({
          apiId: API_ID, apiNombre: 'API de Cotización de Envíos', elementos: RUTAS, totalExpuestas: 4, totalOcultas: 1,
        });
      }),
    );

    envolver(<Routes><Route path="/panel/apis/:id/rutas" element={<PaginaA34Rutas />} /></Routes>, `/panel/apis/${API_ID}/rutas`);
    expect(await screen.findByRole('heading', { name: 'Rutas expuestas' })).toBeDefined();
    expect(document.body.textContent).toContain('Rutas expuestas4');
    expect(document.body.textContent).toContain('Rutas ocultas1');
    expect(screen.getAllByRole('columnheader').map(encabezado => encabezado.textContent))
      .toEqual(['Método', 'Ruta', 'Estado']);
    expect(screen.queryByText('Cotizar un envío')).toBeNull();
    await userEvent.click(screen.getByLabelText('Ocultar POST /cotizaciones'));
    expect(document.body.textContent).toContain('Rutas expuestas3');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar rutas expuestas' }));
    expect(await screen.findByText('Las rutas expuestas se guardaron.')).toBeDefined();
    expect(cuerpo).toEqual(RUTAS.map((ruta, indice) => ({ rutaId: ruta.id, expuesta: indice === 0 ? false : ruta.expuesta })));
  });
});
