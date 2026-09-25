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

export function useSesion() {
  return useQuery({
    queryKey: ['sesion'],
    queryFn: async ({ signal }) => {
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
        } satisfies SesionActual;
      } catch (error) {
        if (error instanceof ProblemDetailsError && error.status === 401) return null;
        throw error;
      }
    },
    retry: false,
  });
}
