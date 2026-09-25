import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../../test/servidor';
import { crearCliente, ProblemDetailsError } from './index';

type ContratoSesion = {
  '/api/auth/sesion': { get: { responses: { 200: { content: { 'application/json': unknown } } } } };
  '/api/auth/salir': { post: { parameters: { header: { 'X-Requested-With': 'shapi' } }; responses: { 200: { content?: never } } } };
};

type Contrato = { '/prueba': { post: { responses: { 200: { content: { 'application/json': { listo: boolean } } } } } } };
const cliente = crearCliente<Contrato>('http://localhost');

describe('RNF-12 · cliente HTTP', () => {
  it('envía X-Requested-With y credenciales', async () => {
    server.use(http.post('http://localhost/prueba', ({ request }) => {
      expect(request.headers.get('X-Requested-With')).toBe('shapi');
      expect(request.credentials).toBe('include');
      return HttpResponse.json({ listo: true });
    }));
    expect((await cliente.POST('/prueba')).data).toEqual({ listo: true });
  });
  it('convierte las extensiones codigo y errores de ProblemDetails', async () => {
    server.use(http.post('http://localhost/prueba', () => HttpResponse.json({
      type: 'about:blank', title: 'Límite del plan', codigo: 'limite_del_plan', errores: { nombre: ['Requerido'] },
    }, { status: 422, headers: { 'Content-Type': 'application/problem+json' } })));
    await expect(cliente.POST('/prueba')).rejects.toMatchObject({
      name: 'ProblemDetailsError', details: { codigo: 'limite_del_plan', titulo: 'Límite del plan', errores: { nombre: ['Requerido'] } },
    });
  });
  it('usa error cuando falta codigo, aunque haya type', async () => {
    server.use(http.post('http://localhost/prueba', () => HttpResponse.json({ type: 'https://ejemplo.test/problema', title: 'Fallo' }, { status: 500, headers: { 'Content-Type': 'application/problem+json' } })));
    await expect(cliente.POST('/prueba')).rejects.toMatchObject({ details: { codigo: 'error' } });
  });
  it.each(['{invalido', 'null', '[]'])('normaliza un ProblemDetails no utilizable: %s', async cuerpo => {
    server.use(http.post('http://localhost/prueba', () => new HttpResponse(cuerpo, { status: 500, headers: { 'Content-Type': 'application/problem+json' } })));
    await expect(cliente.POST('/prueba')).rejects.toBeInstanceOf(ProblemDetailsError);
    await expect(cliente.POST('/prueba')).rejects.toMatchObject({ details: { codigo: 'error', titulo: 'Ocurrió un error inesperado' } });
  });
});

describe('RF-07 · cliente de sesión', () => {
  it('envía CSRF y credenciales al cerrar sesión', async () => {
    server.use(http.post('http://localhost/api/auth/salir', ({ request }) => {
      expect(request.headers.get('X-Requested-With')).toBe('shapi');
      expect(request.credentials).toBe('include');
      return new HttpResponse(null, { status: 200 });
    }));
    const cliente = crearCliente<ContratoSesion>('http://localhost');
    const { response } = await cliente.POST('/api/auth/salir', { params: { header: { 'X-Requested-With': 'shapi' } } });
    expect(response.status).toBe(200);
  });
  it('conserva estado HTTP, código y errores de ProblemDetails', async () => {
    server.use(http.get('http://localhost/api/auth/sesion', () => HttpResponse.json({
      title: 'Sin sesión', codigo: 'sin_sesion', errores: { sesion: ['Vencida'] },
    }, { status: 401, headers: { 'Content-Type': 'application/problem+json' } })));
    const cliente = crearCliente<ContratoSesion>('http://localhost');
    await expect(cliente.GET('/api/auth/sesion')).rejects.toMatchObject({
      status: 401, details: { codigo: 'sin_sesion', titulo: 'Sin sesión', errores: { sesion: ['Vencida'] } },
    });
  });
});
