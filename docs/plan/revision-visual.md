# Revisión visual contra los mockups (DC-16)

Fecha: 2026-10-29. Ambiente productivo simulado con la siembra de demostración (`infra/compose.prod.yml`), Chromium de Playwright a **1280 × 800**. Cada pantalla se capturó y se comparó contra su mockup principal de `mockups/` (textos, datos, orden y estados; no pixel a pixel). Las precisiones de `docs/specs/11-interfaz.md` §4 y de las tareas prevalecen sobre el mockup.

**RNF-12:** en ninguna de las 48 rutas revisadas `document.documentElement.scrollWidth` supera el ancho de la ventana. Dentro de la página, dos tablas recortaban contenido: A3.5 (corregida aquí) y A6.5 (JZ-20).

**Reintentar (criterio 4):** los avisos de error de las pantallas de acceso del panel (`AvisoError` de `frontend/apps/panel/src/modulos/identidad/Formularios.tsx`) y del portal (`frontend/apps/portal/src/modulos/sesion/FormulariosAcceso.tsx`) usan ahora `Boton` secundario, igual que `EstadoError`.

Las pantallas sin ruta propia (A2.3, A2.4, A3.7r, A4.3b, A5.4b, A6.2b, A6.3b, A6.4b, A7.2, B2.4 a B2.6) son diálogos o resultados de un flujo y ya están cubiertos por las pruebas E2E de flujos principales. A0.2 solo existe en desarrollo.

| ID | Resultado | Diferencias | Acción |
|---|---|---|---|
| A0.1 | Coincide | — | — |
| A1.1 | Coincide | — | — |
| A1.2 | Coincide | Sin `?correo=` muestra «su correo» y el formulario de reenvío (precisión EM-04) | — |
| A1.3 | Coincide | — | — |
| A1.4a | Coincide | — | — |
| A1.4b | Coincide | Correo no visible (precisión EM-04) | — |
| A2.1 | Diferencias | No se marca «Plan actual» en el plan vigente | EM-24 |
| A3.1 | Coincide | — | — |
| A3.2 | Coincide | — | — |
| A3.3 | Coincide | Título del documento en vez del nombre del archivo (precisión DC-05) | — |
| A3.4 | Coincide | — | — |
| A3.5 | Corregida | «llamada(s) por petición» se partía y recortaba la columna a 1280 px | Corregido en DC-16 |
| A3.6 | Coincide | «Conectar dominio», «Desconectar» y TLS «Automático» (precisiones DC-14 y JG-24) | — |
| A3.7 | Coincide | — | — |
| A4.1 | Coincide | — | — |
| A4.2 | Coincide | — | — |
| A4.3 | Coincide | Orden y claves según la siembra (datos) | — |
| A5.0 | Coincide | Orden de las claves del JSON de ejemplo, que viene del origen (dato) | — |
| A5.1 | Coincide | — | — |
| A5.2 | Coincide | Navegación pública sin sesión (precisión DC-10) | — |
| A5.3 | Coincide | — | — |
| A5.3b | Coincide | Estado de enlace vencido (precisión DC-08) | — |
| A5.4 | Coincide | Con y sin sesión | — |
| A5.5 | Coincide | Ejemplo de petición y respuesta según la especificación de Agro (dato) | — |
| A5.6 | Coincide | Revisada en el flujo de contratación E2E | — |
| A5.7 | Coincide | — | — |
| A5.8 | Coincide | Sin `?correo=` (precisión DC-08) | — |
| A5.9 | Coincide | — | — |
| A5.10 | Coincide | Precisión DC-08 | — |
| A6.1 | Coincide | — | — |
| A6.2 | Diferencias | Organización «Suspendida» con la acción «Suspender» en vez de «Reactivar» | JZ-20 |
| A6.3 | Diferencias | Mes abreviado «sept» en vez de «sep» | EM-24 |
| A6.4 | Coincide | — | — |
| A6.5 | Diferencias | Columnas Estado y Acción recortadas a 1280 px | JZ-20 |
| A7.1 | Coincide | — | — |
| A8.1 | Coincide | — | — |
| A8.2 | Coincide | Estado de enlace vencido | — |
| B1.1 | Coincide | Selector de rango (precisión de la tarea) | — |
| B1.2 | Coincide | — | — |
| B1.3 | Coincide | — | — |
| B1.4 | Diferencias | Falta el rótulo «Organización», «Cambiar de plan» no está en el encabezado y la fecha usa formato corto | EM-24 |
| B1.5 | Coincide | Variante vacía | — |
| B2.1 | Coincide | — | — |
| B2.2 | Coincide | — | — |
| B2.3 | Coincide | «Revocar» pasa a la línea siguiente (menor, sin pérdida de contenido) | — |
| B3.1 | Diferencias | «Compuerta de APIs está degradado»: falla la concordancia y el texto no es el del mockup `Degradado` | JZ-20 |
| B3.2 | Coincide | — | — |
