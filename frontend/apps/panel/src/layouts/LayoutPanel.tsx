import { Outlet, NavLink, useNavigate } from 'react-router';
import { useSesion } from '../modulos/sesion/useSesion';
import { SelectorApi } from '../modulos/apis/SelectorApi';

export function LayoutPanel() {
  const { data } = useSesion();
  const navigate = useNavigate();

  const cerrarSesion = () => {
    // Provisorio
    navigate('/entrar');
  };

  const navItem = ({ isActive }: { isActive: boolean }) => 
    `block text-[14px] leading-relaxed px-3 py-[7px] rounded-lg transition-colors ${isActive ? 'bg-[#0E1830] text-[var(--principal)] font-medium' : 'text-[#B9C4D8] hover:text-[#E8EDF7]'}`;

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
        <span className="text-[14px] text-[#8B98B0]">Panel del Proveedor</span>
      </header>

      <div className="flex-1 flex min-h-0">
        <nav className="w-[272px] shrink-0 bg-[#060910] border-r border-[#131B2B] p-6 pb-5 flex flex-col justify-between">
          <div className="flex flex-col gap-5">
            <div className="flex flex-col gap-2">
              <span className="text-[11px] tracking-[.14em] uppercase text-[#7F8DA8] font-semibold px-3 leading-snug">Publicación</span>
              <NavLink to="/panel/apis" end className={navItem}>APIs</NavLink>
            </div>

            <div className="flex flex-col gap-2">
              <span className="text-[11px] tracking-[.14em] uppercase text-[#7F8DA8] font-semibold px-3 leading-snug">API</span>
              <SelectorApi />
              <div className="flex flex-col gap-[1px]">
                <NavLink to={`/panel/apis/current/especificacion`} className={navItem}>Especificación</NavLink>
                <NavLink to={`/panel/apis/current/rutas`} className={navItem}>Rutas expuestas</NavLink>
                <NavLink to={`/panel/apis/current/configuracion-rutas`} className={navItem}>Configuración por ruta</NavLink>
                <NavLink to={`/panel/apis/current/dominios`} className={navItem}>Dominios</NavLink>
                <NavLink to={`/panel/apis/current/portal`} className={navItem}>Portal</NavLink>
                <NavLink to={`/panel/apis/current/planes`} className={navItem}>Planes</NavLink>
                <NavLink to={`/panel/apis/current/claves`} className={navItem}>Claves</NavLink>
                <NavLink to={`/panel/apis/current/consumo`} className={navItem}>Consumo</NavLink>
                <NavLink to={`/panel/apis/current/consumidores`} className={navItem}>Consumidores</NavLink>
              </div>
            </div>

            <div className="flex flex-col gap-2">
              <span className="text-[11px] tracking-[.14em] uppercase text-[#7F8DA8] font-semibold px-3 leading-snug">Organización</span>
              <div className="flex flex-col gap-[1px]">
                <NavLink to="/panel/miembros" className={navItem}>Miembros y roles</NavLink>
                <NavLink to="/panel/suscripcion" className={navItem}>Suscripción de plataforma</NavLink>
                <NavLink to="/panel/pagos" className={navItem}>Historial de pagos</NavLink>
                <NavLink to="/panel/soporte" className={navItem}>Casos de soporte</NavLink>
              </div>
            </div>
          </div>

          <div className="flex items-center justify-between gap-3 border-t border-[#131B2B] pt-4 px-1 pl-3">
            <div className="flex flex-col gap-[1px] min-w-0">
              <NavLink to="/panel/perfil" className="text-[14px] font-medium leading-relaxed text-[#E8EDF7] truncate hover:underline">
                {data?.nombre || 'Usuario'}
              </NavLink>
              <span className="text-[13px] leading-relaxed text-[#8B98B0]">{data?.rol || 'Rol'}</span>
            </div>
            <button onClick={cerrarSesion} className="w-9 h-9 shrink-0 flex items-center justify-center border border-[#2A3550] rounded-lg text-[#B9C4D8] hover:text-[#E8EDF7] hover:border-[#7FA6FF]" title="Cerrar sesión">
              <svg width="18" height="18" viewBox="0 0 20 20" fill="none">
                <path d="M8 3.5 H5 A1.5 1.5 0 0 0 3.5 5 V15 A1.5 1.5 0 0 0 5 16.5 H8" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"></path>
                <path d="M8.5 10 H16.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"></path>
                <path d="M13.5 7 L16.5 10 L13.5 13" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"></path>
              </svg>
            </button>
          </div>
        </nav>

        <main className="flex-1 min-w-0 p-[44px] overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
