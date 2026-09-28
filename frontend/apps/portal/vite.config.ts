import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  // El HMR no fija el puerto del cliente: el navegador se conecta al de la página, 443 con Caddy o 5173/5174 sin él (H-99).
  server: {
    // 0.0.0.0 y no 127.0.0.1: en Linux, Caddy llega a Vite por el puente de Docker (manual técnico).
    host: "0.0.0.0",
    allowedHosts: [".shapi.localhost"],
    port: 5174,
    strictPort: true
  }
});
