import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Boton, Etiqueta, Campo } from './index';

describe('UI Components', () => {
  it('renders Boton correctly', () => {
    render(<Boton>Haz click</Boton>);
    expect(screen.getByText('Haz click')).toBeDefined();
  });

  it('renders Etiqueta correctly', () => {
    render(<Etiqueta estado="correcto">OK</Etiqueta>);
    expect(screen.getByText('OK')).toBeDefined();
  });

  it('renders Campo error', () => {
    render(<Campo placeholder="Email" error="Requerido" />);
    expect(screen.getByText('Requerido')).toBeDefined();
  });
});
