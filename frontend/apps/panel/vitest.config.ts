import { defineConfig } from 'vitest/config';

export default defineConfig({
  server: { fs: { allow: ['../../..'] } },
  test: {
    environment: 'jsdom',
    environmentOptions: { jsdom: { url: 'http://localhost/' } },
    setupFiles: ['../../test/servidor.ts'],
    globals: true,
  },
});
