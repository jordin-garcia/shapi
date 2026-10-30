import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import PaginaA21PlanesPlataforma from '../paginas/A2-1-PlanesPlataforma';
import PaginaA63Pagos from '../paginas/A6-3-Pagos';
import PaginaB14Suscripcion from '../paginas/B1-4-Suscripcion';

let cliente: QueryClient;

function mostrar(pagina: React.ReactNode) {
  render(<QueryClientProvider client={cliente}><MemoryRouter>{pagina}</MemoryRouter></QueryClientProvider>);
}

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
});

afterEach(() => {
  cleanup();
  cliente.clear();
});

describe('EM-24 · diferencias visuales contra los mockups', () => {
  it('criterio 1 · A2.1 marca el plan vigente con «Plan actual»', async () => {
    const producto = {
      id: 'producto', nombre: 'Producto', descripcion: 'Proveedores con clientes establecidos', precio: 599,
      vigenciaDias: 30, maxApis: 10, maxMiembros: 10, cuotaPeticiones: 2_000_000, dominioPropio: true, esPrueba: false,
    };
    server.use(
      http.get('*/api/planes-plataforma', () => HttpResponse.json([{
        id: 'prueba', nombre: 'Prueba', descripcion: 'Publicar una primera API sin costo', precio: 0,
        vigenciaDias: 30, maxApis: 1, maxMiembros: 1, cuotaPeticiones: 10_000, dominioPropio: false, esPrueba: true,
      }, producto])),
      http.get('*/api/suscripcion', () => HttpResponse.json({ plan: producto, estado: 'activa' })),
    );

    mostrar(<PaginaA21PlanesPlataforma />);

    const tarjeta = (await screen.findByRole('heading', { name: 'Producto' })).closest('article')!;
    expect(within(tarjeta).getByText('Plan actual', { selector: 'p' })).toBeDefined();
  });

  it('criterio 2 · A6.3 abrevia septiembre como «sep»', async () => {
    server.use(http.get('*/api/admin/pagos', () => HttpResponse.json([{
      id: 'pago-1', fecha: '2026-09-23T06:00:00Z', organizacion: 'Envíos Xelajú, S.A.', plan: 'Producto',
      monto: 599, moneda: 'GTQ', tarjeta: 'Visa •••• 4821', estado: 'pagado',
    }])));

    mostrar(<PaginaA63Pagos />);

    expect(await screen.findByText('23 sep 2026')).toBeDefined();
    expect(screen.queryByText(/sept 2026/)).toBeNull();
  });

  it('criterio 3 · B1.4 reproduce el encabezado y la fecha de renovación del mockup', async () => {
    server.use(
      http.get('*/api/auth/sesion', () => HttpResponse.json({
        usuario: { nombre: 'Ana' }, organizacion: { id: 'org', nombre: 'Envíos Xelajú, S.A.' },
        rol: 'propietario', correoVerificado: true,
      })),
      http.get('*/api/suscripcion', () => HttpResponse.json({
        plan: { id: 'producto', nombre: 'Producto', precio: 599, vigenciaDias: 30, esPrueba: false },
        estado: 'activa', periodo: { inicio: '2026-08-24T06:00:00Z', fin: '2026-09-23T06:00:00Z' },
        proximaRenovacion: '2026-09-23T06:00:00Z', tarjetaEnmascarada: 'Visa •••• 4821',
        periodoVigente: true, avisoPrueba: null, pagoRevertido: null,
      })),
      http.get('*/api/suscripcion/uso', () => HttpResponse.json({ peticiones: 12, cuotaPeticiones: 500, porcentaje: 2.4 })),
    );

    mostrar(<PaginaB14Suscripcion />);

    const encabezado = await screen.findByRole('heading', { name: 'Suscripción de plataforma' });
    const grupo = encabezado.closest('header')!;
    expect(within(grupo).getByText('Organización')).toBeDefined();
    expect(within(grupo).getByRole('link', { name: 'Cambiar de plan' })).toBeDefined();
    expect(await screen.findByText('23 de septiembre de 2026')).toBeDefined();
  });
});
