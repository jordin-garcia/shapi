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
    "/api/apis/{id}/configuracion-rutas": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Configura el consumo de las rutas de una API */
        put: operations["configurarRutasApi"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{id}/publicar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Publica una API */
        post: operations["publicarApi"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{id}/despublicar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Despublica una API sin borrar sus datos */
        post: operations["despublicarApi"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{id}/dominio": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta las direcciones, el secreto enmascarado y el dominio propio */
        get: operations["obtenerDominioApi"];
        put?: never;
        /** Conecta o reemplaza el dominio propio */
        post: operations["conectarDominioApi"];
        /** Desconecta el dominio propio */
        delete: operations["eliminarDominioApi"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{id}/dominio/simular-registro": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Crea el CNAME en el DNS simulado */
        post: operations["simularRegistroDominio"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{id}/dominio/verificar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Verifica el CNAME del dominio propio */
        post: operations["verificarDominioApi"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{id}/secreto/regenerar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Regenera el secreto de origen */
        post: operations["regenerarSecretoOrigen"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/interno/tls/autorizar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Autoriza la emisión bajo demanda de TLS para un dominio verificado */
        get: operations["autorizarTlsDominio"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{id}/portal": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta la personalización del portal y los datos de su vista previa */
        get: operations["obtenerPersonalizacionPortal"];
        /** Actualiza el nombre, el color principal y el texto de bienvenida del portal */
        put: operations["actualizarPersonalizacionPortal"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{id}/portal/logo": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Descarga el logotipo del portal de una API desde el panel */
        get: operations["obtenerLogoPortalApi"];
        /** Sube o reemplaza el logotipo del portal de una API */
        put: operations["cargarLogoPortalApi"];
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
            /** @description Nulo cuando la ruta no tiene límite propio. */
            limiteMinuto: number | null;
            cacheSegundos: number;
            pesoLlamadas: number;
        };
        ConfiguracionRuta: {
            /** Format: uuid */
            rutaId: string;
            /** @description Se omite o se envía nulo para quitar el límite propio. */
            limiteMinuto?: number | null;
            /** @description Solo puede ser mayor que cero en rutas GET. */
            cacheSegundos: number;
            pesoLlamadas: number;
        };
        EstadoPublicacionApi: {
            /** Format: uuid */
            id: string;
            /** @enum {string} */
            estado: "publicada" | "despublicada";
            /** @description Fecha de la publicación más reciente; se conserva al despublicar. */
            publicadaEn: string | null;
            /** Format: uri */
            urlApi: string;
            /** Format: uri */
            urlPortal: string;
        };
        PeticionDominioPropio: {
            /** @example api.enviosxelaju.localhost */
            dominio: string;
        };
        EstadoDominioPropio: {
            /** Format: uuid */
            apiId: string;
            apiNombre: string;
            /** Format: uri */
            urlPortal: string;
            /** Format: uri */
            urlApi: string;
            secretoEnmascarado: string;
            /** @description Nombre del plan de plataforma vigente; null si no hay suscripción. */
            planNombre: string | null;
            /** @description Si el plan vigente permite conectar un dominio propio. */
            dominioPropioPermitido: boolean;
            modoSimulado: boolean;
            dominio: string | null;
            destinoCname: string | null;
            /** @enum {string|null} */
            estado: "pendiente" | "verificado" | "fallido" | null;
            motivo: string | null;
            verificadoEn: string | null;
        };
        SecretoOrigenRegenerado: {
            /** @description Se muestra una sola vez. */
            secretoOrigen: string;
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
        PeticionPersonalizacionPortal: {
            nombre?: string | null;
            color: string;
            bienvenida?: string | null;
        };
        RutaVistaPreviaPortal: {
            metodo: string;
            patron: string;
            resumen?: string | null;
            descripcion?: string | null;
            /** @description Objeto */
            ejemploPeticion?: unknown;
            /** @description Objeto */
            ejemploRespuesta?: unknown;
            codigoRespuesta?: number | null;
            pesoLlamadas: number;
        };
        PlanVistaPreviaPortal: {
            /** Format: uuid */
            id: string;
            nombre: string;
            descripcion: string;
            precio: number;
            esGratuito: boolean;
            vigenciaDias: number;
            cuotaLlamadas: number;
            limiteMinuto: number;
        };
        PersonalizacionPortal: {
            /** Format: uuid */
            apiId: string;
            apiNombre: string;
            nombreOrganizacion: string;
            /** Format: uri */
            urlPortal: string;
            nombre: string | null;
            color: string;
            bienvenida: string | null;
            tieneLogo: boolean;
            urlLogo: string | null;
            descripcionApi: string | null;
            rutas: components["schemas"]["RutaVistaPreviaPortal"][];
            planes: components["schemas"]["PlanVistaPreviaPortal"][];
        };
        /** @description ProblemDetails de la API de control. */
        Problema: {
            type?: string;
            title: string;
            status: number;
            /** @enum {string} */
            codigo: "datos_invalidos" | "csrf" | "api_no_encontrada" | "subdominio_ocupado" | "origen_no_permitido" | "origen_inaccesible" | "especificacion_invalida" | "correo_no_verificado" | "publicacion_incompleta" | "plan_sin_dominio_propio" | "limite_del_plan" | "logo_invalido";
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
            /** @description El origen está prohibido (`origen_no_permitido`), no respondió (`origen_inaccesible`) o se alcanzó el límite de APIs del plan (`limite_del_plan`, `detalle.limite = apis`). */
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
    configurarRutasApi: {
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
                "application/json": components["schemas"]["ConfiguracionRuta"][];
            };
        };
        responses: {
            /** @description Configuración guardada; si la API está publicada, también se republicó en Redis. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ListaRutas"];
                };
            };
            400: components["responses"]["DatosInvalidos"];
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            404: components["responses"]["ApiNoEncontrada"];
        };
    };
    publicarApi: {
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
        requestBody?: never;
        responses: {
            /** @description API publicada y configuración enviada a la compuerta. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["EstadoPublicacionApi"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            404: components["responses"]["ApiNoEncontrada"];
            /** @description Correo sin verificar o configuración incompleta. */
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
    despublicarApi: {
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
        requestBody?: never;
        responses: {
            /** @description API despublicada y configuración reenviada a la compuerta. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["EstadoPublicacionApi"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            404: components["responses"]["ApiNoEncontrada"];
        };
    };
    obtenerDominioApi: {
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
            /** @description Configuración de dominios de la API. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["EstadoDominioPropio"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermiso"];
            404: components["responses"]["ApiNoEncontrada"];
        };
    };
    conectarDominioApi: {
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
                "application/json": components["schemas"]["PeticionDominioPropio"];
            };
        };
        responses: {
            /** @description Dominio pendiente de verificación. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["EstadoDominioPropio"];
                };
            };
            /** @description Dominio inválido, reservado para Shapi o conectado a otra API (`datos_invalidos`). */
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
            /** @description El plan no incluye dominio propio (`plan_sin_dominio_propio`). */
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
    eliminarDominioApi: {
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
        requestBody?: never;
        responses: {
            /** @description Dominio desconectado. */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            404: components["responses"]["ApiNoEncontrada"];
        };
    };
    simularRegistroDominio: {
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
        requestBody?: never;
        responses: {
            /** @description Registro simulado creado. */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            404: components["responses"]["ApiNoEncontrada"];
        };
    };
    verificarDominioApi: {
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
        requestBody?: never;
        responses: {
            /** @description Resultado de la verificación. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["EstadoDominioPropio"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            404: components["responses"]["ApiNoEncontrada"];
        };
    };
    regenerarSecretoOrigen: {
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
        requestBody?: never;
        responses: {
            /** @description El secreto nuevo se muestra únicamente en esta respuesta. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["SecretoOrigenRegenerado"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            404: components["responses"]["ApiNoEncontrada"];
        };
    };
    autorizarTlsDominio: {
        parameters: {
            query: {
                domain: string;
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description El dominio está verificado. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description El dominio no está verificado o la petición no viene de una red privada. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    obtenerPersonalizacionPortal: {
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
            /** @description Personalización actual del portal y datos para la vista previa de inicio. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PersonalizacionPortal"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermiso"];
            404: components["responses"]["ApiNoEncontrada"];
        };
    };
    actualizarPersonalizacionPortal: {
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
                "application/json": components["schemas"]["PeticionPersonalizacionPortal"];
            };
        };
        responses: {
            /** @description Personalización guardada. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PersonalizacionPortal"];
                };
            };
            400: components["responses"]["DatosInvalidos"];
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            404: components["responses"]["ApiNoEncontrada"];
        };
    };
    obtenerLogoPortalApi: {
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
            /** @description Logotipo en PNG o SVG (`Content-Security-Policy: sandbox` para SVG). */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "image/png": string;
                    "image/svg+xml": string;
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermiso"];
            404: components["responses"]["ApiNoEncontrada"];
        };
    };
    cargarLogoPortalApi: {
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
                     * @description Imagen PNG o SVG de hasta 512 KB.
                     */
                    archivo: string;
                };
            };
        };
        responses: {
            /** @description Logotipo guardado y personalización vigente. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PersonalizacionPortal"];
                };
            };
            401: components["responses"]["SinSesion"];
            403: components["responses"]["SinPermisoOCsrf"];
            404: components["responses"]["ApiNoEncontrada"];
            /** @description Logotipo inválido (`logo_invalido`): ausente, mayor de 512 KB o con formato distinto de PNG o SVG. */
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
}
