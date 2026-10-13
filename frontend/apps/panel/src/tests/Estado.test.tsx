import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Routes, Route } from 'react-router';
import { server } from '../../../../test/servidor';
import PaginaB31Estado from '../paginas/B3-1-Estado';
import { LayoutAdmin } from '../layouts/LayoutAdmin';

let cliente: QueryClient;
const ids = ['compuerta', 'api_control', 'trabajador', 'redis', 'postgres', 'pasarela', 'correo'];
function datos(degradado = false) {
  return { comprobadoEn: '2026-09-11T16:40:00Z', latenciaP95Ms: 11, latenciaSupera2500: false,
    componentes: ids.map(id => ({ id, estado: degradado && id === 'correo' ? 'degradado' : 'en_servicio',
      incidenciaDesde: degradado && id === 'correo' ? '2026-09-11T15:20:00Z' : null })) };
}
function mostrar() { render(<QueryClientProvider client={cliente}><PaginaB31Estado /></QueryClientProvider>); }
beforeEach(() => { cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } }); });
afterEach(() => { cleanup(); cliente.clear(); });

describe('RF-39 · B3.1', () => {
  it('reproduce la vista de soporte con su nombre y navegación reducida', async () => {
    server.use(
      http.get('http://localhost/api/admin/estado', () => HttpResponse.json(datos())),
      http.get('http://localhost/api/auth/sesion', () => HttpResponse.json({ usuario: { nombre: 'Sofía Menchú Cojtí', correo: 'sofia@ejemplo.test' }, organizacion: { id: 'plataforma', nombre: 'Shapi' }, rol: 'soporte', correoVerificado: true, destino: '/admin/casos' })),
    );
    render(<QueryClientProvider client={cliente}><MemoryRouter initialEntries={['/admin/estado']}><Routes><Route path="/admin" element={<LayoutAdmin />}><Route path="estado" element={<PaginaB31Estado />} /></Route></Routes></MemoryRouter></QueryClientProvider>);
    expect(await screen.findByText('Sofía Menchú Cojtí')).toBeDefined();
    expect(await screen.findByText('11 ms')).toBeDefined();
    expect(screen.getByRole('link', { name: 'Estado de los componentes' }).getAttribute('aria-current')).toBe('page');
    expect(screen.getByRole('link', { name: 'Casos' })).toBeDefined();
    expect(screen.getByRole('link', { name: 'Bitácora de acciones' })).toBeDefined();
    expect(screen.queryByRole('link', { name: 'Cuentas de plataforma' })).toBeNull();
    expect(screen.queryByRole('link', { name: 'Organizaciones' })).toBeNull();
  });
  it('reproduce las columnas, siete componentes y latencia del mockup normal', async () => {
    server.use(http.get('http://localhost/api/admin/estado', () => HttpResponse.json(datos())));
    mostrar();
    expect(await screen.findByText('11 ms')).toBeDefined();
    expect(screen.getByText('Estado de cada componente de la plataforma. Comprobado el 11 de septiembre de 2026 a las 10:40.')).toBeDefined();
    expect(screen.getAllByRole('columnheader').map(x => x.textContent)).toEqual(['Componente', 'Qué hace', 'Estado']);
    expect(screen.getAllByText('En servicio')).toHaveLength(7);
    for (const nombre of ['Compuerta de APIs', 'API de control', 'Trabajador en segundo plano', 'Caché (Redis)', 'Base de datos (PostgreSQL)', 'Pasarela de pagos simulada', 'Envío de correo']) {
      expect(screen.getByText(nombre)).toBeDefined();
    }
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('muestra la variante degradada con fecha e impacto del correo', async () => {
    server.use(http.get('http://localhost/api/admin/estado', () => HttpResponse.json(datos(true))));
    mostrar();
    expect(await screen.findByText('El envío de correo está degradado')).toBeDefined();
    expect(screen.getByRole('alert').textContent).toContain('Desde las 09:20 del 11 de septiembre de 2026');
    expect(screen.getByRole('alert').textContent).toContain('La compuerta no está afectada');
    expect(screen.getAllByText('En servicio')).toHaveLength(6);
    expect(screen.getByText('Degradado')).toBeDefined();
  });

  it('presenta ausencia de muestras y rango abierto sin inventar una cifra', async () => {
    server.use(http.get('http://localhost/api/admin/estado', () => HttpResponse.json({ ...datos(), latenciaP95Ms: null })));
    mostrar();
    expect(await screen.findByText('Sin peticiones en la última hora')).toBeDefined();
    server.use(http.get('http://localhost/api/admin/estado', () => HttpResponse.json({ ...datos(), latenciaP95Ms: null, latenciaSupera2500: true })));
    await cliente.invalidateQueries({ queryKey: ['estado-componentes'] });
    expect(await screen.findByText('> 2500 ms')).toBeDefined();
  });

  it('no afirma que la compuerta esté sana cuando también está caída', async () => {
    const respuesta = datos(true);
    respuesta.componentes[0].estado = 'fuera_de_servicio';
    server.use(http.get('http://localhost/api/admin/estado', () => HttpResponse.json(respuesta)));
    mostrar();
    expect(await screen.findByText('Fuera de servicio')).toBeDefined();
    expect(screen.queryByText(/La compuerta no está afectada/)).toBeNull();
  });

  it('distingue la latencia no disponible por caída de Redis de la falta de tráfico', async () => {
    const respuesta = { ...datos(), latenciaP95Ms: null };
    respuesta.componentes[3].estado = 'fuera_de_servicio';
    server.use(http.get('http://localhost/api/admin/estado', () => HttpResponse.json(respuesta)));
    mostrar();
    expect(await screen.findByText('Latencia no disponible')).toBeDefined();
    expect(screen.queryByText('Sin peticiones en la última hora')).toBeNull();
  });

  it('muestra carga y permite reintentar errores', async () => {
    server.use(http.get('http://localhost/api/admin/estado', () => new HttpResponse(null, { status: 503 })));
    mostrar();
    await waitFor(() => expect(screen.getByRole('button', { name: /reintentar/i })).toBeDefined());
    server.use(http.get('http://localhost/api/admin/estado', () => HttpResponse.json(datos())));
    screen.getByRole('button', { name: /reintentar/i }).click();
    expect(await screen.findByText('11 ms')).toBeDefined();
  });
});
