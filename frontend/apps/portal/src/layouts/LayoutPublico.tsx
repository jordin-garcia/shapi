import { Link, Outlet } from 'react-router';
import { MarcaPortal } from './MarcaPortal';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';

export function LayoutPublico() {
  const marca = useMarcaPortal();
  return (
    <div className="min-h-screen bg-panel flex flex-col">
      <header className="h-[76px] shrink-0 border-b border-borde bg-panel flex items-center justify-between px-20">
        <MarcaPortal />
        <nav aria-label="Navegación principal" className="flex items-center gap-8 text-[15px] text-tinta-suave">
          <Link to="/documentacion/inicio">Documentación</Link>
          <Link to="/planes">Planes</Link>
          <Link to="/entrar" className="font-medium text-tinta">Entrar</Link>
          <Link to="/registro" className="rounded-base bg-[var(--marca-principal)] px-6 py-[13px] font-medium leading-[1.3] text-white">Crear cuenta</Link>
        </nav>
      </header>
      <main className="flex-1"><Outlet /></main>
      <footer className="border-t border-borde px-20 py-8 flex items-center justify-between text-[14px] text-tinta-suave">
        <span>{marca.nombrePortal}</span>
        <div className="flex gap-8">
          <Link to="/documentacion/inicio">Documentación</Link>
          <Link to="/planes">Planes</Link>
          <Link to="/entrar">Entrar</Link>
        </div>
      </footer>
    </div>
  );
}
