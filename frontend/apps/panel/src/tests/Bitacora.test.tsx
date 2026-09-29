import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import PaginaB32Bitacora from '../paginas/B3-2-Bitacora';

const API = 'http://localhost/api/admin/bitacora';
let cliente: QueryClient;

function mostrar() {
  render(<QueryClientProvider client={cliente}><PaginaB32Bitacora /></QueryClientProvider>);
}

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
});

afterEach(() => {
  cleanup();
  cliente.clear();
});

describe('RF-41 · B3.2 Bitácora de acciones sensibles', () => {
  it('muestra los textos, las columnas y los datos del mockup', async () => {
    // RF-41 · CA2.
    server.use(http.get(API, () => HttpResponse.json({
      total: 2,
      elementos: [
        {
          fecha: '2026-09-11T15:48:00Z',
          actor: { nombre: 'Rodrigo Alvarado', rol: 'administrador', organizacion: 'Plataforma Shapi' },
          accion: 'organizacion.suspendida',
          descripcion: 'Suspendió la organización Datos Chapines, S.A.',
        },
        {
          fecha: '2026-09-11T14:30:00Z',
          actor: { nombre: 'Sofía Menchú Cojtí', rol: 'soporte', organizacion: 'Plataforma Shapi' },
          accion: 'caso.abierto',
          descripcion: 'Abrió el caso CAS-104 de Envíos Xelajú, S.A.',
        },
      ],
    })));

    mostrar();

    expect(await screen.findByRole('heading', { name: 'Bitácora de acciones sensibles' })).toBeDefined();
    expect(screen.getByText('Sistema')).toBeDefined();
    expect(screen.getByText('Quedan registradas las acciones que cambian el acceso, el cobro o el estado de una organización.')).toBeDefined();
    expect((await screen.findAllByRole('columnheader')).map(celda => celda.textContent)).toEqual(['Fecha', 'Usuario', 'Acción']);
    expect(screen.getAllByText('11 sep 2026')).toHaveLength(2);
    expect(screen.getByText('09:48')).toBeDefined();
    expect(screen.getByText('Rodrigo Alvarado')).toBeDefined();
    expect(screen.getByText('Administrador · Plataforma Shapi')).toBeDefined();
    expect(screen.getByText('Suspendió la organización Datos Chapines, S.A.')).toBeDefined();
    expect(screen.getByText('Soporte · Plataforma Shapi')).toBeDefined();
  });

  it('permite elegir un periodo y lo vuelve a consultar', async () => {
    // RF-41 · CA2: selector de fechas.
    const consultas: URL[] = [];
    server.use(http.get(API, ({ request }) => {
      consultas.push(new URL(request.url));
      return HttpResponse.json({ total: 0, elementos: [] });
    }));
    mostrar();
    await screen.findByText('Todavía no hay acciones registradas');

    await userEvent.click(screen.getByRole('button', { name: 'Seleccionar periodo' }));
    await userEvent.clear(screen.getByLabelText('Desde'));
    await userEvent.type(screen.getByLabelText('Desde'), '2026-09-05');
    await userEvent.clear(screen.getByLabelText('Hasta'));
    await userEvent.type(screen.getByLabelText('Hasta'), '2026-09-11');
    await userEvent.click(screen.getByRole('button', { name: 'Aplicar' }));

    await waitFor(() => expect(consultas.length).toBe(2));
    expect(consultas[1].searchParams.get('desde')).toBe('2026-09-05');
    expect(consultas[1].searchParams.get('hasta')).toBe('2026-09-11');
    expect(screen.getByRole('button', { name: 'Seleccionar periodo' }).textContent).toContain('Del 5 al 11 de septiembre de 2026');
  });

  it('reproduce la variante vacía del mockup', async () => {
    // RF-41 · CA2.
    server.use(http.get(API, () => HttpResponse.json({ total: 0, elementos: [] })));
    mostrar();

    expect(await screen.findByRole('heading', { name: 'Todavía no hay acciones registradas' })).toBeDefined();
    expect(screen.getByText('En este periodo nadie suspendió una organización, revirtió un cobro, cambió una cuenta de plataforma ni rotó una clave. Cuando ocurra, aquí aparecerá quién lo hizo, qué hizo y cuándo.')).toBeDefined();
  });
});
