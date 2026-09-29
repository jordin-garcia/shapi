import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, type RouterProviderProps } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import App from '../App';
import { crearRutas } from '../rutas';
import { server } from '../../../../test/servidor';

const configuracion = {
  nombrePortal: 'Envíos Xelajú',
  colorPrincipal: '#B8322A',
  urlLogo: null,
  bienvenida: 'Cotice envíos a todo el país.',
  nombreApi: 'API de Cotización de Envíos',
  descripcionApi: 'Cotizaciones, guías y rastreo.',
  hostPortal: 'envios.shapi.localhost',
  hostApi: 'envios.api.shapi.localhost',
};

let cliente: QueryClient;
let sesionCerrada: boolean;

function montar(ruta: string) {
  const enrutador = createMemoryRouter(crearRutas(), { initialEntries: [ruta] });
  const vista = render(
    <QueryClientProvider client={cliente}>
      <App enrutador={enrutador as RouterProviderProps['router']} />
    </QueryClientProvider>,
  );
  return { ...vista, enrutador };
}

beforeEach(() => {
  sesionCerrada = false;
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  server.use(
    http.get('http://localhost/api/portal/configuracion', () => HttpResponse.json(configuracion)),
    http.get('http://localhost/api/portal/auth/sesion', () => HttpResponse.json({
      consumidor: { nombre: 'María José Quiñónez', nombreEmpresa: 'Mercadito Antigua' },
      correoVerificado: true,
    })),
    http.post('http://localhost/api/portal/auth/salir', () => {
      sesionCerrada = true;
      return new HttpResponse(null, { status: 200 });
    }),
  );
});

afterEach(() => {
  cleanup();
  cliente.clear();
});

describe('RF-15 · marca dinámica del portal', () => {
  it('aplica --marca-principal y muestra nombre e iniciales sin la marca de Shapi', async () => {
    const { container } = montar('/');

    expect(await screen.findByRole('banner')).toBeDefined();
    expect(screen.getAllByText('Envíos Xelajú').length).toBeGreaterThan(0);
    expect(screen.getAllByText('EX').length).toBeGreaterThan(0);
    expect(container.firstElementChild?.getAttribute('style')).toContain('--marca-principal: #B8322A');
    expect(container.textContent).not.toContain('Shapi');
  });

  it('usa el logo cuando la configuración incluye su URL', async () => {
    server.use(http.get('http://localhost/api/portal/configuracion', () => HttpResponse.json({
      ...configuracion,
      urlLogo: '/api/portal/logo',
    })));

    montar('/planes');

    const logo = await screen.findByRole('img', { name: 'Envíos Xelajú' });
    expect(logo.getAttribute('src')).toBe('/api/portal/logo');
    expect(screen.queryByText('EX')).toBeNull();
  });

  it('muestra API no disponible cuando la configuración responde 404', async () => {
    server.use(http.get('http://localhost/api/portal/configuracion', () => new HttpResponse(null, { status: 404 })));

    montar('/');

    expect(await screen.findByRole('heading', { name: 'API no disponible' })).toBeDefined();
  });
});

describe('RF-16 · rutas y navegación pública', () => {
  it.each([
    ['/', 'A5.0'],
    ['/documentacion/cotizaciones', 'A5.1'],
    ['/consola', 'A5.2'],
    ['/registro', 'A5.3'],
    ['/invitacion?token=abc', 'A5.3b'],
    ['/planes', 'A5.4'],
    ['/contratar/basico', 'A5.6'],
    ['/entrar', 'A5.7'],
    ['/verificar-correo', 'A5.8'],
    ['/recuperar', 'A5.9'],
    ['/restablecer?token=abc', 'A5.10'],
  ])('%s muestra la página de relleno %s', async (ruta, id) => {
    montar(ruta);

    expect(await screen.findByRole('heading', { name: new RegExp(`^${id.replace('.', '\\.')}`) })).toBeDefined();
    expect(screen.getByRole('banner')).toBeDefined();
  });

  it('el encabezado público conserva el orden del mockup A5', async () => {
    montar('/');

    const encabezado = await screen.findByRole('banner');
    expect(within(encabezado).getAllByRole('link').map(enlace => enlace.textContent)).toEqual([
      'EXEnvíos Xelajú', 'Documentación', 'Planes', 'Entrar', 'Crear cuenta',
    ]);
  });
});

describe('RF-16 · cuenta del consumidor', () => {
  it.each([
    ['/cuenta/consumo', 'B2.1'],
    ['/cuenta/pagos', 'B2.2'],
    ['/cuenta/suscripcion', 'B2.3'],
    ['/cuenta/cambiar-plan/comercio', 'B2.7'],
  ])('%s exige sesión y muestra la página de relleno %s', async (ruta, id) => {
    montar(ruta);

    expect(await screen.findByRole('heading', { name: new RegExp(`^${id.replace('.', '\\.')}`) })).toBeDefined();
    expect(screen.getByText('María José Quiñónez')).toBeDefined();
    expect(screen.getByText('Mercadito Antigua')).toBeDefined();
  });

  it('muestra los grupos y enlaces de la barra de cuenta en el orden de B2', async () => {
    montar('/cuenta/suscripcion');

    const barra = await screen.findByRole('navigation');
    expect(within(barra).getAllByText(/^(API|Mi cuenta)$/).map(elemento => elemento.textContent)).toEqual(['API', 'Mi cuenta']);
    expect(within(barra).getAllByRole('link').map(enlace => enlace.textContent)).toEqual([
      'Documentación', 'Consola de pruebas', 'Planes', 'Suscripción y claves', 'Consumo', 'Pagos',
    ]);
  });

  it('redirige a entrar cuando no existe sesión de consumidor', async () => {
    server.use(http.get('http://localhost/api/portal/auth/sesion', () => new HttpResponse(null, { status: 401 })));
    const { enrutador } = montar('/cuenta/suscripcion');

    await waitFor(() => expect(enrutador.state.location.pathname).toBe('/entrar'));
  });

  it('cierra la sesión desde el pie de la barra y vuelve a entrar', async () => {
    const { enrutador } = montar('/cuenta/suscripcion');

    await userEvent.click(await screen.findByRole('button', { name: 'Cerrar sesión' }));

    await waitFor(() => expect(enrutador.state.location.pathname).toBe('/entrar'));
    expect(sesionCerrada).toBe(true);
  });
});
