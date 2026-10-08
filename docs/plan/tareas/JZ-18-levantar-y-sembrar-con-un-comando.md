---
id: JZ-18
titulo: Levantar el ambiente con la siembra en un solo comando (RNF-14)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 3
prioridad: P1
estado: pendiente
programada: 2026-10-11
depende_de: [JZ-17]
requisitos: [RNF-14]
pantallas: []
---

# JZ-18 · Levantar el ambiente con la siembra en un solo comando (RNF-14)

**Responsable:** José Pablo Zúñiga · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** JZ-17

## Objetivo
Cumplir RNF-14: que todo el sistema se levante con **un solo comando** de `docker compose` y quede con los datos de siembra de los mockups, sin perder la garantía de que un despliegue real arranca sin datos de demostración.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RNF-14
- `docs/specs/06-arquitectura.md` §7 y §7.1
- `docs/plan/convergencia/2026-10-08.md` (clasificación de RNF-14)
- `docs/manual-tecnico.md` §"Ambiente productivo simulado"

## Archivos que creas o modificas
- `infra/compose.prod.yml` (modificar)
- `infra/verificar.mjs` (modificar)
- `src/Shapi.Trabajador/**` (modificar, si la siembra se dispara al arrancar)
- `docs/manual-tecnico.md` (modificar)
- `docs/specs/06-arquitectura.md` §7 (modificar, para dejar el comando)
- `docs/plan/calendario.md` (modificar solo el paso 1 de los guiones de demostración)

## Criterios de aceptación
1. Un solo comando levanta el ambiente productivo simulado y lo deja sembrado. Por ejemplo, un perfil `demo` con un servicio de siembra de una sola ejecución (`docker compose … --profile demo up -d --build`), o una variable que haga sembrar al trabajador al arrancar. El PR explica la opción elegida.
2. Sin ese perfil o esa variable, el ambiente arranca sin siembra, y `infra/verificar.mjs` lo sigue comprobando.
3. Con el perfil o la variable, `infra/verificar.mjs` comprueba que la siembra quedó: una clave fija responde 200 en la compuerta y `https://envios.shapi.localhost` muestra el portal de Envíos Xelajú.
4. Volver a ejecutar el comando no duplica datos (la siembra ya es idempotente).
5. `docs/manual-tecnico.md` y el paso 1 de los guiones de `docs/plan/calendario.md` usan el comando nuevo.

## Pruebas obligatorias
- El verificador del ambiente en los dos modos (criterios 2 y 3)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node infra/verificar.mjs
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Que la siembra funcione dentro del contenedor (JZ-17)
