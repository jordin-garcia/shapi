---
id: EM-06
titulo: Pasarela de pagos simulada
persona: emilio
responsable: Emilio Méndez
avance: 2
prioridad: P1
estado: hecha
depende_de: [JG-01]
requisitos: [RF-20]
pantallas: []
---

# EM-06 · Pasarela de pagos simulada

**Responsable:** Emilio Méndez · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** JG-01

## Objetivo
Implementar `IPasarelaPagos` y su simulación con validación de Luhn, detección de la marca, tarjetas de prueba, tokens y reembolsos.

## Contexto que debes leer
- `docs/specs/09-cobros-y-suscripciones.md` §1 y §2 **completos**
- `docs/specs/12-decisiones.md` ADR-12 y ADR-13

## Archivos que creas o modificas
- `src/Shapi.Aplicacion/Pagos/IPasarelaPagos.cs` (crear)
- `src/Shapi.Infraestructura/Pagos/PasarelaSimulada.cs` (crear)
- `src/Shapi.Api/Modulos/PagosModulo.cs` (modificar: registro)
- `tests/Shapi.Dominio.Tests/Pagos/**` o `tests/Shapi.Api.Tests/Pagos/**`

## Criterios de aceptación
1. `TokenizarAsync` valida el número (13 a 19 dígitos y Luhn → `numero_invalido`), la marca por BIN (Visa, Mastercard o American Express → si no, `marca_no_soportada`), el vencimiento (`tarjeta_vencida`) y el CVV (`cvv_invalido`), y devuelve `tok_sim_{uuid}` o, para las tarjetas especiales, `tok_sim_{últimos 4}_{uuid}` (por ejemplo, `tok_sim_0341_{uuid}`), junto con la marca, los últimos 4 y el titular.
2. `CobrarAsync` aplica la tabla de tarjetas de prueba de 09 §2: 4242… y 5555…4444 se aprueban, 0002 y 0069 se rechazan con su motivo, y 0341 se aprueba al contratar y se rechaza en las renovaciones (`esRenovacion=true`). Cualquier otra tarjeta válida se aprueba. Devuelve la referencia `ch_sim_{uuid}`.
3. `ReembolsarAsync` devuelve `re_sim_{uuid}`.
4. Con `SHAPI_PASARELA_FALLA=true`, todo falla con `pasarela_no_disponible`. La demora simulada es de 300 a 800 ms y se configura con `Pagos:DemoraMs` (0 en las pruebas).
5. Nunca se registra el número completo ni el CVV.

## Pruebas obligatorias
- Unitarias de cada fila de la tabla de tarjetas de prueba, de Luhn, de las marcas y de los vencimientos

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Suscripciones y pagos en la base de datos (EM-08, EM-09)

## Resultado

Lo terminó Jordin (coordinador, protocolo §E4) el 27 de septiembre, a partir del PR #14 de Emilio Méndez, que no tenía pruebas y tenía hallazgos obligatorios de la revisión automática (paso 14 de `docs/plan/auditoria-2026-09-25.md`, H-103 a H-105 y H-119). Se conservaron los *commits* de Emilio y el PR #14 se cerró con un enlace al nuevo.

- **Interfaz** (`src/Shapi.Aplicacion/Pagos/`): `IPasarelaPagos` con la firma de 09 §2, `DatosTarjeta` (número, mes y año de vencimiento, CVV y titular) y los resultados `ResultadoTokenizacion`, `ResultadoCobro` y `ResultadoReembolso`, con `Exitoso`, `Referencia` o `Token` y el código de `Error`. `DatosTarjeta` es una clase y no un `record`, para que su `ToString()` no muestre el número ni el CVV.
- **`PasarelaSimulada`** (`src/Shapi.Infraestructura/Pagos/`), registrada en `PagosModulo`:
  - valida en el orden de la tabla de 09 §2: número (13 a 19 dígitos ASCII, sin espacios ni guiones, y Luhn), marca por el BIN, vencimiento y CVV (solo dígitos: 3, o 4 si es American Express);
  - reconoce las tarjetas de prueba por el **número completo** y guarda su comportamiento en el token (`tok_sim_0002_`, `tok_sim_0069_` y `tok_sim_0341_`). No guarda nada en memoria;
  - las referencias son `ch_sim_{uuid}` y `re_sim_{uuid}`, con el UUID en minúsculas y con guiones;
  - con `SHAPI_PASARELA_FALLA=true` (se lee de `IConfiguration`, que incluye las variables de entorno), las tres operaciones responden `pasarela_no_disponible`;
  - la demora es al azar entre 300 y 800 ms si no se configura `Pagos:DemoraMs`; si se configura, es ese valor, y 0 la quita;
  - no registra nada, así que nunca escribe el número completo ni el CVV (criterio 5).
- **Correcciones sobre el PR #14:**
  - no había ninguna prueba (el último *commit* las borró);
  - las tarjetas especiales se detectaban por los últimos 4 dígitos, así que `4000 0000 0018 0002` se rechazaba;
  - `0002` y `0069` solo se recordaban en un diccionario estático, que el Trabajador no comparte;
  - sin `Pagos:DemoraMs` no había demora, y el valor configurado se ignoraba;
  - el CVV aceptaba signos y espacios (`+12`);
  - se borró `generar_pagos.py`, un script suelto con una ruta local.
- **Pruebas:** 59 casos en `tests/Shapi.Api.Tests/Pagos/PasarelaSimuladaTests.cs` (RF-20, con NSubstitute para `IReloj`): cada fila de la tabla de tarjetas de prueba al contratar y al renovar, el token de las especiales con otra instancia (como el Trabajador), otras tarjetas con los mismos últimos 4, Luhn, longitudes de 12, 13, 19 y 20, las tres marcas y los límites de sus BIN, `marca_no_soportada`, vencimientos (incluido el cambio de mes en Guatemala), `cvv_invalido`, los formatos `tok_sim_`, `ch_sim_` y `re_sim_`, `pasarela_no_disponible` en las tres operaciones y la demora.
- **Decisiones** (de Jordin, 27 sep), agregadas a 09 §2:
  - las tres tarjetas especiales llevan su comportamiento en el token, no solo `0341`, porque las renovaciones las cobra el Trabajador (EM-09), que es otro proceso;
  - `Pagos:DemoraMs` fija la demora; sin el valor, es al azar entre 300 y 800 ms;
  - el «mes actual» del vencimiento es el de America/Guatemala;
  - `0341` se rechaza en las renovaciones con `fondos_insuficientes`.
- **Contradicción corregida (H-119):** las tarjetas de ejemplo de 09 §2 y de los mockups A2 y A5 (`4024 0071 2244 4821` y `5412 7534 1209 3057`) no pasaban Luhn. Manda la regla de 09 §2: se cambió un dígito del medio y se conservaron los últimos 4 (`4024 0071 2284 4821` y `5412 7534 1203 3057`), en 09 §2, `mockups/A2/Contratacion.dc.html`, `mockups/A5/Pago.dc.html` y sus exportaciones.
- **Verificación:** `dotnet build`, `dotnet test` y `dotnet format --verify-no-changes` pasan.
