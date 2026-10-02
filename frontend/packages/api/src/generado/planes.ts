export interface paths {
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
        /** Obtiene los planes activos para el portal público de una API */
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
        PlanApi: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            apiId: string;
            nombre: string;
            descripcion: string;
            precio: number;
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
    };
    responses: never;
    parameters: never;
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
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
        };
    };
}
