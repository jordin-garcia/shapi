import { EstadoError } from '@shapi/ui';
import { useCerrarSesion } from './useCerrarSesion';

/** Botón de salir del pie de la barra lateral (N.1). */
export function CerrarSesion() {
  const salir = useCerrarSesion();
  return <div>
    <button onClick={() => salir.mutate()} disabled={salir.isPending} className="w-9 h-9 flex items-center justify-center border border-borde-campo rounded-base text-tinta-navegacion transition-colors hover:text-tinta hover:border-principal disabled:opacity-50" title="Cerrar sesión" aria-label="Cerrar sesión">
      <svg width="18" height="18" viewBox="0 0 20 20" fill="none" aria-hidden="true">
        <path d="M8 3.5 H5 A1.5 1.5 0 0 0 3.5 5 V15 A1.5 1.5 0 0 0 5 16.5 H8 M8.5 10 H16.5 M13.5 7 L16.5 10 L13.5 13" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    </button>
    {salir.isError && <EstadoError mensaje="No se pudo cerrar la sesión." reintentar={() => salir.mutate()} />}
  </div>;
}
