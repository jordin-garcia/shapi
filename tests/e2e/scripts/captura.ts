import { chromium } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';

const [urlSolicitada, archivoSolicitado] = process.argv.slice(2);

if (!urlSolicitada || !archivoSolicitado) {
  console.error('Uso: pnpm captura <url> <archivo.png>');
  process.exitCode = 1;
} else {
  const url = new URL(urlSolicitada, 'https://shapi.localhost/').toString();
  const archivo = resolve(archivoSolicitado);
  mkdirSync(dirname(archivo), { recursive: true });

  const navegador = await chromium.launch();
  try {
    const contexto = await navegador.newContext({
      ignoreHTTPSErrors: true,
      viewport: { width: 1440, height: 900 },
    });
    const pagina = await contexto.newPage();
    await pagina.goto(url, { waitUntil: 'networkidle' });
    await pagina.screenshot({ path: archivo, fullPage: true });
    console.log(`Captura guardada en ${archivo}`);
  } finally {
    await navegador.close();
  }
}
