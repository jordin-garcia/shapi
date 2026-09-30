export interface paths {
    "/api/apis/{apiId}/claves": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Lista las claves de los consumidores de una API (A4.3)
         * @description Las claves enmascaradas, agrupadas por consumidor, con el plan, el tipo y el estado. Se pagina por consumidor
         *     (una suscripción sin finalizar por consumidor), en el orden en que contrataron. Propietario, editor o lector.
         */
        get: operations["listarClavesDeApi"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/apis/{apiId}/claves/{claveId}/revocar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Revoca la clave de un consumidor (A4.3b)
         * @description Deja la clave `revocada`, con `revocadaPor: proveedor`, y la borra de Redis: la compuerta responde 401
         *     `clave_invalida` en menos de 10 segundos. Queda en la bitácora (`clave.revocada_por_proveedor`). Revocar una
         *     clave ya revocada no cambia nada y responde igual. Propietario o editor.
         */
        post: operations["revocarClaveDeConsumidor"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/claves": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /**
         * Lista las claves del consumidor en la API del portal (B2.3)
         * @description Las claves enmascaradas de su suscripción sin finalizar. Sin suscripción, la lista está vacía.
         */
        get: operations["listarClavesDelConsumidor"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/claves/emitir": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Emite una clave nueva de un tipo (CU-13, flujo alterno)
         * @description Solo si la suscripción no tiene otra clave activa de ese tipo (por ejemplo, después de revocarla). La clave
         *     completa se devuelve una sola vez.
         */
        post: operations["emitirClave"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/claves/{claveId}/rotar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Rota una clave propia (B2.4 y B2.5)
         * @description Emite una clave nueva activa del mismo tipo y deja la anterior `rotada`, funcionando 24 horas más (`EXPIREAT`
         *     en Redis). Devuelve la clave nueva completa (única vez) y la anterior enmascarada, con la hora a la que deja de
         *     funcionar. Queda en la bitácora (`clave.rotada`). Si otra clave rotada del mismo tipo sigue dentro de sus 24
         *     horas, deja de funcionar en ese momento: nunca coexisten más de dos claves del mismo tipo (RF-27).
         */
        post: operations["rotarClave"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/portal/claves/{claveId}/revocar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /**
         * Revoca una clave propia (B2.6)
         * @description Deja la clave `revocada`, con `revocadaPor: consumidor`, y la borra de Redis: la compuerta responde 401
         *     `clave_invalida` en menos de 10 segundos. Queda en la bitácora (`clave.revocada_por_consumidor`). Revocar una
         *     clave ya revocada no cambia nada y responde igual.
         */
        post: operations["revocarClavePropia"];
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
        /** @enum {string} */
        TipoClave: "produccion" | "pruebas";
        /** @enum {string} */
        EstadoClave: "activa" | "rotada" | "revocada";
        /** @description Una clave enmascarada. */
        Clave: {
            /** Format: uuid */
            id: string;
            tipo: components["schemas"]["TipoClave"];
            /** @example shp_prod_••••7c2e */
            claveEnmascarada: string;
            estado: components["schemas"]["EstadoClave"];
            /**
             * Format: date-time
             * @description En una clave rotada, la hora a la que deja de funcionar.
             */
            expiraEn: string | null;
            /** Format: date-time */
            revocadaEn: string | null;
        };
        /** @description Una clave recién emitida. `clave` es la clave completa y no se vuelve a mostrar. */
        ClaveEmitida: {
            /** Format: uuid */
            id: string;
            tipo: components["schemas"]["TipoClave"];
            /** @example shp_prod_9Hk2wQ7rTz4Lb8Nc1Vx6Pm3Ds0 */
            clave: string;
            /** @example shp_prod_••••s0e5 */
            claveEnmascarada: string;
            /** @constant */
            estado: "activa";
        };
        ClaveRotada: components["schemas"]["ClaveEmitida"] & {
            anterior: components["schemas"]["Clave"];
        };
        PeticionEmitirClave: {
            tipo: components["schemas"]["TipoClave"];
        };
        ClavesDelConsumidor: {
            elementos: components["schemas"]["Clave"][];
        };
        /** @description Un consumidor de la API, con su plan y sus claves (una fila de A4.3). */
        ClavesDeConsumidorDeApi: {
            /** Format: uuid */
            consumidorId: string;
            /**
             * @description El nombre de la empresa del consumidor.
             * @example Mercadito Antigua
             */
            consumidor: string;
            /** @example Comercio */
            plan: string;
            claves: components["schemas"]["Clave"][];
        };
        PaginaClavesDeApi: {
            elementos: components["schemas"]["ClavesDeConsumidorDeApi"][];
            /** @description Cuántos consumidores con suscripción sin finalizar tiene la API. */
            total: number;
        };
        /** @description ProblemDetails de la API de control (convenciones §5). */
        Problema: {
            type?: string;
            title: string;
            status: number;
            /** @enum {string} */
            codigo: "datos_invalidos" | "clave_no_rotable" | "clave_activa_existente" | "csrf";
            /** @description Solo en 400. Mensajes por campo, con el nombre del campo en camelCase. */
            errores?: {
                [key: string]: string[];
            };
        };
    };
    responses: {
        /** @description Datos inválidos (`datos_invalidos`), con los errores por campo. */
        DatosInvalidos: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
        /** @description No hay una sesión vigente. Es un ProblemDetails estándar, sin `codigo`. */
        SinSesion: {
            headers: {
                [name: string]: unknown;
            };
            content?: never;
        };
    };
    parameters: {
        ApiId: string;
        ClaveId: string;
        /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
        XRequestedWith: "shapi";
    };
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    listarClavesDeApi: {
        parameters: {
            query?: {
                pagina?: number;
                tamano?: number;
            };
            header?: never;
            path: {
                apiId: components["parameters"]["ApiId"];
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Una página de consumidores con sus claves. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["PaginaClavesDeApi"];
                };
            };
            400: components["responses"]["DatosInvalidos"];
            401: components["responses"]["SinSesion"];
            /** @description El rol no puede ver las claves. */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description La API no existe o es de otra organización. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    revocarClaveDeConsumidor: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path: {
                apiId: components["parameters"]["ApiId"];
                claveId: components["parameters"]["ClaveId"];
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description La clave revocada. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["Clave"];
                };
            };
            401: components["responses"]["SinSesion"];
            /** @description El rol no puede revocar claves, o falta `X-Requested-With` (`csrf`). */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description La clave no es de un consumidor de esta API o no existe. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    listarClavesDelConsumidor: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Las claves del consumidor. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ClavesDelConsumidor"];
                };
            };
            401: components["responses"]["SinSesion"];
            /** @description La sesión no es de un consumidor. */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description El host no corresponde a una API publicada o la sesión es de otra organización. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    emitirClave: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionEmitirClave"];
            };
        };
        responses: {
            /** @description La clave nueva, completa (única vez). */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ClaveEmitida"];
                };
            };
            400: components["responses"]["DatosInvalidos"];
            401: components["responses"]["SinSesion"];
            /** @description La sesión no es de un consumidor, o falta `X-Requested-With` (`csrf`). */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description El consumidor no tiene una suscripción sin finalizar en esta API. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Ya hay una clave activa de ese tipo (`clave_activa_existente`). */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
        };
    };
    rotarClave: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path: {
                claveId: components["parameters"]["ClaveId"];
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description La clave nueva y la anterior. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ClaveRotada"];
                };
            };
            401: components["responses"]["SinSesion"];
            /** @description La sesión no es de un consumidor, o falta `X-Requested-With` (`csrf`). */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description La clave no es del consumidor o no existe. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description La clave no está activa (ya rotada o revocada) (`clave_no_rotable`). */
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
    revocarClavePropia: {
        parameters: {
            query?: never;
            header: {
                /** @description Protección CSRF (10 §1). El cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path: {
                claveId: components["parameters"]["ClaveId"];
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description La clave revocada. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["Clave"];
                };
            };
            401: components["responses"]["SinSesion"];
            /** @description La sesión no es de un consumidor, o falta `X-Requested-With` (`csrf`). */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description La clave no es del consumidor o no existe. */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
}
