import { lazy } from 'react';

// A0.2 · La lámina solo existe en desarrollo (H-101): en el build de producción no se genera su archivo.
export const A0Lamina = import.meta.env.DEV ? lazy(() => import('./paginas/_UI')) : null;
