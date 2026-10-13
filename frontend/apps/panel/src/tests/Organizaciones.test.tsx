import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import PaginaA62Organizaciones from '../paginas/A6-2-Organizaciones';

const API = 'http://localhost/api/admin/organizaciones';
let cliente: QueryClient;

const organizacion = {
  id: '018f87c0-8c31-7e64-9b6a-4fd21583bdb4',
  nombre: 'Transportes Petén, S.A.',
  propietarioCorreo: 'mario.pop@transportespeten.com',
  numeroApis: 2,
  numeroApisPublicadas: 2,
  numeroConsumidores: 6,
  plan: 'Lanzamiento',
  cicloInicio: '2026-09-03T06:00:00Z',
  cicloFin: '2026-10-03T06:00:00Z',
  estado: 'en_gracia',
  suspendidaAdministrativamente: false,
  motivoSuspension: null,
};

function sesion(rol: 'administrador' | 'soporte' = 'administrador') {
  return http.get('http://localhost/api/auth/sesion', () => HttpResponse.json({
    usuario: { id: '018f87c0-8c31-7e64-9b6a-4fd21583bdb5', nombre: 'Rodrigo Alvarado', correo: 'rodrigo@shapi.test' },
    organizacion: { id: '018f87c0-8c31-7e64-9b6a-4fd21583bdb6', nombre: 'Shapi', tipo: 'plataforma' },
    rol,
    correoVerificado: true,
    destino: rol === 'administrador' ? '/admin/organizaciones' : '/admin/casos',
  }));
}

function mostrar() {
  render(<QueryClientProvider client={cliente}><PaginaA62Organizaciones /></QueryClientProvider>);
}

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
});

afterEach(() => {
  cleanup();
  cliente.clear();
});

describe('RF-38 · A6.2 Administración de organizaciones', () => {
  it('muestra los textos, columnas y datos del mockup', async () => {
    const envios = {
      ...organizacion,
      id: '018f87c0-8c31-7e64-9b6a-4fd21583bdb3',
      nombre: 'Envíos Xelajú, S.A.',
      propietarioCorreo: 'ana.morales@enviosxelaju.com',
      plan: 'Producto',
      estado: 'activa',
      cicloInicio: '2026-08-24T06:00:00Z',
      cicloFin: '2026-09-22T06:00:00Z',
    };
    server.use(sesion(), http.get(API, () => HttpResponse.json([envios, organizacion])));

    mostrar();

    expect(await screen.findByRole('heading', { name: 'Organizaciones' })).toBeDefined();
    expect(screen.getByText('Organizaciones proveedoras registradas en Shapi y el estado de su suscripción de plataforma.')).toBeDefined();
    expect(await screen.findByText('Transportes Petén, S.A.')).toBeDefined();
    expect(screen.getAllByRole('columnheader').map(celda => celda.textContent)).toEqual([
      'Organización y propietario', 'APIs', 'Plan', 'Ciclo vigente', 'Estado', 'Acción',
    ]);
    expect(screen.getByText('mario.pop@transportespeten.com')).toBeDefined();
    expect(screen.getByText('Lanzamiento')).toBeDefined();
    expect(screen.getByText('3 sep – 3 oct 2026')).toBeDefined();
    expect(screen.getByText('En gracia')).toBeDefined();
    expect(screen.getByRole('button', { name: 'Suspender Transportes Petén, S.A.' })).toBeDefined();
    expect(screen.getAllByRole('row').slice(1).map(fila => fila.querySelector('td')?.textContent)).toEqual([
      'Envíos Xelajú, S.A.ana.morales@enviosxelaju.com',
      'Transportes Petén, S.A.mario.pop@transportespeten.com',
    ]);
  });

  it('reproduce la variante vacia del mockup', async () => {
    server.use(sesion(), http.get(API, () => HttpResponse.json([])));
    mostrar();

    expect(await screen.findByRole('heading', { name: 'Todavía no hay organizaciones registradas' })).toBeDefined();
    expect(screen.getByText('Las empresas que se registren en Shapi aparecerán aquí con su plan de plataforma y el estado de su suscripción.')).toBeDefined();
    expect(screen.getAllByRole('columnheader').map(celda => celda.textContent)).toEqual([
      'Organización', 'APIs', 'Plan', 'Ciclo vigente', 'Estado', 'Acción',
    ]);
  });

  it('abre A6.2b, exige el motivo y suspende la organizacion', async () => {
    let motivo = '';
    server.use(
      sesion(),
      http.get(API, () => HttpResponse.json([organizacion])),
      http.post(`${API}/${organizacion.id}/suspender`, async ({ request }) => {
        motivo = ((await request.json()) as { motivo: string }).motivo;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    mostrar();

    await userEvent.click(await screen.findByRole('button', { name: 'Suspender Transportes Petén, S.A.' }));
    expect(screen.getByRole('heading', { name: 'Suspender organización' })).toBeDefined();
    expect(screen.getByText('Consumidores afectados')).toBeDefined();
    expect(screen.getByText('6')).toBeDefined();
    await userEvent.type(screen.getByLabelText('Motivo administrativo'), 'Incumplimiento de los terminos de servicio');
    await userEvent.click(screen.getByRole('button', { name: 'Suspender organización' }));

    await waitFor(() => expect(motivo).toBe('Incumplimiento de los terminos de servicio'));
    expect(await screen.findByRole('heading', { name: 'Organizaciones' })).toBeDefined();
  });

  it('permite reactivar y oculta las acciones al soporte', async () => {
    let reactivada = false;
    server.use(
      sesion(),
      http.get(API, () => HttpResponse.json([{ ...organizacion, estado: 'suspendida', suspendidaAdministrativamente: true }])),
      http.post(`${API}/${organizacion.id}/reactivar`, () => {
        reactivada = true;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    mostrar();
    await userEvent.click(await screen.findByRole('button', { name: 'Reactivar Transportes Petén, S.A.' }));
    await waitFor(() => expect(reactivada).toBe(true));
    cleanup();
    cliente.clear();
    cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    server.use(sesion('soporte'), http.get(API, () => HttpResponse.json([organizacion])));

    mostrar();

    await screen.findByText('Transportes Petén, S.A.');
    expect(screen.queryByRole('button', { name: /Suspender|Reactivar/ })).toBeNull();
  });

  it('muestra y permite reintentar un error al reactivar', async () => {
    let intentos = 0;
    server.use(
      sesion(),
      http.get(API, () => HttpResponse.json([{ ...organizacion, estado: 'suspendida', suspendidaAdministrativamente: true }])),
      http.post(`${API}/${organizacion.id}/reactivar`, () => {
        intentos += 1;
        return intentos === 1 ? HttpResponse.json({ codigo: 'error_interno' }, { status: 500 }) : new HttpResponse(null, { status: 204 });
      }),
    );
    mostrar();

    await userEvent.click(await screen.findByRole('button', { name: 'Reactivar Transportes Petén, S.A.' }));
    expect(await screen.findByText('No se pudo cargar la información.')).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }));

    await waitFor(() => expect(intentos).toBe(2));
  });
});
