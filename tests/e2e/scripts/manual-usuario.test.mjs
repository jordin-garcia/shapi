import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { generarPdf } from '../../../scripts/generar-pdf.mjs';
import { chromium } from '@playwright/test';

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const fuente = resolve(raiz, 'docs/manual-usuario.md');

// JZ-16 CA-1; lineamientos §7: cobertura por perfil de los procedimientos.
test('JZ-16 CA-1 · contiene los procedimientos de los tres perfiles', () => {
  const manual = readFileSync(fuente, 'utf8');
  for (const tema of ['Registro del proveedor', 'Verificación del correo', 'Iniciar y cerrar sesión',
    'Recuperar la contraseña', 'Plan de plataforma', 'Registrar una API', 'Cargar la especificación',
    'Exponer rutas', 'Configurar rutas', 'Planes de la API', 'Publicar una API', 'Personalizar el portal',
    'Dominio propio', 'Claves de los consumidores', 'Consumo y consumidores', 'Miembros y roles',
    'Invitar consumidores', 'Casos de soporte del proveedor', 'Registro del consumidor',
    'Documentación y consola', 'Contratar un plan de API', 'Suscripción y claves',
    'Consumo del consumidor', 'Pagos del consumidor', 'Cambiar el plan de API',
    'Planes de plataforma', 'Organizaciones', 'Pagos de plataforma', 'Cuentas de plataforma',
    'Atender casos', 'Estado de los componentes', 'Bitácora']) {
    assert.ok(manual.includes(tema), `Falta el procedimiento: ${tema}`);
  }
});

// JZ-16 CA-2: cada procedimiento tiene imágenes locales válidas.
test('JZ-16 CA-2 · cada procedimiento incluye capturas PNG existentes', () => {
  const manual = readFileSync(fuente, 'utf8');
  const procedimientos = manual.split(/^### /m).slice(1);
  assert.ok(procedimientos.length >= 32);
  for (const procedimiento of procedimientos) {
    const imagenes = [...procedimiento.matchAll(/!\[[^\]]+\]\((manual-usuario\/img\/[^)]+\.png)\)/g)];
    assert.ok(imagenes.length, `Sin captura: ${procedimiento.split('\n')[0]}`);
    for (const paso of procedimiento.split(/^\d+\. /m).slice(1)) {
      assert.match(paso, /!\[[^\]]+\]\(manual-usuario\/img\/[^)]+\.png\)/, `Paso sin imagen: ${paso.slice(0, 80)}`);
    }
    for (const [, ruta] of imagenes) {
      const archivo = resolve(dirname(fuente), ruta);
      assert.ok(existsSync(archivo), `No existe ${ruta}`);
      assert.equal(readFileSync(archivo).subarray(0, 8).toString('hex'), '89504e470d0a1a0a');
    }
  }
});

test('JZ-16 CA-2 · documenta las pantallas finales de enlaces y secretos de un solo uso', () => {
  const manual = readFileSync(fuente, 'utf8');
  for (const nombre of ['restablecer-personal', 'portal-restablecer', 'api-registrada',
    'plan-contratado', 'clave-nueva', 'aceptar-miembro', 'definir-contrasena-personal']) {
    const ruta = `manual-usuario/img/${nombre}.png`;
    assert.ok(manual.includes(`](${ruta})`), `Falta explicar e ilustrar ${nombre}`);
    assert.ok(existsSync(resolve(dirname(fuente), ruta)), ruta);
  }
});

// JZ-16 CA-3: el entregable real es PDF; la carátula se revisa al renderizarlo.
test('JZ-16 CA-3 · existe el PDF generado', () => {
  const pdf = readFileSync(resolve(raiz, 'docs/pdf/Manual_de_Usuario.pdf'));
  assert.equal(pdf.subarray(0, 5).toString(), '%PDF-');
  assert.ok(pdf.length > 10000);
  assert.match(pdf.toString('latin1'), /\/Outlines/);
});

test('JZ-16 CA-2 y CA-3 · genera el manual con carátula, imágenes y enlaces válidos', { timeout: 120_000 }, async () => {
  const temporal = await mkdtemp(resolve(tmpdir(), 'shapi-manual-'));
  let navegador;
  let resultado;
  try {
    resultado = await generarPdf(resolve(temporal, 'Manual_de_Usuario.pdf'), [fuente], { conservarHtml: true });
    navegador = await chromium.launch();
    const pagina = await navegador.newPage();
    await pagina.goto(resultado.urlHtml);
    const portada = await pagina.locator('.caratula').innerText();
    for (const texto of ['Universidad Rafael Landívar', 'Manual de Usuario', '2427124', '1563224', '1508224', '1507524']) assert.ok(portada.includes(texto), texto);
    assert.equal(await pagina.locator('.caratula .integrante').count(), 4);
    const imagenes = pagina.locator('main img');
    assert.ok(await imagenes.count() >= 40);
    assert.ok(await imagenes.evaluateAll(elementos => elementos.every(i => i.complete && i.naturalWidth >= 1000 && i.alt.trim())));
    assert.ok(await pagina.locator('table').evaluateAll(tablas => tablas.every(t => t.scrollWidth <= t.clientWidth + 1)));
    assert.ok(await pagina.locator('a[href^="#"]').evaluateAll(enlaces => enlaces.every(a => document.getElementById(a.hash.slice(1)))));
    for (const [, destino] of readFileSync(fuente, 'utf8').matchAll(/\]\(([^)]+\.md)(?:#[^)]*)?\)/g)) assert.ok(existsSync(resolve(dirname(fuente), destino)), destino);
  } finally {
    await navegador?.close();
    if (resultado?.temporal) await rm(resultado.temporal, { recursive: true, force: true });
    await rm(temporal, { recursive: true, force: true });
  }
});
