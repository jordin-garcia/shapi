# PDF de Shapi

El generador usa las especificaciones y los manuales Markdown como fuentes, sin LaTeX.
La carátula obtiene los cuatro nombres y carnés de `docs/specs/01-vision-y-alcance.md`.
Incluye índice con enlaces, marcadores del PDF, encabezado, numeración y tablas con
encabezados repetidos. Los diagramas son SVG; los grandes usan páginas A3 horizontales
dentro del mismo documento A4 para conservar su resolución. Los UML y MER extensos
incluyen una vista general y páginas de detalle solapadas, identificadas por fila y
columna, para poder leer sus etiquetas también al imprimir.

## Preparación (Windows y Linux)

Requiere Node.js 24, npm/npx y pnpm, igual que el proyecto. Desde la raíz:

```text
pnpm --dir frontend install --frozen-lockfile
pnpm --dir tests/e2e install --frozen-lockfile
pnpm --dir tests/e2e exec playwright install chromium
```

En Linux, si faltan las bibliotecas del navegador, usar `playwright install --with-deps chromium`.
En Ubuntu con restricciones de AppArmor, Puppeteer puede responder `No usable sandbox`.
Si Google Chrome ya está instalado, se puede reutilizar su helper SUID antes de generar:

```sh
export CHROME_DEVEL_SANDBOX=/opt/google/chrome/chrome-sandbox
```

El workflow configura ese helper del runner, siguiendo la [recomendación de Chromium](https://chromium.googlesource.com/chromium/src/+/main/docs/security/apparmor-userns-restrictions.md#option-3_the-safest-way).
Se conserva el sandbox y no se cambian las restricciones globales del sistema.

El primer diagrama descarga con `npx` `@mermaid-js/mermaid-cli@11.12.0`, indicado por
JG-16. Se reutiliza el Chromium de Playwright, sin instalar otro navegador ni agregar
dependencias al frontend. Esa primera ejecución requiere acceso al registro de npm.

## Generar los entregables

Desde la raíz, los mismos comandos funcionan en PowerShell y en Linux:

```text
node scripts/generar-pdf.mjs docs/pdf/Documento_de_Requisitos.pdf docs/specs/01-vision-y-alcance.md docs/specs/02-glosario.md docs/specs/03-requisitos.md docs/specs/04-roles-y-permisos.md docs/specs/05-casos-de-uso.md
node scripts/generar-pdf.mjs docs/pdf/Documento_de_Diseno.pdf docs/specs/06-arquitectura.md docs/specs/07-modelo-de-datos.md docs/specs/08-compuerta.md docs/specs/09-cobros-y-suscripciones.md docs/specs/10-identidad-y-seguridad.md docs/specs/11-interfaz.md docs/specs/12-decisiones.md
```

El título se toma del nombre de salida. Para los manuales de JZ-15 y JZ-16:

```text
node scripts/generar-pdf.mjs docs/pdf/Manual_Tecnico.pdf docs/manual-tecnico.md
node scripts/generar-pdf.mjs docs/pdf/Manual_de_Usuario.pdf docs/manual-usuario.md
```

Las rutas con espacios se escriben entre comillas. Las imágenes locales de un manual
se resuelven desde la carpeta de su Markdown. Las referencias entre capítulos incluidos
se convierten en enlaces internos; las demás referencias del repositorio apuntan a GitHub.
La fecha de carátula es la fecha de generación en Guatemala.

Los temporales se crean en `.shapi/pdf/` y se eliminan al terminar, también si hay error.
Una fuente o imagen inexistente o un diagrama inválido hacen fallar el comando; no se
sobrescribe el PDF anterior hasta completar la conversión. Los PDF finales sí se versionan.
JG-17 vuelve a generarlos al congelar el código.

## Verificación

```text
node --test scripts/generar-pdf.test.mjs
node --test scripts/pdf/generar-pdf.test.mjs
node scripts/tareas.mjs --validar
```

La primera prueba no requiere instalaciones; la segunda usa Markdown, una tabla de 90
filas y Mermaid reales, genera un PDF y revisa el HTML con Chromium; después genera
ambos documentos completos a partir de las doce especificaciones. El workflow `pdf`
ejecuta ambas en Linux. Mermaid se procesa en lotes de dos para limitar la memoria.
Al cambiar las fuentes, generar los dos entregables y revisar
visualmente páginas con diagramas y tablas, además de la carátula y el índice.
