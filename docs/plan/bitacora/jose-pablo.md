# Bitácora de José Pablo Zúñiga

Cada tarea terminada agrega una entrada **al final** de este archivo (protocolo, paso B10):

```
## AAAA-MM-DD · <ID> · <título>
- Hecho: ...
- Decisiones: ...
- Pendiente o aviso para otros: ...
```

---

## 2026-09-23 · JZ-01 · Infraestructura local: Docker Compose, Caddy, TLS y Mailpit
- Hecho: se creó Compose para PostgreSQL, Redis, Mailpit y Caddy; se configuraron HTTPS, enrutamiento, cabeceras, TLS bajo demanda, variables de entorno, documentación y verificación automatizada.
- Decisiones: Caddy sobrescribe las cabeceras del origen de forma diferida; el verificador usa orígenes locales controlados y espera hasta 90 segundos por la salud de todos los servicios.
- **JZ-02 y JZ-06:** JZ-02 puede agregar `origen-envios` y `origen-agro` a `infra/compose.yml`; JZ-06 puede extender este entorno con las imágenes de la aplicación.
- **Jordin y DC-01:** en Linux nativo, Kestrel debe escuchar en `0.0.0.0` y Vite debe usar `server.host: "0.0.0.0"` y `server.hmr.clientPort: 443` para que Caddy alcance los procesos del equipo.

## 2026-09-23 · JZ-02 · Orígenes de demostración (Envíos Xelajú y Agro Precios)
- Hecho: se crearon ambas Minimal APIs .NET 10, sus contratos OpenAPI 3.0.3, Dockerfiles, servicios de Compose y pruebas automatizadas de rutas, ejemplos, seguridad y salud.
- Decisiones: los datos son deterministas y reproducen los mockups; el secreto del origen es opcional y se compara en tiempo constante cuando está configurado.
- Pendiente o aviso para otros:
  - **JZ-05:** puede registrar Envíos Xelajú en `http://origen-envios:8080` y Agro Precios en `http://origen-agro:8080` dentro de Compose.
  - **JZ-06:** los Dockerfiles de ambos orígenes ya están listos para el ambiente productivo simulado.

## 2026-09-25 · JZ-03 · Envío de correos desde la bandeja de salida
- Hecho: se implementó el procesamiento cada 5 segundos, envío SMTP con MailKit, remitente configurable, plantillas HTML/texto de verificación y recuperación, escape HTML y cinco intentos con espera progresiva. La integración entrega el correo a Mailpit y lo comprueba con su API.
- Decisiones: los enlaces canónicos son `/verificar-correo?token=` y `/restablecer?token=` sobre `SHAPI_DOMINIO_BASE`; el quinto fallo conserva el último intervalo calculado de 1 hora aunque el estado ya sea `fallido`; cada correo guarda su resultado antes de procesar el siguiente.
- Pendiente o aviso para otros:
  - **EM-03:** `verificacion_correo` recibe `{ nombre, token }` y el enlace enviado apunta a `/verificar-correo?token=`; la pantalla ya puede consumir ese token.
  - **EM-04:** `recuperacion` recibe `{ nombre, token }` y el enlace enviado apunta a `/restablecer?token=`. Puede incluir `nombrePortal` para usarlo como remitente visible.
  - **JZ-11:** para agregar plantillas, cree el par `.html`/`.txt` en `Shapi.Infraestructura/Correo/Plantillas`; los marcadores `{{campo}}` se toman de `datos` y se escapan en HTML.
