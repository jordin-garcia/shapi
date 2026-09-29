export interface paths {
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
            /** @description Rol del actor en minúsculas; `consumidor` y `sistema` identifican esos tipos de actor. */
            rol: string;
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
            /** @description Fechas o paginación inválidas (`datos_invalidos`). */
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
