import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { paths } from '@shapi/api/portal';

const clientePortal = crearCliente<paths>(window.location.origin);

export const claveRutasConsola = ['portal', 'documentacion', 'consola'] as const;

export function useRutasConsola() {
  return useQuery({
    queryKey: claveRutasConsola,
    queryFn: async ({ signal }) => {
      const { data, response } = await clientePortal.GET('/api/portal/documentacion', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron consultar las rutas expuestas');
      return data.rutas;
    },
    staleTime: 5 * 60 * 1000,
  });
}
