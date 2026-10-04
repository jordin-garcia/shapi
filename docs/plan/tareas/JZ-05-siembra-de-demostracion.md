---
id: JZ-05
titulo: Siembra de demostración
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 2
prioridad: P1
estado: hecha
programada: 2026-10-03
depende_de: [EM-01, JZ-02, DC-05]
requisitos: [RNF-14]
pantallas: []
---

# JZ-05 · Siembra de demostración

**Responsable:** José Pablo Zúñiga · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** EM-01, JZ-02, DC-05

## Objetivo
Crear un comando que cargue todos los datos de demostración de los mockups, con fechas relativas al día en que se ejecuta, para que el sistema y los diseños coincidan en cualquier demostración.

## Contexto que debes leer
- `docs/specs/07-modelo-de-datos.md` §6 **completo**
- Todos los mockups de A3, A4, A5, A6, B1, B2 y B3 (datos que se muestran)
- `docs/specs/08-compuerta.md` §2 (hash de las claves)
- Sección Resultado de DC-05 (extracción de rutas desde la especificación)

## Archivos que creas o modificas
- `src/Shapi.Infraestructura/Siembra/Demo/**` (crear)
- `src/Shapi.Trabajador/Program.cs` (modificar: comando `sembrar-demo [--reiniciar]`)
- `docs/manual-tecnico.md` (modificar: cuentas y claves de demostración)
- `tests/**/Siembra/**`

## Criterios de aceptación
1. `dotnet run --project src/Shapi.Trabajador -- sembrar-demo` solo funciona con `SHAPI_MODO_DEMO=true`. Es idempotente: si ya existe Envíos Xelajú, no hace nada. Con `--reiniciar`, borra y recrea solo los datos de demostración.
2. Crea todo lo que dice 07 §6: el personal de la plataforma, las organizaciones con sus planes y estados (Envíos Xelajú en Producto; Agro Precios en Lanzamiento; Cafetalera en Prueba; Petén en gracia; Datos Chapines suspendida), los miembros, las APIs `envios` (publicada) y `recolecciones` (despublicada), `agro` (publicada), las rutas sacadas de la especificación de los orígenes de demostración con el peso, el límite y la caché de A3.5, los planes de A4.1 y A5.5, los consumidores con sus suscripciones, las claves, los pagos, los casos CAS-100 a CAS-104 y las entradas de la bitácora de B3.2.
3. Las fechas son **relativas a hoy**, con las mismas proporciones que los mockups (por ejemplo, el ciclo de Mercadito empezó hace 21 días). El consumo de los últimos 30 días se genera con la forma de B1.1 y las proporciones por ruta de B2.1.
4. Las claves de Mercadito Antigua son exactamente las de los mockups: `shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e` y `shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d` (SHA-256 en hex minúsculas en UTF-8). Las demás son fijas, y el manual técnico las lista en una tabla.
5. `url_origen` sale de `SHAPI_URL_ORIGEN_ENVIOS` y `SHAPI_URL_ORIGEN_AGRO` (por defecto `http://localhost:5101` y `http://localhost:5102`; en producción, los nombres de servicio).
6. Todas las cuentas usan la contraseña `Shapi2026!demo`. Al terminar, se ejecuta la resincronización de Redis (si JG-04 ya existe).

## Pruebas obligatorias
- Integración: sembrar dos veces no duplica nada; `--reiniciar` recrea
- Muestras: Envíos tiene 2 APIs, Mercadito tiene 2 claves activas y existe CAS-104

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Datos base (EM-01)

## Resultado
- Se agregó el comando `sembrar-demo [--reiniciar]`, protegido por `SHAPI_MODO_DEMO=true`, idempotente y con reinicio transaccional de los datos de demostración.
- La siembra crea el catálogo completo de 07 §6, las rutas desde las especificaciones OpenAPI, pagos, casos, bitácora, claves fijas y consumo relativo que reproduce A3–A6 y B1–B3.
- Las URLs y secretos de los orígenes son configurables; el ambiente productivo usa los nombres de servicio y al finalizar se resincroniza Redis.
- Se documentaron las cuentas y claves de demostración y se cubrieron modo demo, idempotencia, reinicio, atomicidad, muestras, fechas, métricas y datos exactos con pruebas de integración.
- Archivos principales: `src/Shapi.Infraestructura/Siembra/Demo/`, `src/Shapi.Trabajador/Program.cs`, `tests/Shapi.Api.Tests/Siembra/SiembraDemoTests.cs`, `infra/compose.prod.yml` y `docs/manual-tecnico.md`.

### Correcciones de la auditoría (2026-10-04)

Paso 4 de `docs/plan/auditoria-2026-10-03.md` (H-27 a H-35), hechas por el coordinador:
- **Orígenes en producción (H-27).** `compose.prod.yml` tomaba `SHAPI_URL_ORIGEN_*` del `.env`, que trae los de desarrollo (`localhost:5101` y `:5102`). Por eso, en el ambiente productivo, las APIs sembradas apuntaban a `localhost` y la compuerta no llegaba a los orígenes. Ahora las URL van fijas en los nombres de servicio, y `infra/verificar.mjs` comprueba el valor resuelto con un `.env` como el de ejemplo.
- **Reinicio con un pago rechazado (H-28).** EM-08 guarda el consumidor y la API de un rechazo, sin suscripción. El borrado ahora también quita esos pagos antes de borrar el consumidor y la API; antes chocaba con la FK.
- **Reinicio con actividad del personal (H-29).** Las 11 cuentas de la demostración ya no se borran, porque pueden ser autoras o responsables de casos, mensajes o reversiones en otras organizaciones (FK sin cascada). Se quitan sus sesiones, enlaces y membresías, y la siembra las restablece: nombre, contraseña, correo verificado, sin bloqueo y en su estado.
- **Fechas (H-30).** Los ciclos de plataforma siguen A6.2 y A6.3 («hoy» = 13 sep): Envíos −20, Cafetalera −16, Petén −32 y Datos −39, y la gracia de Petén termina 7 días después de su ciclo. Cada pago cubre el ciclo que empieza ese día; una renovación rechazada, el siguiente. Antes, los pagos cubrían el ciclo anterior. Se documentó en 07 §6.
- **APIs de las otras organizaciones (H-31).** Cafetalera, Petén y Datos Chapines tienen 1, 2 y 1 APIs en borrador, como cuenta A6.2.
- **Números de los casos (H-32).** Si otra organización ya tiene un número entre CAS-100 y CAS-104, los casos de la demostración toman los siguientes libres. Las pruebas comprueban que el siguiente caso que crea la aplicación después de sembrar es CAS-105, y que un caso previo no choca.
- **Comando (H-33).** `sembrar-demo` pasó a `Shapi.Trabajador/Siembra/ComandoSembrarDemo.cs`, con pruebas que comprueban que lee `SHAPI_MODO_DEMO` y `--reiniciar`, que resincroniza después de sembrar y que no lo hace si la siembra falla.
- **Fábricas del dominio (H-34).** Las suscripciones de API, los pagos de contratación y las tarjetas de los consumidores se crean con `SuscripcionApi.Crear`, `Pago.ContratacionAutorizada` y `MedioPago.CrearParaConsumidor`. Lo que no tiene fábrica en el dominio sigue con la reflexión (claves, casos, pagos y tarjetas de plataforma).
- **Cierre (H-35).** Decisiones que estaban solo en la bitácora:
  - la bitácora no se duplica al reiniciar (decisión del paso 4);
  - la clave rotada de Boutique Cayalá vence a las 24 horas de rotarse, así que se siembra rotada hace 9 horas.

  El comando correcto para el entorno E2E está en la bitácora de Jordin, con un aviso para JZ-13.
- **Decisiones del paso 4:**
  - el reinicio no repite las entradas de la bitácora;
  - los dos «hoy» de los mockups quedan en 07 §6;
  - `SHAPI_MODO_DEMO=true` por defecto en el ambiente de la exposición, anotado en el manual técnico.
