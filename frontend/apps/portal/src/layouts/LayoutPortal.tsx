import type { ReactNode } from 'react';
import { Outlet, useLocation } from 'react-router';
import { BotonMenu, EstadoError, FondoCajon } from '@shapi/ui';
import { useCajonMovil } from '@shapi/ui/cajon';
import { BarraLateralPortal } from './BarraLateralPortal';
import { MarcaPortal } from './MarcaPortal';
import { useSesionConsumidor } from '../modulos/sesion/useSesionConsumidor';

/**
 * Estructura del portal con la barra lateral: barra superior de 76 px y barra de 272 px. Debajo de 1024 px la barra
 * superior mide 64 px y la barra lateral se abre como cajón con el botón de menú (JG-32).
 */
export function EstructuraPortal({ claseContenido = '', children }: { claseContenido?: string; children: ReactNode }) {
  const { pathname } = useLocation();
  const cajon = useCajonMovil(pathname);
  return <div className="h-screen max-lg:h-dvh bg-fondo flex flex-col overflow-hidden">
    <header className="h-16 lg:h-[76px] shrink-0 border-b border-borde bg-panel flex items-center justify-between gap-4 px-4 lg:px-7">
      <MarcaPortal />
      <BotonMenu abierto={cajon.abierto} alternar={cajon.alternar} idCajon="barra-lateral-portal" />
    </header>
    <div className="flex-1 flex min-h-0">
      <FondoCajon abierto={cajon.abierto} cerrar={cajon.cerrar} />
      <BarraLateralPortal abierto={cajon.abierto} />
      <main className={`flex-1 min-w-0 overflow-auto ${claseContenido}`}>{children}</main>
    </div>
  </div>;
}

export function LayoutPortal() {
  const sesion = useSesionConsumidor();
  return <EstructuraPortal>
    {sesion.isError ? <EstadoError reintentar={() => void sesion.refetch()} /> : <Outlet />}
  </EstructuraPortal>;
}
