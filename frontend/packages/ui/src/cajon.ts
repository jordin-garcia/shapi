import { useEffect, useState } from 'react';

/**
 * Menú de las barras laterales en pantallas angostas (JG-32, ADR-37). Debajo de 1024 px la barra lateral se oculta y se
 * abre como un cajón con `BotonMenu`; se cierra con Escape, con un toque en el fondo y en cada navegación, aunque sea a la misma página (`clave` es la
 * clave de la ubicación de React Router).
 * Desde 1024 px no hace nada: la barra queda fija como en los mockups.
 */
export function useCajonMovil(clave: string) {
  const [abierto, setAbierto] = useState(false);
  const [claveAnterior, setClaveAnterior] = useState(clave);
  if (clave !== claveAnterior) {
    setClaveAnterior(clave);
    setAbierto(false);
  }
  useEffect(() => {
    if (!abierto) return;
    const teclado = (evento: KeyboardEvent) => { if (evento.key === 'Escape') setAbierto(false); };
    document.addEventListener('keydown', teclado);
    return () => document.removeEventListener('keydown', teclado);
  }, [abierto]);
  return { abierto, alternar: () => setAbierto(actual => !actual), cerrar: () => setAbierto(false) };
}

/** Clases de una barra lateral: cajón fijo bajo la barra superior en pantallas angostas y columna fija desde `lg`. */
export function clasesCajon(abierto: boolean) {
  return `${abierto ? 'flex' : 'max-lg:hidden'} max-lg:fixed max-lg:top-[64px] max-lg:bottom-0 max-lg:left-0 max-lg:z-40 max-lg:max-w-[85vw] max-lg:overflow-y-auto max-lg:shadow-xl`;
}
