export interface paths {
    "/api/pagos": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta el historial de pagos de plataforma de la organización */
        get: operations["listarPagosPlataforma"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/pagos": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta el historial de pagos del consumidor en la API actual */
        get: operations["listarPagosDelConsumidor"];
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
        PagosPaginados: {
            elementos: components["schemas"]["Pago"][];
            total: number;
        };
        Pago: {
            /** Format: uuid */
            id: string;
            /**
             * Format: date-time
             * @description Fecha UTC de registro del pago
             */
            fecha: string;
            /** @enum {string} */
            concepto: "contratacion" | "renovacion" | "cambioPlan" | "reactivacion";
            descripcion: string;
            /** Format: date-time */
            periodoInicio?: string | null;
            /** Format: date-time */
            periodoFin?: string | null;
            /** Format: decimal */
            monto: number;
            /** @constant */
            moneda: "GTQ";
            medioPago: components["schemas"]["MedioPago"] | null;
            /** @enum {string} */
            estado: "autorizado" | "rechazado" | "revertido";
        };
        MedioPago: {
            /** @enum {string} */
            marca: "visa" | "mastercard" | "americanExpress";
            ultimos4: string;
        };
        Problema: {
            type?: string;
            title?: string;
            status?: number;
            codigo?: string;
            errores?: {
                [key: string]: string[];
            };
        };
    };
    responses: never;
    parameters: {
        Pagina: number;
        Tamano: number;
    };
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    listarPagosPlataforma: {
        parameters: {
            query?: {
                pagina?: components["parameters"]["Pagina"];
                tamano?: components["parameters"]["Tamano"];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Historial paginado de pagos de plataforma */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PagosPaginados"];
                };
            };
            /** @description Parámetros de paginación inválidos (`datos_invalidos`) */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Sesión requerida */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Se requiere el rol propietario o lector */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    listarPagosDelConsumidor: {
        parameters: {
            query?: {
                pagina?: components["parameters"]["Pagina"];
                tamano?: components["parameters"]["Tamano"];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Historial de pagos de la API del portal actual */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PagosPaginados"];
                };
            };
            /** @description Parámetros de paginación inválidos (`datos_invalidos`) */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Sesión de consumidor requerida */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Se requiere una sesión del consumidor */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description El host no corresponde a un portal disponible */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
}
