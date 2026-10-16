import { afterEach, expect, it, vi } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { CampoContrasena } from './index';

afterEach(cleanup);

it('RNF-12 · el ojo muestra y oculta por teclado sin enviar ni perder el valor o el error', async () => {
  const enviar = vi.fn(e => e.preventDefault());
  const usuario = userEvent.setup();
  render(<form onSubmit={enviar}><CampoContrasena etiqueta="Contraseña" error="Revise la contraseña" autoComplete="new-password" maxLength={128} /><button>Enviar</button></form>);
  const campo = screen.getByLabelText('Contraseña') as HTMLInputElement;
  await usuario.type(campo, 'UnaClaveDePrueba');
  expect(campo.type).toBe('password');
  const ojo = screen.getByRole('button', { name: 'Mostrar contraseña' });
  expect(ojo.getAttribute('aria-pressed')).toBe('false');
  await usuario.tab();
  expect(document.activeElement).toBe(ojo);
  await usuario.keyboard('{Enter}');
  expect(campo.type).toBe('text');
  expect(screen.getByRole('button', { name: 'Ocultar contraseña' }).getAttribute('aria-pressed')).toBe('true');
  await usuario.keyboard(' ');
  expect(campo.type).toBe('password');
  expect(campo.value).toBe('UnaClaveDePrueba');
  expect(campo.getAttribute('aria-describedby')).toBe(screen.getByText('Revise la contraseña').id);
  expect(campo.autocomplete).toBe('new-password');
  expect(campo.maxLength).toBe(128);
  expect(enviar).not.toHaveBeenCalled();
});
