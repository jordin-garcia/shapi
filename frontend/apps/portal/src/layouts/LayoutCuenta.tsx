import { Outlet } from 'react-router';
import { BarraLateralPortal } from './BarraLateralPortal';
import { MarcaPortal } from './MarcaPortal';

export function LayoutCuenta() {
  return (
    <div className="h-screen bg-fondo flex flex-col overflow-hidden">
      <header className="h-[76px] shrink-0 border-b border-borde bg-panel flex items-center px-7"><MarcaPortal /></header>
      <div className="flex-1 flex min-h-0">
        <BarraLateralPortal />
        <main className="flex-1 min-w-0 overflow-auto p-11"><Outlet /></main>
      </div>
    </div>
  );
}
