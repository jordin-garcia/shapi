import React from 'react';
import { useSesion } from './useSesion';
import PaginaError403 from '../../paginas/Error-403';

/**
 * Muestra su contenido solo a los roles indicados. Con `area`, protege un área completa (panel o administración): el
 * 403 se muestra sin barra lateral y ofrece una salida.
 */
export function RequiereRol({ roles, area = false, children }: { roles: string[]; area?: boolean; children: React.ReactNode }) {
  const { data } = useSesion();

  if (!data) return null; // De la falta de sesión se encarga RequiereSesion.
  if (!roles.includes(data.rol)) {
    return <PaginaError403 conSalida={area} />;
  }

  return <>{children}</>;
}
