export interface paths {
    "/api/portal/suscripciones": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Contrata un plan de API y emite sus claves */
        post: operations["contratarPlanApi"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/suscripcion": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta la suscripción del consumidor en la API del portal */
        get: operations["obtenerSuscripcionApi"];
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
        PeticionContratarPlan: {
            /** Format: uuid */
            planId: string;
            tarjeta?: components["schemas"]["Tarjeta"] | null;
        };
        Tarjeta: {
            numero: string;
            mesVencimiento: string;
            anioVencimiento: string;
            cvv: string;
            titular: string;
        };
        Contratacion: {
            suscripcion: components["schemas"]["Suscripcion"];
            claves: components["schemas"]["ClaveEmitida"][];
        };
        Suscripcion: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            apiId: string;
            /** Format: uuid */
            planId: string;
            nombrePlan: string;
            /** @enum {string} */
            estado: "activa" | "en_gracia" | "suspendida" | "finalizada";
            /** Format: date-time */
            inicio: string;
            /** Format: date-time */
            fin: string;
            /** Format: date-time */
            proximaRenovacion: string;
        };
        ClaveEmitida: {
            /** Format: uuid */
            id: string;
            /** @enum {string} */
            tipo: "produccion" | "pruebas";
            clave: string;
            claveEnmascarada: string;
            /** @constant */
            estado: "activa";
        };
        SuscripcionActual: {
            plan: {
                /** Format: uuid */
                id: string;
                nombre: string;
                precio: number;
                /** @constant */
                moneda: "GTQ";
            };
            /** @enum {string} */
            estado: "activa" | "en_gracia" | "suspendida" | "finalizada";
            periodo: {
                /** Format: date-time */
                inicio: string;
                /** Format: date-time */
                fin: string;
            };
            /** Format: date-time */
            proximaRenovacion: string;
            tarjetaEnmascarada?: string | null;
            /** Format: int64 */
            cuotaLlamadas: number;
            limiteMinuto: number;
        };
        Problema: {
            type?: string;
            title?: string;
            status?: number;
            codigo?: string;
            detalle?: unknown;
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
    contratarPlanApi: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionContratarPlan"];
            };
        };
        responses: {
            /** @description Suscripción activa y claves mostradas una sola vez */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["Contratacion"];
                };
            };
            /** @description Petición inválida o falta la tarjeta requerida */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La pasarela rechazó el pago; el motivo aparece en detalle */
            402: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Portal, plan o consumidor no encontrado */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Ya existe una suscripción vigente en esta API */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Correo sin verificar, plan inactivo o tarjeta inválida */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Pasarela de pagos no disponible */
            503: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
        };
    };
    obtenerSuscripcionApi: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Suscripción, periodo mostrado, renovación, tarjeta y límites */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["SuscripcionActual"];
                };
            };
            /** @description No existe una suscripción vigente */
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
