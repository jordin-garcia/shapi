import { Link, Outlet, useLocation } from 'react-router';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { LayoutPortal } from './LayoutPortal';
import { MarcaPortal } from './MarcaPortal';
import { iniciales } from './iniciales';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';
import { useSesionConsumidor } from '../modulos/sesion/useSesionConsumidor';

export function LayoutPublico() {
  const marca = useMarcaPortal();
  const { pathname } = useLocation();
  const esAcceso = ['/registro', '/invitacion', '/entrar', '/verificar-correo', '/recuperar', '/restablecer'].includes(pathname);
  // JG-30: con la sesión abierta, el inicio y «Página no encontrada» se ven por dentro como la documentación y la
  // cuenta, sin «Entrar», «Crear cuenta» ni el pie público. Sin sesión conservan el encabezado y el pie de su mockup
  // (A5 Main). Si la sesión responde 5xx, se muestra el aviso con «Reintentar» (11 §4), como en RequiereSesionConsumidor.
  const consultaSesion = !esAcceso;
  const sesion = useSesionConsumidor({ enabled: consultaSesion });
  if (consultaSesion && sesion.isPending) return <EstadoCargando />;
  if (consultaSesion && sesion.isError) return <EstadoError reintentar={() => void sesion.refetch()} />;
  if (consultaSesion && sesion.data) return <LayoutPortal />;
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
