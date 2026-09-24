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
- Pendiente o aviso para otros: JZ-02 puede agregar `origen-envios` y `origen-agro` a `infra/compose.yml`; JZ-06 puede extender este entorno con las imágenes de la aplicación.
