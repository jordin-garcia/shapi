import type { ReactNode } from 'react';
import { NavLink, Outlet } from 'react-router';
import { MarcaPortal } from './MarcaPortal';
import { useSesionConsumidor } from '../modulos/sesion/useSesionConsumidor';
import { CerrarSesionConsumidor } from '../modulos/sesion/CerrarSesionConsumidor';

function Grupo({ titulo, children }: { titulo: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-2">
      <span className="px-3 text-[11px] font-semibold leading-[1.2] tracking-[.14em] uppercase text-tinta-suave">{titulo}</span>
      <div className="flex flex-col gap-px">{children}</div>
    </div>
  );
}

function Enlace({ a, children }: { a: string; children: ReactNode }) {
  return (
    <NavLink to={a} className={({ isActive }) =>
      `block rounded-base px-3 py-[7px] text-[14px] leading-[1.5] ${isActive ? 'bg-fondo text-[var(--marca-principal)] font-medium' : 'text-tinta hover:text-[var(--marca-principal)]'}`}>
      {children}
    </NavLink>
  );
}

export function LayoutCuenta() {
  const sesion = useSesionConsumidor();
  return (
    <div className="h-screen bg-fondo flex flex-col overflow-hidden">
      <header className="h-[76px] shrink-0 border-b border-borde bg-panel flex items-center px-7">
        <MarcaPortal />
      </header>
      <div className="flex-1 flex min-h-0">
        <nav className="w-[272px] shrink-0 border-r border-borde bg-panel px-4 pt-6 pb-5 flex flex-col justify-between">
          <div className="flex flex-col gap-5">
            <Grupo titulo="API">
              <Enlace a="/documentacion/inicio">Documentación</Enlace>
              <Enlace a="/consola">Consola de pruebas</Enlace>
              <Enlace a="/planes">Planes</Enlace>
            </Grupo>
            <Grupo titulo="Mi cuenta">
              <Enlace a="/cuenta/suscripcion">Suscripción y claves</Enlace>
              <Enlace a="/cuenta/consumo">Consumo</Enlace>
              <Enlace a="/cuenta/pagos">Pagos</Enlace>
            </Grupo>
          </div>
          <div className="border-t border-borde pt-4 pr-1 pl-3 flex items-center justify-between gap-3">
            <div className="min-w-0 flex flex-col gap-px">
              <span className="truncate text-[14px] font-medium leading-[1.5] text-tinta">{sesion.data?.consumidor.nombre}</span>
              <span className="truncate text-[13px] leading-[1.5] text-tinta-suave">{sesion.data?.consumidor.nombreEmpresa}</span>
            </div>
            <CerrarSesionConsumidor />
          </div>
        </nav>
        <main className="flex-1 min-w-0 overflow-auto p-11"><Outlet /></main>
      </div>
    </div>
  );
}
