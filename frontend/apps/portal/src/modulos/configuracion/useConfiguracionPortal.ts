import { createContext, createElement, useContext, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/portal';

export type ConfiguracionPortal = components['schemas']['ConfiguracionPortal'];

const clientePortal = crearCliente<paths>(window.location.origin);
const ContextoConfiguracionPortal = createContext<ConfiguracionPortal | null>(null);

export const claveConfiguracionPortal = ['portal', 'configuracion'] as const;

export class ApiNoDisponible extends Error {}

export function useConfiguracionPortal() {
  return useQuery({
    queryKey: claveConfiguracionPortal,
    queryFn: async ({ signal }) => {
      const { data, response } = await clientePortal.GET('/api/portal/configuracion', { signal });
      if (response.status === 404) throw new ApiNoDisponible();
      if (!response.ok || !data) throw new Error('No se pudo consultar la configuración del portal');
      return data;
    },
    retry: false,
    staleTime: 5 * 60 * 1000,
  });
}

export function ProveedorConfiguracionPortal({ configuracion, children }: {
  configuracion: ConfiguracionPortal;
  children: ReactNode;
}) {
  return createElement(ContextoConfiguracionPortal.Provider, { value: configuracion }, children);
}

export function useMarcaPortal() {
  const configuracion = useContext(ContextoConfiguracionPortal);
  if (!configuracion) throw new Error('La configuración del portal no está disponible');
  return configuracion;
}
