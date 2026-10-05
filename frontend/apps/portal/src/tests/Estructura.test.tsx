import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
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
  nombreOrganizacion: 'Envíos Xelajú, S.A.',
};

let cliente: QueryClient;

function Explota(): never {
  throw new Error('Falla de render simulada');
}
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
  vi.restoreAllMocks();
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
    // El logo reemplaza la insignia del encabezado; la del pie es la de la organización (H-73).
    expect(within(screen.getByRole('banner')).queryByText('EX')).toBeNull();
  });

  // H-73: el pie lleva la insignia de 24 px (radio de 6 px) con las iniciales y el nombre de la organización (mockup A5).
  it('el pie muestra la insignia y el nombre de la organización', async () => {
    montar('/');

    const pie = await screen.findByRole('contentinfo');
    expect(within(pie).getByText('Envíos Xelajú, S.A.')).toBeDefined();
    const insignia = within(pie).getByText('EX');
    expect(insignia.className).toContain('size-6');
    expect(insignia.className).toContain('rounded-[6px]');
    expect(insignia.className).toContain('bg-[var(--marca-principal)]');
  });

  // H-74: la raíz redefine los colores de @shapi/ui con la marca, así que no queda el azul de Shapi en ninguna pantalla.
  it('la raíz del portal redefine los colores principales con la marca', async () => {
    const { container } = montar('/planes');
    await screen.findByRole('banner');

    const raiz = container.firstElementChild as HTMLElement;
    expect(raiz.style.getPropertyValue('--principal')).toBe('var(--marca-principal)');
    expect(raiz.style.getPropertyValue('--principal-hover')).toContain('var(--marca-principal)');
    expect(raiz.style.getPropertyValue('--anillo-foco')).toContain('var(--marca-principal)');
  });

  // H-79: un 5xx de la configuración es un error recuperable, no «API no disponible».
  it('muestra un error con Reintentar si la configuración responde 5xx', async () => {
    server.use(http.get('http://localhost/api/portal/configuracion', () => new HttpResponse(null, { status: 503 })));

    montar('/');

    expect(await screen.findByRole('button', { name: 'Reintentar' })).toBeDefined();
    expect(screen.queryByRole('heading', { name: 'API no disponible' })).toBeNull();
  });

  it('muestra API no disponible cuando la configuración responde 404', async () => {
    server.use(http.get('http://localhost/api/portal/configuracion', () => HttpResponse.json(
      { status: 404, title: 'Not Found' },
      { status: 404, headers: { 'Content-Type': 'application/problem+json' } },
    )));

    montar('/');

    expect(await screen.findByRole('heading', { name: 'API no disponible' })).toBeDefined();
  });
});

describe('RF-16 · rutas y navegación pública', () => {
  it.each([
    ['/', 'A5.0'],
    ['/documentacion/cotizaciones', 'A5.1'],
    ['/consola', 'A5.2'],
    ['/planes', 'A5.4'],
    ['/contratar/basico', 'A5.6'],
  ])('%s muestra la página de relleno %s', async (ruta, id) => {
    montar(ruta);

    expect(await screen.findByRole('heading', { name: new RegExp(`^${id.replace('.', '\\.')}`) })).toBeDefined();
    expect(screen.getByRole('banner')).toBeDefined();
  });

  // Decidido (3 oct, DC-03): «Documentación» lleva a /documentacion, que existe (DC-07 la redirigirá a la primera ruta).
  it('los enlaces de Documentación llevan a una ruta que existe', async () => {
    const { enrutador } = montar('/');

    const enlace = within(await screen.findByRole('banner')).getByRole('link', { name: 'Documentación' });
    expect(enlace.getAttribute('href')).toBe('/documentacion');
    await userEvent.click(enlace);
    await waitFor(() => expect(enrutador.state.location.pathname).toBe('/documentacion'));
    expect(await screen.findByRole('heading', { name: /^A5\.1/ })).toBeDefined();
  });

  // H-79: la ruta comodín muestra «Página no encontrada».
  it('una ruta que no existe muestra Página no encontrada', async () => {
    montar('/no-existe');

    expect(await screen.findByRole('heading', { name: 'Página no encontrada' })).toBeDefined();
  });

  // H-77: todas las rutas de primer nivel tienen errorElement, para no mostrar la pantalla en inglés de React Router.
  it('cada grupo de rutas tiene un elemento de error en español', async () => {
    expect(crearRutas().every(ruta => ruta.errorElement !== undefined)).toBe(true);

    const rutas = crearRutas();
    rutas[0].children!.unshift({ path: '/explota', element: <Explota /> });
    const silenciar = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    render(
      <QueryClientProvider client={cliente}>
        <App enrutador={createMemoryRouter(rutas, { initialEntries: ['/explota'] }) as RouterProviderProps['router']} />
      </QueryClientProvider>,
    );

    expect(await screen.findByRole('button', { name: 'Reintentar' })).toBeDefined();
    expect(document.body.textContent).not.toContain('Unexpected Application Error');
    expect(silenciar).toHaveBeenCalled();
  });

  // Revisión del paso 9: con una empresa larga, el nombre se recorta y el botón de cerrar sesión queda en su fila.
  it('el bloque del nombre se recorta en vez de bajar el botón de cerrar sesión', async () => {
    server.use(http.get('http://localhost/api/portal/auth/sesion', () => HttpResponse.json({
      consumidor: { nombre: 'María José Quiñónez', nombreEmpresa: 'Distribuidora Comercial del Altiplano, S.A.' },
      correoVerificado: true,
    })));
    montar('/cuenta/suscripcion');

    const empresa = await screen.findByText('Distribuidora Comercial del Altiplano, S.A.');
    expect(empresa.className).toContain('truncate');
    const bloque = empresa.parentElement!;
    for (const clase of ['min-w-0', 'flex-1', 'basis-0']) expect(bloque.className).toContain(clase);
    expect(bloque.parentElement!.contains(screen.getByRole('button', { name: 'Cerrar sesión' }))).toBe(true);
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

  // H-78: /cuenta lleva a la suscripción en vez de mostrar el layout vacío.
  it('/cuenta redirige a la suscripción', async () => {
    const { enrutador } = montar('/cuenta');

    await waitFor(() => expect(enrutador.state.location.pathname).toBe('/cuenta/suscripcion'));
    expect(await screen.findByRole('heading', { name: /^B2\.3/ })).toBeDefined();
  });

  // H-75: el enlace activo usa el fondo teñido con la marca del mockup B2.
  it('el enlace activo de la barra usa el fondo teñido con la marca', async () => {
    montar('/cuenta/suscripcion');

    const activo = await screen.findByRole('link', { name: 'Suscripción y claves' });
    expect(activo.className).toContain('bg-[color-mix(in_srgb,var(--marca-principal)_8%,#FFFFFF)]');
    expect(activo.className).toContain('text-[var(--marca-principal)]');
    expect(screen.getByRole('link', { name: 'Consumo' }).className).toContain('text-[#2B3547]');
  });

  // H-79: un 5xx de la sesión en /cuenta/* muestra un error recuperable, sin mandar a entrar.
  it('muestra un error con Reintentar si la sesión responde 5xx', async () => {
    server.use(http.get('http://localhost/api/portal/auth/sesion', () => new HttpResponse(null, { status: 500 })));
    const { enrutador } = montar('/cuenta/consumo');

    expect(await screen.findByRole('button', { name: 'Reintentar' })).toBeDefined();
    expect(enrutador.state.location.pathname).toBe('/cuenta/consumo');
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

  // H-76: al cerrar la sesión se borran los datos del consumidor de la caché; la configuración del portal se queda.
  it('al cerrar la sesión borra los datos del consumidor de la caché', async () => {
    cliente.setQueryData(['portal', 'claves'], [{ id: 'clave-1' }]);
    const { enrutador } = montar('/cuenta/suscripcion');

    await userEvent.click(await screen.findByRole('button', { name: 'Cerrar sesión' }));

    await waitFor(() => expect(enrutador.state.location.pathname).toBe('/entrar'));
    expect(cliente.getQueryData(['portal', 'claves'])).toBeUndefined();
    expect(cliente.getQueryData(['portal', 'configuracion'])).toBeDefined();
  });

  // H-76: si cerrar la sesión falla, se avisa con «Reintentar» (11 §4) y no se navega.
  it('si cerrar la sesión falla, muestra un aviso con Reintentar', async () => {
    server.use(http.post('http://localhost/api/portal/auth/salir', () => new HttpResponse(null, { status: 503 })));
    const { enrutador } = montar('/cuenta/suscripcion');

    await userEvent.click(await screen.findByRole('button', { name: 'Cerrar sesión' }));

    expect(await screen.findByText('No se pudo cerrar la sesión.')).toBeDefined();
    expect(screen.getByRole('button', { name: 'Reintentar' })).toBeDefined();
    expect(enrutador.state.location.pathname).toBe('/cuenta/suscripcion');
  });
});
