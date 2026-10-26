---
id: JG-16
titulo: Generador de PDF y documentos de requisitos y de diseño
persona: jordin
responsable: Jordin García
avance: final
prioridad: P1
estado: hecha
programada: 2026-10-24
depende_de: []
no_antes_de: 2026-10-24
requisitos: []
pantallas: []
---

# JG-16 · Generador de PDF y documentos de requisitos y de diseño

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P1 · **Sin dependencias** · **No antes del:** 2026-10-24

## Objetivo
Generar en PDF, a partir de `docs/specs`, el Documento de Requisitos y el Documento de Diseño que exigen los lineamientos, con un script reutilizable que también convertirá los manuales.

## Contexto que debes leer
- `docs/lineamientos.md` §7 y §8 (contenido obligatorio y entrega en PDF)
- `docs/specs/README.md` (qué documento tiene qué)

## Archivos que creas o modificas
- `scripts/generar-pdf.mjs` (crear)
- `docs/pdf/plantilla.css` (crear)
- `docs/pdf/Documento_de_Requisitos.pdf` y `docs/pdf/Documento_de_Diseno.pdf` (generar y versionar)

## Criterios de aceptación
1. `node scripts/generar-pdf.mjs <salida.pdf> <archivo1.md> [archivo2.md ...]` funciona en Windows y en Linux **sin LaTeX**: Markdown → HTML, con los diagramas Mermaid como SVG (`@mermaid-js/mermaid-cli` con `npx`) → PDF con Playwright (Chromium).
2. El PDF lleva carátula (Universidad Rafael Landívar, Facultad de Ingeniería, curso, nombre del proyecto, título del documento, los 4 integrantes con carné, Quetzaltenango, fecha), índice, numeración de páginas y encabezado.
3. El Documento de Requisitos incluye introducción, alcance, requisitos funcionales, requisitos no funcionales y casos de uso (01, 02, 03, 04, 05). El Documento de Diseño incluye arquitectura general, diagramas UML, modelo entidad-relación y diseño de la base de datos (06, 07, 08, 09, 10, 11 y 12).
4. Los diagramas Mermaid se ven como imágenes y las tablas no se cortan de forma ilegible.

## Pruebas obligatorias
- Generar los dos PDF y revisar a ojo 3 páginas al azar (captura)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
node scripts/generar-pdf.mjs docs/pdf/Documento_de_Requisitos.pdf docs/specs/01-vision-y-alcance.md docs/specs/02-glosario.md docs/specs/03-requisitos.md docs/specs/04-roles-y-permisos.md docs/specs/05-casos-de-uso.md
```

## Fuera de alcance
- Manuales técnico y de usuario (JZ-15 y JZ-16 usan este script)

## Notas
- JG-17 vuelve a generar todos los PDF al congelar el código, por si cambiaron las especificaciones.

## Resultado

- Se creó `scripts/generar-pdf.mjs`: el comando indicado convierte una o varias
  fuentes Markdown en PDF con React Markdown, SVG de Mermaid CLI por `npx` y
  Chromium de Playwright, sin LaTeX. Reutiliza las dependencias existentes del
  frontend y las E2E, sin agregar paquetes a sus manifiestos o lockfiles.
- `docs/pdf/plantilla.css` da carátula con los datos de 01, índice navegable,
  marcadores, encabezado, numeración, tablas con encabezados repetidos y columnas
  proporcionales. Conserva anclas RF/RNF y referencias Markdown entre capítulos.
- Se generaron y versionaron `Documento_de_Requisitos.pdf` (37 páginas, fuentes
  01–05 y 3 diagramas) y `Documento_de_Diseno.pdf` (109 páginas, fuentes 06–12 y
  18 diagramas). Los diagramas grandes usan A3 horizontal y los más extensos
  incluyen vista general y detalles solapados para su lectura impresa.
- Decisiones: fecha de carátula en Guatemala; títulos derivados del nombre de
  salida; Mermaid CLI fijado en 11.12.0, en lotes de dos para limitar la memoria;
  imágenes locales relativas al Markdown y enlaces externos del repositorio a
  GitHub. Los temporales se eliminan tanto al terminar como ante un error.
- Verificación: 108 pruebas de scripts y 2 de integración real aprobadas, sin
  omisiones; los dos comandos de generación y el validador del plan correctos.
  Se comprobaron contenido, numeración y límites de página con PyMuPDF y se
  revisaron visualmente carátulas, tablas, diagramas y tres páginas aleatorias
  de cada documento. Evidencia y seis capturas en `docs/pdf/VERIFICACION.md`.
- La revisión independiente corrigió la pérdida de anclas y referencias en las
  tablas y dio `LISTO` en la segunda pasada. El workflow `.github/workflows/pdf.yml`
  ejecuta la generación y las pruebas reales en Linux.
- `docs/pdf/README.md` documenta preparación, comandos y reutilización para JZ-15
  y JZ-16. Los manuales quedan fuera de este cambio; JG-17 regenerará los PDF al
  congelar el código.
