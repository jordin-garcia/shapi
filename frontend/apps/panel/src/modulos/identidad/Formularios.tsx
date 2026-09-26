import { useId, type ComponentPropsWithoutRef, type ReactNode } from 'react';
import { Campo } from '@shapi/ui';

/** Tarjeta centrada de las pantallas de acceso (A1): 520 px o 460 px de ancho, 40 px de relleno. */
export function MarcoAcceso({ ancho = 520, children }: { ancho?: 460 | 520; children: ReactNode }) {
  return (
    <div className="flex-grow flex items-center justify-center py-11 px-20">
      <div style={{ width: ancho }} className="max-w-full bg-white border border-borde rounded-base p-10">
        {children}
      </div>
    </div>
  );
}

/** Rótulo, título y bajada con que empieza cada tarjeta de A1. */
export function Encabezado({ rotulo, titulo, children }: { rotulo: string; titulo: string; children?: ReactNode }) {
  return (
    <div className="flex flex-col gap-2">
      <p className="text-xs tracking-[.16em] uppercase text-tinta-suave font-medium m-0">{rotulo}</p>
      <h1 className="font-display text-[32px] leading-[1.2] m-0 tracking-[-0.02em]">{titulo}</h1>
      {children}
    </div>
  );
}

/** Campo con su etiqueta asociada; el error se muestra debajo, en `--alerta` (11 §4). */
export function CampoEtiquetado({ etiqueta, error, ...props }: ComponentPropsWithoutRef<'input'> & { etiqueta: string; error?: string }) {
  const id = useId();
  return (
    <div className="flex flex-col gap-[7px]">
      <label htmlFor={id} className="text-[13px] font-semibold text-tinta">{etiqueta}</label>
      <Campo id={id} error={error} aria-invalid={error ? true : undefined} {...props} />
    </div>
  );
}

/** Aviso de error de las pantallas de acceso, con los colores de alerta y, si se puede, el botón "Reintentar" (11 §4). */
export function AvisoError({ mensaje, reintentar }: { mensaje: string; reintentar?: () => void }) {
  return (
    <div role="alert" className="bg-alerta-fondo border border-alerta-borde text-alerta rounded-base py-4 px-[18px] text-sm leading-[1.5] flex items-start justify-between gap-3">
      <p className="m-0">{mensaje}</p>
      {reintentar && (
        <button type="button" onClick={reintentar} className="shrink-0 underline font-medium bg-transparent border-0 p-0 cursor-pointer text-alerta">
          Reintentar
        </button>
      )}
    </div>
  );
}
