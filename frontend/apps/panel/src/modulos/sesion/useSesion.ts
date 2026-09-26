import { useQuery } from '@tanstack/react-query';
import { crearCliente, ProblemDetailsError } from '@shapi/api';
import type { ContratoSesion } from './contratoSesion';

export const clienteSesion = crearCliente<ContratoSesion>(window.location.origin);

export interface SesionActual {
  nombre: string;
  correo: string;
  rol: 'propietario' | 'editor' | 'lector' | 'administrador' | 'soporte';
  nombreOrganizacion: string;
  organizacionId: string;
  correoVerificado: boolean;
  destino: string;
}

export const claveSesion = ['sesion'] as const;

/** Consulta la sesión actual; sin sesión (401) devuelve null. */
export async function consultarSesion({ signal }: { signal?: AbortSignal } = {}): Promise<SesionActual | null> {
  try {
    const { data, response } = await clienteSesion.GET('/api/auth/sesion', { signal });
    if (response.status === 401) return null;
    if (!response.ok || !data) throw new Error('No se pudo consultar la sesión');
    return {
      nombre: data.usuario.nombre,
      correo: data.usuario.correo,
      rol: data.rol,
      nombreOrganizacion: data.organizacion.nombre,
      organizacionId: data.organizacion.id,
      correoVerificado: data.correoVerificado,
      destino: data.destino,
    };
  } catch (error) {
    if (error instanceof ProblemDetailsError && error.status === 401) return null;
    throw error;
  }
}

export function useSesion() {
  return useQuery({
    queryKey: claveSesion,
    queryFn: consultarSesion,
    retry: false,
  });
}
