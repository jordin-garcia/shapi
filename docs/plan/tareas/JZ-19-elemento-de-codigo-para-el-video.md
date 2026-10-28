---
id: JZ-19
titulo: Elemento de código para el video del Avance 3 (Clean Code y patrón de diseño)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 3
prioridad: P1
estado: hecha
programada: 2026-10-25
depende_de: []
requisitos: []
pantallas: []
---

# JZ-19 · Elemento de código para el video del Avance 3

**Responsable:** José Pablo Zúñiga · **Avance:** 3 · **Prioridad:** P1 · **Sin dependencias**

## Objetivo
Dejar listo, en `docs/video/elementos/jose-pablo.md`, el elemento del código que José Pablo explicará en el video del Avance 3: un patrón de diseño y las características de *Clean Code* que se ven en **su propio código**, con el texto que va a narrar.

**La hace el agente de José Pablo**, siguiendo el protocolo B como cualquier otra tarea. José Pablo solo revisa el texto y lo narra en la grabación del viernes 30.

## Origen
El 23 oct, el curso cambió el Avance 3 por un video de 25 minutos como máximo, que se entrega el sábado 31 a las 23:59. Debe explicar a) toda la funcionalidad y b) **al menos 4 elementos del código** con características de *Clean Code* y patrones de diseño. Participan las cuatro personas. Jordin decidió que cada persona prepare un elemento (`docs/plan/convergencia/2026-10-22.md` §6).

## Contexto que debes leer
- `docs/lineamientos.md` §9 (el cambio del Avance 3)
- `docs/specs/06-arquitectura.md` §7 y §8 y `docs/specs/12-decisiones.md`
- Los `## Resultado` de las tareas de José Pablo (JZ-01 a JZ-18)
- `docs/plan/tareas/JG-29-guion-del-video-del-avance-3.md`: cómo usa este archivo el guion

## Archivos que creas o modificas
- `docs/video/elementos/jose-pablo.md` (crear)

## Criterios de aceptación
1. El archivo describe **un patrón de diseño** que está en código de José Pablo (`Administracion`, `Soporte`, `Bitacora`, `Correo`, `Estado`, `Siembra/Demo/`, `infra/` o sus pantallas): su nombre, su categoría (creacional, estructural, de comportamiento o de arquitectura), qué problema resuelve aquí y por qué se eligió. Sugerencia: la **bandeja de salida transaccional** (*Transactional Outbox*) de `correo_saliente` con el despachador y sus 5 reintentos (RF-46), o el **Método plantilla** del motor de plantillas de correo. Para no repetir, los demás elementos son: la tubería de filtros de la compuerta (Jordin), y los que elijan EM-22 y DC-18. Si eliges otro patrón, que no coincida con ellos.
2. Explica **al menos dos características de *Clean Code*** que se ven en ese mismo código, cada una con su ejemplo concreto: nombres que revelan la intención, funciones pequeñas con una sola responsabilidad, manejo explícito de errores y reintentos, idempotencia, pruebas legibles, etc.
3. Cita la ubicación exacta en `main` (`archivo:líneas`) e incluye un fragmento de 30 líneas como máximo, copiado tal cual. También nombra la prueba automatizada que lo ejercita.
4. Trae el **texto para narrar**, en primera persona y en español, para 2 a 3 minutos, y la lista de lo que se muestra en pantalla mientras tanto (qué archivo y qué líneas, en qué orden).
5. Lista las **funcionalidades que José Pablo construyó** y que el video muestra, con sus pantallas (por ejemplo, el ambiente productivo levantado con un solo comando y la siembra, la administración de organizaciones y de cuentas, los casos de soporte, la bitácora, el estado de los componentes, los correos en Mailpit y las pruebas de carga). JG-29 la usa para repartir la narración.

## Pruebas obligatorias
- No aplica: es documentación. La prueba que cita el criterio 3 debe pasar.

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
```
Además, ejecutar la prueba que cita el archivo (`dotnet test … --filter "FullyQualifiedName~<Prueba>"`) y comprobar que las líneas citadas coinciden con `main`.

## Fuera de alcance
- Cambiar el código para que se vea mejor en el video. Si el agente encuentra algo que corregir, lo anota como aviso para Jordin en la bitácora.

## Resultado

- Se creó `docs/video/elementos/jose-pablo.md` con la bandeja de salida transaccional como patrón de arquitectura, su aplicación en soporte y correo, y dos características de Clean Code con ejemplos concretos.
- Incluye un fragmento literal de 20 líneas de `CorreoSaliente.RegistrarFallo`, las pruebas de RF-46 que lo ejercitan, una narración de 337 palabras para 2 a 3 minutos, una secuencia de pantalla estimada en 2 min 45 s y la tabla de funcionalidades y pantallas para JG-29.
- Se documentan los cinco reintentos además del intento inicial, la eliminación del token y el límite de entrega ante una caída entre SMTP y la confirmación en PostgreSQL. No se cambió código, contratos, dependencias ni comportamiento.
- Verificación: `node scripts/tareas.mjs --validar` dio «Plan válido: 89 tareas»; las cinco pruebas de `Shapi.Dominio.Tests.Correo.CorreoSalienteTests` pasaron, sin fallos ni omitidas. Una comparación contra `main` (`30b14fee9d6f2aba0302e61b0f168e88a09c9570`) confirmó el fragmento exacto y las 15 referencias con archivo y rango completos; se comprobó la extensión de la narración.
- Decisión: se eligió la bandeja de salida, distinta de puertos y adaptadores (Emilio) y la cadena de responsabilidad (Jordin). El elemento de Dominique todavía no estaba creado. JG-29 volverá a comprobar las líneas después de JG-25 y JG-27, antes de grabar.
- Revisión independiente en contexto limpio según `docs/plan/prompts/revision.md`: LISTO, sin hallazgos de corrección ni opcionales.
