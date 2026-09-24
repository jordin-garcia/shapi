import createClient, { Middleware } from 'openapi-fetch';

export interface ErrorApi {
  codigo: string;
  titulo: string;
  errores?: Record<string, string[]>;
}

export class ProblemDetailsError extends Error {
  constructor(public details: ErrorApi) {
    super(details.titulo);
    this.name = 'ProblemDetailsError';
  }
}

const problemDetailsMiddleware: Middleware = {
  async onRequest({ request }) {
    request.headers.set('X-Requested-With', 'shapi');
    return request;
  },
  async onResponse({ response }) {
    if (!response.ok) {
      const contentType = response.headers.get("content-type");
      if (contentType?.includes("application/problem+json")) {
        const problem = await response.clone().json();
        const errorApi: ErrorApi = {
          codigo: problem.type || 'error',
          titulo: problem.title || 'Ocurrió un error inesperado',
          errores: problem.errors || undefined
        };
        throw new ProblemDetailsError(errorApi);
      }
    }
    return response;
  }
};

export function crearCliente(baseUrl?: string) {
  const client = createClient<Record<string, never>>({
    baseUrl,
    // Note: Some options like credentials might be passed per request, but we can configure base options or just add it to fetch
    fetch: async (request: Request) => {
      // For global credentials we can reconstruct the request
      const req = new Request(request, { credentials: 'include' });
      return fetch(req);
    }
  });

  client.use(problemDetailsMiddleware);

  return client;
}
