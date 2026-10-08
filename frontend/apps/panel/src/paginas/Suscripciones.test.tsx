import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router';
import { server } from '../../../../test/servidor';
import PaginaA21PlanesPlataforma from './A2-1-PlanesPlataforma';
import PaginaA22Contratacion from './A2-2-Contratacion';
import PaginaA25CambioPlan from './A2-5-CambioPlan';
import PaginaB14Suscripcion from './B1-4-Suscripcion';

const API = 'http://localhost/api';
const lanzamiento = { id: 'plan-1', nombre: 'Lanzamiento', descripcion: 'Empezar a cobrar por una API existente', precio: 199, moneda: 'GTQ' as const, vigenciaDias: 30, maxApis: 3, maxMiembros: 3, cuotaPeticiones: 250000, dominioPropio: false, esPrueba: false, inicioCicloPrevisto: '2026-08-24T06:00:00Z' };
const producto = { ...lanzamiento, id: 'plan-2', nombre: 'Producto', descripcion: 'Proveedores con clientes establecidos', precio: 599, maxApis: 10, maxMiembros: 10, cuotaPeticiones: 2000000, dominioPropio: true };
const prueba = { ...lanzamiento, id: 'trial', nombre: 'Prueba', precio: 0, maxApis: 1, maxMiembros: 1, cuotaPeticiones: 10000, esPrueba: true };
const suscripcion = {
  plan: lanzamiento,
  estado: 'activa' as const, periodo: { inicio: '2026-08-24T06:00:00Z', fin: '2026-09-22T06:00:00Z' },
  proximaRenovacion: '2026-09-23T06:00:00Z', tarjetaEnmascarada: 'Visa •••• 4821', graciaHasta: null, diasRestantesCiclo: 13, diasRestantes: 0, cambioProgramado: null,
};
const suscripcionPrueba = {
  ...suscripcion,
  plan: prueba,
  tarjetaEnmascarada: null,
};
let client: QueryClient;
let cambioSolicitado = false;

function renderizar(entrada: string, vista: React.ReactNode) {
  return render(<QueryClientProvider client={client}><MemoryRouter initialEntries={[entrada]}>{vista}</MemoryRouter></QueryClientProvider>);
}

beforeEach(() => {
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  server.use(http.get(API + '/auth/sesion', () => HttpResponse.json({
    usuario: { nombre: 'Ana', correo: 'ana@example.com' },
    organizacion: { id: 'org-1', nombre: 'Envíos Xelajú, S.A.' },
    rol: 'propietario', correoVerificado: true, destino: '/panel/apis',
  })));
});
afterEach(() => { cleanup(); client.clear(); server.resetHandlers(); cambioSolicitado = false; });

describe('RF-19, RF-20 y RF-25 · Suscripción de plataforma', () => {
  it('A2.1 muestra los planes, precios y límites de plataforma', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([prueba, lanzamiento, producto])),
      http.get(API + '/suscripcion', () => HttpResponse.json(suscripcionPrueba)),
    );
    renderizar('/panel/suscripcion/planes', <PaginaA21PlanesPlataforma />);
    expect(await screen.findByRole('heading', { name: 'Los planes de Shapi.' })).toBeDefined();
    expect(screen.getByText('Q 199.00')).toBeDefined();
    expect(screen.getByText(/2,000,000/)).toBeDefined();
  });

  it('A2.1 enlaza a cambiar de plan cuando la organización ya tiene un plan de pago', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento, producto])),
      http.get(API + '/suscripcion', () => HttpResponse.json(suscripcion)),
    );
    renderizar('/panel/suscripcion/planes', <PaginaA21PlanesPlataforma />);
    const cambiar = await screen.findByRole('link', { name: 'Cambiar plan' });
    expect(cambiar.getAttribute('href')).toBe('/panel/suscripcion/cambiar/plan-2');
  });

  it('A2.2 y A2.3 contrata con tarjeta y confirma la activación', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento])),
      http.get(API + '/suscripcion', () => HttpResponse.json({ title: 'Sin suscripción', codigo: 'suscripcion_no_encontrada' }, { status: 404, headers: { 'Content-Type': 'application/problem+json' } })),
      http.post(API + '/suscripcion/contratar', async ({ request }) => {
        const body = await request.json() as { planId: string; tarjeta: { numero: string } };
        expect(body.planId).toBe(lanzamiento.id);
        expect(body.tarjeta.numero).toBe('4242424242424242');
        return HttpResponse.json({
          id: 'sub-1', plan: { id: lanzamiento.id, nombre: lanzamiento.nombre, precio: lanzamiento.precio, moneda: 'GTQ', vigenciaDias: 30 },
          estado: 'activa', periodo: { inicio: lanzamiento.inicioCicloPrevisto, fin: '2026-09-22T06:00:00Z' },
          proximaRenovacion: '2026-09-23T06:00:00Z', tarjetaEnmascarada: 'Visa •••• 4242',
        }, { status: 201 });
      }),
    );
    renderizar('/panel/suscripcion/contratar/plan-1', <Routes><Route path="/panel/suscripcion/contratar/:plan" element={<PaginaA22Contratacion />} /></Routes>);
    await userEvent.type(await screen.findByLabelText('Número de tarjeta'), '4242424242424242');
    expect(screen.getByRole('link', { name: 'Elegir otro plan' }).getAttribute('href')).toBe('/panel/suscripcion/planes');
    await userEvent.type(screen.getByLabelText('Mes de vencimiento'), '12');
    await userEvent.type(screen.getByLabelText('Año de vencimiento'), '2030');
    await userEvent.type(screen.getByLabelText('Código de seguridad'), '123');
    await userEvent.type(screen.getByLabelText('Titular de la tarjeta'), 'Ana Morales');
    await userEvent.click(screen.getByRole('button', { name: 'Pagar Q 199.00 y activar el plan' }));
    expect(await screen.findByRole('heading', { name: 'Plan activado' })).toBeDefined();
    expect(screen.getByText('El cobro fue autorizado y su suscripción de plataforma ya está vigente.')).toBeDefined();
    expect(screen.getByText('Lanzamiento · Activa')).toBeDefined();
    expect(screen.getByText('24 ago 2026 – 22 sept 2026')).toBeDefined();
    expect(screen.getByText('Visa •••• 4242')).toBeDefined();
  });

  it.each(['en_gracia', 'suspendida'])('RF-23 · A2.1 dirige un plan de pago %s a reactivar su suscripción', async estado => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento, producto])),
      http.get(API + '/suscripcion', () => HttpResponse.json({ ...suscripcion, estado })),
    );
    renderizar('/panel/suscripcion/planes', <PaginaA21PlanesPlataforma />);
    expect((await screen.findByRole('link', { name: 'Reactivar suscripción' })).getAttribute('href')).toBe('/panel/suscripcion');
    expect(screen.queryByRole('link', { name: 'Contratar' })).toBeNull();
  });

  it('RF-25 · A2.5 explica el precio completo cuando cambia la vigencia', async () => {
    const anual = { ...producto, vigenciaDias: 365, precio: 15000 };
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento, anual])),
      http.get(API + '/suscripcion', () => HttpResponse.json(suscripcion)),
    );
    renderizar('/panel/suscripcion/cambiar/plan-2', <Routes>
      <Route path="/panel/suscripcion/cambiar/:plan" element={<PaginaA25CambioPlan />} />
    </Routes>);
    expect(await screen.findByText('Cargo por el ciclo completo de Producto')).toBeDefined();
    expect(screen.getByText('El cambio de vigencia inicia un ciclo nuevo hoy; se cobra el precio completo del plan de destino menos el crédito del ciclo actual.')).toBeDefined();
    expect(screen.queryByText('Cargo por 13 días de Producto')).toBeNull();
  });

  it('RF-25 · una bajada anual se programa sin prometer cobro ni ciclo nuevo hoy', async () => {
    const mensual = { ...lanzamiento, precio: 1500 };
    const anual = { ...producto, vigenciaDias: 365, precio: 15000 };
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([mensual, anual])),
      http.get(API + '/suscripcion', () => HttpResponse.json({ ...suscripcion, plan: { ...suscripcion.plan, precio: 1500 } })),
    );
    renderizar('/panel/suscripcion/cambiar/plan-2', <Routes><Route path="/panel/suscripcion/cambiar/:plan" element={<PaginaA25CambioPlan />} /></Routes>);
    expect(await screen.findByRole('button', { name: 'Programar cambio de plan' })).toBeDefined();
    expect(screen.queryByText(/El cambio de vigencia inicia un ciclo nuevo hoy/)).toBeNull();
  });

  it('RF-17 y RF-25 · permite cambiar desde un plan que ya no está en el catálogo activo', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([producto])),
      http.get(API + '/suscripcion', () => HttpResponse.json(suscripcion)),
    );
    renderizar('/panel/suscripcion/cambiar/plan-2', <Routes><Route path="/panel/suscripcion/cambiar/:plan" element={<PaginaA25CambioPlan />} /></Routes>);
    expect(await screen.findByRole('heading', { name: 'Lanzamiento' })).toBeDefined();
    expect(screen.getByRole('button', { name: 'Pagar Q 173.34 y cambiar de plan' })).toBeDefined();
  });

  it('RF-20 y RF-44 · un rechazo desde Prueba suspendida conserva el aviso de tráfico detenido', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento])),
      http.get(API + '/suscripcion', () => HttpResponse.json({ ...suscripcionPrueba, estado: 'suspendida' })),
      http.post(API + '/suscripcion/contratar', () => HttpResponse.json({ title: 'No se autorizó el cobro.', codigo: 'pago_rechazado' }, { status: 402, headers: { 'Content-Type': 'application/problem+json' } })),
    );
    renderizar('/panel/suscripcion/contratar/plan-1', <Routes><Route path="/panel/suscripcion/contratar/:plan" element={<PaginaA22Contratacion />} /></Routes>);
    await userEvent.type(await screen.findByLabelText('Número de tarjeta'), '4000000000000002');
    await userEvent.type(screen.getByLabelText('Mes de vencimiento'), '12');
    await userEvent.type(screen.getByLabelText('Año de vencimiento'), '2030');
    await userEvent.type(screen.getByLabelText('Código de seguridad'), '123');
    await userEvent.type(screen.getByLabelText('Titular de la tarjeta'), 'Ana Morales');
    await userEvent.click(screen.getByRole('button', { name: 'Pagar Q 199.00 y activar el plan' }));
    expect(await screen.findByText('Su tráfico sigue detenido. Contrate un plan de pago para restablecerlo.')).toBeDefined();
    expect(screen.queryByText(/Sus APIs y sus claves siguen funcionando/)).toBeNull();
  });

  it('RF-19 · A2.1 informa la cuota de Escala anual', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([
        { ...lanzamiento, nombre: 'Escala mensual', precio: 1500 },
        { ...producto, nombre: 'Escala anual', precio: 15000, vigenciaDias: 365, cuotaPeticiones: 120000000 },
      ])),
      http.get(API + '/suscripcion', () => HttpResponse.json(suscripcionPrueba)),
    );
    renderizar('/panel/suscripcion/planes', <PaginaA21PlanesPlataforma />);
    expect(await screen.findByText(/365 días y 120,000,000 peticiones/)).toBeDefined();
  });

  it('RF-23 y permisos · el lector consulta B1.4 sin controles de pago o cambio', async () => {
    server.use(
      http.get(API + '/auth/sesion', () => HttpResponse.json({
        usuario: { nombre: 'Lector', correo: 'lector@example.com' },
        organizacion: { id: 'org-1', nombre: 'Envíos Xelajú, S.A.' },
        rol: 'lector', correoVerificado: true, destino: '/panel/apis',
      })),
      http.get(API + '/suscripcion', () => HttpResponse.json({
        ...suscripcion, estado: 'en_gracia', cambioProgramado: { nombre: producto.nombre, planId: producto.id, efectivoDesde: suscripcion.proximaRenovacion },
      })),
    );
    renderizar('/panel/suscripcion', <PaginaB14Suscripcion />);
    expect(await screen.findByText('En gracia')).toBeDefined();
    expect(screen.queryByRole('button', { name: 'Pagar con otra tarjeta' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Cancelar cambio programado' })).toBeNull();
    expect(screen.queryByRole('link', { name: 'Cambiar plan' })).toBeNull();
  });

  it('A2.4 informa del rechazo y mantiene el plan vigente', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento])),
      http.get(API + '/suscripcion', () => HttpResponse.json(suscripcionPrueba)),
      http.post(API + '/suscripcion/contratar', () => HttpResponse.json({ title: 'No se autorizó el cobro.', codigo: 'pago_rechazado' }, { status: 402, headers: { 'Content-Type': 'application/problem+json' } })),
    );
    renderizar('/panel/suscripcion/contratar/plan-1', <Routes><Route path="/panel/suscripcion/contratar/:plan" element={<PaginaA22Contratacion />} /></Routes>);
    await userEvent.type(await screen.findByLabelText('Número de tarjeta'), '4000000000000002');
    await userEvent.type(screen.getByLabelText('Mes de vencimiento'), '12');
    await userEvent.type(screen.getByLabelText('Año de vencimiento'), '2030');
    await userEvent.type(screen.getByLabelText('Código de seguridad'), '123');
    await userEvent.type(screen.getByLabelText('Titular de la tarjeta'), 'Ana Morales');
    await userEvent.click(screen.getByRole('button', { name: 'Pagar Q 199.00 y activar el plan' }));
    expect(await screen.findByRole('heading', { name: 'Tarjeta rechazada' })).toBeDefined();
    expect(screen.getByText(/No se autorizó el cobro de Q 199.00/)).toBeDefined();
    expect(screen.getByText('Lanzamiento · Rechazado')).toBeDefined();
    expect(screen.getByText('Visa •••• 0002')).toBeDefined();
    expect(screen.getByText(/Prueba · Q 0.00 · Sin cambios/)).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Usar otra tarjeta' }));
    expect(await screen.findByRole('heading', { name: 'Contratación de un plan superior' })).toBeDefined();
    expect((screen.getByLabelText('Número de tarjeta') as HTMLInputElement).value).toBe('');
    expect((screen.getByLabelText('Código de seguridad') as HTMLInputElement).value).toBe('');
    server.use(http.post(API + '/suscripcion/contratar', () => HttpResponse.json({
      id: 'sub-1', plan: lanzamiento, estado: 'activa', periodo: suscripcion.periodo,
      proximaRenovacion: suscripcion.proximaRenovacion, tarjetaEnmascarada: 'Visa •••• 4242',
    }, { status: 201 })));
    await userEvent.type(screen.getByLabelText('Número de tarjeta'), '4242424242424242');
    await userEvent.type(screen.getByLabelText('Código de seguridad'), '123');
    await userEvent.click(screen.getByRole('button', { name: 'Pagar Q 199.00 y activar el plan' }));
    expect(await screen.findByRole('heading', { name: 'Plan activado' })).toBeDefined();
  });

  it('A2.2 comunica una caída de pasarela sin presentarla como rechazo de tarjeta', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento])),
      http.get(API + '/suscripcion', () => HttpResponse.json(suscripcionPrueba)),
      http.post(API + '/suscripcion/contratar', () => HttpResponse.json({ title: 'La pasarela no está disponible.', codigo: 'pasarela_no_disponible' }, { status: 503, headers: { 'Content-Type': 'application/problem+json' } })),
    );
    renderizar('/panel/suscripcion/contratar/plan-1', <Routes><Route path="/panel/suscripcion/contratar/:plan" element={<PaginaA22Contratacion />} /></Routes>);
    await userEvent.type(await screen.findByLabelText('Número de tarjeta'), '4242424242424242');
    await userEvent.type(screen.getByLabelText('Mes de vencimiento'), '12');
    await userEvent.type(screen.getByLabelText('Año de vencimiento'), '2030');
    await userEvent.type(screen.getByLabelText('Código de seguridad'), '123');
    await userEvent.type(screen.getByLabelText('Titular de la tarjeta'), 'Ana Morales');
    await userEvent.click(screen.getByRole('button', { name: 'Pagar Q 199.00 y activar el plan' }));
    expect(await screen.findByText('La pasarela no está disponible.')).toBeDefined();
    expect(screen.queryByRole('heading', { name: 'Tarjeta rechazada' })).toBeNull();
  });

  it('A2.5 muestra el cálculo de prorrateo y confirma el cambio', async () => {
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento, producto])),
      http.get(API + '/suscripcion', () => HttpResponse.json(cambioSolicitado
        ? { ...suscripcion, plan: { ...suscripcion.plan, id: producto.id, nombre: producto.nombre, precio: producto.precio } }
        : suscripcion)),
      http.post(API + '/suscripcion/cambiar', () => { cambioSolicitado = true; return HttpResponse.json({ estado: 'activa', aPagar: 173.34 }); }),
    );
    renderizar('/panel/suscripcion/cambiar/plan-2', <Routes>
      <Route path="/panel/suscripcion/cambiar/:plan" element={<PaginaA25CambioPlan />} />
      <Route path="/panel/suscripcion" element={<PaginaB14Suscripcion />} />
    </Routes>);
    expect(await screen.findByText('Diferencia prorrateada')).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Lanzamiento' })).toBeDefined();
    expect(screen.getByText('Plan actual')).toBeDefined();
    expect(screen.getByText('Plan destino')).toBeDefined();
    expect(screen.getByText('3 APIs')).toBeDefined();
    expect(screen.getByText('10 APIs')).toBeDefined();
    expect(screen.getByText('Q 173.34')).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Pagar Q 173.34 y cambiar de plan' }));
    await waitFor(() => expect(cambioSolicitado).toBe(true));
    expect(await screen.findByRole('heading', { name: 'Suscripción de plataforma' })).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Producto' })).toBeDefined();
    expect(screen.getByText('Q 599.00')).toBeDefined();
    expect(screen.queryByText('Cambio programado')).toBeNull();
    expect(screen.queryByRole('button', { name: 'Programar cambio de plan' })).toBeNull();
  });

  it('RF-25 · una bajada confirmada vuelve a B1.4 y muestra el cambio programado', async () => {
    const actual = { ...suscripcion, plan: { ...suscripcion.plan, id: producto.id, nombre: producto.nombre, precio: producto.precio } };
    server.use(
      http.get(API + '/planes-plataforma', () => HttpResponse.json([lanzamiento, producto])),
      http.get(API + '/suscripcion', () => HttpResponse.json({
        ...actual, cambioProgramado: cambioSolicitado
          ? { planId: lanzamiento.id, nombre: lanzamiento.nombre, efectivoDesde: suscripcion.proximaRenovacion } : null,
      })),
      http.post(API + '/suscripcion/cambiar', () => {
        cambioSolicitado = true;
        return HttpResponse.json({ estado: 'programado', planSiguienteId: lanzamiento.id, efectivoDesde: suscripcion.proximaRenovacion });
      }),
    );
    renderizar('/panel/suscripcion/cambiar/plan-1', <Routes>
      <Route path="/panel/suscripcion/cambiar/:plan" element={<PaginaA25CambioPlan />} />
      <Route path="/panel/suscripcion" element={<PaginaB14Suscripcion />} />
    </Routes>);
    await userEvent.click(await screen.findByRole('button', { name: 'Programar cambio de plan' }));
    expect(await screen.findByRole('heading', { name: 'Suscripción de plataforma' })).toBeDefined();
    expect(screen.getByText('A partir del 23 sept 2026 su plan será Lanzamiento.')).toBeDefined();
    expect(await screen.findByRole('button', { name: 'Cancelar cambio programado' })).toBeDefined();
  });

  it.each(['en_gracia', 'suspendida'])('RF-44 · Prueba o gratuito %s lleva a contratar sin formulario de reactivación', async estado => {
    server.use(http.get(API + '/suscripcion', () => HttpResponse.json({
      ...suscripcionPrueba, estado, graciaHasta: '2026-09-30T06:00:00Z', diasRestantes: 5,
    })));
    renderizar('/panel/suscripcion', <PaginaB14Suscripcion />);
    const contratar = await screen.findByRole('link', { name: 'Contratar un plan de pago' });
    expect(contratar.getAttribute('href')).toBe('/panel/suscripcion/planes');
    expect(screen.queryByRole('button', { name: 'Pagar con otra tarjeta' })).toBeNull();
    expect(screen.queryByLabelText('Número de tarjeta')).toBeNull();
    expect(screen.queryByText(/El cobro de la renovación fue rechazado/)).toBeNull();
    expect(screen.queryByText('Renovación automática')).toBeNull();
    expect(screen.queryByText('Cobro rechazado')).toBeNull();
    expect(screen.getByText('Fin del periodo')).toBeDefined();
  });

  it('RF-23 · B1.4 muestra el rechazo según la fecha de Guatemala', async () => {
    server.use(http.get(API + '/suscripcion', () => HttpResponse.json({
      ...suscripcion, estado: 'en_gracia', ultimoRechazoEn: '2026-09-24T02:00:00Z',
      graciaHasta: '2026-10-01T02:00:00Z', diasRestantes: 5,
    })));
    renderizar('/panel/suscripcion', <PaginaB14Suscripcion />);
    expect(await screen.findByText(/El cobro de la renovación fue rechazado el 23 sept 2026/)).toBeDefined();
    expect(screen.getByText(/Periodo de gracia hasta 30 sept 2026/)).toBeDefined();
  });

  it('B1.4 muestra el periodo activo y su tarjeta', async () => {
    server.use(http.get(API + '/suscripcion', () => HttpResponse.json(suscripcion)));
    renderizar('/panel/suscripcion', <PaginaB14Suscripcion />);
    expect(await screen.findByRole('heading', { name: 'Suscripción de plataforma' })).toBeDefined();
    expect(screen.getByText('Visa •••• 4821')).toBeDefined();
    expect(screen.getByText('Renovación automática')).toBeDefined();
    expect(screen.getByText('Activa')).toBeDefined();
    expect(screen.getByText('Q 199.00')).toBeDefined();
    expect(screen.getByText('Vigencia de 30 días')).toBeDefined();
  });

  it('B1.4 en gracia permite pagar con otra tarjeta y reactivar', async () => {
    server.use(
      http.get(API + '/suscripcion', () => HttpResponse.json({ ...suscripcion, estado: 'en_gracia', graciaHasta: '2026-09-30T06:00:00Z', diasRestantes: 5 })),
      http.post(API + '/suscripcion/pagar', () => HttpResponse.json({ estado: 'activa' })),
    );
    renderizar('/panel/suscripcion', <PaginaB14Suscripcion />);
    expect(await screen.findByText(/Quedan 5 días/)).toBeDefined();
    expect(screen.getByText('En gracia')).toBeDefined();
    expect(screen.getByText('Q 199.00')).toBeDefined();
    expect(screen.getByText('Vigencia de 30 días')).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Pagar con otra tarjeta' }));
    expect(screen.getByLabelText('Número de tarjeta')).toBeDefined();
  });

  it('B1.4 suspendida indica que el tráfico está detenido y conserva sus claves', async () => {
    server.use(http.get(API + '/suscripcion', () => HttpResponse.json({ ...suscripcion, estado: 'suspendida', graciaHasta: '2026-09-30T06:00:00Z' })));
    renderizar('/panel/suscripcion', <PaginaB14Suscripcion />);
    expect(await screen.findByRole('heading', { name: 'Su tráfico está detenido' })).toBeDefined();
    expect(screen.getByText('Suspendida')).toBeDefined();
    expect(screen.getByText('Q 199.00')).toBeDefined();
    expect(screen.getByText('Vigencia de 30 días')).toBeDefined();
    expect(screen.getByText(/Sus claves, las suscripciones de sus consumidores y su historial se conservan/)).toBeDefined();
    expect(screen.getByRole('button', { name: 'Pagar con otra tarjeta' })).toBeDefined();
  });
});
