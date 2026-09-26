import { StrictMode } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RouterProvider } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { router } from '../rutas';
import { server } from '../../../../test/servidor';

const API = 'http://localhost/api/auth';
const problema = (status: number, codigo: string, title: string, errores?: Record<string, string[]>) =>
  HttpResponse.json({ type: 'about:blank', title, status, codigo, errores }, { status, headers: { 'Content-Type': 'application/problem+json' } });

const destinos: Record<string, string> = {
  propietario: '/panel/apis', editor: '/panel/apis', lector: '/panel/apis',
  administrador: '/admin/organizaciones', soporte: '/admin/casos',
};

// La sesión solo existe después de entrar o de verificar el correo.
let rol = 'propietario';
let conSesion = false;
let cliente: QueryClient;
const peticiones: { ruta: string; cuerpo: unknown; csrf: string | null }[] = [];

async function registrar(request: Request, ruta: string) {
  peticiones.push({ ruta, cuerpo: await request.clone().json().catch(() => null), csrf: request.headers.get('X-Requested-With') });
}

async function abrir(ruta: string) {
  await router.navigate(ruta);
  render(<StrictMode><QueryClientProvider client={cliente}><RouterProvider router={router} /></QueryClientProvider></StrictMode>);
}

const campo = (etiqueta: string) => screen.getByLabelText(etiqueta);
const ENLACE_REENVIADO = 'Si su correo todavía no está confirmado, le llegará un enlace nuevo en unos minutos.';
// Así responde la API ante una excepción no controlada (AddProblemDetails + UseExceptionHandler): sin `codigo`.
const errorDelServidor = () => HttpResponse.json({ type: 'https://tools.ietf.org/html/rfc9110#section-15.6.1', title: 'An error occurred while processing your request.', status: 500 },
  { status: 500, headers: { 'Content-Type': 'application/problem+json' } });
const errorBajo = (etiqueta: string) => campo(etiqueta).parentElement!.textContent;

beforeEach(() => {
  rol = 'propietario';
  conSesion = false;
  peticiones.length = 0;
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  server.use(
    http.get(`${API}/sesion`, () => conSesion
      ? HttpResponse.json({
        usuario: { nombre: 'Ana Lucía Morales', correo: 'ana.morales@enviosxelaju.com' },
        organizacion: { id: 'org-1', nombre: 'Envíos Xelajú, S.A.' },
        rol, correoVerificado: true, destino: destinos[rol],
      })
      : new HttpResponse(null, { status: 401 })),
    http.get('http://localhost/api/apis', () => HttpResponse.json({ elementos: [], total: 0 })),
  );
});
afterEach(() => { cleanup(); cliente.clear(); });

describe('RF-01 · A1.1 Registro del proveedor', () => {
  it('RF-01 muestra los textos, campos y el orden del mockup', async () => {
    await abrir('/registro');
    expect(await screen.findByRole('heading', { name: 'Crear una cuenta' })).toBeDefined();
    expect(screen.getByText('Cuenta de proveedor')).toBeDefined();
    expect(screen.getByText('Con estos datos se crea su organización en Shapi. Queda en el plan Prueba, sin costo y sin pedirle tarjeta.')).toBeDefined();
    const etiquetas = [...document.querySelectorAll('form label')].map(l => l.textContent);
    expect(etiquetas).toEqual(['Nombre', 'Correo electrónico', 'Nombre de la organización', 'Contraseña']);
    expect(screen.getByRole('button', { name: 'Crear cuenta' })).toBeDefined();
    expect(screen.getByRole('link', { name: 'Entrar' }).getAttribute('href')).toBe('/entrar');
  });

  it('RF-01 registra con CSRF y lleva a A1.2 con el correo', async () => {
    server.use(http.post(`${API}/registro`, async ({ request }) => { await registrar(request, 'registro'); return new HttpResponse(null, { status: 200 }); }));
    await abrir('/registro');
    await userEvent.type(await screen.findByLabelText('Nombre'), 'Ana Lucía Morales');
    await userEvent.type(campo('Correo electrónico'), 'ana.morales@enviosxelaju.com');
    await userEvent.type(campo('Nombre de la organización'), 'Envíos Xelajú, S.A.');
    await userEvent.type(campo('Contraseña'), 'ContraValida123');
    await userEvent.click(screen.getByRole('button', { name: 'Crear cuenta' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/verificar-correo'));
    expect(router.state.location.search).toBe('?correo=ana.morales%40enviosxelaju.com');
    expect(await screen.findByText('ana.morales@enviosxelaju.com')).toBeDefined();
    expect(peticiones).toEqual([{ ruta: 'registro', csrf: 'shapi', cuerpo: {
      nombre: 'Ana Lucía Morales', correo: 'ana.morales@enviosxelaju.com', organizacion: 'Envíos Xelajú, S.A.', contrasena: 'ContraValida123',
    } }]);
  });

  it('RF-01 muestra cada error de validación debajo de su campo', async () => {
    server.use(http.post(`${API}/registro`, () => problema(400, 'datos_invalidos', 'Revise los datos del formulario.', {
      nombre: ['Escriba su nombre.'],
      correo: ['Escriba un correo válido.'],
      organizacion: ['El nombre de la organización debe tener entre 2 y 120 caracteres.'],
      contrasena: ['La contraseña debe tener entre 10 y 128 caracteres.'],
    })));
    await abrir('/registro');
    await userEvent.click(await screen.findByRole('button', { name: 'Crear cuenta' }));

    await waitFor(() => expect(errorBajo('Nombre')).toContain('Escriba su nombre.'));
    expect(errorBajo('Correo electrónico')).toContain('Escriba un correo válido.');
    expect(errorBajo('Nombre de la organización')).toContain('El nombre de la organización debe tener entre 2 y 120 caracteres.');
    expect(errorBajo('Contraseña')).toContain('La contraseña debe tener entre 10 y 128 caracteres.');
    expect(campo('Contraseña').getAttribute('aria-invalid')).toBe('true');
    expect(router.state.location.pathname).toBe('/registro');
  });

  it('RF-01 un correo ya registrado se indica debajo del correo', async () => {
    server.use(http.post(`${API}/registro`, () => problema(409, 'correo_ya_registrado', 'Ya existe una cuenta con ese correo.')));
    await abrir('/registro');
    await userEvent.type(await screen.findByLabelText('Correo electrónico'), 'ana.morales@enviosxelaju.com');
    await userEvent.click(screen.getByRole('button', { name: 'Crear cuenta' }));
    await waitFor(() => expect(errorBajo('Correo electrónico')).toContain('Ya existe una cuenta con ese correo.'));
  });

  it('RF-01 un error del servidor muestra un aviso y "Reintentar" vuelve a enviar', async () => {
    let intentos = 0;
    server.use(http.post(`${API}/registro`, () => (++intentos === 1 ? new HttpResponse(null, { status: 500 }) : new HttpResponse(null, { status: 200 }))));
    await abrir('/registro');
    await userEvent.type(await screen.findByLabelText('Correo electrónico'), 'ana.morales@enviosxelaju.com');
    await userEvent.click(screen.getByRole('button', { name: 'Crear cuenta' }));
    const aviso = await screen.findByRole('alert');
    await userEvent.click(within(aviso).getByRole('button', { name: 'Reintentar' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/verificar-correo'));
    expect(intentos).toBe(2);
  });
});

describe('RF-02 · A1.2 Verificación de correo', () => {
  it('RF-02 muestra el aviso del mockup con el correo y reenvía el enlace', async () => {
    server.use(http.post(`${API}/reenviar-verificacion`, async ({ request }) => { await registrar(request, 'reenviar'); return new HttpResponse(null, { status: 200 }); }));
    await abrir('/verificar-correo?correo=ana.morales%40enviosxelaju.com');
    expect(await screen.findByRole('heading', { name: 'Revise su correo' })).toBeDefined();
    expect(screen.getByText('Verificación de correo')).toBeDefined();
    expect(screen.getByText('ana.morales@enviosxelaju.com')).toBeDefined();
    expect(screen.getByText('24 horas')).toBeDefined();
    expect(screen.getByText('No podrá publicar APIs mientras su correo no esté confirmado.')).toBeDefined();

    await userEvent.click(screen.getByRole('button', { name: 'Enviar el enlace otra vez' }));
    expect((await screen.findByRole('status')).textContent).toBe(ENLACE_REENVIADO);
    expect(peticiones).toEqual([{ ruta: 'reenviar', csrf: 'shapi', cuerpo: { correo: 'ana.morales@enviosxelaju.com' } }]);
  });

  it('RF-02 el enlace del correo verifica una sola vez (también en StrictMode) y lleva al destino', async () => {
    server.use(http.post(`${API}/verificar-correo`, async ({ request }) => {
      await registrar(request, 'verificar');
      conSesion = true;
      return new HttpResponse(null, { status: 200 });
    }));
    await abrir('/verificar-correo?token=abc%2B123');
    await waitFor(() => expect(router.state.location.pathname).toBe('/panel/apis'));
    expect(peticiones).toEqual([{ ruta: 'verificar', csrf: 'shapi', cuerpo: { token: 'abc+123' } }]);
  });

  it('RF-02 un enlace vencido o usado ofrece reenviar el enlace al correo que se escriba', async () => {
    server.use(
      http.post(`${API}/verificar-correo`, () => problema(422, 'token_invalido', 'El enlace venció o ya se usó.')),
      http.post(`${API}/reenviar-verificacion`, async ({ request }) => { await registrar(request, 'reenviar'); return new HttpResponse(null, { status: 200 }); }),
    );
    await abrir('/verificar-correo?token=vencido');
    expect(await screen.findByText('El enlace venció o ya se usó.')).toBeDefined();
    await userEvent.type(campo('Correo electrónico'), 'ana.morales@enviosxelaju.com');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar el enlace otra vez' }));
    expect((await screen.findByRole('status')).textContent).toBe(ENLACE_REENVIADO);
    expect(peticiones).toEqual([{ ruta: 'reenviar', csrf: 'shapi', cuerpo: { correo: 'ana.morales@enviosxelaju.com' } }]);
    expect(router.state.location.pathname).toBe('/verificar-correo');
  });

  it('RF-02 una cuenta desactivada muestra su mensaje y no ofrece reenviar', async () => {
    server.use(http.post(`${API}/verificar-correo`, () => problema(403, 'cuenta_desactivada', 'Cuenta desactivada.')));
    await abrir('/verificar-correo?token=abc');
    expect(await screen.findByText('Cuenta desactivada.')).toBeDefined();
    expect(screen.queryByRole('button', { name: 'Enviar el enlace otra vez' })).toBeNull();
  });

  it('RF-02 si falla la consulta de la sesión después de verificar, "Reintentar" no reenvía el token usado', async () => {
    let verificaciones = 0;
    let consultas = 0;
    server.use(
      http.post(`${API}/verificar-correo`, () => { verificaciones++; conSesion = true; return new HttpResponse(null, { status: 200 }); }),
      http.get(`${API}/sesion`, () => (++consultas === 1 ? errorDelServidor() : HttpResponse.json({
        usuario: { nombre: 'Ana', correo: 'ana@enviosxelaju.com' }, organizacion: { id: 'org-1', nombre: 'Envíos Xelajú, S.A.' },
        rol: 'propietario', correoVerificado: true, destino: '/panel/apis',
      }))),
    );
    await abrir('/verificar-correo?token=abc');
    await userEvent.click(within(await screen.findByRole('alert')).getByRole('button', { name: 'Reintentar' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/panel/apis'));
    expect(verificaciones).toBe(1);
  });

  it('RF-02 un error del servidor permite reintentar la verificación', async () => {
    let intentos = 0;
    server.use(http.post(`${API}/verificar-correo`, () => {
      if (++intentos === 1) return new HttpResponse(null, { status: 500 });
      conSesion = true;
      return new HttpResponse(null, { status: 200 });
    }));
    await abrir('/verificar-correo?token=abc');
    await userEvent.click(within(await screen.findByRole('alert')).getByRole('button', { name: 'Reintentar' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/panel/apis'));
    expect(intentos).toBe(2);
  });
});

describe('RF-04 · A1.3 Inicio de sesión', () => {
  it('RF-04 muestra los textos del mockup y "¿Olvidó su contraseña?" lleva a /recuperar', async () => {
    await abrir('/entrar');
    expect(await screen.findByRole('heading', { name: 'Entrar a Shapi' })).toBeDefined();
    expect(screen.getByText('Acceso')).toBeDefined();
    const etiquetas = [...document.querySelectorAll('form label')].map(l => l.textContent);
    expect(etiquetas).toEqual(['Correo electrónico', 'Contraseña']);
    expect(screen.getByRole('button', { name: 'Entrar' })).toBeDefined();
    await userEvent.click(screen.getByRole('link', { name: '¿Olvidó su contraseña?' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/recuperar'));
  });

  it.each(Object.entries(destinos))('RF-04 al entrar como %s lleva a %s', async (perfil, destino) => {
    rol = perfil;
    server.use(http.post(`${API}/entrar`, async ({ request }) => { await registrar(request, 'entrar'); conSesion = true; return new HttpResponse(null, { status: 200 }); }));
    await abrir('/entrar');
    await userEvent.type(await screen.findByLabelText('Correo electrónico'), 'ana.morales@enviosxelaju.com');
    await userEvent.type(campo('Contraseña'), 'ContraValida123');
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    await waitFor(() => expect(router.state.location.pathname).toBe(destino));
    expect(peticiones).toEqual([{ ruta: 'entrar', csrf: 'shapi', cuerpo: { correo: 'ana.morales@enviosxelaju.com', contrasena: 'ContraValida123' } }]);
  });

  it('RF-04 un 500 de la API (ProblemDetails sin codigo) muestra el aviso en español con "Reintentar"', async () => {
    let intentos = 0;
    server.use(http.post(`${API}/entrar`, () => {
      if (++intentos === 1) return errorDelServidor();
      conSesion = true;
      return new HttpResponse(null, { status: 200 });
    }));
    await abrir('/entrar');
    await userEvent.type(await screen.findByLabelText('Correo electrónico'), 'ana.morales@enviosxelaju.com');
    await userEvent.type(campo('Contraseña'), 'ContraValida123');
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    const aviso = await screen.findByRole('alert');
    expect(aviso.textContent).toContain('No se pudo completar la solicitud. Revise su conexión e intente de nuevo.');
    expect(aviso.textContent).not.toContain('An error occurred');
    await userEvent.click(within(aviso).getByRole('button', { name: 'Reintentar' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/panel/apis'));
  });

  it('RF-04 si la sesión no quedó iniciada después de entrar, lo avisa y permite volver a intentar', async () => {
    server.use(http.post(`${API}/entrar`, () => new HttpResponse(null, { status: 200 })));
    await abrir('/entrar');
    await userEvent.type(await screen.findByLabelText('Correo electrónico'), 'ana.morales@enviosxelaju.com');
    await userEvent.type(campo('Contraseña'), 'ContraValida123');
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    expect(within(await screen.findByRole('alert')).getByRole('button', { name: 'Reintentar' })).toBeDefined();
    expect((screen.getByRole('button', { name: 'Entrar' }) as HTMLButtonElement).disabled).toBe(false);
    expect(router.state.location.pathname).toBe('/entrar');
  });

  it.each([
    [401, 'credenciales_invalidas', 'El correo o la contraseña no son correctos.'],
    [423, 'cuenta_bloqueada', 'La cuenta está bloqueada por intentos fallidos. Intente de nuevo en 15 minutos.'],
    [403, 'cuenta_desactivada', 'Cuenta desactivada.'],
    [429, 'demasiadas_peticiones', 'Demasiadas peticiones. Espere un minuto e intente de nuevo.'],
  ])('RF-04 una respuesta %i (%s) muestra su mensaje sin salir de A1.3', async (estado, codigo, mensaje) => {
    server.use(http.post(`${API}/entrar`, () => problema(estado, codigo, mensaje)));
    await abrir('/entrar');
    await userEvent.type(await screen.findByLabelText('Correo electrónico'), 'ana.morales@enviosxelaju.com');
    await userEvent.type(campo('Contraseña'), 'Incorrecta123');
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    expect(within(await screen.findByRole('alert')).getByText(mensaje)).toBeDefined();
    expect(router.state.location.pathname).toBe('/entrar');
  });

  it('RF-04 sin sesión, el guardia de rutas lleva de /panel/apis a /entrar', async () => {
    await abrir('/panel/apis');
    await waitFor(() => expect(router.state.location.pathname).toBe('/entrar'));
    expect(await screen.findByRole('heading', { name: 'Entrar a Shapi' })).toBeDefined();
  });
});
