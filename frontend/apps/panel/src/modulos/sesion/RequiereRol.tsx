import React from 'react';
import { useSesion } from './useSesion';
import PaginaError403 from '../../paginas/Error-403';

export function RequiereRol({ roles, children }: { roles: string[]; children: React.ReactNode }) {
  const { data } = useSesion();

  if (!data) return null; // handled by RequiereSesion
  if (!roles.includes(data.rol || '')) {
    return <PaginaError403 />;
  }

  return <>{children}</>;
}
