---
id: JZ-11
titulo: Plantillas de correo completas
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 3
prioridad: P2
estado: hecha
programada: 2026-10-05
depende_de: [JZ-03]
requisitos: [RF-46]
pantallas: []
---

# JZ-11 · Plantillas de correo completas

**Responsable:** José Pablo Zúñiga · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** JZ-03

## Objetivo
Completar las 12 plantillas de correo de las especificaciones, con la marca del portal en los correos de los consumidores.

## Contexto que debes leer
- `docs/specs/10-identidad-y-seguridad.md` §6 (tabla de plantillas)
- `docs/specs/11-interfaz.md` §1 (colores)

## Archivos que creas o modificas
- `src/Shapi.Infraestructura/Correo/Plantillas/*.{html,txt}` (crear las que faltan y agregar la marca a las dos que ya existen, `verificacion_correo` y `recuperacion`)
- `src/Shapi.Infraestructura/Correo/MotorPlantillasCorreo.cs` (modificar: color y enlace del logotipo del portal)
- `tests/**/Correo/**`

## Criterios de aceptación
1. Existen las 12 plantillas de 10 §6, en HTML y en texto, en español y tratando al usuario de usted.
2. Los correos de los consumidores usan el nombre, el color y el logo (como enlace) del portal, sin la marca de Shapi. Los del personal usan la marca de Shapi (variante 4). Esto vale también para `verificacion_correo` y `recuperacion`, que JZ-03 dejó sin marca. En los `datos` de un correo de consumidor, quien lo encola pasa `nombrePortal`, `hostPortal` y `colorPortal` (el `api.portal_color`); el motor acepta solo un `colorPortal` `#RRGGBB` y arma el enlace del logotipo como `https://{hostPortal}/api/portal/logo` (DC-03). Si el portal tiene logotipo, quien encola agrega `logoPortal: "true"`; si no, el correo muestra solo el nombre.
3. Cada plantilla tiene una prueba que la arma con datos de ejemplo y verifica que no queden `{{…}}` sin reemplazar.

## Pruebas obligatorias
- Unitarias de cada plantilla

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Encolar los correos (cada módulo)

## Resultado

- Se completaron las 12 plantillas de RF-46 en HTML y texto, en español formal, con asuntos específicos y pruebas unitarias que renderizan cada variante sin marcadores pendientes.
- `MotorPlantillasCorreo` aplica una cabecera común: Shapi con el color principal de la variante 4 para el personal, o el nombre, color y logotipo opcional del portal para consumidores. El nombre se escapa en HTML, `colorPortal` acepta únicamente `#RRGGBB` y `hostPortal` conserva la validación de subdominios permitidos.
- El logotipo del portal se carga desde `https://{hostPortal}/api/portal/logo` solo cuando `logoPortal` es `"true"`; sin logotipo se muestra únicamente el nombre del portal.
- Se documentaron en 10 §6 los datos requeridos por cada plantilla para que los módulos que las encolan compartan el mismo contrato.
- Archivos principales: `src/Shapi.Infraestructura/Correo/MotorPlantillasCorreo.cs`, `src/Shapi.Infraestructura/Correo/ColaCorreoBaseDatos.cs`, `src/Shapi.Infraestructura/Correo/Plantillas/` y `tests/Shapi.Api.Tests/Correo/`.
