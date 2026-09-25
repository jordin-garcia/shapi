import React from 'react';
import { Navigate } from 'react-router';
import { useSesion } from './useSesion';

export function RequiereSesion({ children }: { children: React.ReactNode }) {
  const { data, isLoading, error } = useSesion();

  if (isLoading) {
    return <div className="p-8">Cargando sesión...</div>;
  }

  if (error || !data) {
    return <Navigate to="/entrar" replace />;
  }

  return <>{children}</>;
}
