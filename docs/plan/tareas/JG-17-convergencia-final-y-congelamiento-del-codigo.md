---
id: JG-17
titulo: Convergencia final y congelamiento del código
persona: jordin
responsable: Jordin García
avance: final
prioridad: P1
estado: hecha
programada: 2026-10-29
depende_de: [JG-16, JG-21, JG-22, JZ-13, JZ-15, JZ-16]
no_antes_de: 2026-10-29
requisitos: []
pantallas: []
---

# JG-17 · Convergencia final y congelamiento del código

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P1 · **Depende de:** JG-16, JG-21, JG-22, JZ-13, JZ-15 y JZ-16 · **No antes del:** 2026-10-29

## Objetivo
Hacer la última revisión de brechas, congelar el código el jueves 29 de octubre (el viernes 30 se graba el video del Avance 3), marcar la versión entregable y regenerar los PDF.

## Contexto que debes leer
- `docs/plan/prompts/convergencia.md`
- `docs/plan/calendario.md` (semana final y guion final)
- `docs/lineamientos.md` §8

## Archivos que creas o modificas
- `docs/plan/convergencia/2026-10-29.md` (crear)
- `docs/pdf/*.pdf` (regenerar)
- `src/Shapi.Infraestructura/Comun/ServiciosComunes.cs` o `.env.example` (criterio 5)
- `README.md` (modificar: cómo ver el sistema funcionando y enlaces a los PDF)

## Criterios de aceptación
1. El informe final lista cada RF y RNF con su estado y evidencia (prueba o E2E). Lo que queda fuera se declara explícitamente.
2. El ambiente productivo simulado arranca desde cero con los comandos del manual técnico, y el guion final funciona completo.
3. Se crea la etiqueta `v1.0.0` en `main` (`git tag v1.0.0 && git push origin v1.0.0`) y un *release* de GitHub con los PDF y el Word del manual técnico adjuntos.
4. Los PDF de requisitos, de diseño y del manual de usuario están regenerados en `docs/pdf/`. El manual técnico no se genera en PDF: se entrega en Markdown y Word (`docs/manual-tecnico.md` y `docs/Manual_Tecnico.docx`, JZ-15, decisión de Jordin del 1 nov 2026); si cambia, se regenera el Word.
5. En desarrollo, la API y el trabajador usan las mismas llaves de Data Protection. Hoy, con `SHAPI_DPKEYS_DIR=.shapi/dpkeys` de `.env.example`, la ruta relativa se resuelve desde la carpeta de cada proyecto (`src/Shapi.Api/.shapi/dpkeys` y `src/Shapi.Trabajador/.shapi/dpkeys`), así que el trabajador no puede descifrar el secreto de origen que cifró la API (hallazgo de JZ-15, 1 nov 2026). Se resuelve, por ejemplo, interpretando una ruta relativa desde la raíz del repositorio, con una prueba que lo demuestre, y se actualiza la fila de `SHAPI_DPKEYS_DIR` del Anexo A y la de «no puede descifrar el secreto» del capítulo 13 del manual técnico.

## Pruebas obligatorias
- La CI de `main` en verde, y el E2E de JZ-13 en verde

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
gh release view v1.0.0
```

## Fuera de alcance
- Funcionalidades nuevas

## Resultado

Se ejecutó el 9 de noviembre de 2026, después del video del Avance 3 y de JZ-15, JZ-16 y DC-15. El informe está en `docs/plan/convergencia/2026-10-29.md`.

- **Criterio 1:** el informe lista los 47 RF y los 15 RNF con su estado y su evidencia:
  - 47 RF ✅;
  - 12 RNF ✅;
  - RNF-06 y RNF-11 son objetivos de diseño que no se miden;
  - RNF-12 queda ⚠️, porque Firefox no se pudo verificar en esta PC.

  Lo que queda fuera se declara en su §3: Firefox, las pruebas de carga no repetidas y los servicios simulados. No se crearon tareas nuevas.
- **Criterio 2:** con los comandos del manual técnico (12.4), en un proyecto de Compose aislado con volúmenes vacíos (`shapi-jg17`):
  - el ambiente productivo simulado arrancó en 11 min 19 s;
  - la siembra terminó con 0;
  - `node infra/verificar.mjs --demo` pasó;
  - `http://` redirige con 308 a `https://`;
  - las 16 E2E del guion pasaron en 4.2 min.

  En Edge pasaron las 10 pruebas de pantallas.
- **Criterio 3:** al integrar el PR se crean la etiqueta `v1.0.0` y el *release* con los tres PDF y el Word (informe, §5).
- **Criterio 4:** se regeneraron los PDF de requisitos (37 páginas), de diseño (113) y del manual de usuario (64).

  El Word del manual técnico no se regeneró completo, para conservar los ajustes que Jordin hizo a mano en la portada. Se reemplazaron solo los tres párrafos que cambian por el criterio 5.
- **Criterio 5:** `DirectorioLlaves.Resolver` (`src/Shapi.Infraestructura/Comun/DirectorioLlaves.cs`):
  - usa tal cual una ruta absoluta de `SHAPI_DPKEYS_DIR`;
  - resuelve una relativa desde la raíz del repositorio, la carpeta de `Shapi.slnx`;
  - fuera del repositorio, la resuelve desde la raíz de contenido del *host*.

  `ServiciosComunes` la usa con `HostDefaults.ContentRootKey`. `DirectorioLlavesTests` tiene cuatro pruebas. Una de ellas cifra con los servicios de la API y descifra con los del trabajador, cada uno con la carpeta de su proyecto como raíz. Antes de la corrección fallaban dos.

  Se actualizaron:
  - el capítulo 13, el capítulo 14 y el Anexo A del manual técnico (Markdown y Word);
  - 10 §3;
  - `.env.example`.
- **README:** nuevas secciones «Ver el sistema funcionando» (tres comandos, direcciones y cuentas de la demostración) y «Documentación entregable», con enlaces a los PDF, al Word y al *release*.
- **Verificación:**
  - formato y compilación sin errores;
  - 1146 pruebas de `tests/Shapi.Api.Tests`;
  - plan válido.
