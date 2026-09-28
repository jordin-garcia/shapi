import { describe, expect, it } from 'vitest';
import estilos from './style.css?raw';
import componentes from './index.tsx?raw';

// Las variables de todos los bloques que coinciden con el selector, por ejemplo los dos @theme normales.
function variables(selector: RegExp) {
  const bloques = [...estilos.matchAll(new RegExp(selector, 'g'))].map(([, bloque]) => bloque).join('\n');
  return Object.fromEntries([...bloques.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map(([, nombre, valor]) => [nombre, valor.trim()]));
}
const claro = () => variables(/:root\s*\{([^}]*)\}/);
const oscuro = () => variables(/\.dark\s*\{([^}]*)\}/);
const tema = () => variables(/@theme\s*\{([^}]*)\}/);
const temaEnLinea = () => variables(/@theme inline\s*\{([^}]*)\}/);

describe('RNF-12 · hoja de estilos del sistema de diseño', () => {
  // Tailwind v4 solo busca clases dentro de la app que compila (apps/panel o apps/portal). Sin esta directiva,
  // las clases que solo usan los componentes de este paquete (h-11, text-white…) no se generan y los campos y
  // botones pierden su tamaño y color en el navegador, aunque las pruebas con jsdom pasen.
  it('declara la carpeta de los componentes como fuente de Tailwind', () => {
    expect(estilos).toMatch(/^@source "\.\/";$/m);
  });

  it('RNF-12 · H-86 los tokens de la superficie clara son los de 11 §1, con los dos tonos de cada estado', () => {
    expect(claro()).toEqual({
      '--principal': '#3B6FF0', '--principal-hover': '#2C57C9',
      '--tinta': '#0B1220', '--tinta-suave': '#5A6884',
      '--fondo': '#F4F6FA', '--panel': '#FFFFFF',
      '--borde': '#DCE3EE', '--borde-campo': '#C9D2E1', '--borde-fila': '#EBEFF5',
      '--correcto-base': '#1F8A5B', '--correcto': '#146542', '--correcto-fondo': '#E4F3EC', '--correcto-borde': '#B6DCC9',
      '--alerta-base': '#C2481F', '--alerta': '#8E3315', '--alerta-fondo': '#FBE9E3', '--alerta-borde': '#EDC3B4',
      '--neutro': '#5A6884', '--neutro-fondo': '#F4F6FA', '--neutro-borde': '#DCE3EE',
      '--tinta-inactiva': '#A3AEC2', '--borde-inactivo': '#E4E9F1', '--anillo-foco': '#DEE7FC',
    });
  });

  it('RNF-12 · H-86 el tema oscuro solo redefine los valores de la columna oscura de 11 §1', () => {
    // Los tokens sin valor oscuro en 11 §1 (principal-hover, borde-fila, fondos y bordes de estado, neutro) no se
    // inventan: heredan el valor claro. Los cuatro últimos son solo de las barras oscuras (N.1, A6 y B3).
    expect(oscuro()).toEqual({
      '--principal': '#7FA6FF', '--tinta': '#E8EDF7', '--tinta-suave': '#8B98B0',
      '--fondo': '#060910', '--panel': '#0C1220', '--borde': '#1B2436', '--borde-campo': '#2A3550',
      '--correcto-base': '#3FBF88', '--correcto': '#3FBF88', '--alerta-base': '#F08A5F', '--alerta': '#F08A5F',
      '--borde-barra': '#131B2B', '--tinta-rotulo': '#7F8DA8', '--tinta-navegacion': '#B9C4D8', '--fondo-activo': '#0E1830',
    });
  });

  it('RNF-12 · H-86 los colores de Tailwind leen el token en cada elemento, para que .dark los cambie', () => {
    // En un @theme normal, `--color-fondo: var(--fondo)` se resuelve una sola vez en :root y los hijos de .dark heredan
    // el valor claro. Con @theme inline, `bg-fondo` usa `var(--fondo)` directamente y toma el valor de .dark.
    const colores = Object.keys({ ...claro(), ...oscuro() }).map(token => token.slice(2));
    expect(temaEnLinea()).toEqual(Object.fromEntries(colores.map(color => [`--color-${color}`, `var(--${color})`])));
    expect(Object.values(tema()).some(valor => valor.startsWith('var('))).toBe(false);
  });

  it('RNF-12 · H-86 la escala tipográfica es la de 11 §1 (32, 22, 16, 15, 12 y 11)', () => {
    expect(tema()).toMatchObject({
      '--text-titulo': '32px', '--text-titulo--line-height': '1.2',
      '--text-titulo-tarjeta': '22px', '--text-titulo-tarjeta--line-height': '1.3',
      '--text-cuerpo': '16px', '--text-cuerpo--line-height': '1.6',
      '--text-dato': '15px', '--text-dato--line-height': '1.5',
      '--text-etiqueta': '12px', '--text-etiqueta--letter-spacing': '.16em',
      '--text-encabezado': '11px', '--text-encabezado--letter-spacing': '.12em',
      '--radius-base': '8px',
    });
  });

  it('RNF-12 · H-86 la escala de espaciado es la de 11 §1', () => {
    const escala = [4, 8, 12, 16, 20, 24, 32, 44, 64, 80];
    const definidas = tema();
    expect(Object.keys(definidas).filter(nombre => nombre.startsWith('--espacio-'))).toEqual(escala.map(valor => `--espacio-${valor}`));
    for (const valor of escala) expect(definidas[`--espacio-${valor}`]).toBe(`${valor}px`);
  });

  it('RNF-12 · H-86 no usa la paleta por defecto de Tailwind', () => {
    expect(estilos).toMatch(/--color-\*:\s*initial;/);
    expect(componentes).not.toMatch(/\b(?:bg|text|border|ring|outline|fill|stroke)-(?:slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose)-\d/);
  });
});
