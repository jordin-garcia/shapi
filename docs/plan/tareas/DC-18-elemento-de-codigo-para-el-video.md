---
id: DC-18
titulo: Elemento de código para el video del Avance 3 (Clean Code y patrón de diseño)
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P1
estado: pendiente
programada: 2026-10-26
depende_de: []
requisitos: []
pantallas: []
---

# DC-18 · Elemento de código para el video del Avance 3

**Responsable:** Dominique Contreras · **Avance:** 3 · **Prioridad:** P1 · **Sin dependencias**

## Objetivo
Dejar listo, en `docs/video/elementos/dominique.md`, el elemento del código que Dominique explicará en el video del Avance 3: un patrón de diseño y las características de *Clean Code* que se ven en **su propio código**, con el texto que va a narrar.

**La hace el agente de Dominique**, siguiendo el protocolo B como cualquier otra tarea. Dominique solo revisa el texto y lo narra en la grabación del viernes 30.

## Origen
El 23 oct, el curso cambió el Avance 3 por un video de 25 minutos como máximo, que se entrega el sábado 31 a las 23:59. Debe explicar a) toda la funcionalidad y b) **al menos 4 elementos del código** con características de *Clean Code* y patrones de diseño. Participan las cuatro personas. Jordin decidió que cada persona prepare un elemento (`docs/plan/convergencia/2026-10-22.md` §6).

## Contexto que debes leer
- `docs/lineamientos.md` §9 (el cambio del Avance 3)
- `docs/specs/06-arquitectura.md` (capas, frontend y paquetes compartidos) y `docs/specs/12-decisiones.md`
- Los `## Resultado` de las tareas de Dominique (DC-01 a DC-17)
- `docs/plan/tareas/JG-29-guion-del-video-del-avance-3.md`: cómo usa este archivo el guion

## Archivos que creas o modificas
- `docs/video/elementos/dominique.md` (crear)

## Criterios de aceptación
1. El archivo describe **un patrón de diseño** que está en código de Dominique (`Apis`, `Portal`, `Shapi.Contratos/Red/`, `packages/ui`, `packages/api` o sus pantallas): su nombre, su categoría (creacional, estructural, de comportamiento o de arquitectura), qué problema resuelve aquí y por qué se eligió. Sugerencias: el **Repositorio** y el caso de uso de `RegistrarApi` o `GestionarDominios`; la **composición** de componentes de `packages/ui` con *hooks* propios; o el validador de direcciones de origen contra SSRF (RNF-10). Para no repetir, los demás elementos son: la tubería de filtros de la compuerta (Jordin), y los que elijan EM-22 y JZ-19. Si eliges otro patrón, que no coincida con ellos.
2. Explica **al menos dos características de *Clean Code*** que se ven en ese mismo código, cada una con su ejemplo concreto: nombres que revelan la intención, funciones pequeñas con una sola responsabilidad, componentes reutilizables sin duplicación, separación entre presentación y datos, pruebas legibles, etc.
3. Cita la ubicación exacta en `main` (`archivo:líneas`) e incluye un fragmento de 30 líneas como máximo, copiado tal cual. También nombra la prueba automatizada que lo ejercita.
4. Trae el **texto para narrar**, en primera persona y en español, para 2 a 3 minutos, y la lista de lo que se muestra en pantalla mientras tanto (qué archivo y qué líneas, en qué orden).
5. Lista las **funcionalidades que Dominique construyó** y que el video muestra, con sus pantallas (por ejemplo, registrar y publicar una API, el portal de marca blanca, la consola de pruebas, la contratación en el portal, la personalización y el dominio propio). JG-29 la usa para repartir la narración.

## Pruebas obligatorias
- No aplica: es documentación. La prueba que cita el criterio 3 debe pasar.

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
```
Además, ejecutar la prueba que cita el archivo (`dotnet test … --filter …` o `pnpm test -- <archivo>` en `frontend/`) y comprobar que las líneas citadas coinciden con `main`.

## Fuera de alcance
- Cambiar el código para que se vea mejor en el video. Si el agente encuentra algo que corregir, lo anota como aviso para Jordin en la bitácora.
