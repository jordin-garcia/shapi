import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    host: "0.0.0.0",
    allowedHosts: [".shapi.localhost"],
    hmr: { clientPort: 443 },
    port: 5174,
    strictPort: true
  }
});
