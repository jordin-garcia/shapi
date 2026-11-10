---
id: JG-31
titulo: Despliegue público en Azure y documentación para la presentación final
persona: jordin
responsable: Jordin García
avance: final
prioridad: P1
estado: pendiente
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
```

## Fuera de alcance
- Cambiar el código de la aplicación, que quedó congelado en JG-17.
- Automatizar el despliegue con la CI.
- Mover la página de inicio por encima del planeta: Jordin decidió dejarla como está.

## Notas
- El manual técnico se entrega en Markdown y Word. Jordin exporta el Word a PDF.
- El Word del manual técnico tiene ajustes a mano en la portada: la sección nueva se inserta en él, sin regenerarlo.
