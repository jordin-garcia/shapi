---
id: JG-17
titulo: Convergencia final y congelamiento del código
persona: jordin
responsable: Jordin García
avance: final
prioridad: P1
estado: pendiente
depende_de: [JG-16]
no_antes_de: 2026-10-29
requisitos: []
pantallas: []
---

# JG-17 · Convergencia final y congelamiento del código

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P1 · **Depende de:** JG-16 · **No antes del:** 2026-10-29

## Objetivo
Hacer la última revisión de brechas, congelar el código el 30 de octubre, marcar la versión entregable y regenerar los cuatro PDF.

## Contexto que debes leer
- `docs/plan/prompts/convergencia.md`
- `docs/plan/calendario.md` (semana final y guion final)
- `docs/lineamientos.md` §8

## Archivos que creas o modificas
- `docs/plan/convergencia/2026-10-29.md` (crear)
- `docs/pdf/*.pdf` (regenerar)
- `README.md` (modificar: cómo ver el sistema funcionando y enlaces a los PDF)

## Criterios de aceptación
1. El informe final lista cada RF y RNF con su estado y evidencia (prueba o E2E). Lo que queda fuera se declara explícitamente.
2. El ambiente productivo simulado arranca desde cero con los comandos del manual técnico, y el guion final funciona completo.
3. Se crea la etiqueta `v1.0.0` en `main` (`git tag v1.0.0 && git push origin v1.0.0`) y un *release* de GitHub con los PDF adjuntos.
4. Los cuatro PDF (requisitos, diseño, manual técnico y manual de usuario) están regenerados en `docs/pdf/`.

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
