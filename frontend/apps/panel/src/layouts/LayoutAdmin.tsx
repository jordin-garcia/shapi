import { CerrarSesion } from '../modulos/sesion/CerrarSesion';
import { Outlet, NavLink } from 'react-router';
import { useSesion } from '../modulos/sesion/useSesion';

export function LayoutAdmin() {
  const { data } = useSesion();
  const isAdmin = data?.rol === 'administrador';

  const navItem = ({ isActive }: { isActive: boolean }) => 
    `block text-[14px] leading-relaxed px-3 py-[7px] rounded-lg transition-colors ${isActive ? 'bg-[#0E1830] text-[#7FA6FF] font-medium' : 'text-[#B9C4D8] hover:text-[#E8EDF7]'}`;

  return (
    <div className="min-h-screen bg-[var(--fondo)] flex flex-col overflow-hidden">
      <header className="h-[76px] bg-[#060910] border-b border-[#131B2B] flex items-center justify-between px-7 shrink-0">
        <div className="flex items-center gap-[11px]">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
            <rect x="1" y="1" width="22" height="22" rx="6" stroke="#7FA6FF" strokeWidth="1.5"></rect>
            <path d="M6 8.5 H14" stroke="#7FA6FF" strokeWidth="1.5" strokeLinecap="round"></path>
            <path d="M6 15.5 H12" stroke="#7FA6FF" strokeWidth="1.5" strokeLinecap="round"></path>
            <path d="M6 12 H22" stroke="#E8EDF7" strokeWidth="2.5" strokeLinecap="round"></path>
          </svg>
          <span className="font-display text-[20px] font-medium tracking-[-0.03em] text-[#E8EDF7]">Shapi</span>
        </div>
        <span className="text-[14px] text-[#8B98B0]">Plataforma Shapi</span>
      </header>

      <div className="flex-1 flex min-h-0">
        <nav className="w-[272px] shrink-0 bg-[#060910] border-r border-[#131B2B] p-6 pb-5 flex flex-col justify-between">
          <div className="flex flex-col gap-5">
            {isAdmin && (
              <div className="flex flex-col gap-2">
                <span className="text-[11px] tracking-[.14em] uppercase text-[#7F8DA8] font-semibold px-3 leading-snug">Plataforma</span>
                <div className="flex flex-col gap-[1px]">
                  <NavLink to="/admin/planes" className={navItem}>Planes de plataforma</NavLink>
                  <NavLink to="/admin/organizaciones" className={navItem}>Organizaciones</NavLink>
                  <NavLink to="/admin/pagos" className={navItem}>Pagos</NavLink>
                </div>
              </div>
            )}

            <div className="flex flex-col gap-2">
              <span className="text-[11px] tracking-[.14em] uppercase text-[#7F8DA8] font-semibold px-3 leading-snug">Soporte</span>
              <div className="flex flex-col gap-[1px]">
                <NavLink to="/admin/casos" className={navItem}>Casos</NavLink>
              </div>
            </div>

            <div className="flex flex-col gap-2">
              <span className="text-[11px] tracking-[.14em] uppercase text-[#7F8DA8] font-semibold px-3 leading-snug">Sistema</span>
              <div className="flex flex-col gap-[1px]">
                <NavLink to="/admin/estado" className={navItem}>Estado de los componentes</NavLink>
                <NavLink to="/admin/bitacora" className={navItem}>Bitácora de acciones</NavLink>
                {isAdmin && <NavLink to="/admin/cuentas" className={navItem}>Cuentas de plataforma</NavLink>}
              </div>
            </div>
          </div>

          <div className="flex items-center justify-between gap-3 border-t border-[#131B2B] pt-4 px-1 pl-3">
            <div className="flex flex-col gap-[1px] min-w-0">
              <NavLink to="/admin/perfil" className="text-[14px] font-medium leading-relaxed text-[#E8EDF7] truncate hover:underline">
                {data?.nombre || 'Usuario'}
              </NavLink>
              <span className="text-[13px] leading-relaxed text-[#8B98B0]">{data?.rol ? data.rol[0].toUpperCase() + data.rol.slice(1) : 'Rol'}</span>
            </div>
            <CerrarSesion />
          </div>
        </nav>

        <main className="flex-1 min-w-0 p-[44px] overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
