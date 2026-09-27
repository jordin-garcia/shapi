import { afterEach, describe, expect, it, vi } from 'vitest';
import { existsSync } from 'node:fs';
import configuracion from '../vitest.config';

afterEach(() => { vi.restoreAllMocks(); });

describe('RNF-12 · H-91 configuración de Vitest y MSW', () => {
  it('una petición que ninguna prueba simula es un error de MSW, no un aviso', async () => {
    const error = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const aviso = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    await expect(fetch('http://localhost/sin-simular')).rejects.toThrow();
    expect(error.mock.calls.some(([mensaje]) => String(mensaje).includes('[MSW]'))).toBe(true);
    expect(aviso.mock.calls.some(([mensaje]) => String(mensaje).includes('[MSW]'))).toBe(false);
  });

  it('los proyectos de pruebas incluyen archivos .test.ts y .test.tsx', () => {
    const proyectos = (configuracion.test?.projects ?? []) as { test?: { name?: string; include?: string[] } }[];
    for (const nombre of ['ui', 'api', 'panel', 'portal']) {
      const proyecto = proyectos.find(candidato => candidato.test?.name === nombre);
      expect(proyecto?.test?.include, nombre).toEqual([expect.stringMatching(/\*\.test\.\{ts,tsx\}$/)]);
    }
  });

  it('hay una sola configuración de Vitest, la de frontend/', () => {
    for (const ruta of ['../apps/panel/vitest.config.ts', '../apps/portal/vitest.config.ts', '../packages/ui/vitest.config.ts', '../packages/api/vitest.config.ts']) {
      expect(existsSync(new URL(ruta, import.meta.url)), ruta).toBe(false);
    }
  });
});
