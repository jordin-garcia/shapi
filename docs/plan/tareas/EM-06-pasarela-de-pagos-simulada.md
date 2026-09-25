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
1. `TokenizarAsync` valida el número (13 a 19 dígitos y Luhn → `numero_invalido`), la marca por BIN (Visa, Mastercard o American Express → si no, `marca_no_soportada`), el vencimiento (`tarjeta_vencida`) y el CVV (`cvv_invalido`), y devuelve `tok_sim_{uuid}` o, para las tarjetas especiales, `tok_sim_0341_{uuid}`, junto con la marca, los últimos 4 y el titular.
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
