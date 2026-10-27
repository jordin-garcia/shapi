import { defineConfig } from '@playwright/test';

// JG-24: ambiente demo nuevo y aislado; el ensayo avanza el reloj hasta la renovación.
export default defineConfig({
  testDir: './demo-dominios', workers: 1, forbidOnly: true, retries: 0, timeout: 180_000,
  outputDir: '../../.shapi/jg24/demo', reporter: 'list',
  use: { ignoreHTTPSErrors: true, viewport: { width: 1440, height: 900 }, trace: 'retain-on-failure' },
});
