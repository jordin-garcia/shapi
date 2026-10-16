export interface paths {
    "/api/admin/planes-plataforma": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Lista los planes de plataforma para administración */
        get: operations["administrarListarPlanesPlataforma"];
        put?: never;
        /** Crea un plan de plataforma */
        post: operations["crearPlanPlataformaAdministracion"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/planes-plataforma/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Edita un plan de plataforma */
        put: operations["editarPlanPlataformaAdministracion"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/planes-plataforma/{id}/desactivar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Desactiva un plan de plataforma */
        post: operations["desactivarPlanPlataformaAdministracion"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/planes-plataforma": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Lista los planes de plataforma activos por orden */
        get: operations["listarPlanesPlataforma"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{apiId}/planes": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Obtiene los planes de una API */
        get: operations["obtenerPlanes"];
        put?: never;
        /** Crea un plan de API */
        post: operations["crearPlan"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{apiId}/planes/{planId}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Edita un plan de API */
        put: operations["editarPlan"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{apiId}/planes/{planId}/desactivar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Desactiva un plan de API */
        post: operations["desactivarPlan"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/planes": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Obtiene los planes activos para el portal público de una API
         * @description Ordenados por precio y nombre, como en A4.1.
         */
        get: operations["obtenerPlanesPortal"];
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
        PeticionPlanPlataforma: {
            nombre: string;
            descripcion: string;
            precio: number;
            vigenciaDias: number;
            maxApis?: number | null;
            maxMiembros?: number | null;
            /** Format: int64 */
            cuotaPeticiones: number;
            dominioPropio: boolean;
        };
        PlanPlataformaAdministracion: {
            /** Format: uuid */
            id: string;
            nombre: string;
            descripcion: string;
            precio: number;
            /** @constant */
            moneda: "GTQ";
            vigenciaDias: number;
            maxApis?: number | null;
            maxMiembros?: number | null;
            /** Format: int64 */
            cuotaPeticiones: number;
            dominioPropio: boolean;
            esPrueba: boolean;
            activo: boolean;
            orden: number;
        };
        PlanPlataforma: {
            /** Format: uuid */
            id: string;
            nombre: string;
            descripcion: string;
            precio: number;
            /** @constant */
            moneda: "GTQ";
            vigenciaDias: number;
            maxApis?: number | null;
            maxMiembros?: number | null;
            /** Format: int64 */
            cuotaPeticiones: number;
            dominioPropio: boolean;
            esPrueba: boolean;
            /** Format: date-time */
            inicioCicloPrevisto: string;
        };
        PlanApi: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            apiId: string;
            nombre: string;
            descripcion: string;
            /** @description Mayor que 0 si el plan no es gratuito; 0 si es gratuito. Hasta 2 decimales. */
            precio: number;
            /** @constant */
            moneda: "GTQ";
            esGratuito: boolean;
            vigenciaDias: number;
            cuotaLlamadas: number;
            limiteMinuto: number;
            activo: boolean;
        };
        PeticionPlanApi: {
            nombre: string;
            descripcion: string;
            precio: number;
            esGratuito: boolean;
            vigenciaDias: number;
            cuotaLlamadas: number;
            limiteMinuto: number;
        };
        /** @description ProblemDetails de la API de control. */
        Problema: {
            type?: string;
            title: string;
            status: number;
            /** @enum {string} */
            codigo: "datos_invalidos" | "csrf" | "api_no_encontrada" | "plan_no_encontrado" | "plan_duplicado" | "plan_con_suscripciones";
            detalle?: {
                [key: string]: unknown;
            };
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
    administrarListarPlanesPlataforma: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Planes ordenados, incluidos los inactivos */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlanPlataformaAdministracion"][];
                };
            };
            /** @description Solo el administrador puede ver estos planes */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    crearPlanPlataformaAdministracion: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionPlanPlataforma"];
            };
        };
        responses: {
            /** @description Plan creado */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlanPlataformaAdministracion"];
                };
            };
            /** @description Datos inválidos (`datos_invalidos`) */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Solo el administrador puede crear planes */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Ya existe un plan con ese nombre (`plan_duplicado`) */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    editarPlanPlataformaAdministracion: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionPlanPlataforma"];
            };
        };
        responses: {
            /** @description Plan actualizado */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlanPlataformaAdministracion"];
                };
            };
            /** @description Datos inválidos (`datos_invalidos`) */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Solo el administrador puede editar planes */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Plan no encontrado (`plan_no_encontrado`) */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Ya existe un plan con ese nombre (`plan_duplicado`) */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    desactivarPlanPlataformaAdministracion: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Plan desactivado; las suscripciones históricas se conservan */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Solo el administrador puede desactivar planes */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Plan no encontrado (`plan_no_encontrado`) */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    listarPlanesPlataforma: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Planes de plataforma disponibles */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlanPlataforma"][];
                };
            };
        };
    };
    obtenerPlanes: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                apiId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Lista de planes */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlanApi"][];
                };
            };
            /** @description Petición inválida */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description No tiene permisos */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description API no encontrada */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
        };
    };
    crearPlan: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                apiId: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionPlanApi"];
            };
        };
        responses: {
            /** @description Plan creado exitosamente */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlanApi"];
                };
            };
            /** @description Petición inválida */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description No tiene permisos */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description API no encontrada */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Conflicto */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
        };
    };
    editarPlan: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                apiId: string;
                planId: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionPlanApi"];
            };
        };
        responses: {
            /** @description Plan editado exitosamente */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlanApi"];
                };
            };
            /** @description Petición inválida */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description No tiene permisos */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description API o plan no encontrado */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Conflicto */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /**
             * @description Se intentó cambiar entre gratuito y de pago mientras el plan tiene suscripciones vigentes
             *     (`plan_con_suscripciones`, 09 §5).
             */
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
    desactivarPlan: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                apiId: string;
                planId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Plan desactivado exitosamente */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Petición inválida */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description No tiene permisos */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description API o plan no encontrado */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
        };
    };
    obtenerPlanesPortal: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Lista de planes activos */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PlanApi"][];
                };
            };
            /** @description El host no corresponde a ningún portal (`api_no_encontrada`). */
            404: {
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
