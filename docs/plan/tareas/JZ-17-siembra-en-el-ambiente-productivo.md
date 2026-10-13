---
id: JZ-17
titulo: Siembra de demostración en el ambiente productivo simulado
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 2
prioridad: P1
estado: hecha
programada: 2026-10-08
depende_de: []
requisitos: [RNF-14]
pantallas: []
---

# JZ-17 · Siembra de demostración en el ambiente productivo simulado

**Responsable:** José Pablo Zúñiga · **Avance:** 2 · **Prioridad:** P1 · **Sin dependencias**

## Objetivo
Que `sembrar-demo` funcione dentro del contenedor del trabajador del ambiente productivo simulado, como pide el paso 1 del guion 2, y que la CI lo compruebe para que no vuelva a romperse.

## Contexto que debes leer
- `docs/plan/convergencia/2026-10-08.md` (hallazgo del paso 1 del guion)
- `docs/manual-tecnico.md` §"Ambiente productivo simulado" y §"Sembrar la demostración"
- `docs/specs/06-arquitectura.md` §7 y §7.1
- `docs/plan/tareas/JZ-05-*.md` y `docs/plan/tareas/JZ-06-*.md` (`## Resultado`)

## Archivos que creas o modificas
- `src/Shapi.Infraestructura/Siembra/Demo/SiembraDemo.cs` (modificar)
- `infra/verificar.mjs` (modificar)
- `.github/workflows/publicar-imagenes.yml` (modificar, solo si hace falta para el criterio 2)
- `tests/Shapi.Api.Tests/**` (modificar)
- `docs/manual-tecnico.md` (modificar, si cambia algún comando)

## Criterios de aceptación
1. Con el ambiente productivo levantado (`docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml up -d --build`), `docker compose … exec trabajador dotnet Shapi.Trabajador.dll sembrar-demo` termina sin errores y deja los datos de `docs/manual-tecnico.md`. Hoy falla con `CultureNotFoundException: es-gt is an invalid culture identifier`, porque las imágenes Alpine corren en modo de globalización invariante y `SiembraDemo.cs:295` pide `CultureInfo.GetCultureInfo("es-GT")`.
2. El job obligatorio `ambiente-productivo` ejecuta la siembra contra el ambiente productivo y comprueba que, con una clave fija de la siembra, `POST https://envios.api.shapi.localhost/cotizaciones` responde 200. Las comprobaciones de que el ambiente arranca sin siembra se ejecutan antes de sembrar.
3. La siembra no depende de la cultura del sistema. Si un texto necesita el nombre del mes en español, se arma sin `CultureInfo` de un país (como ya hace la compuerta), o se agrega ICU a las imágenes y se documenta la decisión en el PR.
4. Hay una prueba automatizada que habría detectado el error: por ejemplo, la siembra ejecutada con `System.Globalization.Invariant=true`, o el paso del criterio 2.

## Pruebas obligatorias
- Integración de la siembra, o el paso del verificador en la CI (criterios 2 y 4)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~Siembra"
dotnet format Shapi.slnx --verify-no-changes
node infra/verificar.mjs
```

## Fuera de alcance
- Sembrar con un solo comando al levantar el ambiente (JZ-18)

## Notas
- Es P1 porque sin ella falla el paso 1 del guion 2, el de la demostración del viernes 9 de octubre. Mientras no esté integrada, la convergencia del 8 de octubre sembró con un parche local que no se confirmó.

## Resultado
La terminó el coordinador (Jordin), porque era P1 para la demostración del 9 de octubre y José Pablo no la había empezado (protocolo §E1).

- **Causa:** `SiembraDemo.cs` armaba la fecha del mensaje del caso CAS-104 con `CultureInfo.GetCultureInfo("es-GT")`. Las imágenes `dotnet/aspnet:10.0-alpine` corren en modo de globalización invariante, sin ICU, y en ese modo pedir una cultura de país lanza `CultureNotFoundException`.
- **Corrección (criterio 3):** la fecha se arma con `DiaYMes`, a partir de una tabla propia de meses en español, como `FiltroLimitesYCuotas.Fecha` en la compuerta. La siembra ya no depende de la cultura del sistema. No se agregó ICU a las imágenes.
- **Prueba que lo habría detectado (criterio 4):** `tests/Shapi.Api.Tests` corre con `<InvariantGlobalization>true</InvariantGlobalization>`, igual que los contenedores. Con el código anterior, 10 pruebas de `SiembraDemoTests` fallaban con la misma excepción del contenedor. `RNF_14_LasPruebasCorrenSinCulturasDePais_ComoLasImagenesAlpine` impide que se quite ese modo sin que se note. El mensaje esperado de CAS-104 es ahora el texto literal («24 de septiembre»), en lugar de formatearse con la cultura en la prueba.
- **CI (criterio 2):** `node infra/verificar.mjs --sembrar` primero hace todas las comprobaciones de un ambiente sin siembra (incluida la de que `envios.api.shapi.localhost` responde 404). Después ejecuta `sembrar-demo` dentro del contenedor del trabajador y comprueba que `POST https://envios.api.shapi.localhost/cotizaciones`, con la clave de producción de Mercadito Antigua, responde 200. El paso del job `ambiente-productivo` en `publicar-imagenes.yml` usa esa opción, y el verificador exige que la mantenga. Sin el ambiente productivo levantado, `--sembrar` falla con un mensaje claro.
- **Archivos:** `src/Shapi.Infraestructura/Siembra/Demo/SiembraDemo.cs`, `infra/verificar.mjs`, `.github/workflows/publicar-imagenes.yml`, `tests/Shapi.Api.Tests/Shapi.Api.Tests.csproj`, `tests/Shapi.Api.Tests/Siembra/SiembraDemoTests.cs` y `docs/manual-tecnico.md` (§"Levantar y verificar").
