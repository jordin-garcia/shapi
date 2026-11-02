---
id: JZ-15
titulo: Manual técnico
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P1
estado: hecha
programada: 2026-10-26
depende_de: [JG-16]
no_antes_de: 2026-10-24
requisitos: []
pantallas: []
---

# JZ-15 · Manual técnico

**Responsable:** José Pablo Zúñiga · **Avance:** final · **Prioridad:** P1 · **Depende de:** JG-16 · **No antes del:** 2026-10-24

## Objetivo
Completar el manual técnico que exigen los lineamientos, con la estructura que pidió la docente del curso, en Markdown y en Word.

## Contexto que debes leer
- `docs/lineamientos.md` §7 (tecnologías, instalación, configuración y despliegue)
- `docs/specs/01-vision-y-alcance.md`, `03-requisitos.md`, `05-casos-de-uso.md`, `07-modelo-de-datos.md` y `11-interfaz.md` §3
- `docs/specs/06-arquitectura.md`
- `docs/plan/instalacion.md` y `docs/plan/calendario.md`
- `.env.example`

## Archivos que creas o modificas
- `docs/manual-tecnico.md` (completar)
- `docs/manual-tecnico/` (capturas de los mockups que usa el manual)
- `docs/Manual_Tecnico.docx` (versión en Word del mismo contenido)

## Criterios de aceptación
0. Sigue la estructura que pidió la docente (1 nov 2026), en este orden: portada, índice, introducción, descripción del sistema, objetivos (general y específicos), alcance, requisitos funcionales y no funcionales (tablas «ID · Requisito»), arquitectura, definición del modelo, cronograma (pronosticado y real), diseño de interfaces (mockups), diagramas (UML, de flujo y de bases de datos: entidad-relación y relacional), tecnologías utilizadas, manual de instalación, problemas que se pueden presentar, respaldo y recuperación de datos, y anexos. El Word tiene portada (universidad, curso, proyecto e integrantes con su carné) e índice.
1. Tiene: tecnologías usadas (con su versión y su justificación, en resumen), requisitos, instalación en Windows y en Linux, configuración (**todas** las variables de entorno con su descripción y su valor por defecto), despliegue en el ambiente productivo simulado (imágenes de GHCR y compose), siembra y cuentas de demostración, modo demostración y reloj, respaldo y restauración de PostgreSQL (`pg_dump`), disponibilidad (RNF-06 como objetivo de diseño, con los mecanismos que lo respaldan según 03 §2, sin medición) y solución de problemas frecuentes (certificado, puertos ocupados, Docker).
2. Cada comando del manual se ejecutó al menos una vez para comprobarlo. Excepción decidida por Jordin el 1 nov 2026: los comandos exclusivos de Linux (`apt`, `update-ca-certificates`, `certutil` y `systemctl`), que vienen de la versión anterior del manual, no se repiten si no hay un equipo Linux disponible, y el manual lo dice.
3. El manual se entrega en Markdown (`docs/manual-tecnico.md`) y en Word (`docs/Manual_Tecnico.docx`), con el mismo contenido, la portada del proyecto y los diagramas como imágenes. No se genera PDF (decisión de Jordin del 1 nov 2026).

## Pruebas obligatorias
- Seguir el manual desde cero en un equipo limpio (o después de `docker compose down -v`)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Manual de usuario (JZ-16)

## Resultado

Lo terminó el coordinador (Jordin, protocolo §E1) el 1 de noviembre de 2026, a partir del manual que había. La docente pidió una estructura distinta a la de la versión anterior, y Jordin decidió que se entregue en Markdown y en Word, sin PDF.

- **`docs/manual-tecnico.md`** sigue la estructura de la docente: portada, índice y 15 capítulos.
  - Generados de las fuentes: los requisitos (de 03), los ADR (de 12), el catálogo de pantallas (de 11 §3), el cronograma (del historial de git) y el modelo relacional (de `ShapiDbContextModelSnapshot.cs`, 23 tablas).
  - «Definición del modelo» es el modelo de proceso: incremental y dirigido por especificaciones (decisión de Jordin).
  - Cronograma: lo pronosticado es la primera fecha `programada` de cada tarea; lo real, el día en que la tarea quedó `hecha` en `main`.
  - Mockups: 22 capturas, una o dos por tanda, en `docs/manual-tecnico/mockups/`, más el catálogo completo de 64 pantallas.
  - 32 diagramas Mermaid. Los UML, ER y de la compuerta son copia de las especificaciones; tres flujos son nuevos.
  - Anexo A: las 27 variables `SHAPI_*` y tres ajustes de .NET (`Sesion__InactividadHoras`, `Autenticacion__LimitePorMinuto` y `Pagos__DemoraMs`), con su descripción, su valor por defecto y su valor en producción.
- **`docs/Manual_Tecnico.docx`**: el mismo contenido, con la portada (escudo de la URL e integrantes con su carné), el índice actualizado por Word, tablas, capturas y los diagramas en PNG. Lo genera `~/.shapi-demo/manual-tecnico/generar_docx.py` (python-docx y mermaid-cli 11.12.0), que queda fuera del repositorio. Copia en `C:\Users\Hecto\Documents\Shapi_Manual_Tecnico_2026-11-01.docx`.
- **Comandos ejecutados** el 1 nov en Windows, en el proyecto aislado `shapi-manual`:
  - el entorno de desarrollo completo, incluidas la carga del `.env` en bash y en PowerShell, `dotnet run` y `pnpm dev` a través de Caddy;
  - el ambiente productivo con `--build`, el `pull` de GHCR, `verificar.mjs` y `--demo`, `logs -f`, `sembrar-demo --reiniciar` y el reinicio del reloj con `DEL demo:reloj:desplazamiento`;
  - una llamada a la API con la clave de pruebas;
  - el respaldo con `pg_dump` y la copia de `dpkeys` y `redisdata`;
  - la recuperación después de `down -v`: conteos iguales y la compuerta respondiendo 200 con el secreto descifrado;
  - la restauración sobre una base con datos.
  Los comandos exclusivos de Linux no se repitieron (excepción del criterio 2).
- **Otros archivos:**
  - `infra/verificar.mjs` exige ahora las secciones numeradas «Entorno de desarrollo» y «Ambiente productivo simulado» con una expresión regular sobre el título.
  - `docs/pdf/README.md` ya no genera el manual técnico en PDF.
  - `docs/plan/instalacion.md` apunta a 12.8.
  - JG-17 ya no regenera este manual en PDF y recibe el hallazgo de las llaves de Data Protection en desarrollo (criterio 5).
- **Revisión en contexto limpio:** 7 hallazgos de corrección, todos corregidos. El de los comandos de Linux se resolvió con la excepción que decidió Jordin. También se aplicaron los 10 opcionales.
