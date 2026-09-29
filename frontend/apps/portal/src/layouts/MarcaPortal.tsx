import { Link } from 'react-router';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';

function iniciales(nombre: string) {
  return nombre.split(/\s+/).filter(Boolean).slice(0, 2).map(parte => parte[0]?.toUpperCase()).join('');
}

export function MarcaPortal() {
  const marca = useMarcaPortal();
  return (
    <Link to="/" className="flex items-center gap-[11px] text-tinta hover:no-underline">
      {marca.urlLogo
        ? <img src={marca.urlLogo} alt={marca.nombrePortal} className="size-7 rounded-base object-contain" />
        : (
          <span className="size-7 rounded-base bg-[var(--marca-principal)] text-white flex items-center justify-center font-display text-[11px] font-semibold tracking-[-0.02em]">
            {iniciales(marca.nombrePortal)}
          </span>
        )}
      <span className="font-display text-[20px] font-medium tracking-[-0.03em]">{marca.nombrePortal}</span>
    </Link>
  );
}
