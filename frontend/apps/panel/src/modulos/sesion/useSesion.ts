import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { paths } from '@shapi/api/identidad';

const cliente = crearCliente<paths>('/api');

export function useSesion() {
  return useQuery({
    queryKey: ['sesion'],
    queryFn: async () => {
      // Provisional: in reality it calls /api/auth/sesion
      // Wait, there's no backend endpoint yet, so this will fail in test/dev
      // Let's mock a success if we're not using MSW in dev, but for now we should just make the actual call
      const { data, error } = await cliente.GET('/api/auth/sesion');
      if (error) throw error;
      return data;
    },
    retry: false
  });
}
