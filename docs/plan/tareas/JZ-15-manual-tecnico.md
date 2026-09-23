---
id: JZ-15
titulo: Manual técnico
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P1
estado: pendiente
depende_de: [JG-16]
no_antes_de: 2026-10-24
requisitos: []
pantallas: []
---

# JZ-15 · Manual técnico

**Responsable:** José Pablo Zúñiga · **Avance:** final · **Prioridad:** P1 · **Depende de:** JG-16 · **No antes del:** 2026-10-24

## Objetivo
Completar el manual técnico que exigen los lineamientos y generarlo en PDF.

## Contexto que debes leer
- `docs/lineamientos.md` §7 (tecnologías, instalación, configuración y despliegue)
- `docs/specs/06-arquitectura.md`
- `docs/plan/instalacion.md`
- `.env.example`

## Archivos que creas o modificas
- `docs/manual-tecnico.md` (completar)
- `docs/pdf/Manual_Tecnico.pdf` (generar con `scripts/generar-pdf.mjs`)

## Criterios de aceptación
1. Tiene: tecnologías usadas (con su versión y su justificación, en resumen), requisitos, instalación en Windows y en Linux, configuración (**todas** las variables de entorno con su descripción y su valor por defecto), despliegue en el ambiente productivo simulado (imágenes de GHCR y compose), siembra y cuentas de demostración, modo demostración y reloj, respaldo y restauración de PostgreSQL (`pg_dump`), y solución de problemas frecuentes (certificado, puertos ocupados, Docker).
2. Cada comando del manual se ejecutó al menos una vez para comprobarlo.
3. El PDF se genera con la carátula del proyecto.

## Pruebas obligatorias
- Seguir el manual desde cero en un equipo limpio (o después de `docker compose down -v`)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Manual de usuario (JZ-16)
