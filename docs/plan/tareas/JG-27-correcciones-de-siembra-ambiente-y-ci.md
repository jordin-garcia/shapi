---
id: JG-27
titulo: Correcciones de la siembra, el ambiente y la CI
persona: jordin
responsable: Jordin García
avance: final
prioridad: P3
estado: hecha
programada: 2026-10-27
depende_de: [JG-23, JG-24]
requisitos: [RNF-05, RNF-14, RNF-15, RF-39]
pantallas: [B2.1, B3.1]
---

# JG-27 · Correcciones de la siembra, el ambiente y la CI

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P3 · **Depende de:** JG-23 (que también toca `CierreCiclo.cs`) y JG-24 (que también toca la siembra)

## Objetivo
Que el ambiente productivo simulado, levantado con la siembra, se vea igual que los mockups en el video, aunque se haya usado antes. Además, cerrar los detalles pendientes de la CI y del verificador. Son hallazgos de la convergencia del 23 oct, del ensayo del 18 oct y de la auditoría del 3 oct (`docs/plan/convergencia/2026-10-22.md` §4), que Jordin decidió corregir él (protocolo §E1). Los archivos son de JZ-05, JZ-06, JZ-07, JZ-12, JZ-14, JZ-17 y JG-12.

Es P3: si no cabe antes del jueves 29, queda fuera, salvo H-C27 y H-C30, que se ven en el video.

## Hallazgos que corrige
- **H-C27 (media, JZ-17 y JG-12; H-E12):** el encabezado de B2.1 lee la cuota de Redis («1 de 50,000») y la tabla suma `consumo_diario` (31,481). La siembra no llena los contadores `cuota:susc:*` de los consumidores sembrados.
- **H-C29 (baja, JZ-12; H-E14):** después de un arranque en frío, B3.1 marca la compuerta como **degradada**. El 23 oct mostró un p95 de 675 ms; el 18 oct, de 463 ms. Las primeras peticiones después del arranque cuentan en la ventana de la última hora.
- **H-C30 (media, JZ-05; H-E15):** `sembrar-demo --reiniciar` no borra los datos que dejan los ensayos y las E2E: organizaciones como «Café del Volcán», «Organización 1792…» u «Organizacion E2E …», sus pagos y sus casos. Aparecen en A6.2, A6.3 y A6.4.
- **H-C31 (baja, JZ-06):**
  - `infra/verificar.mjs:338-339` acepta cualquier valor de `--appendonly` y `--appendfsync`, no exige `yes` ni `everysec` (RNF-05).
  - `infra/verificar.mjs:710-713` falla si el volumen `caddydata` ya tiene el certificado de `api.enviosxelaju.localhost`. Pasó el 23 oct, después de verificar ese dominio en el ensayo del 18.
- **H-C32 (baja, JZ-14):** las pruebas de Node de `tests/carga/` (`comparar.test.js` y `comun.test.js`) no corren en la CI. `ci.yml:35` solo ejecuta `scripts/*.test.mjs`.
- **H-C33 (baja, JZ-06):** el trabajador del ambiente productivo registra cada consulta SQL en el nivel `Information`: más de 100 KB solo en la siembra. Además, avisa `Cannot load library libgssapi_krb5.so.2` (Npgsql en Alpine). Viene de la convergencia del 8 oct, §6.
- **H-C34 (baja, varios):** código muerto y códigos de error escritos a mano:
  - `frontend/apps/portal/src/paginas/PaginaRelleno.tsx` no se importa en ningún lado;
  - `IVerificadorLimitesPlan.PermiteDominioPropio` (`VerificadorLimitesPlan.cs:11,25`) no se llama;
  - `"pago_rechazado"` está escrito a mano en `CierreCiclo.cs:128` y `:196`.
- **H-116 (baja, JZ-06):** nada comprueba que Caddy corra como `caddy` (H-45).
- **H-117 (baja, JZ-06):** `publicar-imagenes.yml:35`: `docker/setup-buildx-action@v4` no está fijada por SHA y corre en el job con `packages: write`. La excepción de `reglas-repositorio.test.mjs:219` se justifica con un argumento que no es cierto.
- **H-118 (baja, JZ-07, documentación):** el criterio 4 de JZ-07 y su `## Resultado` dicen que las E2E no son obligatorias. Desde JG-18 corren en el check obligatorio `ambiente-productivo`.
- **H-C35 (baja, RNF-15):** la protección de `main` tiene `enforce_admins: false`, así que un administrador puede integrar sin que pasen los checks. Es lo que permite `gh pr merge --admin` en el protocolo B11.

## ❓ Decisiones pendientes (preguntarle a Jordin antes de escribir código)
1. **B3.1 después de un arranque en frío (H-C29).** Recomiendo no cambiar el cálculo de RF-39. En su lugar, que `verificar.mjs --demo` caliente la compuerta y que el manual técnico y el guion del video lo indiquen. La otra opción es descartar las muestras del primer minuto de cada instancia, y precisarlo en 06 §8.
2. **Datos de ensayos (H-C30).** (2a, recomendada) Que `--reiniciar` borre todas las organizaciones que no son de la siembra, junto con sus datos, pero sin tocar la bitácora (append-only). (2b) La otra opción es documentar que, para grabar, se borran los volúmenes.
3. **`enforce_admins` (H-C35, duda 14 del 3 oct incluida).** Recomiendo dejarlo así y precisarlo en RNF-15 y en el protocolo («solo el coordinador, ante un falso positivo de `revision-claude`»). Separar el job de construcción sin `packages: write` en los PR, como pedía la duda 14, sí se hace.

### Decidido por Jordin (2026-10-28)
- H-C29: calentar desde el verificador y documentarlo, conservando RF-39.
- H-C30: borrar organizaciones ajenas a la siembra y sus datos, conservando la bitácora. Autorizó además quitar mediante migración únicamente la FK de `bitacora.organizacion_id`, conservando sus UUID y filas históricas, para permitir el borrado.
- H-C35: conservar `enforce_admins: false`, precisar la excepción manual del coordinador y separar la construcción de PR de la publicación con permisos.

## Contexto que debes leer
- `docs/specs/06-arquitectura.md` §7 y §8
- `docs/specs/07-modelo-de-datos.md` §4 (llaves de cuota)
- `docs/specs/03-requisitos.md` RNF-05, RNF-14, RNF-15 y RF-39
- `docs/manual-tecnico.md` («Sembrar la demostración»)
- Los `## Resultado` de JZ-05, JZ-06, JZ-12, JZ-14 y JZ-17

## Archivos que creas o modificas
- `src/Shapi.Infraestructura/Siembra/Demo/SiembraDemo.cs` (modificar)
- `infra/verificar.mjs`, `infra/compose.prod.yml` y la configuración de registros del trabajador (modificar)
- `.github/workflows/ci.yml` y `publicar-imagenes.yml`, y `scripts/reglas-repositorio.test.mjs` (modificar)
- `frontend/apps/portal/src/paginas/PaginaRelleno.tsx` (borrar) y `src/Shapi.Aplicacion/Suscripciones/VerificadorLimitesPlan.cs` (modificar)
- `src/Shapi.Trabajador/CierreCiclo/CierreCiclo.cs` (modificar, después de JG-23)
- `docs/plan/tareas/JZ-07-*.md`, `docs/manual-tecnico.md`, `docs/specs/03-requisitos.md` y `docs/plan/protocolo.md` (precisar)
- Pruebas (crear o modificar)

## Criterios de aceptación
1. Con la siembra, el encabezado de B2.1 y su tabla dan el mismo total para cada consumidor sembrado (RF-37).
2. H-C29, H-C30 y H-C35, según sus decisiones.
3. `verificar.mjs` exige `--appendonly yes` y `--appendfsync everysec`, y pasa con un volumen `caddydata` que ya tenga certificados de dominios propios.
4. Las pruebas de `tests/carga/` corren en la CI.
5. El trabajador del ambiente productivo no registra las consultas SQL en `Information`, y no avisa `libgssapi_krb5`.
6. H-C34, H-116, H-117 y H-118 quedan corregidos como dice su línea.

## Pruebas obligatorias
- `SiembraDemoTests`: la cuota de Redis de cada consumidor sembrado coincide con su consumo del ciclo (RNF-14).
- Si se elige la opción 2a: `--reiniciar` con una organización de ensayo la borra (RNF-14).
- `reglas-repositorio.test.mjs` para H-116 y H-117 (RNF-15).

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
node --test "scripts/*.test.mjs"
dotnet build Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~Shapi.Api.Tests.Siembra"
node infra/verificar.mjs --demo
```

## Fuera de alcance
- Volver a medir las pruebas de carga: si la compuerta cambió mucho, lo decide JG-17.

## Resultado

- H-C27: la siembra inicializa después del commit las cuotas de suscripción y organización con consumo de producción del ciclo. Aplica el TTL de 07 §4 incluso con fechas pasadas o adelantadas. Repetir sin reiniciar completa cuotas ausentes y conserva contadores existentes.
- H-C30: el reinicio transaccional borra las organizaciones de ensayos y sus datos y restablece la demostración. La migración autorizada `ReferenciaHistoricaBitacora` quita únicamente la FK de `bitacora.organizacion_id`, conservando el índice, los UUID y todas las filas históricas. Conserva la comparación de descripciones con o sin punto final.
- H-C29: el verificador calienta con 90 cotizaciones reales de pruebas, en tres rondas de diez por cada una de tres claves, respetando `Retry-After`. Manual, arquitectura y preparación del video lo documentan. La ventana y el cálculo de RF-39 conservan todas las muestras: en este equipo el ensayo final midió 36,5 ms y mostró correctamente la compuerta degradada y los otros seis componentes en servicio; el calentamiento no promete ocultar una degradación real.
- H-C31: exige AOF `yes` y fsync `everysec`, incluso si una opción se repite. Un certificado almacenado permite completar TLS, pero los endpoints internos siguen devolviendo 404.
- H-C32: el check `plan` ejecuta también `tests/carga/*.test.js`.
- H-C33: el trabajador espera las migraciones de la API, registra comandos SQL desde `Warning` y usa `GSS Encryption Mode=Disable` en el ambiente simulado. El ensayo desde volúmenes vacíos terminó sin errores ni consultas SQL en los registros del trabajador y sin el aviso de GSSAPI.
- H-C34: se eliminó `PaginaRelleno.tsx` del portal y el método sin consumidores de `IVerificadorLimitesPlan`; los errores de renovación usan `CodigosError.PagoRechazado`.
- H-116 y H-117: el verificador comprueba al usuario real de Caddy en `/proc/1`; las acciones de Docker quedan fijadas por SHA y se elimina la excepción de Buildx en las reglas.
- H-118 y H-C35: JZ-07 e instalación explican las E2E obligatorias; la construcción de PR usa solo lectura y la publicación con permiso de paquetes corre solo en push a `main`. RNF-15, ADR-31 y B11 precisan la excepción manual aprobada de Jordin ante un falso positivo de revisión, con los demás checks aprobados y sin pruebas fallidas. La protección existente conserva `enforce_admins: false` y sus seis checks obligatorios.
- Verificación: plan válido (89 tareas), 125 pruebas Node, compilación completa con cero errores y advertencias, formato completo correcto, 33 pruebas de siembra y las 1117 pruebas de API aprobadas sin omisiones. Frontend: lint sin errores (11 avisos previos), typecheck y builds correctos y 537 pruebas en 43 archivos aprobadas. La regresión aislada de Perfil pasó de 1/10 fallida a 10/10 correctas preparando su módulo real antes de comprobar el formulario, sin cambiar aserciones ni plazos.
- Ensayo real aislado `shapi-jg27`: borrado de una organización con historial conservando literalmente su bitácora, repetición idempotente, certificado emitido y reutilizado tras reiniciar, verificador `--demo` aprobado y comparación de B2.1 y B3.1 con sus mockups y las precisiones de 06 §8. Los cinco totales de cuota y tabla coincidieron: 31481, 4116, 69600, 1436 y 0. El primero incluye la llamada de producción del verificador. Capturas, resultados y registros locales en `.shapi/jg27/`.
- Sin dependencias nuevas ni cambios de endpoints o contratos HTTP.
