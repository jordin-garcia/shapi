import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, cleanup, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Boton, Etiqueta, Campo, Tarjeta, Tabla, Aviso, Esqueleto, Toast, DialogoConfirmacion, Selector } from './index';

afterEach(() => { cleanup(); vi.useRealTimers(); });

describe('RNF-12 · componentes base', () => {
  it('Boton muestra su texto y ejecuta la acción', async () => {
    const accion = vi.fn();
    render(<Boton onClick={accion}>Publicar API</Boton>);
    await userEvent.click(screen.getByRole('button', { name: 'Publicar API' }));
    expect(accion).toHaveBeenCalledOnce();
  });
  it('Boton secundario deshabilitado no ejecuta la acción', async () => {
    const accion = vi.fn();
    render(<Boton principal={false} deshabilitado onClick={accion}>Cancelar</Boton>);
    await userEvent.click(screen.getByRole('button'));
    expect(accion).not.toHaveBeenCalled();
    expect(screen.getByRole('button').hasAttribute('disabled')).toBe(true);
  });
  it.each(['correcto', 'alerta', 'neutro'] as const)('Etiqueta muestra estado %s', estado => {
    render(<Etiqueta estado={estado}>Estado</Etiqueta>);
    expect(screen.getByText('Estado').className).toBe({ correcto: 'eti-c', alerta: 'eti-a', neutro: 'eti-n' }[estado]);
  });
  it('Campo muestra error y conserva el valor introducido', async () => {
    render(<Campo aria-label="Correo" error="Requerido" />);
    await userEvent.type(screen.getByRole('textbox'), 'persona@ejemplo.test');
    expect((screen.getByRole('textbox') as HTMLInputElement).value).toBe('persona@ejemplo.test');
    expect(screen.getByText('Requerido')).toBeDefined();
  });
  it('Tarjeta presenta su contenido', () => {
    render(<Tarjeta><h2>Plan Lanzamiento</h2><p>Q 199.00</p></Tarjeta>);
    expect(screen.getByRole('heading', { name: 'Plan Lanzamiento' })).toBeDefined();
    expect(screen.getByText('Q 199.00')).toBeDefined();
  });
  it('Tabla presenta encabezados y datos', () => {
    render(<Tabla headers={['Ruta', 'Llamadas']} rows={[[ '/cotizaciones', '128,400' ], ['/guias', '64,120']]} />);
    expect(screen.getAllByRole('columnheader').map(celda => celda.textContent)).toEqual(['Ruta', 'Llamadas']);
    expect(screen.getAllByRole('row')).toHaveLength(3);
    expect(screen.getByRole('cell', { name: '128,400' })).toBeDefined();
  });
  it.each([['error', 'eti-a'], ['exito', 'eti-c'], ['neutro', 'eti-n']] as const)('Aviso aplica el estado %s', (estado, clase) => {
    render(<Aviso estado={estado}>Información</Aviso>);
    expect(screen.getByText('Información').className).toContain(clase);
  });
  it('Esqueleto ocupa el espacio solicitado mientras carga', () => {
    const { container } = render(<Esqueleto className="h-24 w-full" />);
    expect(container.firstElementChild?.className).toContain('h-24 w-full');
    expect(container.firstElementChild?.className).toContain('animate-pulse');
  });
  it('Toast muestra la confirmación durante cuatro segundos', () => {
    vi.useFakeTimers();
    render(<Toast>Guardado</Toast>);
    expect(screen.getByText('Guardado')).toBeDefined();
    act(() => vi.advanceTimersByTime(3999));
    expect(screen.getByText('Guardado')).toBeDefined();
    act(() => vi.advanceTimersByTime(1));
    expect(screen.queryByText('Guardado')).toBeNull();
  });
  it('DialogoConfirmacion oculta su contenido al cerrarse y ejecuta sus acciones', async () => {
    const confirmar = vi.fn(); const cancelar = vi.fn();
    const { rerender } = render(<DialogoConfirmacion open={false} titulo="Publicar API" onClose={cancelar} onConfirm={confirmar} />);
    expect(screen.queryByText('Publicar API')).toBeNull();
    rerender(<DialogoConfirmacion open titulo="Publicar API" onClose={cancelar} onConfirm={confirmar} />);
    expect(screen.getByRole('heading', { name: 'Publicar API' })).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }));
    await userEvent.click(screen.getByRole('button', { name: 'Cancelar' }));
    expect(confirmar).toHaveBeenCalledOnce(); expect(cancelar).toHaveBeenCalledOnce();
  });
  it('Selector muestra opciones y comunica la selección', async () => {
    const cambio = vi.fn();
    render(<Selector options={[{ label: 'Envíos', value: 'envios' }, { label: 'Agro', value: 'agro' }]} value="envios" onChange={evento => cambio(evento.target.value)} />);
    expect(screen.getAllByRole('option')).toHaveLength(2);
    await userEvent.selectOptions(screen.getByRole('combobox'), 'agro');
    expect(cambio).toHaveBeenCalledWith('agro');
  });
});
