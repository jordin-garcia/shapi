import React, { useEffect, useId, useRef, useState } from 'react';

// Medidas de 11 §1 y de la lámina oficial (mockups/A0/Lamina.dc.html).

export function Boton({ principal = true, deshabilitado = false, disabled = false, className = '', children, ...props }: React.ComponentPropsWithoutRef<"button"> & { principal?: boolean; deshabilitado?: boolean }) {
  const base = "h-[46px] rounded-base px-6 text-[15px] font-medium transition-colors inline-flex items-center justify-center";
  let variante = "bg-principal text-white hover:bg-principal-hover";
  if (!principal) {
    variante = "border border-borde-campo text-tinta bg-panel hover:bg-fondo";
  }
  const inactivo = deshabilitado || disabled;
  if (inactivo) {
    variante = "border border-borde-inactivo text-tinta-inactiva bg-panel cursor-not-allowed";
  }
  return <button disabled={inactivo} className={`${base} ${variante} ${className}`} {...props}>{children}</button>;
}

/** Campo de texto con su etiqueta y su error debajo, en `--alerta` (11 §4). El error se anuncia con `aria-describedby`. */
export function Campo({ etiqueta, error, id, className = '', 'aria-describedby': descritoPor, ...props }: React.ComponentPropsWithoutRef<"input"> & { etiqueta?: string; error?: string }) {
  const idGenerado = useId();
  const idCampo = id ?? idGenerado;
  const idError = `${idCampo}-error`;
  const descripciones = [descritoPor, error ? idError : undefined].filter(Boolean).join(' ') || undefined;
  return (
    <div className={`flex flex-col gap-[6px] ${className}`}>
      {etiqueta && <label htmlFor={idCampo} className="text-[13px] font-semibold text-tinta">{etiqueta}</label>}
      <input
        id={idCampo}
        aria-invalid={error ? true : undefined}
        aria-describedby={descripciones}
        className={`h-12 rounded-base border px-[14px] text-[15px] bg-panel text-tinta outline-none transition-colors placeholder:text-tinta-inactiva focus:ring-[3px] focus:ring-anillo-foco ${error ? 'border-alerta focus:border-alerta' : 'border-borde-campo focus:border-principal'}`}
        {...props}
      />
      {error && <span id={idError} className="text-alerta text-sm">{error}</span>}
    </div>
  );
}

export function Etiqueta({ estado, children }: { estado: 'correcto' | 'alerta' | 'neutro', children: React.ReactNode }) {
  const clases = {
    correcto: 'eti-c',
    alerta: 'eti-a',
    neutro: 'eti-n'
  };
  return <span className={clases[estado]}>{children}</span>;
}

export function Tarjeta({ children, className = '' }: React.ComponentPropsWithoutRef<"div">) {
  return <div className={`bg-panel border border-borde rounded-base p-5 ${className}`}>{children}</div>;
}

export function Tabla({ encabezados, filas }: { encabezados: string[], filas: React.ReactNode[][] }) {
  return (
    <div className="w-full overflow-auto">
      <table className="w-full text-left border-collapse">
        <thead>
          <tr>
            {encabezados.map((encabezado, i) => (
              <th key={i} className="pr-4 pb-[10px] text-encabezado uppercase text-tinta-suave border-b border-borde font-semibold">{encabezado}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {filas.map((fila, i) => (
            <tr key={i} className="h-[42px] border-b border-borde-fila">
              {fila.map((celda, j) => (
                <td key={j} className="pr-4 text-dato text-tinta">{celda}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

/** Aviso con los colores de su estado. No usa las clases `eti-*`, para no heredar las mayúsculas ni el texto en una línea. */
export function Aviso({ estado, children }: { estado: 'error' | 'exito' | 'neutro', children: React.ReactNode }) {
  const colores = {
    error: 'bg-alerta-fondo border-alerta-borde text-alerta',
    exito: 'bg-correcto-fondo border-correcto-borde text-correcto',
    neutro: 'bg-neutro-fondo border-neutro-borde text-neutro',
  };
  return (
    <div role={estado === 'error' ? 'alert' : 'status'} className={`flex items-center justify-between gap-3 rounded-base border py-4 px-[18px] text-sm leading-[1.5] ${colores[estado]}`}>
      {children}
    </div>
  );
}

export function Esqueleto({ className = '' }: React.ComponentPropsWithoutRef<"div">) {
  return <div className={`animate-pulse bg-borde rounded-base ${className}`} />;
}

export function Toast({ children }: React.ComponentPropsWithoutRef<"div">) {
  const [visible, setVisible] = useState(true);
  useEffect(() => {
    const temporizador = setTimeout(() => setVisible(false), 4000);
    return () => clearTimeout(temporizador);
  }, []);
  if (!visible) return null;
  return <div role="status" className="fixed top-4 right-4 bg-tinta text-white px-4 py-2 rounded-base shadow-lg z-50">{children}</div>;
}

const ENFOCABLES = 'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * Diálogo modal de confirmación. Al abrirse enfoca "Cancelar", mantiene el foco dentro (también si se hace clic en el
 * fondo), se cierra con Escape
 * y, al cerrarse, devuelve el foco al elemento que lo tenía.
 */
export function DialogoConfirmacion({ abierto, titulo, children, textoConfirmar = 'Confirmar', textoCancelar = 'Cancelar', confirmar, cerrar }: {
  abierto: boolean;
  titulo: string;
  children?: React.ReactNode;
  textoConfirmar?: string;
  textoCancelar?: string;
  confirmar: () => void;
  cerrar: () => void;
}) {
  const idTitulo = useId();
  const dialogo = useRef<HTMLDivElement>(null);
  const cerrarActual = useRef(cerrar);
  useEffect(() => { cerrarActual.current = cerrar; });

  useEffect(() => {
    if (!abierto) return;
    const anterior = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    dialogo.current?.querySelector<HTMLElement>(ENFOCABLES)?.focus();
    return () => anterior?.focus();
  }, [abierto]);

  if (!abierto) return null;

  function teclado(evento: React.KeyboardEvent) {
    if (evento.key === 'Escape') {
      evento.stopPropagation();
      cerrarActual.current();
      return;
    }
    if (evento.key !== 'Tab' || !dialogo.current) return;
    const enfocables = [...dialogo.current.querySelectorAll<HTMLElement>(ENFOCABLES)];
    if (enfocables.length === 0) return;
    const primero = enfocables[0];
    const ultimo = enfocables[enfocables.length - 1];
    if (evento.shiftKey && document.activeElement === primero) {
      evento.preventDefault();
      ultimo.focus();
    } else if (!evento.shiftKey && document.activeElement === ultimo) {
      evento.preventDefault();
      primero.focus();
    }
  }

  return (
    // Un clic en el fondo no se lleva el foco fuera del diálogo.
    <div onMouseDown={evento => { if (evento.target === evento.currentTarget) evento.preventDefault(); }} className="fixed inset-0 bg-tinta/50 flex items-center justify-center z-50">
      <div ref={dialogo} role="dialog" aria-modal="true" aria-labelledby={idTitulo} tabIndex={-1} onKeyDown={teclado} className="max-w-md w-full bg-panel border border-borde rounded-base p-5">
        <h2 id={idTitulo} className="text-titulo-tarjeta font-display">{titulo}</h2>
        {children && <div className="mt-3 text-tinta-suave">{children}</div>}
        <div className="flex gap-4 justify-end mt-6">
          <Boton type="button" principal={false} onClick={cerrar}>{textoCancelar}</Boton>
          <Boton type="button" onClick={confirmar}>{textoConfirmar}</Boton>
        </div>
      </div>
    </div>
  );
}

/** Selector con la flecha y las medidas del selector de API de N.1. `className` se aplica al contenedor (por ejemplo, el ancho). */
export function Selector({ opciones, className = '', ...props }: Omit<React.ComponentPropsWithoutRef<"select">, 'children'> & { opciones: { etiqueta: string; valor: string; deshabilitada?: boolean }[] }) {
  return (
    <div className={`relative ${className}`}>
      <select
        className="w-full appearance-none rounded-base border border-borde-campo bg-panel py-[9px] pl-3 pr-9 text-[14px] leading-[1.5] font-medium text-tinta outline-none focus:border-principal focus:ring-[3px] focus:ring-anillo-foco"
        {...props}
      >
        {opciones.map(opcion => <option key={opcion.valor} value={opcion.valor} disabled={opcion.deshabilitada}>{opcion.etiqueta}</option>)}
      </select>
      <svg aria-hidden="true" width="16" height="16" viewBox="0 0 16 16" fill="none" className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-tinta-suave">
        <path d="M4 6 L8 10 L12 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    </div>
  );
}

// RNF-12: estados comunes para las páginas y los guardias (11 §4).
export function EstadoCargando() {
  return <div role="status" aria-label="Cargando" className="p-4 space-y-3">
    <Esqueleto className="h-6 w-1/2" /><Esqueleto className="h-24 w-full" />
    <span className="sr-only">Cargando...</span>
  </div>;
}

export function EstadoError({ mensaje = 'No se pudo cargar la información.', reintentar }: { mensaje?: string; reintentar: () => void }) {
  return <Aviso estado="error">
    <p>{mensaje}</p>
    <Boton type="button" principal={false} onClick={reintentar} className="shrink-0">Reintentar</Boton>
  </Aviso>;
}

export function EstadoSinPermiso() {
  return <div role="alert" className="p-8">
    <h1 className="text-titulo font-display mb-2">No tiene permiso para ver esta página</h1>
    <p>Su rol no le permite acceder a este recurso.</p>
  </div>;
}
