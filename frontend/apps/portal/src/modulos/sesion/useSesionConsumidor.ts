import { useQuery } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';

export interface SesionConsumidor {
  consumidor: {
    nombre: string;
    nombreEmpresa: string;
  };
  correoVerificado: boolean;
}

export interface RutasSesionPortal {
  '/api/portal/auth/sesion': {
    get: {
      responses: {
        200: { content: { 'application/json': SesionConsumidor } };
        401: { content?: never };
      };
    };
  };
  '/api/portal/auth/salir': {
    post: {
      responses: {
        200: { content?: never };
        401: { content?: never };
      };
    };
  };
}

export const clienteSesion = crearCliente<RutasSesionPortal>(window.location.origin);
export const claveSesionConsumidor = ['portal', 'sesion'] as const;

async function consultarSesion({ signal }: { signal?: AbortSignal } = {}): Promise<SesionConsumidor | null> {
  try {
    const { data, response } = await clienteSesion.GET('/api/portal/auth/sesion', { signal });
    if (response.status === 401) return null;
    if (!response.ok || !data) throw new Error('No se pudo consultar la sesión del consumidor');
    return data;
  } catch (error) {
    if (error instanceof ErrorApi && error.estado === 401) return null;
    throw error;
  }
}

export function useSesionConsumidor() {
  return useQuery({
    queryKey: claveSesionConsumidor,
    queryFn: consultarSesion,
    retry: false,
    staleTime: 5 * 60 * 1000,
  });
}
