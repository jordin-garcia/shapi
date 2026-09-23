---
id: JZ-13
titulo: Pruebas E2E de los flujos principales
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P1
estado: pendiente
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
- `tests/e2e/specs/*.spec.ts` (crear)
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
