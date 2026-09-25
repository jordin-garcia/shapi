import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../../test/servidor';
import { crearCliente, ProblemDetailsError } from './index';

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
