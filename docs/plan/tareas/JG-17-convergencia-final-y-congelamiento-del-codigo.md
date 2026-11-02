---
id: JG-17
titulo: Convergencia final y congelamiento del código
persona: jordin
responsable: Jordin García
avance: final
prioridad: P1
estado: pendiente
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
