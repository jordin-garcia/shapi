import { Link } from 'react-router';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';
import { iniciales } from './iniciales';

export function MarcaPortal() {
  const marca = useMarcaPortal();
  return (
    <Link to="/" className="flex min-w-0 items-center gap-[11px] text-tinta hover:no-underline">
      {marca.urlLogo
        ? <img src={marca.urlLogo} alt={marca.nombrePortal} className="size-7 shrink-0 rounded-[6px] object-contain" />
        : (
          <span className="size-7 shrink-0 rounded-[6px] bg-[var(--marca-principal)] text-white flex items-center justify-center font-display text-[11px] font-semibold tracking-[-0.02em]">
            {iniciales(marca.nombrePortal)}
          </span>
        )}
      <span className="truncate font-display text-[18px] sm:text-[20px] font-medium tracking-[-0.03em]">{marca.nombrePortal}</span>
    </Link>
  );
}
