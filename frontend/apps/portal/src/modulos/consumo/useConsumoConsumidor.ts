import { useQuery } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/consumo';

const cliente = crearCliente<paths>(window.location.origin);

export function useConsumoConsumidor() {
  return useQuery({
    queryKey: ['portal', 'consumo'],
    queryFn: async ({ signal }): Promise<components['schemas']['ConsumoCiclo'] | null> => {
      try {
        const { data, response } = await cliente.GET('/api/portal/consumo', { signal });
        if (!response.ok || !data) throw new Error('No se pudo cargar el consumo.');
        return data;
      } catch (error) {
        if (error instanceof ErrorApi && error.estado === 404 && error.codigo === 'suscripcion_no_encontrada') return null;
        throw error;
      }
    },
    retry: false,
    refetchInterval: 10_000,
  });
}
