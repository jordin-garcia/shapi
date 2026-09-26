import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RouterProvider } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse, delay } from 'msw';
import catalogo from '../../../../../docs/specs/11-interfaz.md?raw';
import { router } from '../rutas';
import { server } from '../../../../test/servidor';

let rol = 'propietario';
let cliente: QueryClient;
const respuestaSesion = () => ({
  usuario: { nombre: 'Ana', correo: 'ana@enviosxelaju.com' },
  organizacion: { id: 'org-1', nombre: 'Envíos Xelajú, S.A.' },
  rol,
  correoVerificado: true,
  destino: rol === 'administrador' ? '/admin/organizaciones' : rol === 'soporte' ? '/admin/casos' : '/panel/apis',
});
const rutas = [...catalogo.matchAll(/^\| (A[0-468][\w.]*|A7[\w.]*|B[13][\w.]*) \|[^\n]+$/gm)]
  .flatMap(([fila, id]) => [...fila.matchAll(/`(\/(?:[^`]*))`/g)].map(([, ruta]) => ({
    ruta: ruta.replace(':id', 'api-1').replace(':plan', 'escala').replace(':numero', '123'),
    id: id.replace('.', '-'),
  })));
rutas.push({ ruta: '/', id: 'A0-1' });
// Las pantallas ya implementadas se reconocen por su título, en lugar del texto de relleno "<ID> ·".
const implementadas: Record<string, string> = { 'A1-1': 'Crear una cuenta', 'A1-2': 'Revise su correo', 'A1-3': 'Entrar a Shapi' };
async function esperarPantalla(id: string) {
  if (implementadas[id]) expect(await screen.findByRole('heading', { name: implementadas[id] })).toBeDefined();
  else expect(await screen.findByText(new RegExp(`^${id} ·`))).toBeDefined();
}

async function abrir(ruta: string) {
  await router.navigate(ruta);
  render(<QueryClientProvider client={cliente}><RouterProvider router={router} /></QueryClientProvider>);
}

beforeEach(() => {
  rol = 'propietario';
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  server.use(
    http.get('http://localhost/api/auth/sesion', () => HttpResponse.json(respuestaSesion())),
    http.get('http://localhost/api/apis', () => HttpResponse.json({ elementos: [{ id: 'api-1', nombre: 'Envíos Xelajú' }, { id: 'api-2', nombre: 'Agro Precios' }], total: 2 })),
  );
});
afterEach(() => { cleanup(); cliente.clear(); });

describe('RF-07 / RNF-12 · catálogo y permisos', () => {
  it.each(rutas)('$ruta muestra $id', async ({ ruta, id }) => {
    rol = ruta.startsWith('/admin') ? 'administrador' : 'propietario';
    await abrir(ruta);
    await esperarPantalla(id);
  });
  it.each([
    ['administrador', '/panel/apis'], ['soporte', '/panel/apis'],
    ['propietario', '/admin/casos'], ['editor', '/admin/casos'], ['lector', '/admin/casos'],
    ['soporte', '/admin/planes'], ['soporte', '/admin/pagos'], ['soporte', '/admin/cuentas'],
    ['editor', '/panel/pagos'], ['editor', '/panel/suscripcion'], ['editor', '/panel/miembros'],
    ['lector', '/panel/miembros'], ['lector', '/panel/apis/nueva'],
    ['lector', '/panel/apis/api-1/consumidores/invitar'],
    ['lector', '/panel/suscripcion/contratar/escala'], ['editor', '/panel/suscripcion/cambiar/escala'],
  ])('%s no entra a %s', async (perfil, ruta) => {
    rol = perfil;
    await abrir(ruta);
    expect(await screen.findByText('No tiene permiso para ver esta página')).toBeDefined();
  });
  it('RF-04 redirige sin sesión a entrar (incluido ProblemDetails 401)', async () => {
    server.use(http.get('http://localhost/api/auth/sesion', () => HttpResponse.json({ status: 401, title: 'Sin sesión' }, { status: 401, headers: { 'Content-Type': 'application/problem+json' } })));
    await abrir('/panel/apis');
    await waitFor(() => expect(router.state.location.pathname).toBe('/entrar'));
  });
  it.each([['/registro', 'A1-1'], ['/verificar-correo', 'A1-2'], ['/entrar', 'A1-3']])('permite la página pública %s sin consultar la sesión', async (ruta, id) => {
    let solicitudes = 0;
    server.use(http.get('http://localhost/api/auth/sesion', () => { solicitudes++; return new HttpResponse(null, { status: 401 }); }));
    await abrir(ruta);
    await esperarPantalla(id);
    expect(solicitudes).toBe(0);
  });
  it('muestra error recuperable, sin redirigir, si falla el servidor', async () => {
    server.use(http.get('http://localhost/api/auth/sesion', () => new HttpResponse(null, { status: 500 })));
    await abrir('/panel/apis');
    await userEvent.click(await screen.findByRole('button', { name: 'Reintentar' }));
    expect(router.state.location.pathname).toBe('/panel/apis');
    server.use(http.get('http://localhost/api/auth/sesion', () => HttpResponse.json(respuestaSesion())));
    await userEvent.click(await screen.findByRole('button', { name: 'Reintentar' }));
    expect(await screen.findByText(/^A3-1 ·/)).toBeDefined();
  });
  it('selecciona desde la lista y conserva el ID en los enlaces', async () => {
    await abrir('/panel/apis');
    await userEvent.selectOptions(await screen.findByRole('combobox', { name: 'API' }), 'api-2');
    await waitFor(() => expect(router.state.location.pathname).toBe('/panel/apis/api-2/especificacion'));
    await userEvent.click(screen.getByRole('link', { name: 'Planes' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/panel/apis/api-2/planes'));
    expect(screen.getByRole('link', { name: 'Planes' }).getAttribute('aria-current')).toBe('page');
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'API' }), 'api-1');
    await waitFor(() => expect(router.state.location.pathname).toBe('/panel/apis/api-1/planes'));
  });
  it('adapta la respuesta de sesión de EM-02 al encabezado y perfil', async () => {
    await abrir('/panel/apis');
    expect(await screen.findByText('Envíos Xelajú, S.A.')).toBeDefined();
    expect(screen.getByRole('link', { name: 'Ana' }).getAttribute('href')).toBe('/panel/perfil');
  });
  it('muestra lista vacía si DC-04 todavía no existe', async () => {
    server.use(http.get('http://localhost/api/apis', () => new HttpResponse(null, { status: 404 })));
    await abrir('/panel/apis');
    expect(await screen.findByText('Sin APIs')).toBeDefined();
    expect(screen.queryByRole('link', { name: 'Especificación' })).toBeNull();
  });
  it.each(['/panel/apis', '/admin/casos'])('cierra sesión desde %s con CSRF y limpia los datos privados', async (ruta) => {
    rol = ruta.startsWith('/admin') ? 'administrador' : 'propietario';
    let cerrada = false;
    server.use(http.post('http://localhost/api/auth/salir', ({ request }) => {
      expect(request.headers.get('X-Requested-With')).toBe('shapi');
      cerrada = true;
      return new HttpResponse(null, { status: 200 });
    }));
    await abrir(ruta);
    await userEvent.click(await screen.findByRole('button', { name: 'Cerrar sesión' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/entrar'));
    expect(cerrada).toBe(true);
    expect(cliente.getQueryData(['sesion'])).toBeUndefined();
    expect(cliente.getQueryData(['apis'])).toBeUndefined();
  });
  it('RNF-12 muestra esqueleto durante la consulta de sesión', async () => {
    server.use(http.get('http://localhost/api/auth/sesion', async () => {
      await delay(100);
      return HttpResponse.json(respuestaSesion());
    }));
    await abrir('/panel/apis');
    expect(screen.getByRole('status', { name: 'Cargando' })).toBeDefined();
    expect(await screen.findByText(/^A3-1 ·/)).toBeDefined();
  });
  it('RNF-12 · /_ui muestra la lámina de estilo sin consultar la sesión (DC-01)', async () => {
    let solicitudes = 0;
    server.use(http.get('http://localhost/api/auth/sesion', () => { solicitudes++; return new HttpResponse(null, { status: 401 }); }));
    await abrir('/_ui');
    expect(await screen.findByText('Plano azul')).toBeDefined();
    expect(router.state.location.pathname).toBe('/_ui');
    expect(solicitudes).toBe(0);
  });
  it('muestra 404 para una dirección desconocida', async () => {
    await abrir('/pagina-inexistente');
    expect(await screen.findByText('Página no encontrada')).toBeDefined();
  });
  it.each(['editor', 'lector'])('oculta opciones no permitidas al %s', async perfil => {
    rol = perfil;
    await abrir('/panel/apis');
    await screen.findByText(/^A3-1 ·/);
    expect(screen.queryByRole('link', { name: 'Miembros y roles' })).toBeNull();
    if (perfil === 'editor') {
      expect(screen.queryByRole('link', { name: 'Historial de pagos' })).toBeNull();
      expect(screen.queryByRole('link', { name: 'Suscripción de plataforma' })).toBeNull();
    } else {
      expect(screen.getByRole('link', { name: 'Historial de pagos' })).toBeDefined();
      expect(screen.getByRole('link', { name: 'Suscripción de plataforma' })).toBeDefined();
    }
  });
  it('el soporte puede consultar organizaciones en solo lectura', async () => {
    rol = 'soporte';
    await abrir('/admin/organizaciones');
    expect(await screen.findByText(/^A6-2 ·/)).toBeDefined();
  });
  it('mantiene la sesión y permite reintentar si salir falla', async () => {
    server.use(http.post('http://localhost/api/auth/salir', () => new HttpResponse(null, { status: 500 })));
    await abrir('/panel/apis');
    await userEvent.click(await screen.findByRole('button', { name: 'Cerrar sesión' }));
    expect(await screen.findByRole('alert')).toBeDefined();
    expect(router.state.location.pathname).toBe('/panel/apis');
    expect(cliente.getQueryData(['sesion'])).toBeDefined();
  });
});
