import type { CSSProperties } from 'react';

/**
 * 11 §1: en el portal, el color principal es el de la marca. Los componentes de @shapi/ui (Boton, Campo y el anillo de
 * foco) leen `--principal`, `--principal-hover` y `--anillo-foco`, así que la raíz del portal los redefine con
 * `--marca-principal` y no queda ningún azul de Shapi.
 */
export function coloresMarca(colorPrincipal: string) {
  return {
    '--marca-principal': colorPrincipal,
    '--principal': 'var(--marca-principal)',
    '--principal-hover': 'color-mix(in srgb, var(--marca-principal) 85%, #000000)',
    '--anillo-foco': 'color-mix(in srgb, var(--marca-principal) 17%, #FFFFFF)',
  } as CSSProperties;
}
