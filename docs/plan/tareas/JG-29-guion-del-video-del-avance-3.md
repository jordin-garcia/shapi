---
id: JG-29
titulo: Guion del video del Avance 3 y elemento de código de Jordin
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P1
estado: pendiente
programada: 2026-10-29
depende_de: [EM-22, DC-18, JZ-19, JG-22]
requisitos: []
pantallas: []
---

# JG-29 · Guion del video del Avance 3 y elemento de código de Jordin

**Responsable:** Jordin García · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** EM-22, DC-18, JZ-19 y JG-22

## Objetivo
Dejar listo el guion del video del Avance 3, que se graba el viernes 30 y se entrega el sábado 31 a las 23:59, y documentar el elemento de código que explica Jordin. **La hace el agente de Jordin.**

## Origen
El 23 oct, el curso cambió el Avance 3 por un video de 25 minutos como máximo, en el que participan todos los integrantes. Debe explicar:
- a) toda la funcionalidad del proyecto;
- b) al menos 4 elementos del código con características de *Clean Code* y patrones de diseño.

El equipo graba el proyecto terminado (`docs/plan/convergencia/2026-10-22.md` §6).

## Contexto que debes leer
- `docs/lineamientos.md` (requisitos funcionales del curso y §9)
- `docs/plan/calendario.md`: guiones 1, 2 y 3 y el video del Avance 3
- `docs/video/elementos/emilio.md`, `dominique.md` y `jose-pablo.md` (EM-22, DC-18 y JZ-19)
- `docs/manual-tecnico.md` («Sembrar la demostración», con las cuentas y las claves de la siembra)
- `docs/plan/tareas/JG-22-modo-demostracion-en-la-interfaz.md` (reloj de demostración desde el panel)
- El guion y el ensayo de la entrega del 18 oct en `~/.shapi-demo/` (en la PC de Jordin), como punto de partida

## Archivos que creas o modificas
- `docs/video/elementos/jordin.md` (crear)
- `docs/video/guion.md` (crear)

## Criterios de aceptación
1. **Elemento de Jordin.** `docs/video/elementos/jordin.md` sigue el mismo formato que EM-22: patrón, al menos dos características de *Clean Code*, `archivo:líneas`, fragmento, prueba, texto para narrar, qué se muestra y funcionalidades de Jordin. El patrón es la **cadena de responsabilidad** de la tubería de filtros de la compuerta (08 §3), con la regla de RNF-13: agregar una validación es agregar un filtro sin tocar los demás.
2. **Duración.** `docs/video/guion.md` divide el video en bloques. Cada bloque tiene su duración, quién narra, qué pantallas o archivos se muestran y qué datos de la siembra se usan. La suma es de **25 minutos como máximo**, con un margen de 2 minutos.
3. **Funcionalidad.** El guion recorre por la interfaz toda la funcionalidad que piden los lineamientos, con el proyecto terminado, y la lista en una tabla de cobertura (requisito o lineamiento → bloque). Incluye:
   - registro, verificación del correo y recuperación de la contraseña;
   - roles y miembros;
   - publicar una API con su especificación, sus rutas y su configuración;
   - planes de plataforma y de API;
   - contratación, claves, consola y compuerta (200, 401, 403 y 429);
   - consumo y facturación;
   - historial de pagos, renovación, gracia, suspensión y reactivación con el reloj de demostración;
   - cambio de plan con prorrateo;
   - administración de organizaciones, pagos, cuentas, soporte, bitácora y estado de los componentes;
   - dominio propio y personalización del portal;
   - el ambiente productivo levantado con un solo comando.
4. **Código.** El guion incluye los 4 elementos de código, uno por persona, con su tiempo.
5. **Participación.** Las cuatro personas narran: cada una, su elemento de código y una parte de la funcionalidad, en lo posible la que construyó.
6. **Antes de grabar.** El guion trae una lista de pasos: levantar el ambiente desde cero con la siembra (RNF-14); `node infra/verificar.mjs --demo`; calentar la compuerta; importar el certificado; resolución de 1366 × 768 o 1920 × 1080; cerrar las notificaciones; el orden de las cuentas que se usan; y cómo volver a sembrar si hay que repetir una toma.
7. **Código al día.** Antes de cerrar la tarea, el `archivo:líneas` y el fragmento de los cuatro archivos de `docs/video/elementos/` coinciden con `main`. JG-25 a JG-28 cambian código después de EM-22, DC-18 y JZ-19; si algo se movió, se corrige en el elemento y se avisa a su dueño en la bitácora.
8. **Ensayo.** El guion se recorrió una vez en el ambiente productivo simulado, solo por la interfaz, y se anotó el tiempo real de cada bloque. Si se encontraron errores, se anotan como avisos para Jordin en la bitácora.

## Pruebas obligatorias
- No aplica: es documentación. El ensayo del criterio 8 es la verificación.

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
node infra/verificar.mjs --demo
```
Además, abrir cada `archivo:líneas` citado en `docs/video/elementos/*.md` sobre `main` actualizado y compararlo con su fragmento (criterio 7).

## Fuera de alcance
- Grabar y editar el video (viernes 30 y sábado 31, fuera del plan de tareas).
