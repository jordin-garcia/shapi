export interface paths {
    "/api/admin/estado": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Consulta el estado de los siete componentes
         * @description Comprobación compartida durante 30 segundos. La latencia corresponde a la última hora; sin tráfico es nula.
         */
        get: operations["consultarEstado"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/bitacora": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Consulta la bitácora de acciones sensibles
         * @description Devuelve las acciones de la más reciente a la más antigua. Las fechas son días de
         *     America/Guatemala y ambos extremos son inclusivos. Sin fechas consulta hoy y los
         *     seis días anteriores.
         */
        get: operations["consultarBitacora"];
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
        EstadoPlataforma: {
            /** Format: date-time */
            comprobadoEn: string;
            latenciaP95Ms: number | null;
            latenciaSupera2500: boolean;
            componentes: components["schemas"]["EstadoComponente"][];
        };
        EstadoComponente: {
            /** @enum {string} */
            id: "compuerta" | "api_control" | "trabajador" | "redis" | "postgres" | "pasarela" | "correo";
            /** @enum {string} */
            estado: "en_servicio" | "degradado" | "fuera_de_servicio";
            /** Format: date-time */
            incidenciaDesde: string | null;
        };
        PaginaBitacora: {
            elementos: components["schemas"]["EntradaBitacora"][];
            total: number;
        };
        EntradaBitacora: {
            /** Format: date-time */
            fecha: string;
            actor: components["schemas"]["ActorBitacora"];
            /** @example organizacion.suspendida */
            accion: string;
            /** @example Suspendió la organización Datos Chapines, S.A. */
            descripcion: string;
        };
        ActorBitacora: {
            nombre: string;
            /**
             * @description Rol actual del actor (10 §7). `consumidor` y `sistema` identifican esos tipos de actor; `usuario`, a un
             *     usuario que ya no tiene membresía.
             * @enum {string}
             */
            rol: "administrador" | "soporte" | "propietario" | "editor" | "lector" | "consumidor" | "sistema" | "usuario";
            /**
             * @description Organización actual del actor: «Plataforma Shapi» para el personal de plataforma, la del proveedor para sus
             *     miembros y para los consumidores, y `null` para el sistema y para un usuario sin membresía.
             */
            organizacion: string | null;
        };
        Problema: {
            type?: string;
            title: string;
            status: number;
            /** @constant */
            codigo: "datos_invalidos";
            errores?: {
                [key: string]: string[];
            };
        };
    };
    responses: never;
    parameters: never;
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    consultarEstado: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Estado comprobado de la plataforma. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["EstadoPlataforma"];
                };
            };
            /** @description No hay una sesión vigente. */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Se requiere administrador o soporte. */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    consultarBitacora: {
        parameters: {
            query?: {
                /** @description Primer día incluido, en America/Guatemala. */
                desde?: string;
                /** @description Último día incluido, en America/Guatemala. */
                hasta?: string;
                pagina?: number;
                tamano?: number;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Página de acciones sensibles. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PaginaBitacora"];
                };
            };
            /** @description Fechas o paginación inválidas (`datos_invalidos`), con el error de cada parámetro en `errores`. */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description No hay una sesión vigente. */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description La sesión no pertenece a un administrador ni a soporte. */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
}
