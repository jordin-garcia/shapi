import { useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/identidad';
import { claveSesionConsumidor, consultarSesionConsumidor } from './useSesionConsumidor';

export const clienteIdentidadConsumidor = crearCliente<paths>(window.location.origin);
export type RegistroConsumidor = components['schemas']['PeticionRegistroConsumidor'];
export type AceptarInvitacion = components['schemas']['PeticionAceptarInvitacion'];
const csrf = { header: { 'X-Requested-With': 'shapi' } } as const;

export interface ErrorFormulario {
  codigo: string | null;
  mensaje: string;
  errores: Record<string, string[]>;
}

export const MENSAJE_SIN_CONEXION = 'No se pudo completar la solicitud. Revise su conexión e intente de nuevo.';

export function interpretarError(error: unknown): ErrorFormulario {
  if (error instanceof ErrorApi && error.estado < 500 && error.codigo !== 'error') {
    return { codigo: error.codigo, mensaje: error.titulo, errores: error.errores ?? {} };
  }
  return { codigo: null, mensaje: MENSAJE_SIN_CONEXION, errores: {} };
}

function comprobar(respuesta: { response: Response }) {
  if (!respuesta.response.ok) throw new Error(`HTTP ${respuesta.response.status}`);
}

export async function registrarConsumidor(datos: RegistroConsumidor) {
  comprobar(await clienteIdentidadConsumidor.POST('/api/portal/auth/registro', { params: csrf, body: datos }));
}

export async function entrarConsumidor(correo: string, contrasena: string) {
  comprobar(await clienteIdentidadConsumidor.POST('/api/portal/auth/entrar', { params: csrf, body: { correo, contrasena } }));
}

export async function verificarCorreoConsumidor(token: string) {
  comprobar(await clienteIdentidadConsumidor.POST('/api/portal/auth/verificar-correo', { params: csrf, body: { token } }));
}

export async function reenviarVerificacionConsumidor(correo: string) {
  comprobar(await clienteIdentidadConsumidor.POST('/api/portal/auth/reenviar-verificacion', { params: csrf, body: { correo } }));
}

export async function recuperarConsumidor(correo: string) {
  comprobar(await clienteIdentidadConsumidor.POST('/api/portal/auth/recuperar', { params: csrf, body: { correo } }));
}

export async function restablecerConsumidor(token: string, contrasena: string) {
  comprobar(await clienteIdentidadConsumidor.POST('/api/portal/auth/restablecer', { params: csrf, body: { token, contrasena } }));
}

export async function consultarInvitacion(token: string) {
  const { data, response } = await clienteIdentidadConsumidor.GET('/api/portal/auth/invitacion/{token}', { params: { path: { token } } });
  if (!response.ok || !data) throw new Error(`HTTP ${response.status}`);
  return data;
}

export async function aceptarInvitacion(token: string, datos: AceptarInvitacion) {
  comprobar(await clienteIdentidadConsumidor.POST('/api/portal/auth/invitacion/{token}/aceptar', { params: { ...csrf, path: { token } }, body: datos }));
}

export function useIrAlDestinoConsumidor() {
  const cache = useQueryClient();
  const navegar = useNavigate();
  return useCallback(async () => {
    const sesion = await cache.fetchQuery({ queryKey: claveSesionConsumidor, queryFn: consultarSesionConsumidor, staleTime: 0 });
    if (!sesion) throw new Error('La sesión no quedó iniciada');
    await navegar(sesion.destino, { replace: true });
  }, [cache, navegar]);
}
