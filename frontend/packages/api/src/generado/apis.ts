export interface paths {
    "/api/apis": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Lista las APIs de la organización */
        get: operations["listarApis"];
        put?: never;
        /**
         * Registra una API
         * @description Valida el subdominio y la URL contra SSRF, prueba el origen con un GET de hasta
         *     cinco segundos y, si responde con cualquier código HTTP, guarda la API en borrador.
         *     El secreto de origen se devuelve en claro solo en esta respuesta.
         */
        post: operations["registrarApi"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{id}/especificacion": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /**
         * Carga la especificación OpenAPI de una API
         * @description Extrae sus operaciones y reconcilia las rutas existentes sin perder su configuración.
         */
        put: operations["cargarEspecificacionApi"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{id}/rutas": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Lista las rutas extraídas de una API
         * @description La pueden consultar todos los roles con permiso para ver APIs, incluido el lector (04 §3.1).
         *     Incluye el resumen de la especificación cargada, o `null` si todavía no se ha cargado.
         */
        get: operations["listarRutasApi"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{id}/rutas/exposicion": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Expone u oculta rutas de una API */
        put: operations["actualizarExposicionRutasApi"];
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
        /** @description ProblemDetails estándar, sin código de negocio. */
        ProblemaEstandar: {
            type?: string;
            title: string;
            status: number;
        };
        PeticionRegistroApi: {
            nombre: string;
            /** Format: uri */
            urlOrigen: string;
            subdominio: string;
        };
        ApiResumen: {
            /** Format: uuid */
            id: string;
            nombre: string;
            subdominio: string;
            /** @enum {string} */
            estado: "borrador" | "publicada" | "despublicada";
        };
        ListaApis: {
            elementos: components["schemas"]["ApiResumen"][];
            total: number;
            planNombre: string;
            /** @description Nulo para los planes sin límite de APIs. */
            maxApis: number | null;
        };
        ApiRegistrada: {
            /** Format: uuid */
            id: string;
            nombre: string;
            subdominio: string;
            /** @constant */
            estado: "borrador";
            /** @description Se muestra una sola vez. */
            secretoOrigen: string;
            conexionMilisegundos: number;
        };
        RutaAdministrada: {
            /** Format: uuid */
            id: string;
            /** @enum {string} */
            metodo: "GET" | "POST" | "PUT" | "PATCH" | "DELETE" | "HEAD" | "OPTIONS";
            patron: string;
            resumen?: string | null;
            descripcion?: string | null;
            expuesta: boolean;
        };
        /** @description La especificación que la API tiene cargada. No se guarda el nombre del archivo. */
        ResumenEspecificacion: {
            titulo: string;
            version: string;
            /** @example 3.0.3 */
            versionOpenApi: string;
            /** @enum {string} */
            formato: "json" | "yaml";
            /** Format: date-time */
            cargadaEn: string;
            tamanoBytes: number;
        };
        ListaRutas: {
            /** Format: uuid */
            apiId: string;
            apiNombre: string;
            elementos: components["schemas"]["RutaAdministrada"][];
            totalExpuestas: number;
            totalOcultas: number;
            /** @description Nulo si la API todavía no tiene especificación. */
            especificacion: components["schemas"]["ResumenEspecificacion"] | null;
        };
        EspecificacionCargada: {
            /** Format: uuid */
            apiId: string;
            apiNombre: string;
            titulo: string;
            descripcion?: string | null;
            version: string;
            /** @example 3.0.3 */
            versionOpenApi: string;
            /** @enum {string} */
            formato: "json" | "yaml";
            /** Format: date-time */
            cargadaEn: string;
            totalRutas: number;
            rutas: components["schemas"]["RutaAdministrada"][];
        };
        CambioExposicionRuta: {
            /** Format: uuid */
            rutaId: string;
            expuesta: boolean;
        };
        /** @description ProblemDetails de la API de control. */
        Problema: {
            type?: string;
            title: string;
            status: number;
            /** @enum {string} */
            codigo: "datos_invalidos" | "csrf" | "api_no_encontrada" | "subdominio_ocupado" | "origen_no_permitido" | "origen_inaccesible" | "especificacion_invalida";
            detalle?: {
                [key: string]: unknown;
            };
            errores?: {
                [key: string]: string[];
            };
        };
    };
    responses: {
        /** @description La API no existe o pertenece a otra organización. */
        ApiNoEncontrada: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
        /** @description Datos inválidos (`datos_invalidos`), con errores por campo. */
        DatosInvalidos: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
        /** @description No hay una sesión vigente. */
        SinSesion: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": {
                    title?: string;
                    status?: number;
                };
            };
        };
        /** @description La sesión no tiene permiso para consultar APIs. */
        SinPermiso: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": {
                    title?: string;
                    status?: number;
                };
            };
        };
        /** @description La sesión no puede configurar APIs o falló la protección CSRF (`csrf`). */
        SinPermisoOCsrf: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["ProblemaEstandar"] | components["schemas"]["Problema"];
            };
        };
    };
    parameters: {
        ApiId: string;
        /** @description Protección CSRF; el cliente del frontend la agrega siempre. */
        XRequestedWith: "shapi";
    };
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    listarApis: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description APIs visibles para la organización de la sesión y límites de su plan. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ListaApis"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermiso"];
        };
    };
    registrarApi: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF; el cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionRegistroApi"];
            };
        };
        responses: {
            /** @description API registrada; el secreto no se vuelve a mostrar. */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ApiRegistrada"];
                };
            };
            400: components["responses"]["DatosInvalidos"];
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            /** @description El subdominio ya pertenece a otra API (`subdominio_ocupado`). */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description El origen está prohibido (`origen_no_permitido`) o no respondió (`origen_inaccesible`). */
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
    cargarEspecificacionApi: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF; el cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path: {
                id: components["parameters"]["ApiId"];
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "multipart/form-data": {
                    /**
                     * Format: binary
                     * @description OpenAPI 3.0 o 3.1 en JSON o YAML, de hasta 2 MB.
                     */
                    archivo: string;
                };
            };
        };
        responses: {
            /** @description Especificación validada y rutas reconciliadas. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["EspecificacionCargada"];
                };
            };
            /** @description Falta la parte `archivo` del formulario (`datos_invalidos`, con `errores.archivo`). */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            404: components["responses"]["ApiNoEncontrada"];
            /**
             * @description Documento inválido (`especificacion_invalida`). `detalle.ubicacion` lleva la línea o la sección y
             *     `detalle.mensaje`, el motivo en español. Si el error viene de la biblioteca que lee el documento,
             *     `detalle.detalleTecnico` lleva su mensaje original, en inglés.
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
    listarRutasApi: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: components["parameters"]["ApiId"];
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Rutas en el orden de la especificación. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ListaRutas"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermiso"];
            404: components["responses"]["ApiNoEncontrada"];
        };
    };
    actualizarExposicionRutasApi: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF; el cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path: {
                id: components["parameters"]["ApiId"];
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["CambioExposicionRuta"][];
            };
        };
        responses: {
            /** @description Exposición actualizada y resumen vigente. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ListaRutas"];
                };
            };
            /**
             * @description Lote inválido (`datos_invalidos`): un elemento nulo, sin `rutaId` o sin `expuesta`, o una ruta repetida.
             *     `errores` usa la posición del elemento como clave, por ejemplo `[0].expuesta`.
             */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            /**
             * @description La API no existe o es de otra organización, o una de las rutas no es de la API (`api_no_encontrada`).
             *     No se cambia ninguna ruta.
             */
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
