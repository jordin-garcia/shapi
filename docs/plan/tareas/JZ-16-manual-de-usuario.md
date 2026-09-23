---
id: JZ-16
titulo: Manual de usuario
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P1
estado: pendiente
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
