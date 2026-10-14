import type { ReactNode } from 'react';
import { Link, NavLink, useLocation } from 'react-router';
import { useDocumentacionPortal, rutaDocumentacion } from '../paginas/A5-1-Documentacion';
import { CerrarSesionConsumidor } from '../modulos/sesion/CerrarSesionConsumidor';
import { useSesionConsumidor } from '../modulos/sesion/useSesionConsumidor';

function Grupo({ titulo, children }: { titulo: string; children: ReactNode }) {
  return <div className="flex flex-col gap-2"><span className="px-3 text-[11px] font-semibold leading-[1.2] tracking-[.14em] uppercase text-tinta-suave">{titulo}</span><div className="flex flex-col gap-px">{children}</div></div>;
}

function Enlace({ a, activo, children }: { a: string; activo?: boolean; children: ReactNode }) {
  return <NavLink to={a} className={({ isActive }) => {
    const seleccionado = activo ?? isActive;
    return `block rounded-base px-3 py-[7px] text-[14px] leading-[1.5] ${seleccionado ? 'bg-[color-mix(in_srgb,var(--marca-principal)_8%,#FFFFFF)] text-[var(--marca-principal)] font-medium' : 'text-[#2B3547] hover:text-tinta'}`;
  }}>{children}</NavLink>;
}

function RutasDocumentadas() {
  const consulta = useDocumentacionPortal();
  const { pathname } = useLocation();
  if (!consulta.data) return null;
  const identificador = decodeURIComponent(pathname.slice('/documentacion/'.length));
  return consulta.data.rutas.map((ruta, indice) => {
    // /documentacion redirige enseguida a la primera ruta; mantenerla activa evita un parpadeo durante esa transición.
    const activa = (identificador === '' && indice === 0) || identificador === `${ruta.metodo} ${ruta.patron}`;
    return <Link
      key={`${ruta.metodo}-${ruta.patron}`}
      to={rutaDocumentacion(ruta.metodo, ruta.patron)}
      aria-current={activa ? 'page' : undefined}
      className={`flex items-center gap-3 rounded-base py-2 pl-6 pr-3 text-[14px] ${activa ? 'bg-[color-mix(in_srgb,var(--marca-principal)_8%,#FFFFFF)] text-[var(--marca-principal)] font-medium' : 'text-[#2B3547]'}`}
    >
      <span className="w-12 shrink-0 text-[11px] font-semibold tracking-[.06em] text-[var(--marca-principal)]">{ruta.metodo}</span>
      <span className="truncate">{ruta.patron}</span>
    </Link>;
  });
}

export function BarraLateralPortal() {
  const { pathname } = useLocation();
  const sesion = useSesionConsumidor();
  return (
    <nav aria-label="Barra lateral del portal" className="w-[272px] shrink-0 border-r border-borde bg-panel px-4 pt-6 pb-5 flex flex-col justify-between">
      <div className="flex flex-col gap-5">
        <Grupo titulo="API">
          <Enlace a="/documentacion" activo={pathname.startsWith('/documentacion')}>Documentación</Enlace>
          {pathname.startsWith('/documentacion') && <RutasDocumentadas />}
          <Enlace a="/consola">Consola de pruebas</Enlace>
          <Enlace a="/planes" activo={pathname === '/planes' || pathname.startsWith('/contratar/')}>Planes</Enlace>
        </Grupo>
        <Grupo titulo="Mi cuenta">
          <Enlace a="/cuenta/suscripcion">Suscripción y claves</Enlace>
          <Enlace a="/cuenta/consumo">Consumo</Enlace>
          <Enlace a="/cuenta/pagos">Pagos</Enlace>
        </Grupo>
      </div>
      {sesion.data && <div className="border-t border-borde pt-4 pr-1 pl-3 flex flex-wrap items-center justify-between gap-3">
        <div className="min-w-0 flex-1 basis-0 flex flex-col gap-px">
          <span className="truncate text-[14px] font-medium leading-[1.5] text-tinta">{sesion.data.consumidor.nombre}</span>
          <span className="truncate text-[13px] leading-[1.5] text-tinta-suave">{sesion.data.consumidor.nombreEmpresa}</span>
        </div>
        <CerrarSesionConsumidor />
      </div>}
    </nav>
  );
}
