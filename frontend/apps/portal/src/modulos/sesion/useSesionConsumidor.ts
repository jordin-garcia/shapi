import { useQuery } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/identidad';

export type SesionConsumidor = components['schemas']['SesionConsumidor'] & {
  /** EM-05 debe publicar este destino para distinguir una cuenta con suscripción de una nueva. */
  destino?: '/cuenta/suscripcion' | '/planes';
};

export const clienteSesion = crearCliente<paths>(window.location.origin);
export const claveSesionConsumidor = ['portal', 'sesion'] as const;

export async function consultarSesionConsumidor({ signal }: { signal?: AbortSignal } = {}): Promise<SesionConsumidor | null> {
  try {
    const { data, response } = await clienteSesion.GET('/api/portal/auth/sesion', { signal });
    if (response.status === 401) return null;
    if (!response.ok || !data) throw new Error('No se pudo consultar la sesión del consumidor');
    return data as SesionConsumidor;
  } catch (error) {
    if (error instanceof ErrorApi && error.estado === 401) return null;
    throw error;
  }
}

export function useSesionConsumidor() {
  return useQuery({
    queryKey: claveSesionConsumidor,
    queryFn: consultarSesionConsumidor,
    retry: false,
    staleTime: 5 * 60 * 1000,
  });
}
