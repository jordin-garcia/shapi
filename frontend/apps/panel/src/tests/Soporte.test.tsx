import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import PaginaA71Casos from '../paginas/A7-1-Casos';
import PaginaA72Caso from '../paginas/A7-2-Caso';
import PaginaA64Casos from '../paginas/A6-4-Casos';
import PaginaA64bCaso from '../paginas/A6-4b-Caso';

const API = 'http://localhost';
let cliente: QueryClient;

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
});

afterEach(() => {
  cleanup();
  cliente.clear();
});

function mostrar(elemento: React.ReactNode, ruta = '/', patron = '*') {
  render(
    <QueryClientProvider client={cliente}>
      <MemoryRouter initialEntries={[ruta]}>
        <Routes><Route path={patron} element={elemento} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const caso = {
  numero: 104,
  asunto: 'El dominio propio no verifica',
  estado: 'abierto' as const,
  organizacion: 'Envios Xelaju, S.A.',
  apiNombre: 'API de Cotizacion de Envios',
  creadoPor: 'Ana Lucia Morales',
  asignadoA: 'Sofia Menchu Cojti',
  creadoEn: '2026-09-11T14:12:00Z',
  mensajes: [
    { id: crypto.randomUUID(), autor: 'Ana Lucia Morales', rol: 'propietario', cuerpo: 'El dominio sigue pendiente.', creadoEn: '2026-09-11T14:12:00Z', esPersonalPlataforma: false },
    { id: crypto.randomUUID(), autor: 'Sofia Menchu Cojti', rol: 'soporte', cuerpo: 'Estoy revisando el registro.', creadoEn: '2026-09-11T14:30:00Z', esPersonalPlataforma: true },
  ],
};

describe('RF-40 · A7 Casos del proveedor', () => {
  it('reproduce la lista y permite abrir un caso', async () => {
    let enviado: Record<string, unknown> | undefined;
    server.use(
      http.get(`${API}/api/casos`, () => HttpResponse.json({ elementos: [{ numero: 104, asunto: caso.asunto, apiNombre: caso.apiNombre, estado: 'abierto', creadoEn: caso.creadoEn, respuestas: 2 }], total: 1 })),
      http.get(`${API}/api/apis`, () => HttpResponse.json({
        elementos: [{ id: '11111111-1111-1111-1111-111111111111', nombre: caso.apiNombre, subdominio: 'envios', estado: 'publicada' }],
        total: 1, planNombre: 'Producto', maxApis: 5,
      })),
      http.post(`${API}/api/casos`, async ({ request }) => {
        enviado = await request.json() as Record<string, unknown>;
        return HttpResponse.json(caso, { status: 201 });
      }),
    );

    mostrar(<PaginaA71Casos />);

    expect(await screen.findByRole('heading', { name: 'Casos de soporte' })).toBeDefined();
    expect(screen.getByText('Escríbale al equipo de soporte de Shapi. Le avisaremos por correo cada vez que le respondan.')).toBeDefined();
    expect(await screen.findByText('CAS-104')).toBeDefined();
    expect(screen.getByText(/2 respuestas/)).toBeDefined();
    await userEvent.type(screen.getByLabelText('Asunto'), 'No puedo publicar');
    await userEvent.selectOptions(screen.getByLabelText(/API afectada/), '11111111-1111-1111-1111-111111111111');
    await userEvent.type(screen.getByLabelText('Descripción'), 'Falla desde ayer.');
    await userEvent.click(screen.getByRole('button', { name: 'Abrir caso' }));

    await waitFor(() => expect(enviado).toEqual({
      asunto: 'No puedo publicar',
      apiId: '11111111-1111-1111-1111-111111111111',
      descripcion: 'Falla desde ayer.',
    }));
  });

  it('muestra la conversacion y envia respuestas', async () => {
    let cuerpo = '';
    server.use(
      http.get(`${API}/api/casos/104`, () => HttpResponse.json(caso)),
      http.post(`${API}/api/casos/104/mensajes`, async ({ request }) => {
        cuerpo = ((await request.json()) as { cuerpo: string }).cuerpo;
        return HttpResponse.json({ ...caso.mensajes[0], id: crypto.randomUUID(), cuerpo }, { status: 201 });
      }),
    );

    mostrar(<PaginaA72Caso />, '/panel/soporte/104', '/panel/soporte/:numero');

    expect(await screen.findByRole('heading', { name: caso.asunto })).toBeDefined();
    expect(screen.getByText('El dominio sigue pendiente.')).toBeDefined();
    expect(screen.getByText('Estoy revisando el registro.')).toBeDefined();
    const conversacion = screen.getByRole('region', { name: 'Conversación' });
    expect(within(conversacion.parentElement!).getByLabelText('Su respuesta')).toBeDefined();
    await userEvent.type(screen.getByLabelText('Su respuesta'), 'Ya hice el cambio.');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar respuesta' }));
    await waitFor(() => expect(cuerpo).toBe('Ya hice el cambio.'));
  });
});

describe('RF-40 · A6.4 Casos de administracion', () => {
  it('reproduce la variante vacia y registra un caso a nombre de una organizacion', async () => {
    let enviado: Record<string, unknown> | undefined;
    server.use(
      http.get(`${API}/api/admin/casos`, () => HttpResponse.json({ elementos: [], total: 0 })),
      http.get(`${API}/api/admin/casos/organizaciones`, () => HttpResponse.json([{
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', nombre: 'Envios Xelaju, S.A.',
        apis: [{ id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', nombre: 'API de Cotizacion de Envios' }],
      }])),
      http.post(`${API}/api/admin/casos`, async ({ request }) => {
        enviado = await request.json() as Record<string, unknown>;
        return HttpResponse.json(caso, { status: 201 });
      }),
    );

    mostrar(<PaginaA64Casos />);

    expect(await screen.findByRole('heading', { name: 'Todavía no hay casos' })).toBeDefined();
    expect(screen.queryByLabelText(/API afectada/)).toBeNull();
    await userEvent.selectOptions(screen.getByLabelText('Organización'), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    await userEvent.type(screen.getByLabelText('Asunto'), 'Reporte telefonico');
    await userEvent.type(screen.getByLabelText('Descripción'), 'La organización reportó una falla.');
    await userEvent.click(screen.getByRole('button', { name: 'Registrar caso' }));

    await waitFor(() => expect(enviado?.organizacionId).toBe('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'));
  });

  it('muestra conversacion, datos solo lectura y permite asignar y cerrar', async () => {
    let asignaciones = 0;
    let cierres = 0;
    server.use(
      http.get(`${API}/api/admin/casos/104`, () => HttpResponse.json({ ...caso, asignadoA: null })),
      http.get(`${API}/api/admin/casos/104/organizacion`, () => HttpResponse.json({
        organizacion: caso.organizacion, apiAfectada: caso.apiNombre, plan: 'Producto',
        cicloInicio: '2026-08-24T06:00:00Z', cicloFin: '2026-09-23T06:00:00Z', estado: 'activa',
        numeroApis: 2, numeroConsumidores: 4, dominioPropio: 'api.enviosxelaju.localhost', verificacionDominio: 'pendiente',
      })),
      http.post(`${API}/api/admin/casos/104/asignar`, () => { asignaciones += 1; return new HttpResponse(null, { status: 204 }); }),
      http.post(`${API}/api/admin/casos/104/cerrar`, () => { cierres += 1; return new HttpResponse(null, { status: 204 }); }),
    );

    mostrar(<PaginaA64bCaso />, '/admin/casos/104', '/admin/casos/:numero');

    expect(await screen.findByText('Datos de la organización')).toBeDefined();
    expect(screen.getByText('Solo lectura. Con el rol de soporte usted consulta los datos de la organización, pero no puede modificarlos.')).toBeDefined();
    expect(screen.getByText('api.enviosxelaju.localhost')).toBeDefined();
    expect(screen.getByText('24 ago 2026 – 22 sep 2026')).toBeDefined();
    expect(within(screen.getByRole('region', { name: 'Conversación' }).parentElement!).getByLabelText('Respuesta')).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Asignarme el caso' }));
    await userEvent.click(screen.getByRole('button', { name: 'Cerrar el caso' }));
    await waitFor(() => expect([asignaciones, cierres]).toEqual([1, 1]));
  });
});
