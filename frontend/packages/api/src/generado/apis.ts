export interface paths {
    "/api/apis": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Lista las APIs de la organización */
        get: operations["listarApis"];
        put?: never;
        /**
         * Registra una API
         * @description Valida el subdominio y la URL contra SSRF, prueba el origen con un GET de hasta
         *     cinco segundos y, si responde con cualquier código HTTP, guarda la API en borrador.
         *     El secreto de origen se devuelve en claro solo en esta respuesta.
         */
        post: operations["registrarApi"];
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
        /** @description ProblemDetails estándar, sin código de negocio. */
        ProblemaEstandar: {
            type?: string;
            title: string;
            status: number;
        };
        PeticionRegistroApi: {
            nombre: string;
            /** Format: uri */
            urlOrigen: string;
            subdominio: string;
        };
        ApiResumen: {
            /** Format: uuid */
            id: string;
            nombre: string;
            subdominio: string;
            /** @enum {string} */
            estado: "borrador" | "publicada" | "despublicada";
        };
        ListaApis: {
            elementos: components["schemas"]["ApiResumen"][];
            total: number;
            planNombre: string;
            /** @description Nulo para los planes sin límite de APIs. */
            maxApis: number | null;
        };
        ApiRegistrada: {
            /** Format: uuid */
            id: string;
            nombre: string;
            subdominio: string;
            /** @constant */
            estado: "borrador";
            /** @description Se muestra una sola vez. */
            secretoOrigen: string;
            conexionMilisegundos: number;
        };
        /** @description ProblemDetails de la API de control. */
        Problema: {
            type?: string;
            title: string;
            status: number;
            /** @enum {string} */
            codigo: "datos_invalidos" | "csrf" | "subdominio_ocupado" | "origen_no_permitido" | "origen_inaccesible";
            detalle?: {
                [key: string]: unknown;
            };
            errores?: {
                [key: string]: string[];
            };
        };
    };
    responses: {
        /** @description Datos inválidos (`datos_invalidos`), con errores por campo. */
        DatosInvalidos: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
        /** @description No hay una sesión vigente. */
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
        /** @description La sesión no tiene permiso para consultar APIs. */
        SinPermiso: {
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
        /** @description La sesión no puede configurar APIs o falló la protección CSRF (`csrf`). */
        SinPermisoOCsrf: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["ProblemaEstandar"] | components["schemas"]["Problema"];
            };
        };
    };
    parameters: {
        /** @description Protección CSRF; el cliente del frontend la agrega siempre. */
        XRequestedWith: "shapi";
    };
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    listarApis: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description APIs visibles para la organización de la sesión y límites de su plan. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ListaApis"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermiso"];
        };
    };
    registrarApi: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF; el cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionRegistroApi"];
            };
        };
        responses: {
            /** @description API registrada; el secreto no se vuelve a mostrar. */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ApiRegistrada"];
                };
            };
            400: components["responses"]["DatosInvalidos"];
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            /** @description El subdominio ya pertenece a otra API (`subdominio_ocupado`). */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description El origen está prohibido (`origen_no_permitido`) o no respondió (`origen_inaccesible`). */
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
}
