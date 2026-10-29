---
id: JG-26
titulo: Correcciones de pantallas del panel y del portal (fechas, tablas, permisos y estilos)
persona: jordin
responsable: Jordin García
avance: final
prioridad: P3
estado: hecha
programada: 2026-10-27
depende_de: []
requisitos: [RF-07, RF-09, RF-10, RF-15, RF-18, RF-37, RF-38, RF-39, RNF-12]
pantallas: [A3.1, A3.3, A3.4, A3.5, A3.6, A3.7, A4.1, A6.2, B2.1, B3.1, A5.0]
---

# JG-26 · Correcciones de pantallas del panel y del portal

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P3 · **Sin dependencias**

## Objetivo
Corregir los detalles de pantallas que quedaron pendientes en el ensayo del 8 oct, en la auditoría del 3 oct y en la convergencia del 23 oct (`docs/plan/convergencia/2026-10-22.md` §4), para que la interfaz se vea correcta en el video. Jordin decidió corregirlos él (protocolo §E1). Los archivos son de EM-07, JZ-08, JG-12, DC-03 y DC-05.

Es P3: si no cabe antes del jueves 29, es la primera que queda fuera (calendario §"Qué hacer si vamos atrasados").

## Hallazgos que corrige
- **H-C25 (baja, EM-07; H-E2):** el encabezado de la tabla de A4.1 queda pegado al borde de la tarjeta (`A4-1-PlanesApi.tsx:81`, `pt-0`) y no se ve como el de A3.5.
- **H-C26 (baja, JZ-08, ADR-30):** A6.2 muestra el ciclo con `fin` («9 nov – 9 dic 2026»), mientras A6.4b y B1.4 muestran el último día con servicio («9 nov – 8 dic 2026»), como pide ADR-30.
- **H-C28 (baja, JG-12, RF-37):** ninguna prueba compara `X-Cuota-Restante` de una petición real a la compuerta con `restante` de `/api/portal/consumo`, ni prueba que B2.1 consulte de nuevo cada 10 s (`useConsumoConsumidor.ts:21`). Tampoco se prueba que B3.1 consulte cada 30 s (`B3-1-Estado.tsx:44`).
- **H-108 (baja, EM-07):** `A4-1-PlanesApi.tsx:237`: el precio de un plan de pago acepta 0 (`min="0"`); el servidor ya responde 400.
- **H-111 (baja, DC-05):** `A3-3-Especificacion.tsx:48-51`: al cargar con éxito no se invalida `['rutas-api', id]`, así que A3.4 puede abrir con las rutas viejas.
- **H-113 (baja, DC-04 y DC-05):** el lector ve en A3.3 y A3.4 «Registrar una API», la zona de carga y «Guardar rutas expuestas». 04 §3 dice que el frontend oculta las opciones no permitidas.
- **H-114 (baja, DC-05):** `A3-3-Especificacion.tsx:97`: la clase `bg-principal-claro` no existe y la zona de arrastre no se tiñe.
- **H-115 (baja, DC-03):** `frontend/apps/portal/src/layouts/MarcaPortal.tsx:10,12`: la insignia del encabezado tiene 8 px de radio, y los mockups de A5 y B2, 6 px.

## Decisión de Jordin (2026-10-28)
1. **Controles de escritura para el lector (H-113, duda 9 del 3 oct).** Aprobó ocultarlos en todo el panel, conforme a 04 §3. Las consultas conservan los datos como texto de solo lectura; perfil y soporte mantienen las operaciones que permite la matriz. El backend sigue autorizando cada operación.

## Contexto que debes leer
- `docs/specs/12-decisiones.md` ADR-30
- `docs/specs/04-roles-y-permisos.md` §3
- `docs/specs/03-requisitos.md` RF-37
- `docs/specs/11-interfaz.md` A3.3, A3.4, A4.1, A6.2 y B2.1
- Mockups correspondientes en `mockups/A3`, `mockups/A4`, `mockups/A6`, `mockups/A5` y `mockups/B2`

## Archivos que creas o modificas
- `frontend/apps/panel/src/paginas/A4-1-PlanesApi.tsx`, `A6-2-Organizaciones.tsx`, `A3-3-Especificacion.tsx` y `A3-4-*.tsx` (modificar)
- `frontend/apps/portal/src/layouts/MarcaPortal.tsx` (modificar)
- Por la decisión de Jordin sobre H-113: `frontend/apps/panel/src/paginas/A3-1-Apis.tsx`, `A3-5-ConfigRutas.tsx`, `A3-6-Dominio.tsx`, `A3-7-Portal.tsx` y `modulos/sesion/usePuedeConfigurarApi.ts`; las demás pantallas ya ocultan sus escrituras al lector o están protegidas por rol.
- Pruebas del frontend y una prueba de integración de RF-37 en `tests/Shapi.Api.Tests` o `tests/Shapi.Compuerta.Tests` (crear)

## Criterios de aceptación
1. Cada hallazgo queda corregido como lo describe su línea, contra su mockup.
2. RF-37 tiene una prueba que hace peticiones reales con una clave, consolida y comprueba que `restante` de `/api/portal/consumo` coincide con el último `X-Cuota-Restante`.

## Pruebas obligatorias
- Cada hallazgo tiene una prueba que falla sin la corrección, salvo H-C25, H-114 y H-115, que son solo de estilo: esos tres se comprueban con una captura contra el mockup (protocolo B8).

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
dotnet build Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~Shapi.Api.Tests.Consumo"
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- La revisión visual completa contra los mockups (DC-16).

## Resultado

- Corregidos H-C25, H-C26, H-C28, H-108, H-111, H-113, H-114 y H-115: A4.1 recupera el espacio superior del encabezado y exige un precio positivo para planes de pago; A6.2 muestra `fin − 1 día`; una carga OpenAPI invalida las rutas compartidas; el arrastre usa un color existente y la marca del portal tiene radio de 6 px.
- Conforme a la decisión de Jordin del 28 oct, el lector ve como texto los datos de APIs, rutas, configuración, planes, dominios y marca. Se ocultan sus controles de escritura y el secreto de origen, conservando las operaciones permitidas de perfil y soporte. Propietario y editor mantienen sus acciones. La precisión de 11 se alinea con 04 §3 y la duda 9 queda resuelta en la auditoría del 3 oct.
- RF-37 tiene una integración con compuerta Kestrel, origen HTTP real, PostgreSQL y Redis: tres peticiones de peso 5 comparan el portal con cada cabecera y, después de consolidar, comprueban las 15 llamadas y 3 peticiones. B2.1 y B3.1 tienen pruebas que adelantan los intervalos de 10 y 30 segundos y comprueban los cambios visibles; esos intervalos ya estaban implementados.
- Archivos principales: `A3-1`, `A3-3` a `A3-7`, `A4-1`, `A6-2`, `usePuedeConfigurarApi.ts`, `MarcaPortal.tsx`, las pruebas de permisos, organizaciones, estado y consumo y `ConsumoConsumidorTests.cs`. No se agregaron dependencias ni se cambiaron endpoints.
- Verificación: plan válido (89 tareas), compilación completa sin errores ni advertencias, formato completo correcto, 87 pruebas de Consumo y 537 pruebas frontend aprobadas sin omisiones; lint sin errores (11 avisos previos), tipos y builds correctos. Se confirmó la fase roja de permisos, precio y fechas, y la regresión de caché contra A3.3 de `main`. El ciclo de la prueba real se mantiene vigente para el reloj de expiración de Redis.
- Ensayo productivo aislado `shapi-jg26` con los builds actuales: propietario, lector, editor, administrador y consumidor, 21 capturas reales y 12 referencias en `.shapi/jg26/`. Verifica 20 px superiores en A4.1, tinte de arrastre, radio de 6 px en ambas marcas, consulta sin controles para el lector y perfil aún editable. Se compararon los cambios con sus mockups, respetando las precisiones de la especificación y las fechas de demostración.
