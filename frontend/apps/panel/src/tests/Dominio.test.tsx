import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import Dominio from '../paginas/A3-6-Dominio';

const url = 'http://localhost/api/apis/api-1/dominio';
const base = {
  apiId: 'api-1', apiNombre: 'API de Envíos', urlPortal: 'https://envios.shapi.localhost',
  urlApi: 'https://envios.api.shapi.localhost', secretoEnmascarado: 'shps_••••1234', modoSimulado: true,
  dominio: 'api.envios.localhost', destinoCname: 'envios.api.shapi.localhost',
  estado: 'pendiente', motivo: null, verificadoEn: null,
  planNombre: 'Producto', dominioPropioPermitido: true,
};
let cache: QueryClient;
beforeEach(() => {
  server.use(http.get('http://localhost/api/auth/sesion', () => HttpResponse.json({ usuario: { nombre: 'Ana' }, organizacion: { id: 'org-1' }, rol: 'propietario' }))); cache = new QueryClient({ defaultOptions: { queries: { retry: false } } }); });
afterEach(() => { cleanup(); cache.clear(); });
function montar(datos = base) {
  server.use(http.get(url, () => HttpResponse.json(datos)));
  render(<QueryClientProvider client={cache}><MemoryRouter initialEntries={['/panel/apis/api-1/dominios']}>
    <Routes><Route path="/panel/apis/:id/dominios" element={<Dominio />} /></Routes>
  </MemoryRouter></QueryClientProvider>);
}

describe('JG-24 · RF-12 · configuración del dominio', () => {
  it('RF-12 permite vaciar un dominio existente sin restaurar el valor del servidor', async () => {
    montar();
    await userEvent.clear(await screen.findByLabelText('Dominio'));
    expect((screen.getByLabelText('Dominio') as HTMLInputElement).value).toBe('');
    expect((screen.getByRole('button', { name: 'Conectar dominio' }) as HTMLButtonElement).disabled).toBe(true);
  });

  it('RF-12 conecta el valor editado y refresca el registro DNS', async () => {
    let datos = { ...base, dominio: null as string | null, destinoCname: null as string | null };
    server.use(http.post(url, async ({ request }) => {
      expect(await request.json()).toEqual({ dominio: 'api.nuevo.localhost' });
      expect(request.headers.get('X-Requested-With')).toBe('shapi');
      datos = { ...datos, dominio: 'api.nuevo.localhost', destinoCname: base.destinoCname };
      return HttpResponse.json(datos);
    }));
    montar();
    server.use(http.get(url, () => HttpResponse.json(datos)));
    const campo = await screen.findByLabelText('Dominio');
    await userEvent.clear(campo);
    await userEvent.type(campo, 'api.nuevo.localhost');
    await userEvent.click(screen.getByRole('button', { name: 'Conectar dominio' }));
    expect(await screen.findByText('El dominio quedó pendiente de verificación.')).toBeDefined();
    expect(await screen.findByText('api.nuevo.localhost')).toBeDefined();
  });

  it.each([
    ['datos_invalidos', 'Revise el dominio.', { dominio: ['Use un dominio que termine en .localhost.'] }, 'Use un dominio que termine en .localhost.'],
    ['plan_sin_dominio_propio', 'Su plan no incluye dominio propio.', undefined, 'Su plan no incluye dominio propio.'],
  ])('RF-12 muestra el motivo de %s', async (codigo, title, errores, motivo) => {
    server.use(http.post(url, () => HttpResponse.json({ codigo, title, errores }, {
      status: codigo === 'datos_invalidos' ? 400 : 422, headers: { 'Content-Type': 'application/problem+json' },
    })));
    montar();
    await userEvent.click(await screen.findByRole('button', { name: 'Conectar dominio' }));
    expect(await screen.findByText(motivo)).toBeDefined();
  });

  it('RF-12 muestra el plan real que no incluye dominio propio', async () => {
    montar({ ...base, planNombre: 'Lanzamiento', dominioPropioPermitido: false });
    await screen.findByLabelText('Dominio');
    expect(document.body.textContent).toContain('Su plan Lanzamiento no incluye dominio propio.');
    expect(screen.queryByText(/Incluido en su plan Producto/)).toBeNull();
  });

  it.each([
    ['pendiente', 'Pendiente de verificación', 'eti-n', 'Pendiente de verificación'],
    ['verificado', 'Verificado', 'eti-c', 'Se emite con la primera petición'],
    ['fallido', 'Verificación fallida', 'eti-a', 'No disponible'],
  ])('RF-12 distingue DNS y TLS para un dominio %s', async (estado, etiqueta, color, tls) => {
    montar({ ...base, estado });
    await screen.findByLabelText('Dominio');
    expect(screen.getAllByText(etiqueta)[0].className).toContain(color);
    expect(screen.getByLabelText('Certificado TLS del dominio propio').textContent).toBe(tls);
    expect(screen.queryByText('Vigente')).toBeNull();
  });
});
