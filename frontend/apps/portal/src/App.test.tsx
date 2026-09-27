import { afterEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import App from './App';

afterEach(cleanup);

describe('RNF-12 · H-93 portal antes de DC-03', () => {
  it('muestra un texto neutro en español, sin la marca de Shapi ni "Portal Base"', () => {
    const { container } = render(<App />);
    expect(screen.getByText('El portal todavía no está disponible.')).toBeDefined();
    expect(container.textContent).not.toMatch(/Portal Base|Shapi/);
  });
});
