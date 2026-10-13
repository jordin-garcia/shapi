---
id: JZ-12
titulo: Estado de los componentes (B3.1)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 3
prioridad: P2
estado: hecha
programada: 2026-10-14
depende_de: [JZ-06, JG-09]
requisitos: [RF-39]
pantallas: [B3.1]
---

# JZ-12 · Estado de los componentes (B3.1)

**Responsable:** José Pablo Zúñiga · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** JZ-06, JG-09

## Objetivo
Mostrarle al administrador y al soporte el estado de cada componente y la latencia que añade la compuerta.

## Contexto que debes leer
- `docs/specs/06-arquitectura.md` §8 (tabla de estados)
- `docs/specs/07-modelo-de-datos.md` §4 (latidos y medición de la última hora)
- `docs/specs/08-compuerta.md` §7 (histogramas y p95)
- Mockups: `mockups/B3/Main.dc.html`, `Degradado.dc.html`, `Soporte.dc.html`

## Archivos que creas o modificas
- `src/Shapi.Trabajador/Estado/**` (crear: latido `salud:trabajador`)
- `src/Shapi.{Aplicacion,Api}/Estado/**` (crear)
- `src/Shapi.Api/Modulos/EstadoModulo.cs` (registrar el módulo)
- `src/Shapi.Infraestructura/Estado/**` (crear: sondas de salud)
- `src/Shapi.Compuerta/Medicion/MedicionMiddleware.cs` y `src/Shapi.Contratos/Redis/LlavesRedis.cs` (medición global de la última hora)
- `infra/compose.prod.yml` (configuración SMTP de la API)
- `contratos/openapi/sistema.yaml` (modificar)
- `frontend/apps/panel/src/paginas/B3-1-Estado.tsx`
- `tests/**`

## Criterios de aceptación
1. El trabajador escribe `salud:trabajador` cada 10 s, con TTL de 30 s.
2. `GET /api/admin/estado` (administrador y soporte) calcula, con caché de 30 s, el estado de los 7 componentes según la tabla de 06 §8, más la latencia p95 de la compuerta en la última hora y la fecha de la comprobación.
3. B3.1 reproduce las tres variantes: normal, un componente degradado (con el aviso) y la vista del soporte.

## Pruebas obligatorias
- Unitarias de las reglas de cada componente
- Vitest

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Alertas por correo

## Notas
- El 12 de octubre, José Pablo autorizó ampliar el alcance a los archivos del módulo e infraestructura, agregar la medición de la última hora en los archivos de Jordin y configurar SMTP en la API productiva. La medición se completa en esta tarea, sin crear una dependencia para Jordin.
- El latido del trabajador ya lo implementó JG-09, en `Consolidacion/TrabajoConsolidacion.cs`; se reutiliza sin duplicar el servicio.

## Resultado

- Se implementó `GET /api/admin/estado`, protegido para administrador y soporte, con las reglas de los siete componentes de 06 §8, fecha de comprobación y caché concurrente de 30 segundos en memoria. Las sondas de HTTP local, Redis, PostgreSQL y SMTP tienen plazos independientes y no envían correos.
- Se reutilizó el latido del trabajador de JG-09. Se comprueban los TTL de ambos procesos y la consolidación reciente solo cuando hay métricas identificadas o lotes pendientes, sin confundir el reloj real con el de demostración.
- Se agregó el histograma global de la última hora en la compuerta: campos por segundo y rango, agrupados por minuto, con TTL de 65 minutos, en el mismo pipeline sin espera. Conserva la consolidación diaria y agrega la llave en `LlavesRedis`. La consulta interpola el p95 y distingue ausencia de muestras y rango abierto.
- B3.1 muestra las variantes normal, degradada y soporte, se actualiza cada 30 segundos y contempla carga, error, reintento y latencia no disponible. Se actualizaron el contrato, los tipos generados y las precisiones de arquitectura, medición y modelo de Redis. La API productiva recibe la configuración SMTP de Mailpit.
- Verificación: fase roja confirmada; compilación completa sin errores ni advertencias; formato completo correcto; 33 pruebas dirigidas de Estado y del latido del trabajador, 285 pruebas de la compuerta y 380 pruebas del frontend correctas, sin omitidas. Lint, TypeScript y build correctos. La suite del frontend pasó con `pnpm test --maxWorkers=1` para evitar saturación durante los builds Docker; no se alteraron pruebas ni plazos.
- Se construyó y sembró un ambiente productivo aislado (`shapi-jz12`, puertos 8443/8088 y volúmenes propios), preservando el ambiente existente. Chromium comprobó la API real con siete componentes en servicio, caché compartida, acceso de administrador y soporte, y comparó normal/degradado/soporte con datos controlados de sus mockups. Capturas locales: `.shapi/jz12/`.
- Revisión independiente según B9: LISTO, sin hallazgos de corrección ni opcionales.
- Archivos principales: `Aplicacion/Estado/`, `Infraestructura/Estado/`, `Api/Estado/`, `Api/Modulos/EstadoModulo.cs`, `Compuerta/Medicion/MedicionMiddleware.cs`, `Contratos/Redis/LlavesRedis.cs`, `B3-1-Estado.tsx`, `contratos/openapi/sistema.yaml`, `infra/compose.prod.yml` y sus pruebas.
