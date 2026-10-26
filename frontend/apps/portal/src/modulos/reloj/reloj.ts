import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { paths } from '@shapi/api/suscripciones';

const cliente = crearCliente<paths>(window.location.origin);

export function useFechaServidor(habilitada = true) {
  return useQuery({
    queryKey: ['portal', 'fecha-servidor'],
    queryFn: async ({ signal }) => {
      const { data } = await cliente.GET('/api/portal/reloj', { signal });
      if (!data) throw new Error('No se pudo consultar la fecha del servidor.');
      return data;
    },
    enabled: habilitada,
    retry: false,
  });
}
