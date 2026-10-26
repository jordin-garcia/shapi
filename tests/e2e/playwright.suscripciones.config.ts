import { defineConfig } from '@playwright/test';

// JG-23: ambiente demo recién sembrado y aislado; no comparte el reloj global con el ensayo de JG-22.
export default defineConfig({
  testDir: './demo-suscripciones',
  workers: 1,
  forbidOnly: true,
  retries: 0,
  timeout: 180_000,
  outputDir: '../../.shapi/jg23/demo',
  reporter: 'list',
  use: { ignoreHTTPSErrors: true, viewport: { width: 1440, height: 900 }, trace: 'retain-on-failure' },
});
