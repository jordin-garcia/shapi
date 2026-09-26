import { useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router';
import { crearCliente, ProblemDetailsError } from '@shapi/api';
import type { components, paths } from '@shapi/api/identidad';
import { claveSesion, consultarSesion } from '../sesion/useSesion';

export const clienteIdentidad = crearCliente<paths>(window.location.origin);

// Todo método que no es GET lleva la cabecera CSRF (10 §1). El cliente la agrega, y el contrato la declara.
const csrf = { header: { 'X-Requested-With': 'shapi' } } as const;

export type DatosRegistro = components['schemas']['PeticionRegistro'];

/** Un error que se puede mostrar en el formulario: el `codigo` de ProblemDetails, su mensaje y los errores por campo. */
export interface ErrorFormulario {
  codigo: string | null;
  mensaje: string;
  errores: Record<string, string[]>;
}

export const MENSAJE_SIN_CONEXION = 'No se pudo completar la solicitud. Revise su conexión e intente de nuevo.';

export function interpretarError(error: unknown): ErrorFormulario {
  // Los errores de negocio traen un `codigo` del contrato. Un 5xx, aunque venga como ProblemDetails (el manejador de
  // excepciones de la API responde sin `codigo` y con un título en inglés), es un error del servidor.
  if (error instanceof ProblemDetailsError && (error.status ?? 0) < 500 && error.details.codigo !== 'error') {
    return { codigo: error.details.codigo, mensaje: error.details.titulo, errores: error.details.errores ?? {} };
  }
  // Red caída, error del servidor o respuesta sin ProblemDetails (por ejemplo, un 502 del borde).
  return { codigo: null, mensaje: MENSAJE_SIN_CONEXION, errores: {} };
}

function comprobar({ response }: { response: Response }) {
  // Los ProblemDetails ya se lanzan en el cliente; aquí solo quedan las respuestas sin ese formato.
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
}

export async function registrar(datos: DatosRegistro) {
  comprobar(await clienteIdentidad.POST('/api/auth/registro', { params: csrf, body: datos }));
}

export async function entrar(correo: string, contrasena: string) {
  comprobar(await clienteIdentidad.POST('/api/auth/entrar', { params: csrf, body: { correo, contrasena } }));
}

export async function verificarCorreo(token: string) {
  comprobar(await clienteIdentidad.POST('/api/auth/verificar-correo', { params: csrf, body: { token } }));
}

export async function reenviarVerificacion(correo: string) {
  comprobar(await clienteIdentidad.POST('/api/auth/reenviar-verificacion', { params: csrf, body: { correo } }));
}

/**
 * Después de entrar o de verificar el correo, consulta la sesión recién creada y lleva a su destino según el rol
 * (10 §1). La respuesta queda en la caché, así que el guardia de rutas no vuelve a pedirla.
 */
export function useIrAlDestino() {
  const cache = useQueryClient();
  const navegar = useNavigate();
  return useCallback(async () => {
    const sesion = await cache.fetchQuery({ queryKey: claveSesion, queryFn: consultarSesion, staleTime: 0 });
    // Sin sesión (por ejemplo, si el navegador no guardó la cookie) se avisa en lugar de volver a la misma página.
    if (!sesion) throw new Error('La sesión no quedó iniciada');
    await navegar(sesion.destino, { replace: true });
  }, [cache, navegar]);
}
