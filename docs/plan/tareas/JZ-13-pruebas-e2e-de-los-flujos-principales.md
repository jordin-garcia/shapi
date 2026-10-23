---
id: JZ-13
titulo: Pruebas E2E de los flujos principales
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P1
estado: hecha
programada: 2026-10-24
depende_de: [JZ-07]
no_antes_de: 2026-10-22
requisitos: [RNF-15]
pantallas: []
---

# JZ-13 · Pruebas E2E de los flujos principales

**Responsable:** José Pablo Zúñiga · **Avance:** final · **Prioridad:** P1 · **Depende de:** JZ-07 · **No antes del:** 2026-10-22

## Objetivo
Automatizar con Playwright los guiones de demostración 2 y 3 sobre el ambiente productivo simulado con la siembra de demostración.

## Contexto que debes leer
- `docs/plan/calendario.md` (guiones 2 y 3)
- `docs/specs/05-casos-de-uso.md`

## Archivos que creas o modificas
- `tests/e2e/tests/*.spec.ts` (crear; directorio configurado por JZ-07)
- `tests/e2e/soporte/*.ts` y `tests/e2e/playwright.config.ts` (ayudantes y orden de los escenarios)
- `docs/plan/tareas/<nuevas>.md` (una por cada falla en un módulo ajeno)

## Criterios de aceptación
1. Hay pruebas para: publicar una API de punta a punta; que un consumidor se registre, contrate con 4242…, reciba sus claves y la API responda con ellas; los 429 por límite; rotar y revocar una clave; que el proveedor vea el consumo; subir de plan de plataforma con prorrateo; el reloj de demostración con la tarjeta 0341 (gracia → suspensión → 403 → pago → activa); que el administrador suspenda una organización (403) y revierta un pago; y que el soporte atienda un caso.
2. Todas pasan en `e2e.yml`. Si una falla por un error de otro módulo, se crea una tarea P1 para su dueño.

## Pruebas obligatorias
- Las propias

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd tests/e2e && pnpm test
```

## Fuera de alcance
- Corregir el código de otros

## Resultado

- Se automatizaron los guiones 2 y 3 con Playwright contra el ambiente productivo simulado: registro y publicación completa de una API, documentación pública, registro y verificación del consumidor en Mailpit, contratación con 4242, emisión de claves una sola vez, consola real, recuperación de contraseña, ruta oculta, 429 con `Retry-After`, rotación y revocación, consumo consolidado, p95, errores y facturación, atención de soporte, suspensión administrativa, bitácora, estado de componentes y reversión de un pago.
- Un escenario independiente verifica el cambio prorrateado de Lanzamiento a Producto en un ciclo parcial, conserva la fecha de renovación y comprueba con 0341 la gracia, la suspensión, el 403 y la reactivación mediante pago sin emitir otra clave. El consumidor usa un plan anual propio para aislar la suspensión de plataforma de su renovación.
- Cada escenario crea sus propios usuarios y recursos; la siembra aporta únicamente las cuentas de administración y soporte y los orígenes. El proyecto del reloj depende del resto de pruebas, porque su avance afecta globalmente al ambiente. Los intentos de autenticación respetan el límite real y `Retry-After`; no se modifica la configuración de seguridad.
- Se detectó y registró EM-23: el registro de un proveedor en Prueba no publicaba su organización en Redis. José Pablo autorizó corregir el endpoint de Identidad y agregar la regresión en el mismo PR. La fase roja confirmó el contexto ausente en Redis y el 403 de la E2E; tras publicar la organización después de guardar, el flujo recibe 200 dentro de diez segundos.
- Verificación: ambiente productivo construido y verificado con `node infra/verificar.mjs --demo`; compilación completa sin errores ni advertencias; 268 pruebas dirigidas de Identidad correctas, sin omisiones; TypeScript estricto de las nuevas E2E correcto. `pnpm test` completó 14 pruebas en 3 min 51 s: cero fallos, cero reintentos y cero omisiones. Revisión independiente en contexto limpio, incluida una segunda revisión de los ajustes de autenticación: LISTO, sin hallazgos.
- No se cambiaron endpoints ni contratos HTTP. La especificación de datos precisa la publicación inmediata de la organización al registrarse.
- El formato completo con `dotnet format Shapi.slnx --verify-no-changes` pasó sin cambios y el plan valida 89 tareas.

Archivos principales: `tests/e2e/tests/flujos-principales.spec.ts`, `tests/e2e/tests/reloj-demo.spec.ts`, `tests/e2e/soporte/flujos.ts`, `tests/e2e/playwright.config.ts` y los archivos de Identidad enumerados en EM-23.
