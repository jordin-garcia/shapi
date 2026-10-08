export interface paths {
    "/api/miembros": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Lista los miembros y el uso del límite de la organización */
        get: operations["listarMiembros"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/miembros/invitaciones": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Invita a una persona como editor o lector */
        post: operations["invitarMiembro"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/miembros/{id}/rol": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Cambia el rol de un miembro */
        put: operations["cambiarRolMiembro"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/miembros/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post?: never;
        /** Quita a un miembro de la organización */
        delete: operations["quitarMiembro"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/invitaciones/{token}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Consulta una invitación de miembro vigente */
        get: operations["consultarInvitacionMiembro"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/invitaciones/{token}/aceptar": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Acepta una invitación y crea la cuenta del miembro */
        post: operations["aceptarInvitacionMiembro"];
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
        ListaMiembros: {
            elementos: components["schemas"]["Miembro"][];
            /** @description Miembros e invitaciones vigentes */
            total: number;
            invitacionesPendientes: number;
            organizacion: string;
            /** Format: uuid */
            usuarioActualId: string;
            plan: components["schemas"]["PlanMiembros"];
        };
        Miembro: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            usuarioId: string;
            nombre: string;
            /** Format: email */
            correo: string;
            /** @enum {string} */
            rol: "propietario" | "editor" | "lector";
            esActual: boolean;
        };
        PlanMiembros: {
            nombre: string;
            maxMiembros?: number | null;
        };
        PeticionInvitarMiembro: {
            /** Format: email */
            correo: string;
            /** @enum {string} */
            rol: "editor" | "lector";
        };
        PeticionCambiarRolMiembro: {
            /** @enum {string} */
            rol: "editor" | "lector";
        };
        InvitacionMiembro: {
            organizacion: string;
            nombrePropietario: string;
            /** Format: email */
            correo: string;
            /** @enum {string} */
            rol: "editor" | "lector";
            /** Format: date-time */
            expiraEn: string;
        };
        PeticionAceptarInvitacionMiembro: {
            nombre: string;
            contrasena: string;
        };
        Problema: {
            type?: string;
            title?: string;
            status?: number;
            codigo?: string;
            detalle?: {
                limite?: string;
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
    listarMiembros: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Miembros, cantidad total con invitaciones vigentes y plan actual */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["ListaMiembros"];
                };
            };
            /** @description Se requiere una sesión personal */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Solo el propietario puede administrar miembros */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Organización o suscripción no encontrada */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    invitarMiembro: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionInvitarMiembro"];
            };
        };
        responses: {
            /** @description Invitación creada y correo encolado */
            202: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Correo o rol inválido */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Se requiere una sesión personal */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Solo el propietario puede invitar miembros */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description La persona ya pertenece a la organización o tiene una invitación vigente */
            409: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Correo de otra organización o límite de miembros alcanzado */
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
    cambiarRolMiembro: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionCambiarRolMiembro"];
            };
        };
        responses: {
            /** @description Rol actualizado */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Rol inválido */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Se requiere una sesión personal */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Solo el propietario puede cambiar roles */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Miembro no encontrado en esta organización */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description No se puede cambiar el rol del propietario */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    quitarMiembro: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Miembro quitado */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Se requiere una sesión personal */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Solo el propietario puede quitar miembros */
            403: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Miembro no encontrado en esta organización */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description No se puede quitar al propietario */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    consultarInvitacionMiembro: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                token: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Datos de la invitación */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["InvitacionMiembro"];
                };
            };
            /** @description Invitación vencida */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    aceptarInvitacionMiembro: {
        parameters: {
            query?: never;
            header?: never;
            path: {
                token: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["PeticionAceptarInvitacionMiembro"];
            };
        };
        responses: {
            /** @description Cuenta verificada y membresía creadas */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description Nombre o contraseña inválidos */
            400: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["Problema"];
                };
            };
            /** @description Invitación vencida */
            404: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
            /** @description El correo ya pertenece a otra organización */
            422: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
}
