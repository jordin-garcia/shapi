# Verificación de JG-16 · 25 de octubre de 2026

Se generaron los dos entregables con los comandos de [README](README.md), en Windows,
con Node.js 24, React Markdown, Mermaid CLI 11.12.0 y el Chromium de Playwright.

| Entregable | Fuentes | Diagramas originales | Páginas | Marcadores | Enlaces |
|---|---|---:|---:|---:|---:|
| Documento de Requisitos | Especificaciones 01 a 05 | 3 | 37 | 70 | 93 |
| Documento de Diseño | Especificaciones 06 a 12 | 18 | 109 | 138 | 131 |

## Pruebas automatizadas

- `node --test "scripts/*.test.mjs"`: 108 pruebas aprobadas, ninguna fallida u omitida.
- `node --test scripts/pdf/generar-pdf.test.mjs`: 2 pruebas aprobadas, ninguna fallida
  u omitida. La primera genera un manual con 90 filas, anclas y referencias dentro
  de celdas, Mermaid real y navegación comprobada en Chromium. La segunda genera
  ambos documentos completos, verifica las doce fuentes, todos sus diagramas y
  contenido hasta CU-26 y ADR-35, además de las vistas de detalle.
- `node scripts/tareas.mjs --validar`: `Plan válido: 88 tareas.`
- Los dos comandos de generación de README terminaron con código 0: requisitos,
  5 fuentes y 3 SVG; diseño, 7 fuentes y 18 SVG.
- Extracción con PyMuPDF: carátulas con los cuatro carnés, universidad y lugar;
  contenido obligatorio; numeración correcta en todas las páginas; ningún texto
  fuera de los límites de las páginas. Los diagramas muy extensos conservan la
  vista general y agregan detalles con solapamiento; no se exige leer la vista
  general reducida como si fuera una página de detalle.

La prueba de integración se ejecuta también en Linux mediante el workflow `PDF`.
El runner instala un perfil AppArmor limitado a la ruta exacta del Chromium de
Playwright para permitir su sandbox sin desactivar las restricciones globales.
La evidencia de esa plataforma es su check `pdf` en el PR de JG-16.

## Inspección visual

Se renderizaron los PDF finales a PNG y se revisaron sus carátulas, índices, tablas,
diagramas generales y detalles del UML y MER. Las filas conservan el encabezado al
cambiar de página y no se cortan entre páginas. Los títulos de los diagramas grandes
acompañan a su imagen en páginas A3 horizontales.

La muestra aleatoria usa una semilla fija (`20261025 + número de fuentes`) y excluye
carátula e índice. Se revisaron estas tres páginas de cada PDF:

- Requisitos: [17](verificacion/requisitos-017.png), [24](verificacion/requisitos-024.png)
  y [37](verificacion/requisitos-037.png).
- Diseño: [13](verificacion/diseno-013.png), [29](verificacion/diseno-029.png)
  y [90](verificacion/diseno-090.png).

No quedaron defectos de disposición, recortes accidentales ni texto ilegible en
las capturas revisadas. Los cortes de las páginas de detalle son deliberados y se
continúan en las páginas contiguas, identificadas por fila y columna.

## Revisión independiente

La revisión B9 con `docs/plan/prompts/revision.md` detectó dos errores de navegación:
se descartaban las anclas explícitas RF/RNF y las referencias abreviadas de las
celdas no veían sus definiciones. Ambos se corrigieron con regresiones. La segunda
revisión del código dio `VEREDICTO: LISTO`, sin hallazgos de corrección ni opcionales.

JG-17 volverá a generar los documentos al congelar el código; estos PDF son la
instantánea de las especificaciones del 25 de octubre.
