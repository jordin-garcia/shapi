export interface paths {
  "/api/auth/registro": {
    post: {
      requestBody: {
        content: {
          "application/json": {
            nombre: string;
            correo: string;
            organizacion: string;
            contrasena: string;
          };
        };
      };
      responses: {
        200: {
          content: never;
        };
        400: {
          content: never;
        };
        409: {
          content: never;
        };
      };
    };
  };
  "/api/auth/verificar-correo": {
    post: {
      requestBody: {
        content: {
          "application/json": {
            token: string;
          };
        };
      };
      responses: {
        200: {
          content: never;
        };
        422: {
          content: never;
        };
      };
    };
  };
  "/api/auth/reenviar-verificacion": {
    post: {
      requestBody: {
        content: {
          "application/json": {
            correo: string;
          };
        };
      };
      responses: {
        200: {
          content: never;
        };
      };
    };
  };
  "/api/auth/entrar": {
    post: {
      requestBody: {
        content: {
          "application/json": {
            correo: string;
            contrasena: string;
          };
        };
      };
      responses: {
        200: {
          content: never;
        };
        401: {
          content: never;
        };
      };
    };
  };
  "/api/auth/salir": {
    post: {
      responses: {
        200: {
          content: never;
        };
        401: {
          content: never;
        };
      };
    };
  };
  "/api/auth/sesion": {
    get: {
      responses: {
        200: {
          content: {
            "application/json": {
              usuario: {
                nombre?: string;
                correo?: string;
              };
              organizacion: {
                id?: string;
                nombre?: string;
              };
              rol?: string;
              correoVerificado?: boolean;
              destino?: string;
            };
          };
        };
        401: {
          content: never;
        };
      };
    };
  };
}
