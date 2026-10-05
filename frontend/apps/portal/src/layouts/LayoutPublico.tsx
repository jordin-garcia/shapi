import { Link, Outlet, useLocation } from 'react-router';
import { MarcaPortal } from './MarcaPortal';
import { iniciales } from './iniciales';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';

export function LayoutPublico() {
  const marca = useMarcaPortal();
  const { pathname } = useLocation();
  const esAcceso = ['/registro', '/invitacion', '/entrar', '/verificar-correo', '/recuperar', '/restablecer'].includes(pathname);
  return (
    <div className="min-h-screen bg-panel flex flex-col">
      <header className="h-[76px] shrink-0 border-b border-borde bg-panel flex items-center justify-between px-20">
        <MarcaPortal />
        {!esAcceso && <nav aria-label="Navegación principal" className="flex items-center gap-8 text-[15px] text-tinta-suave">
          <Link to="/documentacion">Documentación</Link>
          <Link to="/planes">Planes</Link>
          <Link to="/entrar" className="font-medium text-tinta">Entrar</Link>
          <Link to="/registro" className="rounded-base bg-[var(--marca-principal)] px-6 py-[13px] font-medium leading-[1.3] text-white">Crear cuenta</Link>
        </nav>}
      </header>
      <main className="flex-1"><Outlet /></main>
      {!esAcceso && <footer className="border-t border-borde px-20 py-8 flex items-center justify-between text-[14px] text-tinta-suave">
        {/* Mockup A5 (Main.dc.html): insignia de 24 px, con radio de 6 px, con las iniciales y el nombre de la organización. */}
        <div className="flex items-center gap-[11px]">
          <span aria-hidden="true" className="size-6 rounded-[6px] bg-[var(--marca-principal)] text-white flex items-center justify-center font-display text-[10px] font-semibold">
            {iniciales(marca.nombreOrganizacion)}
          </span>
          <span>{marca.nombreOrganizacion}</span>
        </div>
        <div className="flex gap-8">
          <Link to="/documentacion">Documentación</Link>
          <Link to="/planes">Planes</Link>
          <Link to="/entrar">Entrar</Link>
        </div>
      </footer>}
    </div>
  );
}
