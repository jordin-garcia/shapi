import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, cleanup, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Boton, Etiqueta, Campo, Tarjeta, Tabla, Aviso, Esqueleto, Toast, DialogoConfirmacion, Selector, EstadoCargando, EstadoError, EstadoSinPermiso } from './index';

afterEach(() => { cleanup(); vi.useRealTimers(); });

// Clases de las etiquetas de estado que un aviso no debe heredar (H-89).
const clasesDeEtiqueta = ['whitespace-nowrap', 'uppercase', 'text-xs', 'eti-a', 'eti-c', 'eti-n'];

describe('RNF-12 · componentes base', () => {
  it('Boton muestra su texto y ejecuta la acción', async () => {
    const accion = vi.fn();
    render(<Boton onClick={accion}>Publicar API</Boton>);
    await userEvent.click(screen.getByRole('button', { name: 'Publicar API' }));
    expect(accion).toHaveBeenCalledOnce();
  });
  it('RNF-12 · H-87 Boton mide 46 px de alto y su texto 15 px (11 §1)', () => {
    render(<Boton>Publicar API</Boton>);
    expect(screen.getByRole('button').className).toContain('h-[46px]');
    expect(screen.getByRole('button').className).toContain('text-[15px]');
  });
  it('Boton secundario deshabilitado no ejecuta la acción', async () => {
    const accion = vi.fn();
    render(<Boton principal={false} deshabilitado onClick={accion}>Cancelar</Boton>);
    await userEvent.click(screen.getByRole('button'));
    expect(accion).not.toHaveBeenCalled();
    expect(screen.getByRole('button').hasAttribute('disabled')).toBe(true);
    expect(screen.getByRole('button').className).toContain('border-borde-inactivo');
    expect(screen.getByRole('button').className).toContain('text-tinta-inactiva');
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
  it('RNF-12 · H-88 Campo tiene etiqueta propia y anuncia su error con aria-invalid y aria-describedby', () => {
    render(<Campo etiqueta="Subdominio" error="Este campo es obligatorio" aria-describedby="ayuda" />);
    const campo = screen.getByRole('textbox', { name: 'Subdominio' });
    expect(campo.getAttribute('aria-invalid')).toBe('true');
    const descripciones = campo.getAttribute('aria-describedby')!.split(' ');
    expect(descripciones).toContain('ayuda');
    const error = screen.getByText('Este campo es obligatorio');
    expect(descripciones).toContain(error.id);
    expect(campo.className).toContain('border-alerta');
  });
  it('RNF-12 · H-88 Campo sin error no se marca como inválido y respeta el id recibido', () => {
    render(<Campo etiqueta="Servidor de origen" id="origen" placeholder="https://" />);
    const campo = screen.getByRole('textbox', { name: 'Servidor de origen' });
    expect(campo.id).toBe('origen');
    expect(campo.hasAttribute('aria-invalid')).toBe(false);
    expect(campo.hasAttribute('aria-describedby')).toBe(false);
  });
  it('RNF-12 · H-87 Campo mide 48 px, con texto de 15 px, anillo de foco y texto guía (lámina)', () => {
    render(<Campo etiqueta="Servidor de origen" placeholder="https://" />);
    const clases = screen.getByRole('textbox').className;
    for (const clase of ['h-12', 'text-[15px]', 'px-[14px]', 'border-borde-campo', 'focus:border-principal', 'focus:ring-[3px]', 'focus:ring-anillo-foco', 'placeholder:text-tinta-inactiva']) {
      expect(clases).toContain(clase);
    }
    expect(screen.getByText('Servidor de origen').className).toContain('text-[13px]');
  });
  it('Tarjeta presenta su contenido', () => {
    render(<Tarjeta><h2>Plan Lanzamiento</h2><p>Q 199.00</p></Tarjeta>);
    expect(screen.getByRole('heading', { name: 'Plan Lanzamiento' })).toBeDefined();
    expect(screen.getByText('Q 199.00')).toBeDefined();
  });
  it('RNF-12 · H-87 Tarjeta tiene 20 px de relleno, borde de 1 px y no tiene sombra', () => {
    const { container } = render(<Tarjeta>Plan</Tarjeta>);
    const clases = container.firstElementChild!.className;
    expect(clases).toContain('p-5');
    expect(clases).toContain('border-borde');
    expect(clases).not.toContain('shadow');
  });
  it('Tabla presenta encabezados y datos', () => {
    render(<Tabla encabezados={['Ruta', 'Llamadas']} filas={[['/cotizaciones', '128,400'], ['/guias', '64,120']]} />);
    expect(screen.getAllByRole('columnheader').map(celda => celda.textContent)).toEqual(['Ruta', 'Llamadas']);
    expect(screen.getAllByRole('row')).toHaveLength(3);
    expect(screen.getByRole('cell', { name: '128,400' })).toBeDefined();
  });
  it('RNF-12 · H-87 Tabla: encabezado de 11 px seminegro con borde, filas de 42 px y relleno de la lámina', () => {
    render(<Tabla encabezados={['Ruta']} filas={[['/cotizaciones'], ['/guias']]} />);
    const encabezado = screen.getByRole('columnheader').className;
    for (const clase of ['text-encabezado', 'uppercase', 'font-semibold', 'border-borde', 'pr-4', 'pb-[10px]']) expect(encabezado).toContain(clase);
    expect(encabezado).not.toContain('px-4');
    const [, ...filas] = screen.getAllByRole('row');
    for (const fila of filas) {
      expect(fila.className).toContain('h-[42px]');
      expect(fila.className).toContain('border-borde-fila');
      expect(fila.className).not.toContain('last:border-none');
    }
    const celda = screen.getByRole('cell', { name: '/cotizaciones' }).className;
    expect(celda).toContain('text-dato');
    expect(celda).toContain('pr-4');
    expect(celda).not.toContain('px-4');
  });
  it.each([
    ['error', 'bg-alerta-fondo', 'alert'], ['exito', 'bg-correcto-fondo', 'status'], ['neutro', 'bg-neutro-fondo', 'status'],
  ] as const)('RNF-12 · H-89 Aviso %s usa los colores de su estado sin heredar el formato de las etiquetas', (estado, clase, rol) => {
    render(<Aviso estado={estado}>No se pudo cargar la información.</Aviso>);
    const aviso = screen.getByRole(rol);
    expect(aviso.className).toContain(clase);
    for (const heredada of clasesDeEtiqueta) expect(aviso.className.split(' ')).not.toContain(heredada);
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
    const confirmar = vi.fn(); const cerrar = vi.fn();
    const { rerender } = render(<DialogoConfirmacion abierto={false} titulo="Publicar API" cerrar={cerrar} confirmar={confirmar} />);
    expect(screen.queryByText('Publicar API')).toBeNull();
    rerender(<DialogoConfirmacion abierto titulo="Publicar API" cerrar={cerrar} confirmar={confirmar} />);
    expect(screen.getByRole('heading', { name: 'Publicar API' })).toBeDefined();
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }));
    await userEvent.click(screen.getByRole('button', { name: 'Cancelar' }));
    expect(confirmar).toHaveBeenCalledOnce(); expect(cerrar).toHaveBeenCalledOnce();
  });
  it('RNF-12 · H-88 DialogoConfirmacion admite textos y contenido propios y se nombra por su título', () => {
    render(<DialogoConfirmacion abierto titulo="Revocar la clave" textoConfirmar="Revocar" textoCancelar="Volver" cerrar={vi.fn()} confirmar={vi.fn()}>
      <p>La clave dejará de funcionar de inmediato.</p>
    </DialogoConfirmacion>);
    const dialogo = screen.getByRole('dialog', { name: 'Revocar la clave' });
    expect(dialogo.getAttribute('aria-modal')).toBe('true');
    expect(dialogo.hasAttribute('aria-label')).toBe(false);
    expect(document.getElementById(dialogo.getAttribute('aria-labelledby')!)?.textContent).toBe('Revocar la clave');
    expect(screen.getByText('La clave dejará de funcionar de inmediato.')).toBeDefined();
    expect(screen.getByRole('button', { name: 'Revocar' })).toBeDefined();
    expect(screen.getByRole('button', { name: 'Volver' })).toBeDefined();
  });
  it('RNF-12 · H-88 DialogoConfirmacion atrapa el foco, se cierra con Escape y devuelve el foco', async () => {
    const cerrar = vi.fn();
    function Prueba({ abierto }: { abierto: boolean }) {
      return <><button type="button">Abrir</button><button type="button">Fuera</button>
        <DialogoConfirmacion abierto={abierto} titulo="Publicar API" cerrar={cerrar} confirmar={vi.fn()} /></>;
    }
    const { rerender } = render(<Prueba abierto={false} />);
    screen.getByRole('button', { name: 'Abrir' }).focus();
    rerender(<Prueba abierto />);
    const cancelar = screen.getByRole('button', { name: 'Cancelar' });
    const confirmar = screen.getByRole('button', { name: 'Confirmar' });
    expect(document.activeElement).toBe(cancelar);
    await userEvent.tab();
    expect(document.activeElement).toBe(confirmar);
    await userEvent.tab();
    expect(document.activeElement).toBe(cancelar);
    await userEvent.tab({ shift: true });
    expect(document.activeElement).toBe(confirmar);
    await userEvent.keyboard('{Escape}');
    expect(cerrar).toHaveBeenCalledOnce();
    rerender(<Prueba abierto={false} />);
    expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Abrir' }));
  });
  it('RNF-12 · H-88 DialogoConfirmacion no pierde el foco si se hace clic en el fondo', async () => {
    const cerrar = vi.fn();
    render(<DialogoConfirmacion abierto titulo="Publicar API" cerrar={cerrar} confirmar={vi.fn()} />);
    const cancelar = screen.getByRole('button', { name: 'Cancelar' });
    expect(document.activeElement).toBe(cancelar);
    await userEvent.click(screen.getByRole('dialog').parentElement!);
    expect(document.activeElement).toBe(cancelar);
    // Un clic en el título tampoco lo saca: el foco queda en el diálogo, y Escape y Tab siguen funcionando.
    await userEvent.click(screen.getByRole('heading', { name: 'Publicar API' }));
    expect(screen.getByRole('dialog').contains(document.activeElement)).toBe(true);
    await userEvent.keyboard('{Escape}');
    expect(cerrar).toHaveBeenCalledOnce();
  });
  it('Selector muestra opciones y comunica la selección', async () => {
    const cambio = vi.fn();
    render(<Selector aria-label="API" opciones={[{ etiqueta: 'Envíos', valor: 'envios' }, { etiqueta: 'Agro', valor: 'agro' }]} value="envios" onChange={evento => cambio(evento.target.value)} />);
    expect(screen.getAllByRole('option')).toHaveLength(2);
    await userEvent.selectOptions(screen.getByRole('combobox'), 'agro');
    expect(cambio).toHaveBeenCalledWith('agro');
  });
  it('RNF-12 · H-88 Selector recibe aria-label e id', () => {
    render(<Selector id="selector-api" aria-label="API" opciones={[{ etiqueta: 'Envíos', valor: 'envios' }]} value="envios" onChange={vi.fn()} />);
    expect(screen.getByRole('combobox', { name: 'API' }).id).toBe('selector-api');
  });
});

describe('RNF-12 · H-92 estados comunes (11 §4)', () => {
  it('EstadoCargando muestra un esqueleto y lo anuncia', () => {
    const { container } = render(<EstadoCargando />);
    expect(screen.getByRole('status', { name: 'Cargando' })).toBeDefined();
    expect(container.querySelectorAll('.animate-pulse').length).toBeGreaterThan(0);
  });
  it('EstadoError muestra el mensaje con los colores de alerta y "Reintentar" es un Boton', async () => {
    const reintentar = vi.fn();
    render(<EstadoError reintentar={reintentar} />);
    const aviso = screen.getByRole('alert');
    expect(aviso.textContent).toContain('No se pudo cargar la información.');
    expect(aviso.className).toContain('bg-alerta-fondo');
    for (const heredada of clasesDeEtiqueta) expect(aviso.className.split(' ')).not.toContain(heredada);
    const boton = screen.getByRole('button', { name: 'Reintentar' });
    expect(boton.className).toContain('h-[46px]');
    expect(boton.className).not.toContain('underline');
    await userEvent.click(boton);
    expect(reintentar).toHaveBeenCalledOnce();
  });
  it('EstadoError acepta un mensaje propio', () => {
    render(<EstadoError mensaje="No se pudo cerrar la sesión." reintentar={vi.fn()} />);
    expect(screen.getByText('No se pudo cerrar la sesión.')).toBeDefined();
  });
  it('EstadoSinPermiso muestra "No tiene permiso para ver esta página" como título', () => {
    render(<EstadoSinPermiso />);
    const titulo = screen.getByRole('heading', { level: 1, name: 'No tiene permiso para ver esta página' });
    expect(titulo.className).toContain('text-titulo');
  });
});
