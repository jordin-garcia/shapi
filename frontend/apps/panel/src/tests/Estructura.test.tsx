import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import type { components } from '@shapi/api/identidad';
import { crearRutas, router } from '../rutas';
import { ErrorRuta } from '../paginas/Error-Ruta';
import { pagina } from '../modulos/navegador';
import { claveSesion } from '../modulos/sesion/useSesion';
import { server } from '../../../../test/servidor';
import fuenteLayoutPanel from '../layouts/LayoutPanel.tsx?raw';
import fuenteLayoutAdmin from '../layouts/LayoutAdmin.tsx?raw';
import fuenteLayoutPublico from '../layouts/LayoutPublico.tsx?raw';
import fuenteNavegacion from '../layouts/Navegacion.tsx?raw';
import fuenteSelectorApi from '../modulos/apis/SelectorApi.tsx?raw';
import fuenteCerrarSesion from '../modulos/sesion/CerrarSesion.tsx?raw';
import fuenteUseCerrarSesion from '../modulos/sesion/useCerrarSesion.ts?raw';
import configuracionPanel from '../../vite.config.ts?raw';
import configuracionPortal from '../../../portal/vite.config.ts?raw';

type Sesion = components['schemas']['Sesion'];
const destinos: Record<Sesion['rol'], Sesion['destino']> = {
  propietario: '/panel/apis', editor: '/panel/apis', lector: '/panel/apis', administrador: '/admin/organizaciones', soporte: '/admin/casos',
};
const nombres: Record<Sesion['rol'], string> = {
  propietario: 'Ana Lucía Morales', editor: 'Luis Pérez', lector: 'Marta Díaz', administrador: 'Carlos Rivas', soporte: 'Sofía Cruz',
};

let rol: Sesion['rol'] = 'propietario';
let cliente: QueryClient;
const respuestaSesion = (): Sesion => ({
  usuario: { nombre: nombres[rol], correo: 'persona@enviosxelaju.com' },
  organizacion: { id: '0b8f7c3e-6d1a-4f2b-9c5e-1a2b3c4d5e6f', nombre: 'Envíos Xelajú, S.A.' },
  rol,
  correoVerificado: true,
  destino: destinos[rol],
});

function montar(enrutador: ReturnType<typeof createMemoryRouter> | typeof router) {
  render(<QueryClientProvider client={cliente}><RouterProvider router={enrutador} /></QueryClientProvider>);
}
async function abrir(ruta: string) {
  await router.navigate(ruta);
  montar(router);
}
const barraLateral = () => screen.findByRole('navigation');

beforeEach(() => {
  rol = 'propietario';
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  server.use(
    http.get('http://localhost/api/auth/sesion', () => HttpResponse.json(respuestaSesion())),
    http.get('http://localhost/api/apis', () => HttpResponse.json({ elementos: [{ id: 'api-1', nombre: 'API de Cotización de Envíos' }], total: 1 })),
    http.get('http://localhost/api/apis/:id/planes', () => HttpResponse.json([])),
  );
});
afterEach(() => { cleanup(); cliente.clear(); vi.restoreAllMocks(); });

describe('RF-07 · H-102 barras laterales completas (N.1, A6 y B3)', () => {
  it('RF-07 N.1: el propietario ve los grupos, los enlaces y el pie en el orden del mockup', async () => {
    await abrir('/panel/apis/api-1/planes');
    const barra = await barraLateral();
    await within(barra).findByRole('combobox', { name: 'API' });
    expect(within(barra).getAllByText(/^(Publicación|API|Organización)$/).map(grupo => grupo.textContent)).toEqual(['Publicación', 'API', 'Organización']);
    expect(within(barra).getAllByRole('link').map(enlace => enlace.textContent)).toEqual([
      'APIs',
      'Especificación', 'Rutas expuestas', 'Configuración por ruta', 'Dominios', 'Portal', 'Planes', 'Claves', 'Consumo', 'Consumidores',
      'Miembros y roles', 'Suscripción de plataforma', 'Historial de pagos', 'Casos de soporte',
      'Ana Lucía Morales',
    ]);
    expect(within(barra).getByRole('link', { name: 'Planes' }).getAttribute('aria-current')).toBe('page');
    expect(within(barra).getByText('Propietario')).toBeDefined();
    expect(within(barra).getByRole('button', { name: 'Cerrar sesión' })).toBeDefined();
    expect(screen.getByRole('banner').textContent).toContain('Envíos Xelajú, S.A.');
  });

  it('RF-07 A6: el administrador ve Plataforma, Soporte y Sistema en el orden del mockup', async () => {
    rol = 'administrador';
    await abrir('/admin/planes');
    const barra = await barraLateral();
    await within(barra).findByRole('link', { name: 'Planes de plataforma' });
    expect(within(barra).getAllByText(/^(Plataforma|Soporte|Sistema)$/).map(grupo => grupo.textContent)).toEqual(['Plataforma', 'Soporte', 'Sistema']);
    expect(within(barra).getAllByRole('link').map(enlace => enlace.textContent)).toEqual([
      'Planes de plataforma', 'Organizaciones', 'Pagos', 'Casos', 'Estado de los componentes', 'Bitácora de acciones', 'Cuentas de plataforma', 'Carlos Rivas',
    ]);
    expect(within(barra).getByText('Administrador')).toBeDefined();
    expect(screen.getByRole('banner').textContent).toContain('Plataforma Shapi');
  });

  it('RF-07 B3: el soporte ve solo Soporte y Sistema, con los textos del mockup', async () => {
    rol = 'soporte';
    await abrir('/admin/estado');
    const barra = await barraLateral();
    await within(barra).findByRole('link', { name: 'Casos' });
    expect(within(barra).getAllByText(/^(Plataforma|Soporte|Sistema)$/).map(grupo => grupo.textContent)).toEqual(['Soporte', 'Sistema', 'Soporte']);
    expect(within(barra).getAllByRole('link').map(enlace => enlace.textContent)).toEqual([
      'Casos', 'Estado de los componentes', 'Bitácora de acciones', 'Sofía Cruz',
    ]);
    expect(within(barra).getByRole('link', { name: 'Estado de los componentes' }).getAttribute('aria-current')).toBe('page');
    expect(screen.getByRole('banner').textContent).toContain('Plataforma Shapi');
  });

  it('RF-07 un 501 del listado de APIs se muestra como "Sin APIs"', async () => {
    server.use(http.get('http://localhost/api/apis', () => new HttpResponse(null, { status: 501 })));
    await abrir('/panel/apis');
    expect(await within(await barraLateral()).findByText('Sin APIs')).toBeDefined();
    expect(within(await barraLateral()).queryByRole('link', { name: 'Especificación' })).toBeNull();
  });
});

describe('RNF-12 · H-94 y H-95 sitio público y errores', () => {
  it('RNF-12 A0.1 (/) se muestra sin el encabezado de A1', async () => {
    await abrir('/');
    expect(await screen.findByText(/^A0-1 ·/)).toBeDefined();
    expect(screen.queryByRole('banner')).toBeNull();
  });

  it('RNF-12 las páginas de A1 conservan el encabezado público', async () => {
    await abrir('/entrar');
    expect(await screen.findByRole('heading', { name: 'Entrar a Shapi' })).toBeDefined();
    expect(screen.getByRole('banner').textContent).toContain('Shapi');
  });

  it('RNF-12 un error que no es 404 muestra el aviso de error con "Reintentar", que vuelve a cargar la página', async () => {
    const recargar = vi.spyOn(pagina, 'recargar').mockImplementation(() => undefined);
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    function Falla(): never { throw new Error('fallo al mostrar la página'); }
    montar(createMemoryRouter([{ path: '/', element: <Falla />, errorElement: <ErrorRuta /> }]));
    const aviso = await screen.findByRole('alert');
    expect(aviso.textContent).toContain('No se pudo cargar la información.');
    expect(screen.queryByText('Página no encontrada')).toBeNull();
    await userEvent.click(within(aviso).getByRole('button', { name: 'Reintentar' }));
    expect(recargar).toHaveBeenCalledOnce();
  });

  it('RNF-12 una dirección que no existe muestra "Página no encontrada"', async () => {
    montar(createMemoryRouter(crearRutas({ desarrollo: true }), { initialEntries: ['/no-existe'] }));
    expect(await screen.findByText('Página no encontrada')).toBeDefined();
    expect(screen.queryByRole('button', { name: 'Reintentar' })).toBeNull();
  });
});

describe('RF-07 · H-96 rutas índice, 403 entre áreas y 404 dentro del panel', () => {
  it.each([
    ['propietario', '/panel', '/panel/apis'],
    ['administrador', '/admin', '/admin/organizaciones'],
    ['soporte', '/admin', '/admin/casos'],
    ['propietario', '/panel/apis/api-1', '/panel/apis/api-1/especificacion'],
  ] as const)('RF-07 el %s que abre %s llega a %s', async (perfil, ruta, destino) => {
    rol = perfil;
    await abrir(ruta);
    await waitFor(() => expect(router.state.location.pathname).toBe(destino));
  });

  it.each([
    ['propietario', '/admin/casos', '/panel/apis'],
    ['administrador', '/panel/apis', '/admin/organizaciones'],
    ['soporte', '/panel/apis', '/admin/casos'],
  ] as const)('RF-07 el %s en %s puede ir a su panel (%s) o cerrar sesión', async (perfil, ruta, destino) => {
    rol = perfil;
    await abrir(ruta);
    expect(await screen.findByText('No tiene permiso para ver esta página')).toBeDefined();
    expect(screen.getByRole('link', { name: 'Ir a su panel' }).getAttribute('href')).toBe(destino);
    let cerrada = false;
    server.use(http.post('http://localhost/api/auth/salir', () => { cerrada = true; return new HttpResponse(null, { status: 200 }); }));
    await userEvent.click(screen.getByRole('button', { name: 'Cerrar sesión' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/entrar'));
    expect(cerrada).toBe(true);
  });

  it('RF-07 dentro del área, el 403 conserva la barra lateral', async () => {
    rol = 'editor';
    await abrir('/panel/miembros');
    expect(await screen.findByText('No tiene permiso para ver esta página')).toBeDefined();
    expect(screen.getByRole('navigation')).toBeDefined();
  });

  it.each([['propietario', '/panel/no-existe'], ['propietario', '/panel/apis/api-1/no-existe'], ['administrador', '/admin/no-existe']] as const)(
    'RF-07 un 404 en %s (%s) conserva el layout', async (perfil, ruta) => {
      rol = perfil;
      await abrir(ruta);
      expect(await screen.findByText('Página no encontrada')).toBeDefined();
      expect(screen.getByRole('navigation')).toBeDefined();
      expect(screen.getByRole('banner')).toBeDefined();
    });
});

describe('RNF-12 · H-97 a H-99 medidas, tokens y altura', () => {
  it('RNF-12 N.1: rellenos de las barras, alturas de línea, selector con flecha y salir con cursor encima', async () => {
    await abrir('/panel/apis/api-1/planes');
    const barra = await barraLateral();
    const selector = await within(barra).findByRole('combobox', { name: 'API' });
    expect(selector.parentElement!.querySelector('svg')).not.toBeNull();
    expect(selector.className).toContain('py-[9px]');
    expect(screen.getByRole('banner').className).toContain('pl-7');
    expect(screen.getByRole('banner').className).toContain('pr-11');
    for (const clase of ['px-4', 'pt-6', 'pb-5', 'w-[272px]']) expect(barra.className).toContain(clase);
    expect(within(barra).getByText('Publicación').className).toContain('leading-[1.2]');
    expect(within(barra).getByRole('link', { name: 'APIs' }).className).toContain('leading-[1.5]');
    const salir = within(barra).getByRole('button', { name: 'Cerrar sesión' });
    expect(salir.className).toContain('hover:border-principal');
    expect(salir.className).toContain('hover:text-tinta');
  });

  it('RNF-12 las barras usan tokens: sin colores hexadecimales escritos a mano', () => {
    for (const [nombre, fuente] of Object.entries({ fuenteLayoutPanel, fuenteLayoutAdmin, fuenteLayoutPublico, fuenteNavegacion, fuenteSelectorApi, fuenteCerrarSesion })) {
      expect(fuente, nombre).not.toMatch(/#[0-9A-Fa-f]{3,8}\b/);
    }
  });

  it('RNF-12 el panel ocupa la altura de la ventana, así que el pie de la barra lateral queda fijo', async () => {
    await abrir('/panel/apis');
    await within(await barraLateral()).findByText('Casos de soporte');
    const raiz = screen.getByRole('banner').parentElement!;
    expect(raiz.className).toContain('h-screen');
    expect(raiz.className).not.toContain('min-h-screen');
    expect(screen.getByRole('main').className).toContain('overflow-auto');
  });

  it('RNF-12 el HMR de Vite no fija el puerto del cliente, así funciona con Caddy y sin él', () => {
    expect(configuracionPanel).not.toContain('clientPort');
    expect(configuracionPortal).not.toContain('clientPort');
  });
});

describe('RNF-12 · H-100 y H-101 sesión, selector y lámina', () => {
  it('RNF-12 la consulta de la sesión se considera vigente 5 minutos', async () => {
    await abrir('/panel/apis');
    await within(await barraLateral()).findByText('Casos de soporte');
    const consulta = cliente.getQueryCache().find({ queryKey: claveSesion });
    expect((consulta?.options as { staleTime?: number }).staleTime).toBe(5 * 60 * 1000);
  });

  it('RNF-12 cerrar sesión usa el mismo cliente que la consulta de la sesión, y el selector usa Selector', () => {
    for (const fuente of [fuenteCerrarSesion, fuenteUseCerrarSesion]) expect(fuente).not.toContain('crearCliente');
    expect(fuenteUseCerrarSesion).toContain("import { clienteSesion } from './useSesion'");
    expect(fuenteSelectorApi).toContain('<Selector');
    expect(fuenteSelectorApi).not.toContain('<select');
  });

  it('RNF-12 /_ui solo existe en desarrollo', async () => {
    montar(createMemoryRouter(crearRutas({ desarrollo: false }), { initialEntries: ['/_ui'] }));
    expect(await screen.findByText('Página no encontrada')).toBeDefined();
    expect(screen.queryByText('Plano azul')).toBeNull();
  });
});
