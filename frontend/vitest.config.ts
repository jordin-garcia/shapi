import { defineConfig } from 'vitest/config';

export default defineConfig({
  server: { fs: { allow: ['..'] } },
  test: {
    environment: 'jsdom',
    environmentOptions: { jsdom: { url: 'http://localhost/' } },
    setupFiles: ['./test/servidor.ts'],
    globals: true,
    projects: [
      { extends: true, test: { name: 'ui', include: ['packages/ui/src/**/*.test.{ts,tsx}'], css: { include: [/style\.css/] } } },
      { extends: true, test: { name: 'api', environment: 'node', include: ['packages/api/src/**/*.test.{ts,tsx}'] } },
      // La lámina (/_ui) compara sus muestras con style.css: el proyecto necesita leerlo con ?raw.
      { extends: true, test: { name: 'panel', include: ['apps/panel/src/**/*.test.{ts,tsx}'], css: { include: [/style.css/] } } },
      { extends: true, test: { name: 'portal', include: ['apps/portal/src/**/*.test.{ts,tsx}'] } },
      { test: { name: 'generador', environment: 'node', include: ['packages/api/*.test.mjs'] } },
      { extends: true, test: { name: 'configuracion', environment: 'node', include: ['test/*.test.ts'] } },
    ],
  },
});
