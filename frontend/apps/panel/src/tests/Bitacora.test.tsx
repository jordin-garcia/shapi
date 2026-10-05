import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
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
  vi.useRealTimers();
});

function entrada(indice: number) {
  return {
    fecha: '2026-09-11T15:48:00Z',
    actor: { nombre: `Persona ${indice}`, rol: 'administrador', organizacion: 'Plataforma Shapi' },
    accion: 'organizacion.suspendida',
    descripcion: `Acción ${indice}`,
  };
}

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

  // H-91: la primera consulta pide hoy y los seis días anteriores, en Guatemala (10 §7).
  it('la primera consulta lleva el periodo predeterminado en Guatemala', async () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-09-12T05:30:00Z')); // 11 sep, 23:30 en Guatemala
    const consultas: URL[] = [];
    server.use(http.get(API, ({ request }) => {
      consultas.push(new URL(request.url));
      return HttpResponse.json({ total: 0, elementos: [] });
    }));

    mostrar();

    await screen.findByText('Todavía no hay acciones registradas');
    expect(consultas[0].searchParams.get('desde')).toBe('2026-09-05');
    expect(consultas[0].searchParams.get('hasta')).toBe('2026-09-11');
    expect(consultas[0].searchParams.get('pagina')).toBe('1');
    expect(screen.getByRole('button', { name: 'Seleccionar periodo' }).textContent).toContain('Del 5 al 11 de septiembre de 2026');
  });

  // H-91: si la consulta falla, se muestra el error con «Reintentar» (11 §4).
  it('muestra el error con Reintentar si la consulta falla', async () => {
    let intentos = 0;
    server.use(http.get(API, () => {
      intentos += 1;
      return intentos === 1 ? new HttpResponse(null, { status: 500 }) : HttpResponse.json({ total: 1, elementos: [entrada(1)] });
    }));
    mostrar();

    await userEvent.click(await screen.findByRole('button', { name: 'Reintentar' }));

    expect(await screen.findByText('Acción 1')).toBeDefined();
  });

  // H-86 (decidido el 3 oct): con más de 20 acciones aparece un paginador; con una sola página, no.
  it('pagina los resultados cuando hay más de una página', async () => {
    const paginas: string[] = [];
    server.use(http.get(API, ({ request }) => {
      const pagina = new URL(request.url).searchParams.get('pagina')!;
      paginas.push(pagina);
      return HttpResponse.json({ total: 25, elementos: pagina === '1' ? [entrada(1)] : [entrada(21)] });
    }));
    mostrar();
    await screen.findByText('Acción 1');

    const paginador = screen.getByRole('navigation', { name: 'Páginas de la bitácora' });
    expect(paginador.textContent).toContain('Página 1 de 2');
    expect((screen.getByRole('button', { name: 'Anterior' }) as HTMLButtonElement).disabled).toBe(true);
    await userEvent.click(screen.getByRole('button', { name: 'Siguiente' }));

    expect(await screen.findByText('Acción 21')).toBeDefined();
    expect(paginas).toEqual(['1', '2']);
    expect(screen.getByRole('navigation', { name: 'Páginas de la bitácora' }).textContent).toContain('Página 2 de 2');
  });

  it('al cambiar el periodo vuelve a la página 1', async () => {
    const consultas: URL[] = [];
    server.use(http.get(API, ({ request }) => {
      const url = new URL(request.url);
      consultas.push(url);
      return HttpResponse.json({ total: 25, elementos: [entrada(url.searchParams.get('pagina') === '1' ? 1 : 21)] });
    }));
    mostrar();
    await screen.findByText('Acción 1');
    await userEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await screen.findByText('Acción 21');

    await userEvent.click(screen.getByRole('button', { name: 'Seleccionar periodo' }));
    await userEvent.clear(screen.getByLabelText('Desde'));
    await userEvent.type(screen.getByLabelText('Desde'), '2026-09-01');
    await userEvent.clear(screen.getByLabelText('Hasta'));
    await userEvent.type(screen.getByLabelText('Hasta'), '2026-09-03');
    await userEvent.click(screen.getByRole('button', { name: 'Aplicar' }));

    await waitFor(() => expect(consultas.at(-1)?.searchParams.get('desde')).toBe('2026-09-01'));
    expect(consultas.at(-1)?.searchParams.get('pagina')).toBe('1');
  });

  it('no muestra el paginador con una sola página', async () => {
    server.use(http.get(API, () => HttpResponse.json({ total: 1, elementos: [entrada(1)] })));
    mostrar();
    await screen.findByText('Acción 1');

    expect(screen.queryByRole('navigation', { name: 'Páginas de la bitácora' })).toBeNull();
  });

  it('reproduce la variante vacía del mockup', async () => {
    // RF-41 · CA2.
    server.use(http.get(API, () => HttpResponse.json({ total: 0, elementos: [] })));
    mostrar();

    expect(await screen.findByRole('heading', { name: 'Todavía no hay acciones registradas' })).toBeDefined();
    expect(screen.getByText('En este periodo nadie suspendió una organización, revirtió un cobro, cambió una cuenta de plataforma ni rotó una clave. Cuando ocurra, aquí aparecerá quién lo hizo, qué hizo y cuándo.')).toBeDefined();
  });
});
