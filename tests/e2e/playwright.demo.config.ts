import { defineConfig } from '@playwright/test';

// Este ensayo adelanta el reloj global del ambiente demo. Se ejecuta separado de las E2E de datos base.
export default defineConfig({
  testDir: './demo',
  workers: 1,
  forbidOnly: true,
  retries: 0,
  timeout: 180_000,
  outputDir: '../../.shapi/jg22/demo',
  reporter: 'list',
  use: { ignoreHTTPSErrors: true, viewport: { width: 1440, height: 900 }, trace: 'retain-on-failure' },
});
