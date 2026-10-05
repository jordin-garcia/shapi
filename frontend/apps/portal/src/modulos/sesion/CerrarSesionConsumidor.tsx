import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router';
import { ErrorApi } from '@shapi/api';
import { clienteSesion, claveSesionConsumidor } from './useSesionConsumidor';
import { limpiarCacheConsumidor } from './cacheConsumidor';
import { AvisoError } from './FormulariosAcceso';

export function CerrarSesionConsumidor() {
  const navegar = useNavigate();
  const cache = useQueryClient();
  const cerrar = useMutation({
    mutationFn: async () => {
      try {
        const { response } = await clienteSesion.POST('/api/portal/auth/salir', { params: { header: { 'X-Requested-With': 'shapi' } } });
        if (!response.ok && response.status !== 401) throw new Error('No se pudo cerrar la sesión');
      } catch (error) {
        if (!(error instanceof ErrorApi && error.estado === 401)) throw error;
      }
    },
    onSuccess: async () => {
      // Como el panel (useCerrarSesion): otro consumidor en el mismo navegador no debe ver claves ni pagos del anterior.
      await limpiarCacheConsumidor(cache);
      cache.setQueryData(claveSesionConsumidor, null);
      await navegar('/entrar', { replace: true });
    },
  });

  // Fragmento: el aviso ocupa una línea propia, a todo el ancho del pie de la barra (que usa flex-wrap).
  return (
    <>
      <button
        type="button"
        aria-label="Cerrar sesión"
        title="Cerrar sesión"
        disabled={cerrar.isPending}
        onClick={() => cerrar.mutate()}
        className="size-9 shrink-0 rounded-base border border-borde-campo text-tinta-suave flex items-center justify-center hover:border-[var(--marca-principal)] hover:text-tinta disabled:opacity-50"
      >
        <svg width="18" height="18" viewBox="0 0 20 20" fill="none" aria-hidden="true">
          <path d="M8 3.5 H5 A1.5 1.5 0 0 0 3.5 5 V15 A1.5 1.5 0 0 0 5 16.5 H8" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
          <path d="M8.5 10 H16.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
          <path d="M13.5 7 L16.5 10 L13.5 13" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>
      {cerrar.isError && (
        <div className="basis-full">
          <AvisoError mensaje="No se pudo cerrar la sesión." reintentar={() => cerrar.mutate()} />
        </div>
      )}
    </>
  );
}
