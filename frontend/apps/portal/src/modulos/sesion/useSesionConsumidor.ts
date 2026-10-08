import { useQuery } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/identidad';

/** La sesión del consumidor, con el `destino` de 10 §1 que calcula la API (EM-18). */
export type SesionConsumidor = components['schemas']['SesionConsumidor'];

export const clienteSesion = crearCliente<paths>(window.location.origin);
export const claveSesionConsumidor = ['portal', 'sesion'] as const;

export async function consultarSesionConsumidor({ signal }: { signal?: AbortSignal } = {}): Promise<SesionConsumidor | null> {
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

export function useSesionConsumidor({ enabled = true }: { enabled?: boolean } = {}) {
  return useQuery({
    queryKey: claveSesionConsumidor,
    queryFn: consultarSesionConsumidor,
    enabled,
    retry: false,
    staleTime: 5 * 60 * 1000,
  });
}
