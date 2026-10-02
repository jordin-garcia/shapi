import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, type RouterProviderProps } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import App from '../App';
import { crearRutas } from '../rutas';
import { server } from '../../../../test/servidor';

const API = 'http://localhost/api/portal/auth';
const configuracion = { nombrePortal: 'Envíos Xelajú', colorPrincipal: '#B8322A', urlLogo: null, bienvenida: 'Bienvenido', nombreApi: 'Cotización de Envíos', descripcionApi: 'API', hostPortal: 'envios.shapi.localhost', hostApi: 'envios.api.shapi.localhost' };
let cliente: QueryClient;
function montar(ruta: string) {
  const enrutador = createMemoryRouter(crearRutas(), { initialEntries: [ruta] });
  render(<QueryClientProvider client={cliente}><App enrutador={enrutador as RouterProviderProps['router']} /></QueryClientProvider>);
  return enrutador;
}
beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  server.use(http.get('http://localhost/api/portal/configuracion', () => HttpResponse.json(configuracion)));
});
afterEach(() => { cleanup(); cliente.clear(); });

describe('DC-08 · acceso del consumidor', () => {
  it('RF-05 registra con los datos del mockup y muestra A5.8', async () => {
    let cuerpo: unknown;
    server.use(http.post(`${API}/registro`, async ({ request }) => { cuerpo = await request.json(); return new HttpResponse(null, { status: 200 }); }));
    const router = montar('/registro');
    expect(await screen.findByRole('heading', { name: 'Crear una cuenta' })).toBeDefined();
    expect(screen.queryByRole('navigation', { name: 'Navegación principal' })).toBeNull();
    await userEvent.type(screen.getByLabelText('Nombre'), 'María José Quiñónez');
    await userEvent.type(screen.getByLabelText('Correo electrónico'), 'maria@tienda.test');
    await userEvent.type(screen.getByLabelText('Nombre de la empresa'), 'Mercadito Antigua');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'Contrasena123');
    await userEvent.click(screen.getByRole('button', { name: 'Crear cuenta' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/verificar-correo'));
    expect(cuerpo).toEqual({ nombre: 'María José Quiñónez', correo: 'maria@tienda.test', nombreEmpresa: 'Mercadito Antigua', contrasena: 'Contrasena123' });
    expect(await screen.findByRole('heading', { name: 'Revise su correo' })).toBeDefined();
  });

  it('RF-05 precarga el correo invitado y crea la cuenta verificada', async () => {
    let cuerpo: unknown;
    server.use(
      http.get(`${API}/invitacion/abc`, () => HttpResponse.json({ correo: 'invitada@tienda.test' })),
      http.post(`${API}/invitacion/abc/aceptar`, async ({ request }) => { cuerpo = await request.json(); return new HttpResponse(null, { status: 200 }); }),
      http.get(`${API}/sesion`, () => HttpResponse.json({ consumidor: { nombre: 'Inés', nombreEmpresa: 'Tienda' }, correoVerificado: true, destino: '/planes' })),
    );
    const router = montar('/invitacion?token=abc');
    expect((await screen.findByLabelText('Correo electrónico')).getAttribute('readonly')).not.toBeNull();
    await userEvent.type(screen.getByLabelText('Nombre'), 'Inés'); await userEvent.type(screen.getByLabelText('Nombre de la empresa'), 'Tienda'); await userEvent.type(screen.getByLabelText('Contraseña'), 'Contrasena123');
    await userEvent.click(screen.getByRole('button', { name: 'Aceptar la invitación y crear la cuenta' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/planes'));
    expect(cuerpo).toEqual({ nombre: 'Inés', nombreEmpresa: 'Tienda', contrasena: 'Contrasena123' });
  });

  it.each(['/planes', '/cuenta/suscripcion'] as const)('RF-04 entrar navega al destino %s de la sesión', async destino => {
    server.use(http.post(`${API}/entrar`, () => new HttpResponse(null, { status: 200 })), http.get(`${API}/sesion`, () => HttpResponse.json({ consumidor: { nombre: 'Ana', nombreEmpresa: 'Tienda' }, correoVerificado: true, destino })));
    const router = montar('/entrar');
    expect(await screen.findByText('¿Olvidó su contraseña?')).toBeDefined();
    await userEvent.type(screen.getByLabelText('Correo electrónico'), 'ana@tienda.test'); await userEvent.type(screen.getByLabelText('Contraseña'), 'Contrasena123'); await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    await waitFor(() => expect(router.state.location.pathname).toBe(destino));
  });

  it('RF-02 reenvía la verificación con una respuesta neutral', async () => {
    server.use(http.post(`${API}/reenviar-verificacion`, () => new HttpResponse(null, { status: 200 })));
    montar('/verificar-correo?correo=ana%40tienda.test');
    expect(await screen.findByText(/Enviamos un enlace de confirmación/)).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: /Enviar el enlace otra vez/ }));
    expect(await screen.findByText(/Si su correo todavía no está confirmado/)).toBeDefined();
  });

  it('RF-02 confirma el enlace una vez y entra', async () => {
    let verificaciones = 0;
    server.use(http.post(`${API}/verificar-correo`, () => { verificaciones++; return new HttpResponse(null, { status: 200 }); }), http.get(`${API}/sesion`, () => HttpResponse.json({ consumidor: { nombre: 'Ana', nombreEmpresa: 'Tienda' }, correoVerificado: true, destino: '/planes' })));
    const router = montar('/verificar-correo?token=abc');
    await waitFor(() => expect(router.state.location.pathname).toBe('/planes'));
    expect(verificaciones).toBe(1);
  });

  it('RF-03 recuperación muestra el mismo mensaje neutral', async () => {
    server.use(http.post(`${API}/recuperar`, () => new HttpResponse(null, { status: 200 })));
    montar('/recuperar');
    expect(await screen.findByRole('heading', { name: 'Recuperar la contraseña' })).toBeDefined();
    await userEvent.type(screen.getByLabelText('Correo electrónico'), 'nadie@tienda.test'); await userEvent.click(screen.getByRole('button', { name: 'Enviar el enlace' }));
    expect(await screen.findByRole('heading', { name: 'Revise su correo' })).toBeDefined();
    expect(screen.getAllByText(/Si el correo tiene una cuenta en este portal/).length).toBeGreaterThan(0);
  });

  it('RF-03 guarda la contraseña y no reutiliza el token al consultar la sesión', async () => {
    let restablecimientos = 0;
    server.use(http.post(`${API}/restablecer`, () => { restablecimientos++; return new HttpResponse(null, { status: 200 }); }), http.get(`${API}/sesion`, () => HttpResponse.json({ consumidor: { nombre: 'Ana', nombreEmpresa: 'Tienda' }, correoVerificado: true, destino: '/cuenta/suscripcion' })));
    const router = montar('/restablecer?token=abc');
    expect(await screen.findByRole('heading', { name: 'Definir la contraseña' })).toBeDefined();
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'NuevaContra123'); await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));
    await waitFor(() => expect(router.state.location.pathname).toBe('/cuenta/suscripcion'));
    expect(restablecimientos).toBe(1);
  });
});
