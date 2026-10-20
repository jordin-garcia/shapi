export interface paths {
    "/api/apis/{apiId}/consumidores": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Consumo del ciclo y facturación por consumidor (RF-36)
         * @description Propietario, editor o lector de la organización dueña. Una fila por suscripción no finalizada, ordenada por empresa y suscripción. Suma las llamadas de producción consolidadas desde el inicio del ciclo hasta su fin o el día actual si está vencido, incluyendo rutas retiradas. La facturación suma los pagos autorizados cuyo periodo coincide con el ciclo vigente, incluidos prorrateos; no usa el precio actual del plan. El último cobro es el último intento de la suscripción, ordenado por fecha e ID; es nulo si nunca hubo cobros. Las fechas son inclusivas en Guatemala. Los totales abarcan todas las filas. Sin paginación.
         */
        get: operations["consultarConsumoConsumidores"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
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
    "/api/portal/consumo": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Consumo del ciclo vigente del consumidor
         * @description Sesión de consumidor y API publicada resuelta por el host. Solo producción. Inicio y fin son días inclusivos en Guatemala (fin de la suscripción menos un día). Las llamadas usadas se leen de cuota:susc con el inicio del ciclo; una llave ausente vale cero. El detalle conserva rutas retiradas, suma llamadas realmente descontadas y puede ir hasta diez segundos detrás de Redis. Los porcentajes de las rutas usan las llamadas consolidadas como denominador. El peso es el configurado actualmente y es nulo para rutas retiradas. Mientras no haya renovación, se incluye también el consumo posterior al fin durante la gracia.
         */
        get: operations["consultarConsumoConsumidor"];
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
        ConsumoConsumidores: {
            /** Format: uuid */
            apiId: string;
            apiNombre: string;
            elementos: components["schemas"]["ConsumoConsumidor"][];
            total: number;
            activas: number;
            /** Format: int64 */
            llamadas: number;
            /** Format: int64 */
            cuota: number;
            facturacion: number;
            /** @constant */
            moneda: "GTQ";
        };
        ConsumoConsumidor: {
            /** Format: uuid */
            suscripcionId: string;
            /** Format: uuid */
            consumidorId: string;
            consumidor: string;
            plan: string;
            /** Format: date */
            inicio: string;
            /** Format: date */
            fin: string;
            /** Format: int64 */
            llamadas: number;
            /** Format: int64 */
            cuota: number;
            porcentaje: number;
            facturacion: number;
            /** @enum {string|null} */
            ultimoCobro: "autorizado" | "rechazado" | "revertido" | null;
            /** @enum {string} */
            estadoSuscripcion: "activa" | "en_gracia" | "suspendida";
        };
        ConsumoRutaCiclo: {
            /** Format: uuid */
            rutaId: string | null;
            metodo: string | null;
            patron: string;
            pesoLlamadas: number | null;
            /** Format: int64 */
            peticiones: number;
            /** Format: int64 */
            llamadas: number;
            porcentaje: number;
        };
        ConsumoCiclo: {
            /** Format: uuid */
            suscripcionId: string;
            /** Format: date */
            inicio: string;
            /** Format: date */
            fin: string;
            /** Format: int64 */
            llamadas: number;
            /** Format: int64 */
            cuota: number;
            /** Format: int64 */
            restante: number;
            porcentaje: number;
            /** Format: int64 */
            peticiones: number;
            /** Format: int64 */
            llamadasConsolidadas: number;
            rutas: components["schemas"]["ConsumoRutaCiclo"][];
        };
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
    consultarConsumoConsumidores: {
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
            /** @description Suscripciones vigentes y totales, o lista vacía; sin caché HTTP. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ConsumoConsumidores"];
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
    consultarConsumoConsumidor: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Cuota en tiempo real y detalle consolidado del ciclo; sin caché HTTP. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ConsumoCiclo"];
                };
            };
            /** @description Sin sesión del consumidor. */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Sesión de otro ámbito. */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description suscripcion_no_encontrada si no hay una suscripción no finalizada; api_no_encontrada si el host es inválido o pertenece a otra organización. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description servicio_no_disponible si no se puede leer el contador de Redis; nunca se sustituye un fallo por consumo cero. */
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
