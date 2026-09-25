import createClient, { type Middleware } from 'openapi-fetch';

export interface ErrorApi {
  codigo: string;
  titulo: string;
  errores?: Record<string, string[]>;
}

export class ProblemDetailsError extends Error {
  constructor(public details: ErrorApi, public status?: number) {
    super(details.titulo);
    this.name = 'ProblemDetailsError';
  }
}

function esObjeto(valor: unknown): valor is Record<string, unknown> {
  return typeof valor === 'object' && valor !== null && !Array.isArray(valor);
}

const problemDetailsMiddleware: Middleware = {
  onRequest({ request }) {
    request.headers.set('X-Requested-With', 'shapi');
    return request;
  },
  async onResponse({ response }) {
    if (!response.ok && response.headers.get('content-type')?.includes('application/problem+json')) {
      let problem: Record<string, unknown> = {};
      try {
        const cuerpo: unknown = await response.clone().json();
        if (esObjeto(cuerpo)) problem = cuerpo;
      } catch {
        // Un cuerpo inválido sigue siendo un error HTTP normalizado.
      }
      const errores = esObjeto(problem.errores)
        ? Object.fromEntries(Object.entries(problem.errores).filter((entrada): entrada is [string, string[]] =>
          Array.isArray(entrada[1]) && entrada[1].every(valor => typeof valor === 'string')))
        : undefined;
      throw new ProblemDetailsError({
        codigo: typeof problem.codigo === 'string' && problem.codigo ? problem.codigo : 'error',
        titulo: typeof problem.title === 'string' && problem.title ? problem.title : 'Ocurrió un error inesperado',
        errores,
      }, response.status);
    }
    return response;
  },
};

export function crearCliente<T extends object>(baseUrl?: string) {
  const client = createClient<T>({
    baseUrl,
    credentials: 'include',
    fetch: (...args) => globalThis.fetch(...args),
  });
  client.use(problemDetailsMiddleware);
  return client;
}
