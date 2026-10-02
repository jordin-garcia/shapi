import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'https://shapi.localhost',
    ignoreHTTPSErrors: true,
    viewport: { width: 1440, height: 900 },
    trace: 'retain-on-failure',
  },
});
