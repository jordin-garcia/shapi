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
# Sin privilegios, como las demás imágenes (auditoría 2026-10-03, H-45). El binario de la imagen oficial ya trae
# cap_net_bind_service, así que puede escuchar en 80 y 443; /data (certificados) y /config son suyos.
RUN addgroup -S caddy && adduser -S -D -H -G caddy caddy && chown -R caddy:caddy /data /config
USER caddy
