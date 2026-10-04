FROM node:24-alpine AS frontend
WORKDIR /fuente/frontend

RUN corepack enable && corepack prepare pnpm@11.0.0 --activate
COPY frontend/package.json frontend/pnpm-lock.yaml frontend/pnpm-workspace.yaml ./
COPY frontend/apps/panel/package.json apps/panel/package.json
COPY frontend/apps/portal/package.json apps/portal/package.json
COPY frontend/packages/api/package.json packages/api/package.json
COPY frontend/packages/ui/package.json packages/ui/package.json
RUN pnpm config set fetch-retries 5 \
    && pnpm config set fetch-timeout 120000 \
    && pnpm install --frozen-lockfile \
    && node -e "Promise.all([import('vite'), import('@tailwindcss/vite')]).then(() => console.log('Binarios nativos cargados'))"

COPY frontend/ ./
RUN pnpm build

FROM caddy:2-alpine AS final
COPY infra/caddy/Caddyfile.prod /etc/caddy/Caddyfile
COPY --from=frontend /fuente/frontend/apps/panel/dist /srv/panel
COPY --from=frontend /fuente/frontend/apps/portal/dist /srv/portal
# Caddy corre sin privilegios, con el usuario caddy (UID 1000), como las demás imágenes (auditoría 2026-10-03, H-45). El
# binario oficial ya trae cap_net_bind_service, así que escucha en 80 y 443. El volumen caddydata (/data) lo comparte el
# borde de desarrollo, que corre como root y deja archivos de root: por eso el contenedor arranca como root solo para
# hacer a caddy dueño de /data y /config, y luego ejecuta Caddy con su-exec.
RUN apk add --no-cache su-exec && addgroup -S -g 1000 caddy && adduser -S -D -H -u 1000 -G caddy caddy
ENTRYPOINT ["/bin/sh", "-c", "chown -R caddy:caddy /data /config && exec su-exec caddy:caddy \"$0\" \"$@\""]
CMD ["caddy", "run", "--config", "/etc/caddy/Caddyfile", "--adapter", "caddyfile"]
