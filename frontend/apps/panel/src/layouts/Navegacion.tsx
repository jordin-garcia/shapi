import type { ReactNode } from 'react';
import { NavLink, Outlet } from 'react-router';
import { CerrarSesion } from '../modulos/sesion/CerrarSesion';

// Piezas comunes de N.1 (proveedor), A6 (administrador) y B3 (soporte). Las barras son superficie oscura: llevan la
// clase `dark` y usan los tokens de 11 §1, incluidos los de las barras (`borde-barra`, `tinta-rotulo`…).

/** Logotipo de la barra superior oscura (N.1 y A1). */
export function MarcaShapi() {
  return (
    <div className="flex items-center gap-[11px]">
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <rect x="1" y="1" width="22" height="22" rx="6" stroke="var(--principal)" strokeWidth="1.5" />
        <path d="M6 8.5 H14" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
        <path d="M6 15.5 H12" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
        <path d="M6 12 H22" stroke="var(--tinta)" strokeWidth="2.5" strokeLinecap="round" />
      </svg>
      <span className="font-display text-[20px] font-medium tracking-[-0.03em] text-tinta">Shapi</span>
    </div>
  );
}

export function GrupoNavegacion({ titulo, children }: { titulo: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-2">
      <span className="px-3 text-[11px] leading-[1.2] tracking-[.14em] uppercase font-semibold text-tinta-rotulo">{titulo}</span>
      {children}
    </div>
  );
}

export function ListaEnlaces({ children }: { children: ReactNode }) {
  return <div className="flex flex-col gap-px">{children}</div>;
}

export function EnlaceNavegacion({ a, exacto = false, children }: { a: string; exacto?: boolean; children: ReactNode }) {
  return (
    <NavLink to={a} end={exacto} className={({ isActive }) =>
      `block px-3 py-[7px] rounded-base text-[14px] leading-[1.5] transition-colors ${isActive ? 'bg-fondo-activo text-principal font-medium' : 'text-tinta-navegacion hover:text-tinta'}`}>
      {children}
    </NavLink>
  );
}

const nombresDeRol: Record<string, string> = {
  propietario: 'Propietario', editor: 'Editor', lector: 'Lector', administrador: 'Administrador', soporte: 'Soporte',
};

/**
 * Estructura del panel y de la administración: barra superior de 76 px, barra lateral de 272 px con el pie fijo y el
 * contenido con 44 px de margen, que es lo único que se desplaza (11 §1).
 */
export function EstructuraConBarras({ textoSuperior, perfil, nombre, rol, children }: {
  textoSuperior: string;
  perfil: string;
  nombre?: string;
  rol?: string;
  children: ReactNode;
}) {
  return (
    <div className="h-screen bg-fondo flex flex-col overflow-hidden">
      <header className="dark h-[76px] shrink-0 bg-fondo border-b border-borde-barra flex items-center justify-between pl-7 pr-11">
        <MarcaShapi />
        <span className="text-[14px] text-tinta-suave">{textoSuperior}</span>
      </header>
      <div className="flex-1 flex min-h-0">
        <nav className="dark w-[272px] shrink-0 bg-fondo border-r border-borde-barra px-4 pt-6 pb-5 flex flex-col justify-between overflow-y-auto">
          <div className="flex flex-col gap-5">{children}</div>
          <div className="flex items-center justify-between gap-3 border-t border-borde-barra pt-4 pr-1 pl-3">
            <div className="flex flex-col gap-px min-w-0">
              <NavLink to={perfil} className="text-[14px] font-medium leading-[1.5] text-tinta truncate hover:underline">{nombre || 'Usuario'}</NavLink>
              <span className="text-[13px] leading-[1.5] text-tinta-suave">{rol ? nombresDeRol[rol] ?? rol : 'Rol'}</span>
            </div>
            <CerrarSesion />
          </div>
        </nav>
        <main className="flex-1 min-w-0 p-11 overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
