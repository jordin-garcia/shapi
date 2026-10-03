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
         * @description Genera un enlace nuevo si la cuenta existe y su correo no está verificado, hasta 3 reenvíos por cuenta en una hora
         *     (el enlace del registro no cuenta). Responde igual exista o no la cuenta y aunque se haya pasado del límite, para
         *     no revelar qué correos están registrados. Límite de 10 por minuto por IP.
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
         * @description No exige una sesión vigente. Si la cookie corresponde a una sesión, la revoca en el servidor. Siempre borra la
         *     cookie, también cuando la sesión ya venció.
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
    "/api/auth/recuperar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Solicita un enlace de recuperación
         * @description Genera y encola un correo con el token de recuperación si la cuenta existe. Responde siempre 200 exista o no.
         *     Límite de 10 por minuto por IP.
         */
        post: operations["solicitarRecuperacion"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth/restablecer": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Restablece la contraseña
         * @description Cambia la contraseña utilizando el token de un solo uso. Si es exitoso, inicia sesión y revoca todas
         *     las sesiones previas.
         */
        post: operations["restablecerContrasena"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/auth/registro": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Registra un consumidor en el portal actual
         * @description Crea el consumidor dentro de la organización resuelta por el host. El correo es único por organización. Encola una verificación con la marca del portal y el host canónico `{sub}.{dominio_base}`.
         */
        post: operations["registrarConsumidor"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/auth/verificar-correo": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Verifica el correo del consumidor */
        post: {
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
                /** @description Correo verificado e inicio de sesión en el portal. */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                400: components["responses"]["CuerpoInvalido"];
                /** @description Falta CSRF o la cuenta está desactivada. */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["Problema"];
                    };
                };
                /** @description Portal no disponible. */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Token vencido */
                422: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                429: components["responses"]["DemasiadasPeticiones"];
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/auth/reenviar-verificacion": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Reenvía la verificación de correo
         * @description Responde siempre 200 y limita los reenvíos a 3 por consumidor en una hora.
         */
        post: {
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
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/auth/entrar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Inicia sesión del consumidor
         * @description Crea una cookie `portal_sesion` limitada al host actual. Tras 5 intentos fallidos bloquea la cuenta por 15 minutos.
         */
        post: {
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
                /** @description Sesión iniciada. */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                400: components["responses"]["CuerpoInvalido"];
                /** @description Credenciales incorrectas (`credenciales_invalidas`). */
                401: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["Problema"];
                    };
                };
                /** @description Falta CSRF o la cuenta está desactivada. */
                403: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["Problema"];
                    };
                };
                /** @description Portal no disponible. */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Cuenta bloqueada (`cuenta_bloqueada`). */
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
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/auth/salir": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Cierra sesión del consumidor */
        post: {
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
                /** @description Sesión revocada y cookie borrada. */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                403: components["responses"]["Csrf"];
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/auth/sesion": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Devuelve la cuenta y verificación del consumidor */
        get: {
            parameters: {
                query?: never;
                header?: never;
                path?: never;
                cookie?: never;
            };
            requestBody?: never;
            responses: {
                /** @description Sesión vigente para el host y organización actuales. */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/json": components["schemas"]["SesionConsumidor"];
                    };
                };
                401: components["responses"]["SinSesion"];
                /** @description Portal no disponible. */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
            };
        };
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/auth/recuperar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Solicita la recuperación de la cuenta del portal
         * @description Responde 200 exista o no el correo. El mensaje usa marca y host del portal.
         */
        post: {
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
                    "application/json": components["schemas"]["PeticionRecuperar"];
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
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/auth/restablecer": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Restablece la contraseña del consumidor */
        post: {
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
                    "application/json": components["schemas"]["PeticionRestablecer"];
                };
            };
            responses: {
                /** @description Contraseña restablecida; sesiones anteriores revocadas. */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                400: components["responses"]["DatosInvalidos"];
                403: components["responses"]["Csrf"];
                /** @description Portal no disponible. */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Token inválido o vencido. */
                422: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/auth/invitacion/{token}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta el correo de la invitación */
        get: operations["consultarInvitacionConsumidor"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/auth/invitacion/{token}/aceptar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Acepta una invitación al portal */
        post: {
            parameters: {
                query?: never;
                header: {
                    /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
                    "X-Requested-With": components["parameters"]["XRequestedWith"];
                };
                path: {
                    token: string;
                };
                cookie?: never;
            };
            requestBody: {
                content: {
                    "application/json": components["schemas"]["PeticionAceptarInvitacion"];
                };
            };
            responses: {
                /** @description Consumidor creado con correo verificado y sesión iniciada. */
                200: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                400: components["responses"]["DatosInvalidos"];
                403: components["responses"]["Csrf"];
                /** @description Portal no disponible. */
                404: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                /** @description Ya existe una cuenta con ese correo en la organización (`correo_ya_registrado`). */
                409: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content: {
                        "application/problem+json": components["schemas"]["Problema"];
                    };
                };
                /** @description Invitación vencida */
                422: {
                    headers: {
                        [name: string]: unknown;
                    };
                    content?: never;
                };
                429: components["responses"]["DemasiadasPeticiones"];
            };
        };
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/perfil": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Devuelve los datos del perfil
         * @description Devuelve el nombre del usuario autenticado.
         */
        get: operations["obtenerPerfil"];
        /**
         * Edita el nombre del perfil
         * @description Actualiza el nombre del usuario.
         */
        put: operations["editarPerfil"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/perfil/contrasena": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Cambia la contraseña actual
         * @description Valida la contraseña actual y si es correcta, establece la nueva y revoca todas las demás sesiones.
         */
        post: operations["cambiarContrasena"];
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
            /**
             * Format: email
             * @description Una parte local, una arroba y un dominio con al menos un punto, sin espacios.
             */
            correo: string;
            organizacion: string;
            /** @description No puede ser igual al correo. */
            contrasena: string;
        };
        PeticionRegistroConsumidor: {
            nombre: string;
            nombreEmpresa: string;
            /** Format: email */
            correo: string;
            /** @description No puede ser igual al correo. */
            contrasena: string;
        };
        PeticionAceptarInvitacion: {
            nombre: string;
            nombreEmpresa: string;
            contrasena: string;
        };
        SesionConsumidor: {
            consumidor: {
                nombre: string;
                nombreEmpresa: string;
            };
            correoVerificado: boolean;
            /**
             * @description A dónde lleva el portal después de iniciar sesión (10 §1): `/cuenta/suscripcion` si el consumidor tiene una suscripción vigente (no finalizada) a la API de este portal; si no, `/planes`.
             * @enum {string}
             */
            destino: "/cuenta/suscripcion" | "/planes";
        };
        /** @description Si falta `token`, responde 422 `token_invalido`. */
        PeticionVerificacion: {
            /** @description El valor del enlace de verificación. */
            token?: string;
        };
        /** @description Si falta `correo`, responde 200 sin hacer nada. */
        PeticionReenviar: {
            /** Format: email */
            correo?: string;
        };
        /** @description Si falta `correo` o `contrasena`, responde 401 `credenciales_invalidas`. */
        PeticionEntrar: {
            /** Format: email */
            correo?: string;
            contrasena?: string;
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
        /** @description Si falta `correo`, responde 200 sin hacer nada. */
        PeticionRecuperar: {
            /** Format: email */
            correo?: string;
        };
        PeticionRestablecer: {
            token: string;
            contrasena: string;
        };
        Perfil: {
            nombre: string;
        };
        PeticionPerfil: {
            nombre: string;
        };
        PeticionCambiarContrasena: {
            contrasenaActual: string;
            contrasenaNueva: string;
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
        /** @description El cuerpo no es un JSON válido (`datos_invalidos`, sin `errores`). */
        CuerpoInvalido: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
        /** @description Falta la cabecera `X-Requested-With`, o el `Origin` no tiene el mismo esquema, host y puerto que la petición (`csrf`). */
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
            400: components["responses"]["CuerpoInvalido"];
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
            400: components["responses"]["CuerpoInvalido"];
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
            400: components["responses"]["CuerpoInvalido"];
            /** @description El correo o la contraseña no son correctos, o falta alguno de los dos (`credenciales_invalidas`). */
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
            /** @description Sesión revocada, si existía, y cookie borrada. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
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
    solicitarRecuperacion: {
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
                "application/json": components["schemas"]["PeticionRecuperar"];
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
            400: components["responses"]["CuerpoInvalido"];
            403: components["responses"]["Csrf"];
            429: components["responses"]["DemasiadasPeticiones"];
        };
    };
    restablecerContrasena: {
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
                "application/json": components["schemas"]["PeticionRestablecer"];
            };
        };
        responses: {
            /** @description Contraseña restablecida y sesión iniciada. */
            200: {
                headers: {
                    "Set-Cookie": components["headers"]["CookieSesion"];
                    [name: string]: unknown;
                };
                content?: never;
            };
            400: components["responses"]["DatosInvalidos"];
            403: components["responses"]["Csrf"];
            /** @description El enlace venció o ya se usó (`token_invalido`). */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
        };
    };
    registrarConsumidor: {
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
                "application/json": components["schemas"]["PeticionRegistroConsumidor"];
            };
        };
        responses: {
            /** @description Cuenta creada y correo encolado. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            400: components["responses"]["DatosInvalidos"];
            403: components["responses"]["Csrf"];
            /** @description Portal no disponible. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Correo ya registrado en esta organización (`correo_ya_registrado`). */
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
    consultarInvitacionConsumidor: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                token: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Invitación vigente y correo invitado. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: email */
                        correo: string;
                    };
                };
            };
            /** @description Portal no disponible. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Invitación vencida */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            429: components["responses"]["DemasiadasPeticiones"];
        };
    };
    obtenerPerfil: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Perfil devuelto. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["Perfil"];
                };
            };
            401: components["responses"]["SinSesion"];
        };
    };
    editarPerfil: {
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
                "application/json": components["schemas"]["PeticionPerfil"];
            };
        };
        responses: {
            /** @description Nombre actualizado. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            400: components["responses"]["DatosInvalidos"];
            401: components["responses"]["SinSesion"];
            403: components["responses"]["Csrf"];
        };
    };
    cambiarContrasena: {
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
                "application/json": components["schemas"]["PeticionCambiarContrasena"];
            };
        };
        responses: {
            /** @description Contraseña cambiada. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            400: components["responses"]["DatosInvalidos"];
            /** @description Sin sesión o la contraseña actual es incorrecta (`credenciales_invalidas`). */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            403: components["responses"]["Csrf"];
        };
    };
}
