import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router';
import { ErrorApi } from '@shapi/api';
import { clienteSesion } from './useSesion';

/** Revoca la sesión en el servidor y, solo si lo logra, lleva a /entrar y borra los datos privados de la caché. */
export function useCerrarSesion() {
  const cache = useQueryClient();
  const navigate = useNavigate();
  return useMutation({
    mutationFn: async () => {
      try {
        const { response } = await clienteSesion.POST('/api/auth/salir', {
          params: { header: { 'X-Requested-With': 'shapi' } },
        });
        if (!response.ok && response.status !== 401) throw new Error('No se pudo cerrar la sesión');
      } catch (error) {
        if (!(error instanceof ErrorApi && error.estado === 401)) throw error;
      }
    },
    onSuccess: async () => {
      await cache.cancelQueries();
      await navigate('/entrar', { replace: true });
      cache.clear();
    },
  });
}
