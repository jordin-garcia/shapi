export interface paths {
    "/api/admin/demo/reloj": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta la fecha de demostración (solo administrador) */
        get: operations["consultarRelojDemo"];
        put?: never;
        /** Adelanta el reloj de demostración y dispara el cierre de ciclo */
        post: operations["adelantarRelojDemo"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/reloj": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta la hora de negocio para las pantallas del personal */
        get: operations["consultarFechaServidor"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/reloj": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta la hora de negocio para el consumidor del portal */
        get: operations["consultarFechaPortal"];
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
    "/api/suscripcion/uso": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta el consumo de cuota de la suscripción de plataforma */
        get: operations["obtenerUsoSuscripcionPlataforma"];
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
        /** Paga y reactiva un plan de pago en gracia o suspendido */
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
    "/api/portal/suscripcion/pagar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Reactiva la suscripción API del consumidor en gracia o suspendida */
        post: operations["reactivarSuscripcionApi"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/suscripcion/cambiar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Cambia o programa un cambio de plan del consumidor */
        post: operations["cambiarPlanSuscripcionApi"];
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
        FechaServidor: {
            /** Format: date-time */
            ahora: string;
            /**
             * Format: date-time
             * @description Inicio del día actual en America/Guatemala, según IReloj
             */
            inicioCiclo: string;
            /**
             * Format: date-time
             * @description Previsión de ahora más 24 horas; la rotación confirma la fecha definitiva
             */
            finClaveAnteriorPrevisto: string;
        };
        PeticionRelojDemo: {
            adelantarDias: number;
        };
        ResultadoContratacionPlataforma: {
            /** Format: uuid */
            id: string;
            plan: {
                /** Format: uuid */
                id: string;
                nombre: string;
                precio: number;
                /** @constant */
                moneda: "GTQ";
                vigenciaDias: number;
            };
            /** @constant */
            estado: "activa";
            periodo: {
                /** Format: date-time */
                inicio: string;
                /** Format: date-time */
                fin: string;
            };
            /** Format: date-time */
            proximaRenovacion: string;
            tarjetaEnmascarada: string;
        };
        ResultadoCambioProrrateado: {
            /** @constant */
            estado: "activa";
            credito: number;
            cargo: number;
            aPagar: number;
            /** Format: date-time */
            inicio: string;
            /** Format: date-time */
            fin: string;
        };
        ResultadoCambioProgramado: {
            /** @constant */
            estado: "programado";
            /** Format: uuid */
            planSiguienteId: string;
            /** Format: date-time */
            efectivoDesde: string;
        };
        ResultadoSinCambios: {
            /** @constant */
            estado: "sin_cambios";
        };
        ResultadoPagoSuscripcionPlataforma: {
            /** @constant */
            estado: "activa";
            /** Format: date-time */
            inicio: string;
            /** Format: date-time */
            fin: string;
        };
        ResultadoReactivacionSuscripcionApi: {
            /** @constant */
            estado: "activa";
            /** Format: date-time */
            inicio: string;
            /** Format: date-time */
            fin: string;
        };
        ResultadoCambioSuscripcionApi: {
            /** @constant */
            estado: "activa";
            credito: number;
            cargo: number;
            aPagar: number;
            /** Format: date-time */
            inicio: string;
            /** Format: date-time */
            fin: string;
            claves: components["schemas"]["ClaveEmitida"][];
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
                id: string;
                nombre: string;
                descripcion: string;
                precio: number;
                /** @constant */
                moneda: "GTQ";
                vigenciaDias: number;
                maxApis: number | null;
                maxMiembros: number | null;
                /** Format: int64 */
                cuotaPeticiones: number;
                dominioPropio: boolean;
                esPrueba: boolean;
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
            medioPago: {
                /** @enum {string} */
                marca: "Visa" | "Mastercard" | "American Express";
                ultimos4: string;
            } | null;
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
            apiDisponible: boolean;
            /** Format: date-time */
            graciaHasta: string | null;
            /** Format: date-time */
            ultimoRechazoEn: string | null;
            diasRestantes: number;
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
    consultarRelojDemo: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Fecha actual del servidor, con el desplazamiento aplicado */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["FechaServidor"];
                };
            };
            /** @description Sin sesión del personal */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Requiere el rol administrador */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Solo está disponible con `SHAPI_MODO_DEMO=true` */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    adelantarRelojDemo: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionRelojDemo"];
            };
        };
        responses: {
            /** @description Reloj adelantado y cierre de ciclo solicitado */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description La cantidad debe estar entre 1 y 365 días (`datos_invalidos`) */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Solo está disponible con `SHAPI_MODO_DEMO=true` */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    consultarFechaServidor: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Hora de IReloj e inicio del día de negocio en Guatemala */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["FechaServidor"];
                };
            };
            /** @description Sin sesión del personal */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    consultarFechaPortal: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Hora de IReloj e inicio del día de negocio en Guatemala */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["FechaServidor"];
                };
            };
            /** @description Sin sesión de consumidor válida para este portal */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
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
    obtenerUsoSuscripcionPlataforma: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Peticiones utilizadas, cuota y porcentaje del ciclo actual */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: int64 */
                        peticiones: number;
                        /** Format: int64 */
                        cuotaPeticiones: number;
                        porcentaje: number;
                    };
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
            /** @description Redis no está disponible para consultar el uso de cuota (`servicio_no_disponible`) */
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
            /** @description Plan contratado y datos del ciclo activado */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ResultadoContratacionPlataforma"];
                };
            };
            /** @description Datos inválidos o no hay tarjeta registrada (`datos_invalidos`) */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La pasarela rechazó el cobro (`pago_rechazado`) */
            402: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Plan no encontrado */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Ya existe una suscripción de pago vigente */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Tarjeta inválida o vencida */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La pasarela de pagos no está disponible (`pasarela_no_disponible`) */
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
            /** @description Cambio aplicado, contratado desde plan de prueba/gratuito, sin cambios o programado */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ResultadoCambioProrrateado"] | components["schemas"]["ResultadoContratacionPlataforma"] | components["schemas"]["ResultadoCambioProgramado"] | components["schemas"]["ResultadoSinCambios"];
                };
            };
            /** @description Datos inválidos o no hay tarjeta registrada (`datos_invalidos`) */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La pasarela rechazó el cobro (`pago_rechazado`) */
            402: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Suscripción o plan no encontrado */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Tarjeta inválida o vencida, u organización fuera de los límites del plan */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La pasarela de pagos no está disponible (`pasarela_no_disponible`) */
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
            /** @description Suscripción reactivada con el ciclo nuevo */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ResultadoPagoSuscripcionPlataforma"];
                };
            };
            /** @description Datos inválidos o no hay tarjeta registrada (`datos_invalidos`) */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La pasarela rechazó el cobro (`pago_rechazado`) */
            402: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description No hay una suscripción que requiera pago */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Tarjeta inválida o vencida, o el plan es Prueba o gratuito (`requiere_plan_de_pago`); debe contratar un plan de pago */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La pasarela de pagos no está disponible (`pasarela_no_disponible`) */
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
    reactivarSuscripcionApi: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    tarjeta: components["schemas"]["Tarjeta"];
                };
            };
        };
        responses: {
            /** @description Suscripción reactivada con un ciclo nuevo desde hoy */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ResultadoReactivacionSuscripcionApi"];
                };
            };
            /** @description Falta la tarjeta o sus datos no son válidos */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La pasarela rechazó el pago */
            402: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Portal, consumidor o suscripción por reactivar no encontrado */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La tarjeta no es válida o la suscripción no es de pago */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La pasarela no está disponible */
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
    cambiarPlanSuscripcionApi: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    planId: string;
                    tarjeta?: components["schemas"]["Tarjeta"];
                };
            };
        };
        responses: {
            /** @description Cambio prorrateado, plan cambiado, sin cambios o cambio programado */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ResultadoCambioSuscripcionApi"] | components["schemas"]["ResultadoCambioProgramado"] | components["schemas"]["ResultadoSinCambios"];
                };
            };
            /** @description Falta la tarjeta o sus datos no son válidos */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La pasarela rechazó el pago y no se cambió el plan */
            402: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Portal, consumidor, suscripción o plan no encontrado */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description La pasarela no está disponible */
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
}
