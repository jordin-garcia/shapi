import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router';
import { clienteSesion, claveSesionConsumidor } from './useSesionConsumidor';

export function CerrarSesionConsumidor() {
  const navegar = useNavigate();
  const cache = useQueryClient();
  const cerrar = useMutation({
    mutationFn: async () => {
      const { response } = await clienteSesion.POST('/api/portal/auth/salir');
      if (!response.ok && response.status !== 401) throw new Error('No se pudo cerrar la sesión');
    },
    onSuccess: async () => {
      cache.setQueryData(claveSesionConsumidor, null);
      await navegar('/entrar', { replace: true });
    },
  });

  return (
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
  );
}
