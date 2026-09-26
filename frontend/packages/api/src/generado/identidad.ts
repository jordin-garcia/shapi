export interface paths {
    "/api/auth/registro": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Registra un proveedor
         * @description Crea el usuario, la organización proveedora, la membresía de propietario y la suscripción activa al plan Prueba
         *     (ciclo de 09 §4), y encola el correo `verificacion_correo` con un enlace que vence a las 24 horas.
         *     Límite: 10 peticiones por minuto por IP.
         */
        post: operations["registrarProveedor"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth/verificar-correo": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Verifica el correo e inicia la sesión
         * @description Marca el correo como verificado y el enlace como usado, y envía la cookie de sesión. Límite de 10 por minuto por IP.
         */
        post: operations["verificarCorreo"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth/reenviar-verificacion": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Reenvía el correo de verificación
         * @description Genera un enlace nuevo si la cuenta existe y su correo no está verificado.
         *     Responde igual exista o no la cuenta, para no revelar qué correos están registrados. Límite de 10 por minuto por IP.
         */
        post: operations["reenviarVerificacion"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth/entrar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Inicia la sesión del personal
         * @description Tras 5 intentos fallidos seguidos, la cuenta se bloquea 15 minutos. El mensaje de credenciales incorrectas es
         *     genérico. Límite de 10 por minuto por IP.
         */
        post: operations["entrar"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth/salir": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Cierra la sesión
         * @description Revoca la sesión en el servidor y borra la cookie.
         */
        post: operations["salir"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth/sesion": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Devuelve la sesión actual
         * @description Usuario, organización, rol, si el correo está verificado y el destino según el rol (10 §1).
         */
        get: operations["obtenerSesion"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
}
export type webhooks = Record<string, never>;
export interface components {
    schemas: {
        PeticionRegistro: {
            nombre: string;
            /** Format: email */
            correo: string;
            organizacion: string;
            /** @description No puede ser igual al correo. */
            contrasena: string;
        };
        PeticionVerificacion: {
            /** @description El valor del enlace de verificación. */
            token: string;
        };
        PeticionReenviar: {
            /** Format: email */
            correo: string;
        };
        PeticionEntrar: {
            /** Format: email */
            correo: string;
            contrasena: string;
        };
        Sesion: {
            usuario: {
                nombre: string;
                /** Format: email */
                correo: string;
            };
            organizacion: {
                /** Format: uuid */
                id: string;
                nombre: string;
            };
            /** @enum {string} */
            rol: "propietario" | "editor" | "lector" | "administrador" | "soporte";
            correoVerificado: boolean;
            /**
             * @description `/admin/organizaciones` (administrador), `/admin/casos` (soporte) o `/panel/apis` (propietario, editor y lector).
             * @enum {string}
             */
            destino: "/panel/apis" | "/admin/organizaciones" | "/admin/casos";
        };
        /** @description ProblemDetails de la API de control (convenciones §5). */
        Problema: {
            type?: string;
            title: string;
            status: number;
            /** @enum {string} */
            codigo: "datos_invalidos" | "correo_ya_registrado" | "credenciales_invalidas" | "cuenta_bloqueada" | "cuenta_desactivada" | "token_invalido" | "csrf" | "demasiadas_peticiones";
            /** @description Solo en 400. Mensajes por campo, con el nombre del campo en camelCase. */
            errores?: {
                [key: string]: string[];
            };
        };
    };
    responses: {
        /** @description Datos inválidos (`datos_invalidos`), con los errores por campo. */
        DatosInvalidos: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                /**
                 * @example {
                 *       "type": "about:blank",
                 *       "title": "Revise los datos del formulario.",
                 *       "status": 400,
                 *       "codigo": "datos_invalidos",
                 *       "errores": {
                 *         "contrasena": [
                 *           "La contraseña debe tener entre 10 y 128 caracteres."
                 *         ]
                 *       }
                 *     }
                 */
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
        /** @description Falta la cabecera `X-Requested-With` o el `Origin` no es el del host (`csrf`). */
        Csrf: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
        /** @description No hay una sesión vigente. Es un ProblemDetails estándar, sin `codigo`. */
        SinSesion: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": {
                    title?: string;
                    status?: number;
                };
            };
        };
        /** @description Más de 10 peticiones por minuto desde la misma IP (`demasiadas_peticiones`). */
        DemasiadasPeticiones: {
            headers: {
                /** @description Segundos que hay que esperar. */
                "Retry-After"?: number;
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
    };
    parameters: {
        /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
        XRequestedWith: "shapi";
    };
    requestBodies: never;
    headers: {
        /** @description `shapi_sesion=<valor>; Path=/; Secure; HttpOnly; SameSite=Lax`. Vence a los 7 días o tras 8 horas de inactividad. */
        CookieSesion: string;
    };
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    registrarProveedor: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionRegistro"];
            };
        };
        responses: {
            /** @description Cuenta creada; el correo de verificación quedó en la cola. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            400: components["responses"]["DatosInvalidos"];
            403: components["responses"]["Csrf"];
            /** @description Ya existe una cuenta con ese correo (`correo_ya_registrado`). */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            429: components["responses"]["DemasiadasPeticiones"];
        };
    };
    verificarCorreo: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionVerificacion"];
            };
        };
        responses: {
            /** @description Correo verificado y sesión iniciada. */
            200: {
                headers: {
                    "Set-Cookie": components["headers"]["CookieSesion"];
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Falta la cabecera CSRF (`csrf`) o la cuenta está desactivada (`cuenta_desactivada`). */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description El enlace no existe, venció o ya se usó (`token_invalido`). */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            429: components["responses"]["DemasiadasPeticiones"];
        };
    };
    reenviarVerificacion: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionReenviar"];
            };
        };
        responses: {
            /** @description Solicitud recibida. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            403: components["responses"]["Csrf"];
            429: components["responses"]["DemasiadasPeticiones"];
        };
    };
    entrar: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionEntrar"];
            };
        };
        responses: {
            /** @description Sesión iniciada. El destino según el rol se consulta con `GET /api/auth/sesion`. */
            200: {
                headers: {
                    "Set-Cookie": components["headers"]["CookieSesion"];
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description El correo o la contraseña no son correctos (`credenciales_invalidas`). */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Falta la cabecera CSRF (`csrf`) o la cuenta está desactivada (`cuenta_desactivada`, solo con la contraseña correcta). */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La cuenta está bloqueada por intentos fallidos (`cuenta_bloqueada`). */
            423: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            429: components["responses"]["DemasiadasPeticiones"];
        };
    };
    salir: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Sesión revocada. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["Csrf"];
        };
    };
    obtenerSesion: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Sesión vigente. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["Sesion"];
                };
            };
            401: components["responses"]["SinSesion"];
        };
    };
}
