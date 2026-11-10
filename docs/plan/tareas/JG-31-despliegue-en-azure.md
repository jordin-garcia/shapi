---
id: JG-31
titulo: Despliegue público en Azure y documentación para la presentación final
persona: jordin
responsable: Jordin García
avance: final
prioridad: P1
estado: hecha
programada: 2026-11-10
depende_de: []
requisitos: [RNF-09, RNF-14]
pantallas: []
---

# JG-31 · Despliegue público en Azure y documentación para la presentación final

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P1

## Objetivo
Que el despliegue público de Shapi en Azure (`https://shapi.tech`), hecho el 9 de noviembre de 2026 para la presentación final, quede versionado y documentado: la configuración propia del servidor en el repositorio, la decisión en un ADR, el ambiente en 06 §7 y los pasos en el manual técnico, de modo que otra persona pueda repetirlo sin ayuda.

## Origen
Los lineamientos de la presentación final (11 y 13 de noviembre de 2026) califican la "Funcionalidad demostrada" con 14–15 puntos solo "en ambiente desplegado", y con 6–10 si "el sistema solo corre en local". Además, la "Documentación técnica" exige que el manual técnico permita "instalar y desplegar sin ayuda" y que sea coherente con el sistema demostrado. El 9 de noviembre Jordin decidió desplegar en Azure for Students con el dominio `shapi.tech`.

## Contexto que debes leer
- `docs/specs/06-arquitectura.md` §7 (Despliegue).
- `docs/specs/12-decisiones.md` ADR-08 y ADR-09.
- `docs/specs/10-identidad-y-seguridad.md` §1 (verificación de correo) y §4 (SSRF y modo demostración).
- `docs/manual-tecnico.md` capítulo 12 y Anexo A.
- `infra/compose.yml`, `infra/compose.prod.yml` e `infra/caddy/Caddyfile.prod`.

## Archivos que creas o modificas
- `infra/compose.servidor.yml` (crear): borde en los puertos públicos y Mailpit con reenvío a Resend.
- `infra/caddy/Caddyfile.servidor` (crear): certificados de Let's Encrypt y Mailpit con contraseña.
- `docs/specs/12-decisiones.md` (modificar): ADR-36 y una nota en ADR-09.
- `docs/specs/06-arquitectura.md` (modificar): §7.1 y §7.2.
- `docs/manual-tecnico.md` y `docs/Manual_Tecnico.docx` (modificar): sección 12.11 y Anexo A.
- `docs/manual-usuario.md` (modificar): direcciones del ambiente desplegado.
- `docs/pdf/Documento_de_Diseno.pdf` y `docs/pdf/Manual_de_Usuario.pdf` (regenerar).
- `README.md` (modificar): el sistema desplegado y la documentación.
- `.gitignore` (modificar): los videos de las entregas no se versionan.

## Criterios de aceptación
1. El repositorio contiene la configuración del servidor tal como corre en `shapi.tech`, sin ningún secreto: las contraseñas, el hash de Mailpit y la clave de Resend se leen del `.env` del servidor.
2. ADR-36 registra la decisión, sus alternativas descartadas y sus consecuencias, y ADR-09 remite a él.
3. 06 §7 describe el ambiente desplegado en Azure junto a los demás ambientes.
4. El manual técnico explica, paso a paso, cómo crear la máquina virtual, apuntar el dominio, preparar el `.env`, levantar, re-sembrar, activar el correo real y apagar, con los mismos comandos que se usaron.
5. El manual de usuario y el README indican las direcciones del ambiente desplegado.
6. Los PDF generados desde las fuentes modificadas están actualizados.

## Pruebas obligatorias
- No aplica código nuevo de la aplicación. La configuración del servidor se valida con `docker compose config` y `caddy validate`, y el despliegue con las pruebas manuales de `https://shapi.tech` descritas en el `## Resultado`.

## Verificación
Todos estos comandos deben pasar:
```
node scripts/tareas.mjs --validar
node --test scripts/generar-pdf.test.mjs
docker compose --env-file .env.example -f infra/compose.yml -f infra/compose.prod.yml config --quiet
SHAPI_MAILPIT_HASH='$2a$14$prueba' SHAPI_RESEND_CLAVE=re_prueba docker compose --env-file .env.example -f infra/compose.yml -f infra/compose.prod.yml -f infra/compose.servidor.yml config --quiet
```

## Fuera de alcance
- Cambiar el código de la aplicación, que quedó congelado en JG-17.
- Automatizar el despliegue con la CI.
- Mover la página de inicio por encima del planeta: Jordin decidió dejarla como está.

## Notas
- El manual técnico se entrega en Markdown, Word y PDF (`docs/pdf/Manual_Tecnico.pdf`, exportado del Word).
- Al integrar se publica la etiqueta `v1.0.1` y su *release* con los cuatro PDF y el Word, y el QR de la documentación apunta a `https://github.com/jordin-garcia/shapi/releases/latest`.
- El Word del manual técnico tiene ajustes a mano en la portada: la sección nueva se inserta en él, sin regenerarlo.

## Resultado

**Despliegue (9 y 10 nov 2026).** Shapi corre en `https://shapi.tech`, en la máquina `shapi-vm` del grupo `shapi-rg` (Azure for Students, `Standard_B2as_v2`, North Central US, IP fija). El grupo de seguridad abre el 80 y el 443 a internet y el 22 solo a la IP del coordinador. El DNS está en Name.com, con tres registros A, y los registros de Resend (DKIM, `send`, `rsend` y DMARC) están verificados.

**Versionado:**
- `infra/compose.servidor.yml` e `infra/caddy/Caddyfile.servidor`, idénticos a los del servidor y sin secretos.
- Los secretos (PostgreSQL, administrador inicial, secretos de origen, hash de Mailpit y clave de Resend) están solo en el `.env` del servidor, con permisos `600`.

**Documentación:**
- ADR-36, con una nota en ADR-09.
- 06 §7.1, §7.2 y §9.
- El manual técnico: introducción del capítulo 12, 6.7, la sección nueva 12.11 (ocho pasos) y los Anexos A y B. El Word se actualizó insertando esos fragmentos sin regenerarlo, para conservar la portada, y se exportó a `docs/pdf/Manual_Tecnico.pdf`.
- El manual de usuario y el README.
- Se regeneraron el Documento de Diseño y el Manual de Usuario en PDF.

**Decisiones:**
- La configuración del servidor se versiona tal cual, con `shapi.tech` escrito, por decisión de Jordin.
- Los certificados de los subdominios de la plataforma se emiten *on-demand*. Caddy los autoriza en un puerto local (`localhost:5555`); los dominios propios siguen preguntando a `/interno/tls/autorizar`. Se descartó un certificado comodín porque exige un desafío DNS con un módulo de Caddy que no viene en la imagen.
- `correo.shapi.tech` también usa `tls { on_demand }`. Sin eso, Caddy lo daba por cubierto por el comodín `*.shapi.tech` y no le emitía certificado (alerta TLS `internal error`).
- El correo real pasa por Mailpit: Mailpit guarda una copia de todo y reenvía a Resend, salvo los dominios ficticios de la siembra, del guion y de los manuales. Así no hubo que cambiar la configuración SMTP de Shapi ni el código.
- El manual técnico se entrega también en PDF, porque los lineamientos de la presentación final piden los cuatro documentos en ese formato. Jordin lo exporta del Word.

**Verificación:**
- `node scripts/tareas.mjs --validar`: "Plan válido: 93 tareas".
- `node --test scripts/generar-pdf.test.mjs`: 6 aprobadas.
- `docker compose … config --quiet`: válido, con y sin `compose.servidor.yml` (con valores de prueba para el hash y la clave).
- `caddy validate`: "Valid configuration".
- En `https://shapi.tech`:
  - El panel, los portales de Envíos y Agro, la compuerta (200 con clave válida, 401 con una inventada) y Mailpit (401 sin contraseña) responden con certificados de Let's Encrypt.
  - `/interno` responde 404 desde internet.
  - Un registro con un correo real recibe el correo de verificación por Resend.
  - Una cuenta sin verificar no puede publicar ni contratar (422 `correo_no_verificado`).
  - `sembrar-demo --reiniciar` deja solo las cuentas de la siembra.
- La revisión en contexto limpio (subagente `revisor`) dejó 3 correcciones y 8 opcionales, y se aplicaron todas: el orden del reloj y la re-siembra, el costo real de la máquina, el *release* v1.0.1 en la tarea, los riesgos en ADR-36, la clave de pruebas en el manual y otras.

**Archivos principales:** `infra/compose.servidor.yml`, `infra/caddy/Caddyfile.servidor`, `docs/specs/12-decisiones.md` (ADR-36), `docs/manual-tecnico.md` (12.11) y `docs/pdf/Manual_Tecnico.pdf`.
