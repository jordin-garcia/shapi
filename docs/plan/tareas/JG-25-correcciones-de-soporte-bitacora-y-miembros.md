---
id: JG-25
titulo: Correcciones de soporte, bitácora y miembros
persona: jordin
responsable: Jordin García
avance: final
prioridad: P2
estado: pendiente
programada: 2026-10-26
depende_de: []
requisitos: [RF-06, RF-40, RF-41, RF-42]
pantallas: [A4.2, A6.4, A7.1, A7.2, B3.2]
---

# JG-25 · Correcciones de soporte, bitácora y miembros

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P2 · **Sin dependencias**

## Objetivo
Que la bitácora muestre el nombre de quien hizo cada acción con textos correctos, que los casos de soporte notifiquen a quien corresponde y que A4.2 y A7.1 respondan como pide 11 §4. Son hallazgos de la convergencia del 23 oct y de los ensayos del 8 y el 18 oct (`docs/plan/convergencia/2026-10-22.md` §4). Jordin decidió corregirlos él (protocolo §E1). Los archivos son de JZ-08, JZ-10, EM-07, EM-09, EM-12, EM-14, DC-04, DC-05 y DC-14.

## Hallazgos que corrige
- **H-C21 (media; H-E7):** la bitácora guarda un rol o un correo en `actor_nombre`, contra 07 y 10 §7. Se vio en la base el 23 oct:
  - «Propietario» en `suscripcion_plataforma.contratada` y `.cambiada`;
  - «Usuario» en `plan_api.creado`;
  - «Administrador» en `pago.revertido`;
  - el correo en `organizacion.suspendida` y `.reactivada`, `caso.abierto` y `caso.cerrado`.

  El nombre sale del *claim* `ClaimTypes.Name`, que la sesión no trae, con un respaldo fijo: `PlataformaSuscripcionesApi.cs:478` («Propietario»), `Planes/Endpoints.cs:84` («Administrador»), `:104`, `:119` y `:133` («Usuario»), y `Soporte/Endpoints.cs:162-163` (el correo). Lo mismo pasa en los endpoints de A6.2 y A6.3.
- **H-C20 (baja; H-E8 y H-E16):** textos sin tilde que se ven en B3.2 y en los casos:
  - `GestionarCasos.cs`: «Abrio el caso CAS-105: …?.» (:89) y «Cerro el caso» (:235), además de «maximo», «descripcion», «organizacion» y «El caso esta cerrado» (:58, :110, :115, :120, :176 y :182);
  - `Caso.cs:34` y `:49`.

  10 §7 usa «Abrió el caso CAS-104 de <organización>». En la bitácora también apareció «la API API de Cotización de Envíos».
- **H-104 (baja, de la auditoría del 3 oct):** unas descripciones de la bitácora terminan con punto y otras no: `GestionarPlanes.cs:83`, `:221` y `:271`; `RegistrarApi.cs:122`; `GestionarEspecificacion.cs:242` (líneas del 23 oct; la auditoría citaba otras).
- **H-C17 (media, JZ-10, RF-40):** ninguna prueba comprueba el correo **hacia el soporte** cuando responde el proveedor (`NotificadorCasos`, `GestionarCasos.cs:252-271`), y ninguna prueba del backend usa el rol administrador en los casos.
- **H-C18 (media, JZ-10 y JZ-09):** `RepositorioSoporte.cs:232-252` (`DestinatarioProveedor`) y `:254-276` (`DestinatarioPlataforma`) no filtran por `usuario.Estado == Activo`. Una cuenta de soporte desactivada (RF-42) puede seguir recibiendo correos o ser la elegida por omisión.
- **H-C19 (baja, JZ-10):** se puede asignar un caso cerrado: `Caso.cs:85-89` (`Asignar`) y `GestionarCasos.cs:193-208` no revisan su estado.
- **H-C22 (baja, JZ-10; H-E13):** al abrir un caso en A7.1, la lista no se actualiza hasta recargar.
- **H-C23 (baja, JZ-10; H-E9):** al abrir un caso sin asignar, se envía «Nueva respuesta en su caso» a la persona de soporte más antigua (`GestionarCasos.cs:258` y `RepositorioSoporte.cs:273-275`). RF-40 y 10 §6 no dicen a quién avisar.
- **H-C24 (baja, EM-12; H-E4):**
  - enviar una invitación en A4.2 no muestra el aviso «Acción confirmada» de 11 §4 (`A4-2-Miembros.tsx` no usa `Toast`);
  - el error de :34 es fijo y no distingue `limite_del_plan` de `correo_en_otra_organizacion`;
  - `MiembrosApi.cs:117`, `:282`, `:301` y `:338` escriben los códigos a mano en vez de usar `CodigosError`.

## Decisiones de Jordin (27 oct 2026)
1. **A quién avisar al abrir un caso sin asignar (H-C23).** Enviar un correo «Nuevo caso» a todas las cuentas de soporte activas, y ninguno a los administradores. Si el caso ya tiene asignado, solo a esa persona activa.
2. **Punto final en la bitácora (H-104, duda 6 del 3 oct).** Todas las descripciones llevan punto final. Se precisa en 10 §7; esta decisión prevalece sobre los ejemplos anteriores y el mockup B3.2.

## Contexto que debes leer
- `docs/specs/10-identidad-y-seguridad.md` §6 (correos) y §7 (bitácora)
- `docs/specs/07-modelo-de-datos.md` (tabla `bitacora`)
- `docs/specs/03-requisitos.md` RF-06, RF-40, RF-41 y RF-42
- `docs/specs/11-interfaz.md` §4 (avisos) y las precisiones de A4.2, A6.4, A7.1, A7.2 y B3.2
- Mockups: `mockups/B3/Bitacora*.dc.html`, `mockups/A7/*.dc.html`, `mockups/A6/Casos*.dc.html` y `mockups/A4/Miembros*.dc.html`

## Archivos que creas o modificas
- Los endpoints y casos de uso que registran en la bitácora (`src/Shapi.Api/**`, `src/Shapi.Aplicacion/**`), con un solo lugar que resuelva el nombre del actor desde la base, sin repetirlo en cada endpoint (modificar)
- `src/Shapi.Aplicacion/Soporte/GestionarCasos.cs`, `src/Shapi.Dominio/Soporte/Caso.cs` y `src/Shapi.Infraestructura/Soporte/RepositorioSoporte.cs` (modificar)
- `src/Shapi.Api/Organizaciones/MiembrosApi.cs` (modificar)
- `frontend/apps/panel/src/paginas/A4-2-Miembros.tsx` y `A7-1-Casos.tsx` (modificar)
- Pruebas del backend y del frontend (crear o modificar)
- `docs/specs/10-identidad-y-seguridad.md` §6 (según la decisión 1)

## Criterios de aceptación
1. **H-C21.** Toda fila de la bitácora tiene en `actor_nombre` el nombre de la persona, o «Sistema» si la acción la hace el trabajador. Nunca un rol ni un correo (RF-41).
2. **H-C20 y H-104.** Los textos de la bitácora y de los casos llevan tildes y siguen los ejemplos de 10 §7. El punto final se resuelve según la decisión 2, igual en todas las descripciones. «API» no se repite.
3. **H-C17.** Hay pruebas del correo al soporte cuando responde el proveedor y de las acciones de los casos con el rol administrador (RF-40).
4. **H-C18.** Una cuenta de plataforma desactivada no recibe correos de casos ni se elige como destinataria (RF-42).
5. **H-C19.** Asignar un caso cerrado responde 422 `caso_cerrado` (RF-40).
6. **H-C22.** Al abrir un caso, A7.1 lo muestra en la lista sin recargar.
7. **H-C23.** Según la decisión 1, precisado en 10 §6.
8. **H-C24.** A4.2 muestra «Acción confirmada» al invitar y el mensaje del error que devolvió el servidor. `MiembrosApi.cs` usa `CodigosError`.

## Pruebas obligatorias
- Cada hallazgo tiene una prueba que falla sin la corrección, con el código del requisito en el nombre. Para H-C21, una prueba que recorre las acciones de 10 §7 que se pueden hacer desde la API y comprueba que `actor_nombre` es el nombre de la persona (RF-41).

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
dotnet build Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
dotnet test tests/Shapi.Api.Tests
dotnet test tests/Shapi.Dominio.Tests
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```
Además, en el ambiente productivo simulado (protocolo B8): abrir un caso desde A7.1, responderlo y cerrarlo desde A6.4b, y revisar B3.2 y Mailpit.

## Fuera de alcance
- Corregir las filas viejas de la bitácora: no admite UPDATE (RF-41). Un ambiente levantado desde cero, con volúmenes limpios, ya no las tiene.
