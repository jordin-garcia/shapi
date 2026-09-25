import { useMutation, useQueryClient } from '@tanstack/react-query';
import { crearCliente, ProblemDetailsError } from '@shapi/api';
import type { paths } from '@shapi/api/identidad';

export const clienteIdentidad = crearCliente<paths>(window.location.origin);

export function useRegistro() {
  return useMutation({
    mutationFn: async (datos: paths['/api/auth/registro']['post']['requestBody']['content']['application/json']) => {
      const { data, response, error } = await clienteIdentidad.POST('/api/auth/registro', {
        body: datos,
      });
      if (error) {
        if (error instanceof ProblemDetailsError) throw error;
        throw new Error('Error al registrar');
      }
      if (!response.ok) {
        throw new Error('Error desconocido');
      }
      return data;
    },
  });
}

export function useEntrar() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (datos: paths['/api/auth/entrar']['post']['requestBody']['content']['application/json']) => {
      const { data, response, error } = await clienteIdentidad.POST('/api/auth/entrar', {
        body: datos,
      });
      if (error) {
        throw error;
      }
      if (!response.ok) {
        throw new Error('Credenciales inválidas');
      }
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sesion'] });
    },
  });
}

export function useVerificarCorreo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (token: string) => {
      const { data, response, error } = await clienteIdentidad.POST('/api/auth/verificar-correo', {
        body: { token },
      });
      if (error) throw error;
      if (!response.ok) throw new Error('Token inválido');
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sesion'] });
    },
  });
}

export function useReenviarVerificacion() {
  return useMutation({
    mutationFn: async (correo: string) => {
      const { data, response, error } = await clienteIdentidad.POST('/api/auth/reenviar-verificacion', {
        body: { correo },
      });
      if (error) throw error;
      if (!response.ok) throw new Error('Error al reenviar');
      return data;
    },
  });
}
