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
    && test -f node_modules/.pnpm/lightningcss-linux-x64-musl@1.32.0/node_modules/lightningcss-linux-x64-musl/lightningcss.linux-x64-musl.node \
    && test -f node_modules/.pnpm/@rolldown+binding-linux-x64-musl@1.2.9/node_modules/@rolldown/binding-linux-x64-musl/rolldown-binding.linux-x64-musl.node

COPY frontend/ ./
RUN pnpm build

FROM caddy:2-alpine AS final
COPY infra/caddy/Caddyfile.prod /etc/caddy/Caddyfile
COPY --from=frontend /fuente/frontend/apps/panel/dist /srv/panel
COPY --from=frontend /fuente/frontend/apps/portal/dist /srv/portal
