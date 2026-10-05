import type { ReactNode } from 'react';
import { Aviso } from '@shapi/ui';
import { useMarcaPortal } from '../configuracion/useConfiguracionPortal';

/** Los colores de la marca los define la raíz del portal (`coloresMarca`, en App), para todas las pantallas. */
export function MarcoAcceso({ children, ancho = 520 }: { children: ReactNode; ancho?: number }) {
  return (
    <div className="flex min-h-[calc(100vh-76px)] items-center justify-center bg-fondo px-6 py-11">
      <section className="w-full rounded-base border border-borde bg-panel p-10" style={{ maxWidth: ancho }}>
        {children}
      </section>
    </div>
  );
}

export function EncabezadoAcceso({ rotulo, titulo, children }: { rotulo: ReactNode; titulo: string; children?: ReactNode }) {
  return (
    <div className="flex flex-col gap-2">
      <p className="text-etiqueta font-medium uppercase tracking-[.16em] text-tinta-suave">{rotulo}</p>
      <h1 className="font-display text-titulo leading-[1.2] tracking-[-.02em] text-tinta">{titulo}</h1>
      {children}
    </div>
  );
}

export function RotuloPortal({ invitacion = false }: { invitacion?: boolean }) {
  const marca = useMarcaPortal();
  return <>{invitacion ? 'Invitación de' : 'Portal de'} {marca.nombrePortal}</>;
}

export function AvisoError({ mensaje, reintentar }: { mensaje: string; reintentar?: () => void }) {
  return (
    <Aviso estado="error">
      <span>{mensaje}</span>
      {reintentar && <button type="button" onClick={reintentar} className="font-medium underline">Reintentar</button>}
    </Aviso>
  );
}
