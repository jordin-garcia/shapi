import createClient, { type Middleware } from 'openapi-fetch';

/** Error de la API de control: el ProblemDetails de la respuesta con su `codigo`, su título y sus errores por campo. */
export class ErrorApi extends Error {
  constructor(
    public codigo: string,
    public titulo: string,
    public estado: number,
    public errores?: Record<string, string[]>,
    public detalle?: Record<string, unknown>,
  ) {
    super(titulo);
    this.name = 'ErrorApi';
  }
}

function esObjeto(valor: unknown): valor is Record<string, unknown> {
  return typeof valor === 'object' && valor !== null && !Array.isArray(valor);
}

// Agrega la cabecera CSRF (10 §1) y convierte cada respuesta ProblemDetails con error en un ErrorApi.
const convertirProblemas: Middleware = {
  onRequest({ request }) {
    request.headers.set('X-Requested-With', 'shapi');
    return request;
  },
  async onResponse({ response }) {
    if (!response.ok && response.headers.get('content-type')?.includes('application/problem+json')) {
      let problema: Record<string, unknown> = {};
      try {
        const cuerpo: unknown = await response.clone().json();
        if (esObjeto(cuerpo)) problema = cuerpo;
      } catch {
        // Un cuerpo inválido sigue siendo un error HTTP normalizado.
      }
      const errores = esObjeto(problema.errores)
        ? Object.fromEntries(Object.entries(problema.errores).filter((entrada): entrada is [string, string[]] =>
          Array.isArray(entrada[1]) && entrada[1].every(valor => typeof valor === 'string')))
        : undefined;
      const detalle = esObjeto(problema.detalle) ? problema.detalle : undefined;
      throw new ErrorApi(
        typeof problema.codigo === 'string' && problema.codigo ? problema.codigo : 'error',
        typeof problema.title === 'string' && problema.title ? problema.title : 'Ocurrió un error inesperado',
        response.status,
        errores,
        detalle,
      );
    }
    return response;
  },
};

export function crearCliente<T extends object>(baseUrl?: string) {
  const cliente = createClient<T>({
    baseUrl,
    credentials: 'include',
    fetch: (...args) => globalThis.fetch(...args),
  });
  cliente.use(convertirProblemas);
  return cliente;
}
