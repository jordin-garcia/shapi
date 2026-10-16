import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import type { components } from '@shapi/api/planes';
import PaginaA01Inicio from '../paginas/A0-1-Inicio';
import { server } from '../../../../test/servidor';

type PlanPlataforma = components['schemas']['PlanPlataforma'];

const planesSiembra: PlanPlataforma[] = [
  {
    id: '00000000-0000-0000-0000-000000000001',
    nombre: 'Prueba',
    descripcion: 'Publicar una primera API sin costo',
    precio: 0,
    moneda: 'GTQ',
    vigenciaDias: 30,
    maxApis: 1,
    maxMiembros: 1,
    cuotaPeticiones: 10000,
    dominioPropio: false,
    esPrueba: true,
    inicioCicloPrevisto: '2026-10-15T06:00:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000002',
    nombre: 'Lanzamiento',
    descripcion: 'Empezar a cobrar por una API existente',
    precio: 199,
    moneda: 'GTQ',
    vigenciaDias: 30,
    maxApis: 3,
    maxMiembros: 3,
    cuotaPeticiones: 250000,
    dominioPropio: false,
    esPrueba: false,
    inicioCicloPrevisto: '2026-10-15T06:00:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000003',
    nombre: 'Producto',
    descripcion: 'Proveedores con clientes establecidos',
    precio: 599,
    moneda: 'GTQ',
    vigenciaDias: 30,
    maxApis: 10,
    maxMiembros: 10,
    cuotaPeticiones: 2000000,
    dominioPropio: true,
    esPrueba: false,
    inicioCicloPrevisto: '2026-10-15T06:00:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000004',
    nombre: 'Escala mensual',
    descripcion: 'Varias APIs y alto volumen',
    precio: 1500,
    moneda: 'GTQ',
    vigenciaDias: 30,
    maxApis: null,
    maxMiembros: null,
    cuotaPeticiones: 10000000,
    dominioPropio: true,
    esPrueba: false,
    inicioCicloPrevisto: '2026-10-15T06:00:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000005',
    nombre: 'Escala anual',
    descripcion: 'Alto volumen, con pago anual',
    precio: 15000,
    moneda: 'GTQ',
    vigenciaDias: 365,
    maxApis: null,
    maxMiembros: null,
    cuotaPeticiones: 120000000,
    dominioPropio: true,
    esPrueba: false,
    inicioCicloPrevisto: '2026-10-15T06:00:00Z',
  },
];

let cliente: QueryClient;

function montar() {
  const enrutador = createMemoryRouter(
    [
      { path: '/', element: <PaginaA01Inicio /> },
      { path: '/registro', element: <h1>Registro destino</h1> },
      { path: '/entrar', element: <h1>Entrar destino</h1> },
    ],
    { initialEntries: ['/'] },
  );
  render(
    <QueryClientProvider client={cliente}>
      <RouterProvider router={enrutador} />
    </QueryClientProvider>,
  );
  return enrutador;
}

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  server.use(
    http.get('http://localhost/api/planes-plataforma', () => HttpResponse.json(planesSiembra)),
  );
});

afterEach(() => {
  cleanup();
  cliente.clear();
});

describe('RF-19 · A0.1 Sitio público: inicio de Shapi (DC-15)', () => {
  it('RF-19 reproduce todas las secciones del mockup: encabezado, héroe, cómo funciona (3 pasos), qué recibe el proveedor, planes y pie', async () => {
    montar();

    // Héroe
    expect(await screen.findByRole('heading', { level: 1, name: 'Shapi' })).toBeDefined();
    expect(screen.getByText('Plataforma como servicio')).toBeDefined();
    expect(
      screen.getByText('Publique su API, controle quién la usa y cobre por ella. Sin modificar el servidor que ya tiene.'),
    ).toBeDefined();

    // Cómo funciona y sus 3 pasos
    expect(screen.getByText('Cómo funciona')).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Tres pasos para publicar y cobrar por su API.' })).toBeDefined();
    expect(
      screen.getByText('Usted conecta su servidor, sube su especificación OpenAPI y define los planes. Shapi se encarga del resto.'),
    ).toBeDefined();
    expect(screen.getByText('01')).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Conectar el servidor de origen' })).toBeDefined();
    expect(screen.getByText('02')).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Cargar la especificación OpenAPI' })).toBeDefined();
    expect(screen.getByText('03')).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Definir los planes' })).toBeDefined();

    // Qué recibe el proveedor
    expect(screen.getByText('Qué recibe el proveedor')).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Todo lo que hace falta para operar una API de paga.' })).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Un portal con su propia marca' })).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Claves de acceso' })).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Cuotas aplicadas en cada petición' })).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Registro de consumo y pagos' })).toBeDefined();

    // Planes y pie
    expect(screen.getByRole('heading', { name: 'Los planes de Shapi.' })).toBeDefined();
    expect(screen.getByText('Precios en quetzales. La vigencia se cuenta desde la contratación del plan.')).toBeDefined();
    expect(
      screen.getByText('Shapi es un proyecto académico de la Universidad Rafael Landívar, Facultad de Ingeniería.'),
    ).toBeDefined();
  });

  it('RF-19 carga los planes de GET /api/planes-plataforma y agrupa Escala mensual y Escala anual en una sola tarjeta', async () => {
    montar();

    const seccionPlanes = await screen.findByRole('region', { name: 'Planes' });
    const tarjetas = await within(seccionPlanes).findAllByRole('article');
    expect(tarjetas).toHaveLength(4);

    // Títulos de las 4 tarjetas
    expect(within(seccionPlanes).getByRole('heading', { name: 'Prueba' })).toBeDefined();
    expect(within(seccionPlanes).getByRole('heading', { name: 'Lanzamiento' })).toBeDefined();
    expect(within(seccionPlanes).getByRole('heading', { name: 'Producto' })).toBeDefined();
    expect(within(seccionPlanes).getByRole('heading', { name: 'Escala' })).toBeDefined();
    expect(within(seccionPlanes).queryByRole('heading', { name: 'Escala anual' })).toBeNull();

    // Datos de Prueba, Lanzamiento y Producto
    expect(within(seccionPlanes).getByText('Q 0.00')).toBeDefined();
    expect(within(seccionPlanes).getByText('Prueba gratis por 30 días')).toBeDefined();
    expect(within(seccionPlanes).getByText('Q 199.00')).toBeDefined();
    expect(within(seccionPlanes).getByText('250,000 peticiones')).toBeDefined();
    expect(within(seccionPlanes).getByText('Q 599.00')).toBeDefined();
    expect(within(seccionPlanes).getByText('2,000,000 peticiones')).toBeDefined();

    // Tarjeta Escala agrupada (mensual + anual)
    const tarjetaEscala = tarjetas[3];
    expect(within(tarjetaEscala).getByText('Varias APIs y alto volumen')).toBeDefined();
    expect(within(tarjetaEscala).getByText('Q 1,500.00')).toBeDefined();
    expect(within(tarjetaEscala).getByText('Mensual · vigencia de 30 días')).toBeDefined();
    expect(
      within(tarjetaEscala).getByText('o Q 15,000.00 anual · 365 días y 120,000,000 peticiones'),
    ).toBeDefined();
    expect(within(tarjetaEscala).getByText('APIs ilimitadas')).toBeDefined();
    expect(within(tarjetaEscala).getByText('10,000,000 peticiones')).toBeDefined();
    expect(within(tarjetaEscala).getByText('Miembros ilimitados')).toBeDefined();
    expect(within(tarjetaEscala).getByText('Dominio propio')).toBeDefined();
  });

  it('RF-19 los enlaces "Crear cuenta" llevan a /registro y "Entrar" lleva a /entrar', async () => {
    const enrutador = montar();
    await screen.findByRole('heading', { level: 1, name: 'Shapi' });

    const enlacesCrear = screen.getAllByRole('link', { name: 'Crear cuenta' });
    expect(enlacesCrear).toHaveLength(2);
    for (const enlace of enlacesCrear) {
      expect(enlace.getAttribute('href')).toBe('/registro');
    }

    const enlaceEntrar = screen.getByRole('link', { name: 'Entrar' });
    expect(enlaceEntrar.getAttribute('href')).toBe('/entrar');

    await userEvent.click(enlacesCrear[0]);
    expect(enrutador.state.location.pathname).toBe('/registro');
  });

  it('RF-19 / RNF-12 permite reintentar cuando falla la consulta de planes de plataforma', async () => {
    server.use(
      http.get('http://localhost/api/planes-plataforma', () => new HttpResponse(null, { status: 500 })),
    );
    montar();

    const reintentar = await screen.findByRole('button', { name: 'Reintentar' });
    server.use(
      http.get('http://localhost/api/planes-plataforma', () => HttpResponse.json(planesSiembra)),
    );
    await userEvent.click(reintentar);

    expect(await screen.findByText('Q 1,500.00')).toBeDefined();
  });
});
