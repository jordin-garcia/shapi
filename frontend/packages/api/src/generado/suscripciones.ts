export interface paths {
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
    "/api/suscripcion": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta la suscripción de plataforma de la organización */
        get: operations["obtenerSuscripcionPlataforma"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/suscripcion/contratar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Contrata un plan de plataforma */
        post: operations["contratarPlanPlataforma"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/suscripcion/cambiar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Cambia o programa un cambio de plan de plataforma */
        post: operations["cambiarPlanPlataforma"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/suscripcion/cambio-programado": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post?: never;
        /** Cancela el cambio de plan programado */
        delete: operations["cancelarCambioPlanPlataforma"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/suscripcion/pagar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Paga y reactiva una suscripción en gracia o suspendida */
        post: operations["pagarSuscripcionPlataforma"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
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
        };
        PeticionSuscripcionPlataforma: {
            /** Format: uuid */
            planId: string;
            /** @default false */
            usarRegistrada: boolean;
            tarjeta?: components["schemas"]["Tarjeta"] | null;
        };
        PeticionPagoPlataforma: {
            /** @default false */
            usarRegistrada: boolean;
            tarjeta?: components["schemas"]["Tarjeta"] | null;
        };
        SuscripcionPlataforma: {
            plan: {
                /** Format: uuid */
                id?: string;
                nombre?: string;
                descripcion?: string;
                precio?: number;
                /** @constant */
                moneda?: "GTQ";
                vigenciaDias?: number;
            };
            /** @enum {string} */
            estado: "activa" | "en_gracia" | "suspendida" | "finalizada";
            periodo: {
                /** Format: date-time */
                inicio?: string;
                /** Format: date-time */
                fin?: string;
            };
            /** Format: date-time */
            proximaRenovacion: string;
            tarjetaEnmascarada?: string | null;
            /** Format: date-time */
            graciaHasta?: string | null;
            diasRestantesCiclo?: number;
            diasRestantes?: number;
            /** Format: date-time */
            ultimoRechazoEn?: string | null;
            consumidoresAfectados?: number;
            cambioProgramado?: {
                /** Format: uuid */
                planId?: string;
                nombre?: string;
                /** Format: date-time */
                efectivoDesde?: string;
            } | null;
        };
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
            /** @description Errores por campo de `datos_invalidos` (convenciones §5). */
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
    obtenerSuscripcionPlataforma: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Plan, estado, periodo, tarjeta y cambio programado */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["SuscripcionPlataforma"];
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
    contratarPlanPlataforma: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionSuscripcionPlataforma"];
            };
        };
        responses: {
            /** @description Plan contratado */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Cobro rechazado */
            402: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Plan no encontrado */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Ya existe una suscripción de pago vigente */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    cambiarPlanPlataforma: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionSuscripcionPlataforma"];
            };
        };
        responses: {
            /** @description Cambio aplicado o programado */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Cobro rechazado */
            402: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description La organización excede los límites del plan */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    cancelarCambioPlanPlataforma: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Cambio cancelado */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    pagarSuscripcionPlataforma: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionPagoPlataforma"];
            };
        };
        responses: {
            /** @description Suscripción reactivada */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Cobro rechazado */
            402: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
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
            /**
             * @description Datos inválidos (`datos_invalidos`), con el error en cada campo de `errores`: falta la tarjeta que pide un
             *     plan de pago (`tarjeta`), o el vencimiento o el titular no son válidos (`tarjeta.vencimiento` y
             *     `tarjeta.titular`).
             */
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
            /**
             * @description Correo sin verificar (`correo_no_verificado`), plan inactivo (`plan_no_encontrado`) o tarjeta que la
             *     pasarela no acepta (`numero_invalido`, `marca_no_soportada`, `tarjeta_vencida` o `cvv_invalido`).
             */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /**
             * @description La pasarela de pagos no respondió al tokenizar o al cobrar (`pasarela_no_disponible`). No se registra
             *     ningún pago. Si el cobro se autorizó pero la contratación falla después, se reembolsa.
             */
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
