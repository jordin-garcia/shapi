import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  // El reloj de demostración afecta a todas las organizaciones: se adelanta al final.
  projects: [
    { name: 'flujos', testIgnore: '**/reloj-demo.spec.ts' },
    { name: 'reloj', testMatch: '**/reloj-demo.spec.ts', dependencies: ['flujos'] },
  ],
  use: {
    baseURL: 'https://shapi.localhost',
    ignoreHTTPSErrors: true,
    viewport: { width: 1440, height: 900 },
    trace: 'retain-on-failure',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
  },
});
