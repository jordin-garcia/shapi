import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    environment: 'jsdom',
    setupFiles: ['./test/servidor.ts'],
    globals: true,
    projects: [
      { extends: true, test: { name: 'ui', include: ['packages/ui/src/**/*.test.tsx'] } },
      { extends: true, test: { name: 'api', environment: 'node', include: ['packages/api/src/**/*.test.ts'] } },
      { extends: true, test: { name: 'panel', include: ['apps/panel/src/**/*.test.tsx'] } },
      { extends: true, test: { name: 'portal', include: ['apps/portal/src/**/*.test.tsx'] } },
      { test: { name: 'generador', environment: 'node', include: ['packages/api/*.test.mjs'] } },
    ],
  },
});
