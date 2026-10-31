---
id: JG-29
titulo: Guion del video del Avance 3 y elemento de código de Jordin
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P1
estado: hecha
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

## Resultado

Se escribieron el guion del video (`docs/video/guion.md`) y el elemento de código de Jordin (`docs/video/elementos/jordin.md`), y se recorrió el guion completo en el ambiente productivo simulado.

- **Guion:** 16 bloques y 22:45 planeados (25 minutos como máximo, con 2:15 de margen). La parte 1 (13:30) recorre por la interfaz toda la funcionalidad del criterio 3: registro, verificación y recuperación; roles y miembros; publicar una API; planes de plataforma y de API; contratación, claves y consola con 200, 401, 429 y 403; consumo y facturación; pagos, renovación, gracia, suspensión y reactivación con el reloj; prorrateo; administración, soporte, bitácora y estado; dominio propio y portal; y el ambiente con un solo comando. La parte 2 (9:00) son los cuatro elementos de código, de 2:15 cada uno. Cada bloque dice quién narra, cuánto dura, qué pantallas muestra y qué datos de la siembra usa. Incluye la tabla de cobertura, la participación (Jordin 5:00, Emilio 6:45, Dominique 6:00 y José Pablo 5:00), la lista «Antes de grabar» del criterio 6, el orden de las cuentas por ventana, cómo repetir una toma y el registro del ensayo.
- **Elemento de Jordin:** cadena de responsabilidad en `TuberiaCompuerta` e `IFiltroCompuerta` (08 §3, RNF-13), con tres características de Clean Code, dos fragmentos literales de `TuberiaCompuerta.cs`, las pruebas `RNF_13_*` y de filtros, la narración (unas 320 palabras) y la tabla de funcionalidades. Las 27 pruebas citadas pasaron.
- **Criterio 7:** los cinco fragmentos de los cuatro elementos coinciden con `main` (`8dc94e0`). Se corrigieron 13 referencias de líneas que se habían movido, cortaban un método o no mostraban lo que decía el texto: 5 de Emilio, 3 de Dominique y 5 de José Pablo, sin cambiar sus fragmentos ni sus narraciones. La tabla de pantalla de José Pablo se ajustó a los 2:15 del guion.
- **Ensayo (criterio 8):** ambiente `shapi-jg29` recién construido y sembrado, recorrido por la interfaz con un navegador automatizado que siguió cada bloque. Los once bloques funcionales funcionaron. Se midió el tiempo de la interfaz de cada bloque y se estimó la narración. El ensayo corrigió ocho detalles del guion (nombres de botones, una casilla que ya viene marcada y dos esperas). También se comprobó `sembrar-demo --reiniciar` para repetir tomas.

### Decisiones
- El código va en una parte aparte, después de la funcionalidad, para grabarlo sin cambiar de ambiente. El bloque del reloj va al final, porque el reloj no se puede retroceder.
- El ensayo fue automatizado: mide las pantallas y no a las personas. Por eso el guion pide cronometrar la primera toma real y define en qué orden recortar.
- Se dejó fuera del alcance y se avisó en la bitácora: el encabezado público del portal con sesión y la compuerta «degradada» en B3.1 cuando esta PC pasa de 15 ms de p95.

### Archivos principales
- `docs/video/guion.md` y `docs/video/elementos/jordin.md` (nuevos)
- `docs/video/elementos/emilio.md`, `dominique.md` y `jose-pablo.md` (referencias de líneas)

### Cambio del 30 oct 2026: una parte por integrante
Jordin decidió que cada integrante grabe su parte por su cuenta y que después se unan los cuatro videos. Por eso el guion pasó de 16 bloques intercalados a cuatro partes seguidas, una por persona, cada una con su funcionalidad y su elemento de código: Jordin 7:15, Emilio 4:45, Dominique 5:30 y José Pablo 5:15, en total 22:45.
- Cada parte se graba en un ambiente recién sembrado y solo usa la siembra y los datos que crea ella misma. Ya no hay datos compartidos entre partes: la consola de Jordin usa la clave de pruebas de la siembra; Jordin registra su propia consumidora con 0341 para el reloj; Dominique registra a un consumidor distinto (Pedro); el soporte de José Pablo usa la API sembrada.
- Abre Jordin con la presentación y cierra José Pablo. Jordin pidió quedarse con el bloque del reloj (lo construyó en JG-22), que va al final de su funcionalidad, antes de su elemento de código.
- «Antes de grabar» quedó como lista para cada integrante. El certificado se importa con `certutil`, porque `Import-Certificate` falló desde Git Bash. Se agregaron el formato y el nombre de los archivos de video.
- No se repitió el ensayo: las pantallas, los botones y los datos son los mismos. La tabla del ensayo indica a qué bloque nuevo corresponde cada resultado.
