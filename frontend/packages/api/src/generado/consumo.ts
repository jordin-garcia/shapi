export interface paths {
    "/api/apis/{apiId}/consumo": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Consumo, latencia y errores de una API
         * @description Propietario, editor o lector de la organización dueña. Por defecto los últimos diez días, incluido hoy en Guatemala. Se permite un periodo de hasta 366 días. Se incluyen días sin peticiones y consumo sin ruta identificada o de rutas retiradas.
         */
        get: operations["consultarConsumoApi"];
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
        Percentil95: {
            milisegundos: number | null;
            /** @description El rango abierto se presenta como «> 2500 ms», con milisegundos nulo. */
            supera2500: boolean;
        };
        ConsumoDia: {
            /** Format: date */
            fecha: string;
            /** Format: int64 */
            peticiones: number;
        };
        ConsumoRuta: {
            /** Format: uuid */
            rutaId: string | null;
            metodo: string | null;
            patron: string;
            /** Format: int64 */
            peticiones: number;
            porcentaje: number;
            latenciaTotalP95: components["schemas"]["Percentil95"];
            latenciaCompuertaP95: components["schemas"]["Percentil95"];
        };
        ConsumoError: {
            codigo: string;
            motivo: string;
            /** Format: int64 */
            peticiones: number;
            porcentaje: number;
        };
        ConsumoApi: {
            /** Format: uuid */
            apiId: string;
            apiNombre: string;
            /** Format: date */
            desde: string;
            /** Format: date */
            hasta: string;
            /** Format: int64 */
            peticiones: number;
            /** Format: int64 */
            llamadas: number;
            /** Format: int64 */
            bytesEntrada: number;
            /** Format: int64 */
            bytesSalida: number;
            latenciaTotalP95: components["schemas"]["Percentil95"];
            latenciaCompuertaP95: components["schemas"]["Percentil95"];
            dias: components["schemas"]["ConsumoDia"][];
            rutas: components["schemas"]["ConsumoRuta"][];
            rechazosCompuerta: components["schemas"]["ConsumoError"][];
            erroresOrigen: components["schemas"]["ConsumoError"][];
        };
        Problema: {
            type: string;
            title: string;
            status: number;
            codigo: string;
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
    consultarConsumoApi: {
        parameters: {
            query?: {
                /** @description Inicio inclusivo; por defecto nueve días antes de hasta. */
                desde?: string;
                /** @description Fin inclusivo; por defecto hoy en Guatemala. */
                hasta?: string;
            };
            header?: never;
            path: {
                apiId: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Indicadores del periodo; p95 nulo cuando no hay muestras. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ConsumoApi"];
                };
            };
            /** @description Fechas inválidas, invertidas o periodo mayor que 366 días. */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Sin sesión del personal. */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description El rol no puede consultar el consumo. */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description La API no existe o pertenece a otra organización. */
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
