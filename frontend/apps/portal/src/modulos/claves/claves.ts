import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components as componentesClaves, paths as rutasClaves } from '@shapi/api/claves';
import type { components as componentesSuscripciones, paths as rutasSuscripciones } from '@shapi/api/suscripciones';

const clienteClaves = crearCliente<rutasClaves>(window.location.origin);
const clienteSuscripciones = crearCliente<rutasSuscripciones>(window.location.origin);

export type Clave = componentesClaves['schemas']['Clave'];
export type ClaveEmitida = componentesClaves['schemas']['ClaveEmitida'];
export type ClaveRotada = componentesClaves['schemas']['ClaveRotada'];
export type TipoClave = componentesClaves['schemas']['TipoClave'];
export type SuscripcionActual = componentesSuscripciones['schemas']['SuscripcionActual'];

export const claveSuscripcionConsumidor = ['portal', 'suscripcion'] as const;
export const claveClavesConsumidor = ['portal', 'claves'] as const;

export class ApiProveedorNoDisponible extends Error {}

export function useSuscripcionConsumidor() {
  return useQuery({
    queryKey: claveSuscripcionConsumidor,
    queryFn: async ({ signal }): Promise<SuscripcionActual | null> => {
      try {
        const { data, response } = await clienteSuscripciones.GET('/api/portal/suscripcion', { signal });
        if (response.status === 404) return null;
        if (!response.ok || !data) throw new Error('No se pudo cargar la suscripción.');
        return data;
      } catch (error) {
        if (error instanceof ErrorApi && error.estado === 404) return null;
        if (error instanceof ErrorApi && error.codigo === 'api_no_disponible') throw new ApiProveedorNoDisponible();
        throw error;
      }
    },
    retry: false,
  });
}

export function useClavesConsumidor(habilitada: boolean) {
  return useQuery({
    queryKey: claveClavesConsumidor,
    queryFn: async ({ signal }) => {
      const { data, response } = await clienteClaves.GET('/api/portal/claves', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron cargar las claves.');
      return data.elementos;
    },
    enabled: habilitada,
    retry: false,
  });
}

export function useRotarClave() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: async (claveId: string) => {
      const { data, response } = await clienteClaves.POST('/api/portal/claves/{claveId}/rotar', {
        params: { path: { claveId }, header: { 'X-Requested-With': 'shapi' } },
      });
      if (!response.ok || !data) throw new Error('No se pudo rotar la clave.');
      return data;
    },
    onSuccess: async () => cache.invalidateQueries({ queryKey: claveClavesConsumidor }),
  });
}

export function useRevocarClave() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: async (claveId: string) => {
      const { data, response } = await clienteClaves.POST('/api/portal/claves/{claveId}/revocar', {
        params: { path: { claveId }, header: { 'X-Requested-With': 'shapi' } },
      });
      if (!response.ok || !data) throw new Error('No se pudo revocar la clave.');
      return data;
    },
    onSuccess: async () => cache.invalidateQueries({ queryKey: claveClavesConsumidor }),
  });
}

export function useEmitirClave() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: async (tipo: TipoClave) => {
      const { data, response } = await clienteClaves.POST('/api/portal/claves/emitir', {
        params: { header: { 'X-Requested-With': 'shapi' } }, body: { tipo },
      });
      if (!response.ok || !data) throw new Error('No se pudo emitir la clave.');
      return data;
    },
    onSuccess: async () => cache.invalidateQueries({ queryKey: claveClavesConsumidor }),
  });
}

export function mensajeError(error: unknown, alterno: string) {
  return error instanceof ErrorApi ? error.titulo : alterno;
}
