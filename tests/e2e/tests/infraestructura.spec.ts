import { test, expect } from '@playwright/test';
import { spawnSync } from 'node:child_process';
import { createServer } from 'node:http';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { esperarEnlaceCorreo } from '../soporte/mailpit';

const carpetaE2e = join(dirname(fileURLToPath(import.meta.url)), '..');
const raiz = join(carpetaE2e, '..', '..');

test('RNF-15: captura guarda la pagina completa con un viewport de 1440 x 900', async () => {
  const temporal = mkdtempSync(join(tmpdir(), 'shapi-captura-'));
  const pagina = join(temporal, 'pagina.html');
  const archivo = join(temporal, 'pagina.png');
  writeFileSync(pagina, '<!doctype html><style>body{margin:0;height:1500px}</style><h1>Captura E2E</h1>');
  try {
    const paquete = JSON.parse(readFileSync(join(carpetaE2e, 'package.json'), 'utf8')) as {
      scripts?: Record<string, string>;
    };
    expect(paquete.scripts?.captura).toBe('node scripts/captura.ts');

    const resultado = spawnSync(process.execPath, [
      join(carpetaE2e, 'scripts', 'captura.ts'),
      pathToFileURL(pagina).toString(),
      archivo,
    ], {
      cwd: carpetaE2e,
      encoding: 'utf8',
    });
    expect(resultado.status, resultado.stderr || resultado.stdout).toBe(0);

    const png = readFileSync(archivo);
    expect(png.subarray(1, 4).toString('ascii')).toBe('PNG');
    expect(png.readUInt32BE(16)).toBe(1440);
    expect(png.readUInt32BE(20)).toBeGreaterThanOrEqual(1500);
  } finally {
    rmSync(temporal, { recursive: true, force: true });
  }
});

test('RNF-15: el ayudante consulta Mailpit y extrae el enlace del destinatario', async () => {
  const correo = 'jose.e2e@ejemplo.com';
  let consultas = 0;
  const servidor = createServer((peticion, respuesta) => {
    respuesta.setHeader('Content-Type', 'application/json');
    if (peticion.url === '/api/v1/messages') {
      consultas++;
      respuesta.end(JSON.stringify({
        messages: consultas === 1 ? [] : [{ ID: 'mensaje-1', To: [{ Address: correo }] }],
      }));
      return;
    }
    if (peticion.url === '/api/v1/message/mensaje-1') {
      respuesta.end(JSON.stringify({
        HTML: '<a href="https://shapi.localhost/verificar-correo?token=abc-123">Verificar</a>',
        Text: 'Verifique su correo.',
      }));
      return;
    }
    respuesta.writeHead(404).end('{}');
  });
  await new Promise<void>((resolver) => servidor.listen(0, '127.0.0.1', resolver));
  const direccion = servidor.address();
  if (!direccion || typeof direccion === 'string') throw new Error('No se pudo abrir Mailpit de prueba.');

  try {
    const enlace = await esperarEnlaceCorreo(correo, {
      baseUrl: `http://127.0.0.1:${direccion.port}`,
      contiene: '/verificar-correo?token=',
      intervaloMs: 10,
      tiempoLimiteMs: 1_000,
    });
    expect(enlace).toBe('https://shapi.localhost/verificar-correo?token=abc-123');
    expect(consultas).toBeGreaterThan(1);
  } finally {
    servidor.close();
  }
});

test('RNF-15: el workflow ejecuta E2E en main y conserva el informe', () => {
  const workflow = readFileSync(join(raiz, '.github', 'workflows', 'e2e.yml'), 'utf8');
  expect(workflow).toContain('workflow_dispatch:');
  expect(workflow).toMatch(/push:\s*\n\s*branches: \[main\]/);
  expect(workflow).toContain('infra/compose.prod.yml');
  expect(workflow).toContain('pnpm test');
  expect(workflow).toContain('actions/upload-artifact@');
  expect(workflow).toContain('playwright-report');
});
