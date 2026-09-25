export interface RespuestaSesion {
  usuario: { nombre: string; correo: string };
  organizacion: { id: string; nombre: string };
  rol: 'propietario' | 'editor' | 'lector' | 'administrador' | 'soporte';
  correoVerificado: boolean;
  destino: string;
}

export interface ContratoSesion {
  '/api/auth/sesion': {
    parameters: Record<string, never>;
    get: {
      responses: {
        200: { content: { 'application/json': RespuestaSesion } };
        401: { content?: never };
      };
    };
  };
  '/api/auth/salir': {
    parameters: Record<string, never>;
    post: {
      parameters: { header: { 'X-Requested-With': 'shapi' } };
      responses: {
        200: { content?: never };
        401: { content?: never };
        403: { content?: never };
      };
    };
  };
}
