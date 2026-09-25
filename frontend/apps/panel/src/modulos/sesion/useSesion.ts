import { useQuery } from '@tanstack/react-query';
import { crearCliente, ProblemDetailsError } from '@shapi/api';
import type { paths } from '@shapi/api/identidad';

export const clienteSesion = crearCliente<paths>(window.location.origin);

export function useSesion() {
  return useQuery({
    queryKey: ['sesion'],
    queryFn: async ({ signal }) => {
      try {
        const { data, response } = await clienteSesion.GET('/api/auth/sesion', { signal });
        if (response.status === 401) return null;
        if (!response.ok || !data) throw new Error('No se pudo consultar la sesión');
        return data;
      } catch (error) {
        if (error instanceof ProblemDetailsError && error.status === 401) return null;
        throw error;
      }
    },
    retry: false,
  });
}
