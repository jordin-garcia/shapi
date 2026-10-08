export interface paths {
    "/api/casos": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: operations["listarCasosProveedor"];
        put?: never;
        post: operations["abrirCasoProveedor"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/casos/{numero}": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        get: operations["consultarCasoProveedor"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/casos/{numero}/mensajes": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: operations["responderCasoProveedor"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/casos": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: operations["listarCasosAdministracion"];
        put?: never;
        post: operations["abrirCasoAdministracion"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/casos/organizaciones": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: operations["listarOrganizacionesParaCaso"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/casos/{numero}": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        get: operations["consultarCasoAdministracion"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/casos/{numero}/asignar": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: operations["asignarCaso"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/casos/{numero}/mensajes": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: operations["responderCasoAdministracion"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/casos/{numero}/cerrar": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: operations["cerrarCaso"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/casos/{numero}/organizacion": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        get: operations["consultarOrganizacionDelCaso"];
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
        AbrirCasoProveedor: {
            asunto: string;
            /** Format: uuid */
            apiId?: string | null;
            descripcion: string;
        };
        AbrirCasoAdministracion: components["schemas"]["AbrirCasoProveedor"] & {
            /** Format: uuid */
            organizacionId: string;
        };
        NuevoMensaje: {
            cuerpo: string;
        };
        CasoListado: {
            numero: number;
            asunto: string;
            apiNombre?: string | null;
            /** @enum {string} */
            estado: "abierto" | "cerrado";
            /** Format: date-time */
            creadoEn: string;
            respuestas: number;
        };
        CasoListadoAdministracion: components["schemas"]["CasoListado"] & {
            organizacion: string;
        };
        CasoDetalle: {
            numero: number;
            asunto: string;
            /** @enum {string} */
            estado: "abierto" | "cerrado";
            organizacion: string;
            apiNombre?: string | null;
            creadoPor: string;
            asignadoA?: string | null;
            /** Format: date-time */
            creadoEn: string;
            mensajes: components["schemas"]["MensajeCaso"][];
        };
        MensajeCaso: {
            /** Format: uuid */
            id: string;
            autor: string;
            /** @enum {string} */
            rol: "propietario" | "editor" | "lector" | "administrador" | "soporte";
            cuerpo: string;
            /** Format: date-time */
            creadoEn: string;
            esPersonalPlataforma: boolean;
        };
        ResumenOrganizacionCaso: {
            organizacion: string;
            apiAfectada?: string | null;
            plan: string;
            /** Format: date-time */
            cicloInicio: string;
            /** Format: date-time */
            cicloFin: string;
            /** @enum {string} */
            estado: "activa" | "en_gracia" | "suspendida";
            numeroApis: number;
            numeroConsumidores: number;
            dominioPropio?: string | null;
            /** @enum {string|null} */
            verificacionDominio?: "pendiente" | "verificado" | "fallido" | null;
        };
        OpcionOrganizacionCaso: {
            /** Format: uuid */
            id: string;
            nombre: string;
            apis: components["schemas"]["OpcionApiCaso"][];
        };
        OpcionApiCaso: {
            /** Format: uuid */
            id: string;
            nombre: string;
        };
        Problema: {
            type?: string;
            title: string;
            status: number;
            codigo: string;
            errores?: {
                [key: string]: string[];
            };
        };
    };
    responses: {
        /** @description Datos invalidos. */
        DatosInvalidos: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
        /** @description Caso u organizacion inexistente. */
        NoEncontrado: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
        /** @description El caso ya esta cerrado. */
        CasoCerrado: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
    };
    parameters: {
        NumeroCaso: number;
    };
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    listarCasosProveedor: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Casos de la organizacion de la sesion. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["CasoListado"][];
                };
            };
        };
    };
    abrirCasoProveedor: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["AbrirCasoProveedor"];
            };
        };
        responses: {
            /** @description Caso abierto. */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["CasoDetalle"];
                };
            };
            400: components["responses"]["DatosInvalidos"];
        };
    };
    consultarCasoProveedor: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Caso con su conversacion. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["CasoDetalle"];
                };
            };
            404: components["responses"]["NoEncontrado"];
        };
    };
    responderCasoProveedor: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["NuevoMensaje"];
            };
        };
        responses: {
            /** @description Mensaje agregado. */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["MensajeCaso"];
                };
            };
            400: components["responses"]["DatosInvalidos"];
            404: components["responses"]["NoEncontrado"];
            422: components["responses"]["CasoCerrado"];
        };
    };
    listarCasosAdministracion: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Todos los casos de soporte. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["CasoListadoAdministracion"][];
                };
            };
        };
    };
    abrirCasoAdministracion: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["AbrirCasoAdministracion"];
            };
        };
        responses: {
            /** @description Caso registrado a nombre de la organizacion. */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["CasoDetalle"];
                };
            };
            400: components["responses"]["DatosInvalidos"];
            404: components["responses"]["NoEncontrado"];
        };
    };
    listarOrganizacionesParaCaso: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Organizaciones proveedoras y sus APIs para registrar un caso. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["OpcionOrganizacionCaso"][];
                };
            };
        };
    };
    consultarCasoAdministracion: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Caso con su conversacion. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["CasoDetalle"];
                };
            };
            404: components["responses"]["NoEncontrado"];
        };
    };
    asignarCaso: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Caso asignado al usuario de la sesion. */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            404: components["responses"]["NoEncontrado"];
        };
    };
    responderCasoAdministracion: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["NuevoMensaje"];
            };
        };
        responses: {
            /** @description Mensaje agregado. */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["MensajeCaso"];
                };
            };
            400: components["responses"]["DatosInvalidos"];
            404: components["responses"]["NoEncontrado"];
            422: components["responses"]["CasoCerrado"];
        };
    };
    cerrarCaso: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Caso cerrado. */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            404: components["responses"]["NoEncontrado"];
        };
    };
    consultarOrganizacionDelCaso: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                numero: components["parameters"]["NumeroCaso"];
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Resumen de la organizacion, solo lectura. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ResumenOrganizacionCaso"];
                };
            };
            404: components["responses"]["NoEncontrado"];
        };
    };
}
