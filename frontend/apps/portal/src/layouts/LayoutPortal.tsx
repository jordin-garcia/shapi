import { Outlet } from 'react-router';
import { EstadoError } from '@shapi/ui';
import { BarraLateralPortal } from './BarraLateralPortal';
import { MarcaPortal } from './MarcaPortal';
import { useSesionConsumidor } from '../modulos/sesion/useSesionConsumidor';

export function LayoutPortal() {
  const sesion = useSesionConsumidor();
  return <div className="h-screen bg-fondo flex flex-col overflow-hidden">
    <header className="h-[76px] shrink-0 border-b border-borde bg-panel flex items-center px-7"><MarcaPortal /></header>
    <div className="flex-1 flex min-h-0">
      <BarraLateralPortal />
      <main className="flex-1 min-w-0 overflow-auto">
        {sesion.isError ? <EstadoError reintentar={() => void sesion.refetch()} /> : <Outlet />}
      </main>
    </div>
  </div>;
}
