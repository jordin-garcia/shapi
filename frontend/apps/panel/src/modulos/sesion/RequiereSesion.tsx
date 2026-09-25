import type { ReactNode } from 'react';
import { Navigate } from 'react-router';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { useSesion } from './useSesion';

export function RequiereSesion({ children }: { children: ReactNode }) {
  const { data, isPending, error, refetch } = useSesion();
  if (isPending) return <EstadoCargando />;
  if (error) return <EstadoError reintentar={() => void refetch()} />;
  if (!data) return <Navigate to="/entrar" replace />;
  return <>{children}</>;
}
