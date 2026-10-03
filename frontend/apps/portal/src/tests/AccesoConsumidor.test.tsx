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
function problema(estado: number, codigo: string, title: string) {
  return HttpResponse.json({ status: estado, codigo, title }, { status: estado, headers: { 'Content-Type': 'application/problem+json' } });
}
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

  it('RF-05 distingue una invitación inválida de un fallo recuperable', async () => {
    server.use(http.get(`${API}/invitacion/vencida`, () => problema(422, 'token_invalido', 'La invitación ya no sirve.')));
    const router = montar('/invitacion?token=vencida');
    expect(await screen.findByRole('heading', { name: 'El enlace ya no sirve' })).toBeDefined();

    server.use(http.get(`${API}/invitacion/red`, () => new HttpResponse(null, { status: 503 })));
    await router.navigate('/invitacion?token=red');
    expect(await screen.findByRole('heading', { name: 'No se pudo consultar la invitación' })).toBeDefined();
    expect(screen.getByRole('button', { name: 'Reintentar' })).toBeDefined();
  });

  it.each(['/planes', '/cuenta/suscripcion'] as const)('RF-04 entrar navega al destino %s de la sesión', async destino => {
    server.use(http.post(`${API}/entrar`, () => new HttpResponse(null, { status: 200 })), http.get(`${API}/sesion`, () => HttpResponse.json({ consumidor: { nombre: 'Ana', nombreEmpresa: 'Tienda' }, correoVerificado: true, destino })));
    const router = montar('/entrar');
    expect(await screen.findByText('¿Olvidó su contraseña?')).toBeDefined();
    await userEvent.type(screen.getByLabelText('Correo electrónico'), 'ana@tienda.test'); await userEvent.type(screen.getByLabelText('Contraseña'), 'Contrasena123'); await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    await waitFor(() => expect(router.state.location.pathname).toBe(destino));
  });

  it('RF-04 muestra credenciales inválidas sin navegar', async () => {
    server.use(http.post(`${API}/entrar`, () => problema(401, 'credenciales_invalidas', 'El correo o la contraseña no son correctos.')));
    const router = montar('/entrar');
    await screen.findByRole('heading', { name: 'Entrar' });
    await userEvent.type(screen.getByLabelText('Correo electrónico'), 'ana@tienda.test');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'incorrecta');
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));
    expect((await screen.findByRole('alert')).textContent).toContain('El correo o la contraseña no son correctos.');
    expect(router.state.location.pathname).toBe('/entrar');
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

  it('RF-02 muestra el estado de enlace de verificación inválido', async () => {
    server.use(http.post(`${API}/verificar-correo`, () => problema(422, 'token_invalido', 'El enlace venció o ya se usó.')));
    montar('/verificar-correo?token=vencido');
    expect(await screen.findByRole('heading', { name: 'Enlace no válido' })).toBeDefined();
    expect(screen.getByText('El enlace venció o ya se usó.')).toBeDefined();
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

  it('RF-03 ofrece pedir otro enlace cuando el token de recuperación es inválido', async () => {
    server.use(http.post(`${API}/restablecer`, () => problema(422, 'token_invalido', 'El enlace venció o ya se usó.')));
    montar('/restablecer?token=vencido');
    await screen.findByRole('heading', { name: 'Definir la contraseña' });
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'NuevaContra123');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));
    expect(await screen.findByRole('heading', { name: 'El enlace ya no sirve' })).toBeDefined();
    expect(screen.getByRole('link', { name: 'Solicitar un enlace nuevo' }).getAttribute('href')).toBe('/recuperar');
  });

  // El token es de un solo uso: si la consulta de la sesión falla después de usarlo, «Reintentar» solo vuelve a pedir
  // el destino. Un segundo envío del token respondería token_invalido aunque la cuenta ya tenga la sesión iniciada.
  const sesionQueFallaUnaVez = (destino: string) => {
    let consultas = 0;
    return http.get(`${API}/sesion`, () => {
      consultas++;
      if (consultas === 1) return new HttpResponse(null, { status: 503 });
      return HttpResponse.json({ consumidor: { nombre: 'Ana', nombreEmpresa: 'Tienda' }, correoVerificado: true, destino });
    });
  };

  it('RF-05 si la sesión falla después de aceptar la invitación, reintentar no la acepta otra vez', async () => {
    let aceptaciones = 0;
    server.use(
      http.get(`${API}/invitacion/abc`, () => HttpResponse.json({ correo: 'invitada@tienda.test' })),
      http.post(`${API}/invitacion/abc/aceptar`, () => { aceptaciones++; return new HttpResponse(null, { status: 200 }); }),
      sesionQueFallaUnaVez('/planes'),
    );
    const router = montar('/invitacion?token=abc');
    await screen.findByLabelText('Nombre');
    await userEvent.type(screen.getByLabelText('Nombre'), 'Inés');
    await userEvent.type(screen.getByLabelText('Nombre de la empresa'), 'Tienda');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'Contrasena123');
    await userEvent.click(screen.getByRole('button', { name: 'Aceptar la invitación y crear la cuenta' }));

    await userEvent.click(await screen.findByRole('button', { name: 'Reintentar' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/planes'));
    expect(aceptaciones).toBe(1);
  });

  it('RF-02 si la sesión falla después de confirmar el correo, reintentar no reenvía el token', async () => {
    let verificaciones = 0;
    server.use(
      http.post(`${API}/verificar-correo`, () => { verificaciones++; return new HttpResponse(null, { status: 200 }); }),
      sesionQueFallaUnaVez('/cuenta/suscripcion'),
    );
    const router = montar('/verificar-correo?token=abc');

    await userEvent.click(await screen.findByRole('button', { name: 'Reintentar' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/cuenta/suscripcion'));
    expect(verificaciones).toBe(1);
  });

  it('RF-03 si la sesión falla después de restablecer, reintentar no reenvía el token', async () => {
    let restablecimientos = 0;
    server.use(
      http.post(`${API}/restablecer`, () => { restablecimientos++; return new HttpResponse(null, { status: 200 }); }),
      sesionQueFallaUnaVez('/planes'),
    );
    const router = montar('/restablecer?token=abc');
    await screen.findByRole('heading', { name: 'Definir la contraseña' });
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'NuevaContra123');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar la contraseña' }));

    await userEvent.click(await screen.findByRole('button', { name: 'Reintentar' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/planes'));
    expect(restablecimientos).toBe(1);
  });

  it('RF-02 con el enlace de verificación vencido se puede pedir uno nuevo con el correo', async () => {
    let cuerpo: unknown;
    server.use(
      http.post(`${API}/verificar-correo`, () => problema(422, 'token_invalido', 'El enlace venció o ya se usó.')),
      http.post(`${API}/reenviar-verificacion`, async ({ request }) => { cuerpo = await request.json(); return new HttpResponse(null, { status: 200 }); }),
    );
    montar('/verificar-correo?token=vencido');
    await screen.findByRole('heading', { name: 'Enlace no válido' });
    expect(screen.getByText('Escriba su correo y le enviaremos un enlace nuevo.')).toBeDefined();

    await userEvent.type(screen.getByLabelText('Correo electrónico'), 'ana@tienda.test');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar el enlace otra vez' }));

    expect(await screen.findByText(/Si su correo todavía no está confirmado/)).toBeDefined();
    expect(cuerpo).toEqual({ correo: 'ana@tienda.test' });
  });

  it('RF-02 si el reenvío falla por la red, se puede reintentar', async () => {
    let intentos = 0;
    server.use(http.post(`${API}/reenviar-verificacion`, () => {
      intentos++;
      return intentos === 1 ? new HttpResponse(null, { status: 503 }) : new HttpResponse(null, { status: 200 });
    }));
    montar('/verificar-correo?correo=ana%40tienda.test');
    await userEvent.click(await screen.findByRole('button', { name: /Enviar el enlace otra vez/ }));

    await userEvent.click(await screen.findByRole('button', { name: 'Reintentar' }));

    expect(await screen.findByText(/Si su correo todavía no está confirmado/)).toBeDefined();
    expect(intentos).toBe(2);
  });

  it('RF-02 sin correo en la dirección, el campo de correo no desaparece al escribir', async () => {
    montar('/verificar-correo');
    await userEvent.type(await screen.findByLabelText('Correo electrónico'), 'ana@tienda.test');

    expect((screen.getByLabelText('Correo electrónico') as HTMLInputElement).value).toBe('ana@tienda.test');
  });

  it('RF-15 el botón principal y el campo enfocado usan la marca del portal, no el azul de Shapi', async () => {
    montar('/entrar');
    const marco = (await screen.findByRole('heading', { name: 'Entrar' })).closest('section')!;

    expect(marco.style.getPropertyValue('--principal')).toBe('var(--marca-principal)');
    expect(marco.style.getPropertyValue('--principal-hover')).toContain('var(--marca-principal)');
    expect(marco.style.getPropertyValue('--anillo-foco')).toBe('color-mix(in srgb, var(--marca-principal) 17%, #FFFFFF)');
    expect(marco.contains(screen.getByRole('button', { name: 'Entrar' }))).toBe(true);
  });
});
