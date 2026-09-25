import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router';
import { ProblemDetailsError, crearCliente } from '@shapi/api';
import { EstadoError } from '@shapi/ui';
import type { ContratoSesion } from './contratoSesion';

const cliente = crearCliente<ContratoSesion>(window.location.origin);

export function CerrarSesion() {
  const cache = useQueryClient();
  const navigate = useNavigate();
  const salir = useMutation({
    mutationFn: async () => {
      try {
        const { response } = await cliente.POST('/api/auth/salir', {
          params: { header: { 'X-Requested-With': 'shapi' } },
        });
        if (!response.ok && response.status !== 401) throw new Error('No se pudo cerrar la sesión');
      } catch (error) {
        if (!(error instanceof ProblemDetailsError && error.status === 401)) throw error;
      }
    },
    onSuccess: async () => {
      await cache.cancelQueries();
      await navigate('/entrar', { replace: true });
      cache.clear();
    },
  });

  return <div>
    <button onClick={() => salir.mutate()} disabled={salir.isPending} className="w-9 h-9 flex items-center justify-center border border-[#2A3550] rounded-lg text-[#B9C4D8] hover:text-[#E8EDF7] disabled:opacity-50" title="Cerrar sesión" aria-label="Cerrar sesión">
      <svg width="18" height="18" viewBox="0 0 20 20" fill="none" aria-hidden="true">
        <path d="M8 3.5 H5 A1.5 1.5 0 0 0 3.5 5 V15 A1.5 1.5 0 0 0 5 16.5 H8 M8.5 10 H16.5 M13.5 7 L16.5 10 L13.5 13" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    </button>
    {salir.isError && <EstadoError mensaje="No se pudo cerrar la sesión." reintentar={() => salir.mutate()} />}
  </div>;
}
