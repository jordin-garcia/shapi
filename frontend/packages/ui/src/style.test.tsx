import { describe, expect, it } from 'vitest';
import estilos from './style.css?raw';

describe('RNF-12 · hoja de estilos del sistema de diseño', () => {
  // Tailwind v4 solo busca clases dentro de la app que compila (apps/panel o apps/portal). Sin esta directiva,
  // las clases que solo usan los componentes de este paquete (h-11, text-white…) no se generan y los campos y
  // botones pierden su tamaño y color en el navegador, aunque las pruebas con jsdom pasen.
  it('declara la carpeta de los componentes como fuente de Tailwind', () => {
    expect(estilos).toMatch(/^@source "\.\/";$/m);
  });
});
