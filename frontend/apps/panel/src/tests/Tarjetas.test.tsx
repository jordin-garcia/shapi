import { afterEach, beforeEach, expect, it } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import Contratacion from '../paginas/A2-2-Contratacion';
import Cambio from '../paginas/A2-5-CambioPlan';
import Suscripcion from '../paginas/B1-4-Suscripcion';

const plan = { id: 'p1', nombre: 'Lanzamiento', precio: 199, inicioCicloPrevisto: '2026-09-01T06:00:00Z', vigenciaDias: 30, maxApis: 3, maxMiembros: 3, cuotaPeticiones: 250000, esPrueba: false };
const actual = { plan, estado: 'en_gracia', periodo: { inicio: '2026-09-01T06:00:00Z', fin: '2026-09-30T06:00:00Z' }, proximaRenovacion: '2026-10-01T06:00:00Z', diasRestantesCiclo: 15, tarjetaEnmascarada: null };
let cache: QueryClient;
beforeEach(() => {
  cache = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  server.use(
    http.get('*/api/planes-plataforma', () => HttpResponse.json([plan, { ...plan, id: 'p2', nombre: 'Producto', precio: 599 }])),
    http.get('*/api/suscripcion', () => HttpResponse.json(actual)),
    http.get('*/api/auth/sesion', () => HttpResponse.json({ usuario: { nombre: 'Ana' }, organizacion: { id: 'org' }, rol: 'propietario', correoVerificado: true })),
  );
});
afterEach(() => { cleanup(); cache.clear(); });

for (const [pantalla, endpoint, entrada, Vista] of [
  ['A2.2', 'contratar', '/contratar/p1', Contratacion],
  ['A2.5', 'cambiar', '/cambiar/p2', Cambio],
  ['B1.4', 'pagar', '/pagar/p1', Suscripcion],
] as const) {
  it.each(['rechazo', 'validacion', 'red', 'aprobado'])(`RF-20 · ${pantalla} vacía solo tras pago: %s`, async resultado => {
    server.use(http.post(`*/api/suscripcion/${endpoint}`, () => resultado === 'red' ? HttpResponse.error()
      : resultado === 'aprobado' ? HttpResponse.json({ id: 'sub', plan, estado: 'activa', periodo: actual.periodo, proximaRenovacion: actual.proximaRenovacion })
      : HttpResponse.json({ title: 'Respuesta de prueba', codigo: resultado === 'rechazo' ? 'pago_rechazado' : 'datos_invalidos' }, { status: resultado === 'rechazo' ? 402 : 400, headers: { 'Content-Type': 'application/problem+json' } })));
    render(<QueryClientProvider client={cache}><MemoryRouter initialEntries={[entrada]}><Routes><Route path="/:accion/:plan" element={<Vista />} /><Route path="/panel/suscripcion" element={<div>Cambio confirmado</div>} /></Routes></MemoryRouter></QueryClientProvider>);
    if (pantalla === 'B1.4') await userEvent.click(await screen.findByRole('button', { name: 'Pagar con otra tarjeta' }));
    const etiquetas = ['Número de tarjeta', 'Mes de vencimiento', 'Año de vencimiento', 'Código de seguridad', 'Titular de la tarjeta'];
    const valores = ['4000000000000002', '12', '2030', '123', 'Ana Morales'];
    await screen.findByLabelText(etiquetas[0]);
    etiquetas.forEach((etiqueta, i) => fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valores[i] } }));
    await userEvent.click(screen.getByRole('button', { name: /Pagar .*activar|Pagar .*cambiar|Pagar y reactivar/ }));
    if (resultado === 'aprobado') {
      await waitFor(() => expect(screen.queryByLabelText(etiquetas[0])).toBeNull());
      if (pantalla === 'B1.4') await userEvent.click(screen.getByRole('button', { name: 'Pagar con otra tarjeta' }));
      else return;
    } else if (resultado === 'rechazo' && pantalla === 'A2.2') {
      await userEvent.click(await screen.findByRole('button', { name: 'Usar otra tarjeta' }));
    } else {
      await screen.findByRole('alert');
    }
    etiquetas.forEach((etiqueta, i) => expect((screen.getByLabelText(etiqueta) as HTMLInputElement).value).toBe(resultado === 'rechazo' || resultado === 'aprobado' ? '' : valores[i]));
  });
}
