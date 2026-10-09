export interface paths {
    "/api/admin/organizaciones": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: operations["listarOrganizacionesAdministracion"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/organizaciones/{id}/suspender": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: components["parameters"]["OrganizacionId"];
            };
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: operations["suspenderOrganizacion"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/admin/organizaciones/{id}/reactivar": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: components["parameters"]["OrganizacionId"];
            };
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: operations["reactivarOrganizacion"];
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
        OrganizacionAdministracion: {
            /** Format: uuid */
            id: string;
            nombre: string;
            /** Format: email */
            propietarioCorreo: string;
            numeroApis: number;
            numeroApisPublicadas: number;
            numeroConsumidores: number;
            plan: string;
            /** Format: date-time */
            cicloInicio: string;
            /** Format: date-time */
            cicloFin: string;
            /** @enum {string} */
            estado: "activa" | "en_gracia" | "suspendida";
            suspendidaAdministrativamente: boolean;
            motivoSuspension?: string | null;
        };
        SuspensionOrganizacion: {
            motivo: string;
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
        /** @description El motivo de suspension no es valido. */
        DatosInvalidos: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
        /** @description La organizacion no existe o no es proveedora. */
        OrganizacionNoEncontrada: {
            headers: {
                [name: string]: unknown;
            };
            content: {
                "application/problem+json": components["schemas"]["Problema"];
            };
        };
    };
    parameters: {
        OrganizacionId: string;
        /** @description Proteccion CSRF; el cliente del frontend la agrega siempre. */
        XRequestedWith: "shapi";
    };
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    listarOrganizacionesAdministracion: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Organizaciones proveedoras con su suscripcion de plataforma. */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["OrganizacionAdministracion"][];
                };
            };
        };
    };
    suspenderOrganizacion: {
        parameters: {
            query?: never;
            header: {
                /** @description Proteccion CSRF; el cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path: {
                id: components["parameters"]["OrganizacionId"];
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["SuspensionOrganizacion"];
            };
        };
        responses: {
            /** @description Organizacion suspendida administrativamente. */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            400: components["responses"]["DatosInvalidos"];
            404: components["responses"]["OrganizacionNoEncontrada"];
        };
    };
    reactivarOrganizacion: {
        parameters: {
            query?: never;
            header: {
                /** @description Proteccion CSRF; el cliente del frontend la agrega siempre. */
                "X-Requested-With": components["parameters"]["XRequestedWith"];
            };
            path: {
                id: components["parameters"]["OrganizacionId"];
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Suspension administrativa retirada. */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            404: components["responses"]["OrganizacionNoEncontrada"];
        };
    };
}
