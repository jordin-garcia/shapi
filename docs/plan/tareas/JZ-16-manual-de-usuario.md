---
id: JZ-16
titulo: Manual de usuario
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P1
estado: hecha
programada: 2026-10-27
depende_de: [JG-16, JZ-13]
no_antes_de: 2026-10-26
requisitos: []
pantallas: []
---

# JZ-16 · Manual de usuario

**Responsable:** José Pablo Zúñiga · **Avance:** final · **Prioridad:** P1 · **Depende de:** JG-16, JZ-13 · **No antes del:** 2026-10-26

## Objetivo
Escribir el manual de usuario con capturas reales del sistema y generarlo en PDF.

## Contexto que debes leer
- `docs/lineamientos.md` §7 (registro, inicio de sesión, contratación de servicios y administración de recursos)
- `docs/specs/05-casos-de-uso.md`
- `docs/specs/11-interfaz.md` §3

## Archivos que creas o modificas
- `docs/manual-usuario.md` (crear)
- `docs/manual-usuario/img/*.png` (capturas con `pnpm captura`)
- `docs/pdf/Manual_de_Usuario.pdf` (generar)

## Criterios de aceptación
1. Tiene secciones para cada tipo de usuario: **proveedor** (registro, verificación, inicio de sesión, recuperación, contratar o cambiar el plan de plataforma, registrar y publicar una API, rutas, planes, portal, dominio propio, claves, consumo, miembros, invitar consumidores y soporte), **consumidor** (registro en el portal, documentación, consola, contratar un plan, claves, consumo, pagos y cambio de plan) y **administración y soporte** (planes, organizaciones, pagos, cuentas, casos, estado y bitácora).
2. Cada paso lleva una captura real del sistema con la siembra de demostración.
3. El PDF se genera con la carátula del proyecto.

## Pruebas obligatorias
- Revisar que cada captura corresponda al paso que acompaña

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Manual técnico (JZ-15)

## Resultado

- Se creó `docs/manual-usuario.md` con 33 procedimientos para proveedor, consumidor, administración y soporte, más acceso al ambiente, cuentas públicas de demostración, diferencias entre roles y solución de problemas frecuentes.
- Cada paso numerado lleva una captura real del sistema. Las 59 imágenes de `docs/manual-usuario/img/` se tomaron con Chromium sobre el ambiente local con siembra de demostración. Los formularios de enlaces y las pantallas que revelan secretos se recorrieron con cuentas nuevas de ejemplo; no se cambiaron contraseñas ni claves de las cuentas sembradas. Los valores completos del secreto de origen y las claves nuevas se ocultaron al capturar y se explica en el manual.
- Se generó `docs/pdf/Manual_de_Usuario.pdf` con `scripts/generar-pdf.mjs`: 64 páginas, carátula con los cuatro integrantes y sus carnés, índice, marcadores y capturas locales.
- `tests/e2e/scripts/manual-usuario.test.mjs` contiene cinco comprobaciones automatizadas de cobertura por perfil, capturas PNG por paso, siete pantallas finales obligatorias, archivo PDF y generación real con carátula, imágenes, tablas y enlaces válidos. La primera ejecución confirmó la ausencia de los entregables; una regresión posterior confirmó la falta de las pantallas finales antes de agregarlas.
- Verificación: cinco pruebas aprobadas, cero fallidas y cero omitidas; `node scripts/tareas.mjs --validar` válido (92 tareas); `git diff --check` correcto. Poppler renderizó las 64 páginas y se revisó la correspondencia de sus capturas; pypdf comprobó la carátula y pdfplumber confirmó que no hay texto fuera de página. No se modificaron backend, frontend ni contratos.
- Revisión independiente en contexto limpio según B9: se corrigió el hallazgo de pantallas finales faltantes y se agregó una prueba específica. Segunda revisión: **LISTO**, sin hallazgos de corrección ni opcionales.
