---
id: JZ-09
titulo: Cuentas de administración y soporte (A6.5)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 3
prioridad: P1
estado: hecha
programada: 2026-10-12
depende_de: [EM-04]
requisitos: [RF-42]
pantallas: [A6.5]
---

# JZ-09 · Cuentas de administración y soporte (A6.5)

**Responsable:** José Pablo Zúñiga · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** EM-04

## Objetivo
Permitir que el administrador cree, desactive y reactive las cuentas de administración y de soporte, que definen su contraseña con un enlace.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-42
- `docs/specs/05-casos-de-uso.md` CU-21
- `docs/specs/10-identidad-y-seguridad.md` §1 (enlace `definir_contrasena`)
- Mockups: `mockups/A6/Cuentas.dc.html`, `CuentasVacia.dc.html` y `mockups/A1/NuevaContrasena.dc.html` (para definir la contraseña)

## Archivos que creas o modificas
- `src/*/Administracion/**` (modificar)
- `contratos/openapi/administracion.yaml`
- `frontend/apps/panel/src/paginas/A6-5-Cuentas.tsx`
- Página pública `/definir-contrasena?token=` (crear `paginas/A6-5b-DefinirContrasena.tsx`, basada en el diseño de A1.4b)
- `tests/**`

## Criterios de aceptación
1. `GET` y `POST /api/admin/cuentas` `{nombre, correo, rol: administrador|soporte}` crean el usuario sin contraseña, con su membresía en la organización de plataforma y un token `definir_contrasena` de 7 días, y encolan el correo.
2. `POST /api/auth/definir-contrasena` `{token, contrasena}` define la contraseña, verifica el correo e inicia sesión.
3. `POST /api/admin/cuentas/{id}/desactivar` y `/activar`. Un administrador no puede desactivar su propia cuenta (422). Una cuenta desactivada no puede entrar. Bitácora `cuenta_plataforma.*`.
4. A6.5 (con su variante vacía) reproduce el mockup.

## Pruebas obligatorias
- Integración
- Vitest

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Miembros de proveedores (EM-12)

## Resultado

- Se implementaron la consulta y creación de cuentas de plataforma, activación y desactivación, y definición inicial de contraseña con sesión del personal. Las cuentas se restringen a membresías de administrador o soporte de la organización de plataforma; los usuarios de proveedores reciben 404 al intentar modificarlos.
- El enlace es de un solo uso, vence exactamente a los siete días y solo sirve para una cuenta activa sin contraseña. Su consumo y la sesión se guardan en una transacción; dos solicitudes simultáneas solo producen una sesión. La desactivación propia responde 422 y desactivar otra cuenta revoca sus sesiones.
- A6.5 muestra las variantes poblada y vacía, creación, acciones, carga, errores por campo y reintentos. La nueva página pública A6.5b conserva el diseño de A1.4b con siete días de vigencia e inicio de sesión; si solo falla consultar la sesión, el reintento no consume otra vez el enlace.
- Decisiones: los selectores de roles existentes son de solo lectura, pues RF-42 no incluye editar roles. La variante vacía conserva la cuenta inicial. El motor construye el enlace desde `token` al renderizar, de forma que la cola elimina el secreto después de enviarlo o del fallo definitivo. Se documentó en 10 §1/§6 y 11 §4.
- Archivos principales: `Administracion/CuentasPlataforma.cs`, `Administracion/RepositorioCuentasPlataforma.cs`, `Administracion/EndpointsCuentas.cs`, contrato `administracion.yaml`, páginas `A6-5-Cuentas.tsx` y `A6-5b-DefinirContrasena.tsx` y sus pruebas. El usuario autorizó los cambios necesarios en `Usuario.cs`, `Token.cs`, `rutas.tsx`, `paginasDiferidas.tsx` y `MotorPlantillasCorreo.cs`.
- Verificación local: compilación sin errores ni advertencias; formato correcto; 152 pruebas de integración y correo del área afectada, 58 de dominio y 361 del frontend correctas. Lint y TypeScript correctos, compilación de ambas aplicaciones correcta. Plan válido (75 tareas). Las pruebas iniciales fallaron por la pantalla pendiente y las operaciones ausentes; la regresión de correo falló antes de construir el enlace desde el token.
- Verificación visual: Chromium a 1440 × 900 contra la API real y una base aislada `shapi_jz09_visual`; capturas de A6.5 poblada/vacía y A6.5b normal/enlace inválido comparadas con los tres mockups. El flujo real creó tres cuentas, desactivó una y definió la contraseña de soporte, con redirección a `/admin/casos`.
- La revisión independiente detectó la persistencia del token dentro de la URL y el correo usado como nombre del actor. Ambos se corrigieron y quedaron cubiertos por pruebas. Segunda revisión en contexto limpio: LISTO, sin hallazgos obligatorios ni opcionales.
