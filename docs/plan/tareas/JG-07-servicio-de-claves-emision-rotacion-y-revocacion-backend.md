---
id: JG-07
titulo: Servicio de claves: emisión, rotación y revocación (backend)
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P1
estado: pendiente
depende_de: [JG-04]
requisitos: [RF-26, RF-27, RF-28, RNF-07]
pantallas: []
---

# JG-07 · Servicio de claves: emisión, rotación y revocación (backend)

**Responsable:** Jordin García · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** JG-04

## Objetivo
Implementar el ciclo de vida completo de las claves de API: emisión (que usará la contratación, EM-08), rotación y revocación, y sus endpoints para el portal y para el panel.

## Contexto que debes leer
- `docs/specs/08-compuerta.md` §2 (formato de la clave)
- `docs/specs/07-modelo-de-datos.md` §3.3 (`clave`) y §5 (estados de la clave)
- `docs/specs/10-identidad-y-seguridad.md` §3 y §7 (acciones `clave.*`)
- `docs/specs/04-roles-y-permisos.md` §3.1 y §3.3
- `docs/specs/05-casos-de-uso.md` CU-13
- `docs/specs/06-arquitectura.md` §5.4
- Mockups: `mockups/A4/Claves.dc.html`, `mockups/B2/Suscripcion.dc.html`, `mockups/B2/RotarClave.dc.html`, `mockups/B2/ClaveNueva.dc.html`

## Archivos que creas o modificas
- `src/Shapi.{Dominio,Aplicacion,Infraestructura,Api}/Claves/**` (crear)
- `src/Shapi.Api/Modulos/ClavesModulo.cs` (modificar)
- `contratos/openapi/claves.yaml` (crear)
- `tests/*/Claves/**` (crear)

## Criterios de aceptación
1. El servicio de aplicación `EmitirClavesParaSuscripcion(suscripcionId)` crea una clave `produccion` y otra `pruebas` con formato `shp_prod_`/`shp_prueba_` + 26 caracteres base62 (`RandomNumberGenerator`). Guarda solo el prefijo, los últimos 4 caracteres y el SHA-256, las publica en Redis y devuelve los valores en claro **una sola vez**.
2. `POST /api/portal/claves/{id}/rotar` (el consumidor dueño) crea una clave nueva activa y deja la anterior como `rotada` con `expira_en = ahora + 24 h` (EXPIREAT en Redis). Devuelve la clave nueva en claro. Rotar una clave que ya está rotada → 422 `clave_no_rotable`.
3. `POST /api/portal/claves/{id}/revocar` (el consumidor dueño) y `POST /api/apis/{apiId}/claves/{id}/revocar` (propietario o editor de la organización) marcan la clave como `revocada`, guardan `revocada_por` y la eliminan de Redis. La compuerta responde 401 en menos de 10 s.
4. `POST /api/portal/claves/emitir` `{tipo}` emite una clave nueva de ese tipo solo si no hay otra activa del mismo tipo.
5. `GET /api/apis/{apiId}/claves` (proveedor: enmascaradas y agrupadas por consumidor, con el plan, el tipo y el estado, igual que A4.3) y `GET /api/portal/claves` (consumidor: enmascaradas, con estado y `expira_en`).
6. Bitácora: `clave.revocada_por_proveedor`, `clave.rotada` y `clave.revocada_por_consumidor`, con los textos de 10 §7.
7. Una clave completa nunca aparece en registros ni en respuestas, salvo en la emisión y en la rotación.
8. Aislamiento: la clave de otro consumidor o de otra organización → 404.

## Pruebas obligatorias
- Unitarias: formato, entropía (longitud y alfabeto) y hash
- Integración de cada endpoint, con Redis y PostgreSQL
- Aislamiento entre organizaciones y entre consumidores

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Pantalla A4.3 (JG-10)
- Pantallas B2.3 a B2.6 (DC-11)
- Contratación (EM-08)
