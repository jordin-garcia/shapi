import { describe, it, expect, afterEach } from 'vitest';
import { render, screen, cleanup } from '@testing-library/react';
import UI from './_UI';

afterEach(cleanup);
describe('RNF-12 · lámina oficial', () => {
  it('presenta textos, cifras y componentes de A0.Lamina sin imágenes externas', () => {
    const { container } = render(<UI />);
    for (const texto of ['Plano azul','Tipografía','Logotipo','Espaciado y radio','Sora','IBM Plex Sans','Plan Lanzamiento','Q 199.00','cada 30 días','/cotizaciones','128,400','/guias','64,120','/tarifas','9,860']) expect(screen.getByText(texto)).toBeDefined();
    expect(screen.getByRole('textbox', { name: 'Servidor de origen' })).toBeDefined();
    expect(screen.getByRole('textbox', { name: 'Subdominio' })).toBeDefined();
    expect(container.querySelector('img[src^="https://"]')).toBeNull();
  });
});
