export interface paths {
    "/api/portal/configuracion": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Obtiene la configuración pública del portal */
        get: operations["obtenerConfiguracionPortal"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/logo": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Obtiene el logotipo del portal */
        get: operations["obtenerLogoPortal"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/documentacion": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Obtiene la documentación pública de las rutas expuestas */
        get: operations["obtenerDocumentacionPortal"];
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
        ConfiguracionPortal: {
            /** @description Nombre personalizado; si no existe, se usa el nombre de la API. */
            nombrePortal: string;
            /** @example #B8322A */
            colorPrincipal: string;
            /** @example /api/portal/logo */
            urlLogo: string | null;
            bienvenida: string | null;
            nombreApi: string;
            descripcionApi: string | null;
            /** @example envios.shapi.localhost */
            hostPortal: string;
            /**
             * @description Nombre de la organización dueña de la API; el pie del portal lo muestra con su insignia.
             * @example Envíos Xelajú, S.A.
             */
            nombreOrganizacion: string;
            /** @example envios.api.shapi.localhost */
            hostApi: string;
        };
        DocumentacionPortal: {
            rutas: components["schemas"]["RutaDocumentada"][];
        };
        RutaDocumentada: {
            /** @example POST */
            metodo: string;
            /** @example /cotizaciones */
            patron: string;
            resumen: string | null;
            /** @description Markdown proveniente de la especificación OpenAPI. */
            descripcion: string | null;
            parametros: components["schemas"]["ParametroDocumentado"][];
            /** @description Ejemplo JSON de cuerpo o valores de parámetros, si la especificación lo incluye. */
            ejemploPeticion: {
                [key: string]: unknown;
            } | unknown[] | string | number | boolean | null;
            /** @description Ejemplo JSON de la primera respuesta exitosa que lo incluya. */
            ejemploRespuesta: {
                [key: string]: unknown;
            } | unknown[] | string | number | boolean | null;
            /** @example 200 */
            codigoRespuesta: number | null;
            pesoLlamadas: number;
            /**
             * Format: uri
             * @example https://envios.api.shapi.localhost/cotizaciones
             */
            urlCompleta: string;
        };
        ParametroDocumentado: {
            nombre: string;
            /** @example string */
            tipo: string;
            obligatorio: boolean;
            descripcion: string | null;
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
    obtenerConfiguracionPortal: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Configuración de la API publicada que corresponde al host. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ConfiguracionPortal"];
                };
            };
            /** @description El host no corresponde a una API publicada. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    obtenerLogoPortal: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Logotipo configurado para el portal. */
            200: {
                headers: {
                    /** @description Evita que el navegador interprete un tipo distinto al declarado. */
                    "X-Content-Type-Options"?: "nosniff";
                    /** @description Para SVG se envía `sandbox`. */
                    "Content-Security-Policy"?: string;
                    [name: string]: unknown;
                };
                content: {
                    "image/png": string;
                    "image/svg+xml": string;
                };
            };
            /** @description El host no corresponde a una API publicada o el portal no tiene logotipo. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    obtenerDocumentacionPortal: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Documentación generada desde la especificación de la API publicada. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["DocumentacionPortal"];
                };
            };
            /** @description El host no corresponde a una API publicada. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
}
