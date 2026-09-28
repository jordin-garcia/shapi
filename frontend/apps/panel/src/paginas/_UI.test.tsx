import { describe, it, expect, afterEach } from 'vitest';
import { render, screen, cleanup, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import estilos from '../../../../packages/ui/src/style.css?raw';
import UI from './_UI';

afterEach(cleanup);

function variables(selector: RegExp) {
  const bloque = estilos.match(selector)?.[1] ?? '';
  return Object.fromEntries([...bloque.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map(([, nombre, valor]) => [nombre, valor.trim()]));
}

describe('RNF-12 · lámina oficial', () => {
  it('presenta textos, cifras y componentes de A0.Lamina sin imágenes externas', () => {
    const { container } = render(<UI />);
    for (const texto of ['Plano azul','Tipografía','Logotipo','Espaciado y radio','Sora','IBM Plex Sans','Plan Lanzamiento','Q 199.00','cada 30 días','/cotizaciones','128,400','/guias','64,120','/tarifas','9,860']) expect(screen.getByText(texto)).toBeDefined();
    expect(screen.getByRole('textbox', { name: 'Servidor de origen' })).toBeDefined();
    expect(screen.getByRole('textbox', { name: 'Subdominio' })).toBeDefined();
    expect(container.querySelector('img[src^="https://"]')).toBeNull();
  });

  it('RNF-12 · H-87 y H-92 cada muestra de la paleta tiene el valor de su token, en el orden y con la descripción de la lámina', () => {
    const { container } = render(<UI />);
    const claro = variables(/:root\s*\{([^}]*)\}/);
    const oscuro = { ...claro, ...variables(/\.dark\s*\{([^}]*)\}/) };
    const muestras = [...container.querySelectorAll<HTMLElement>('[data-token]')].map(muestra => ({
      nombre: muestra.dataset.nombre, color: muestra.dataset.color, token: muestra.dataset.token!, oscura: muestra.closest('.dark') !== null,
    }));
    expect(muestras.map(muestra => `${muestra.nombre} ${muestra.color}`)).toEqual([
      'Fondo #060910', 'Panel #0C1220', 'Borde #1B2436', 'Principal aclarado #7FA6FF',
      'Texto y acción #E8EDF7', 'Texto suave #8B98B0', 'Correcto #3FBF88', 'Alerta #F08A5F',
      'Principal #3B6FF0', 'Tinta #0B1220', 'Correcto #1F8A5B', 'Alerta #C2481F',
      'Base #FFFFFF', 'Banda #F4F6FA', 'Borde #DCE3EE', 'Tinta suave #5A6884',
    ]);
    for (const muestra of muestras) expect((muestra.oscura ? oscuro : claro)[muestra.token], muestra.nombre).toBe(muestra.color);
    for (const descripcion of ['Acciones, énfasis, plan destacado', 'Texto principal y cabeceras', 'Activa, pago confirmado', 'Cuota agotada, suspensión']) {
      expect(screen.getByText(descripcion)).toBeDefined();
    }
  });

  it('RNF-12 · H-87 el logotipo tiene la medida y los trazos de la lámina, en positivo y en negativo', () => {
    render(<UI />);
    const logotipos = screen.getAllByRole('img', { name: 'Shapi' });
    expect(logotipos).toHaveLength(2);
    for (const logotipo of logotipos) {
      expect(logotipo.getAttribute('width')).toBe('48');
      const trazos = [...logotipo.querySelectorAll('rect, path')].map(trazo => trazo.getAttribute('stroke-width'));
      expect(trazos).toEqual(['1.5', '1.5', '1.5', '2.5']);
    }
    expect(screen.getByText(/Positivo sobre claro/)).toBeDefined();
    expect(screen.getByText(/Negativo sobre oscuro/)).toBeDefined();
  });

  it('RNF-12 · H-92 muestra los estados de 11 §4 y el diálogo confirma con un aviso breve', async () => {
    render(<UI />);
    const estados = screen.getByRole('region', { name: 'Estados e interacción' });
    expect(within(estados).getByRole('status', { name: 'Cargando' })).toBeDefined();
    expect(within(estados).getByRole('button', { name: 'Reintentar' })).toBeDefined();
    expect(within(estados).getByText('No tiene permiso para ver esta página')).toBeDefined();
    expect(within(estados).getByRole('textbox', { name: 'Nombre' }).getAttribute('aria-invalid')).toBe('true');
    expect(within(estados).getByRole('combobox', { name: 'API' })).toBeDefined();

    await userEvent.click(screen.getAllByRole('button', { name: 'Publicar API' })[0]);
    expect(screen.getByRole('dialog', { name: 'Publicar API' })).toBeDefined();
    await userEvent.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).toBeNull();

    await userEvent.click(screen.getAllByRole('button', { name: 'Publicar API' })[0]);
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Publicar' }));
    expect(screen.getByText('API publicada.')).toBeDefined();
  });
});
