import type { ReactNode } from 'react';
import { Navigate } from 'react-router';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { useSesionConsumidor } from './useSesionConsumidor';

export function RequiereSesionConsumidor({ children }: { children: ReactNode }) {
  const sesion = useSesionConsumidor();
  if (sesion.isPending) return <EstadoCargando />;
  if (sesion.isError) return <EstadoError reintentar={() => void sesion.refetch()} />;
  if (!sesion.data) return <Navigate to="/entrar" replace />;
  return children;
}
