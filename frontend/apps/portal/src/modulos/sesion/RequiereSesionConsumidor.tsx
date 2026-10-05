import { useEffect, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Navigate } from 'react-router';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { useSesionConsumidor } from './useSesionConsumidor';
import { limpiarCacheConsumidor } from './cacheConsumidor';

export function RequiereSesionConsumidor({ children }: { children: ReactNode }) {
  const sesion = useSesionConsumidor();
  const cache = useQueryClient();
  const sinSesion = sesion.isSuccess && !sesion.data;
  // H-110: si la sesión venció, los datos del consumidor anterior no deben quedar para quien entre después.
  useEffect(() => {
    if (sinSesion) void limpiarCacheConsumidor(cache);
  }, [sinSesion, cache]);

  if (sesion.isPending) return <EstadoCargando />;
  if (sesion.isError) return <EstadoError reintentar={() => void sesion.refetch()} />;
  if (!sesion.data) return <Navigate to="/entrar" replace />;
  return children;
}
