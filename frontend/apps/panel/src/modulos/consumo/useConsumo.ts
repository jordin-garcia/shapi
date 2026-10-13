import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { paths } from '@shapi/api/consumo';

const cliente = crearCliente<paths>(window.location.origin);
export function useConsumo(apiId: string, periodo?: { desde: string; hasta: string }) {
  return useQuery({
    queryKey: ['consumo', apiId, periodo?.desde, periodo?.hasta],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/apis/{apiId}/consumo', {
        params: { path: { apiId }, query: periodo }, signal,
      });
      if (!response.ok || !data) throw new Error('No se pudo cargar el consumo.');
      return data;
    },
    enabled: !!apiId,
    retry: false,
  });
}
