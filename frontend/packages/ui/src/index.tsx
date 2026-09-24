import React from 'react';

export function Boton({ principal = true, deshabilitado = false, className = '', children, ...props }: React.ComponentPropsWithoutRef<"button"> & { principal?: boolean; deshabilitado?: boolean }) {
  const base = "h-[46px] rounded-md px-6 font-medium transition-colors inline-flex items-center justify-center";
  let variant = "bg-principal text-white hover:bg-principal-hover";
  if (!principal) {
    variant = "border border-borde-campo text-tinta bg-white hover:bg-gray-50";
  }
  if (deshabilitado) {
    variant = "border border-[#E4E9F1] text-[#A3AEC2] bg-white cursor-not-allowed";
  }
  return <button disabled={deshabilitado} className={`${base} ${variant} ${className}`} {...props}>{children}</button>;
}

export function Campo({ error, className = '', ...props }: React.ComponentPropsWithoutRef<"input"> & { error?: string }) {
  return (
    <div className={`flex flex-col gap-1 ${className}`}>
      <input 
        className={`h-11 rounded-md border px-3 outline-none transition-colors ${error ? 'border-alerta focus:border-alerta' : 'border-borde-campo focus:border-principal'} bg-white text-tinta`} 
        {...props} 
      />
      {error && <span className="text-alerta text-sm">{error}</span>}
    </div>
  );
}

export function Etiqueta({ estado, children }: { estado: 'correcto' | 'alerta' | 'neutro' | 'info', children: React.ReactNode }) {
  const map = {
    correcto: 'eti-c',
    alerta: 'eti-a',
    neutro: 'eti-n',
    info: 'eti-i'
  };
  return <span className={map[estado]}>{children}</span>;
}

export function Tarjeta({ children, className = '' }: React.ComponentPropsWithoutRef<"div">) {
  return <div className={`bg-white border border-borde rounded-md p-6 ${className}`}>{children}</div>;
}

export function Tabla({ headers, rows }: { headers: string[], rows: React.ReactNode[][] }) {
  return (
    <div className="w-full overflow-auto">
      <table className="w-full text-left border-collapse">
        <thead>
          <tr>
            {headers.map((h, i) => (
              <th key={i} className="py-3 px-4 text-[11px] uppercase tracking-[.12em] text-tinta-suave border-b border-borde-fila font-medium">{h}</th>
            ))}
          </tr>
        </thead>
        <tbody className="tabular-nums">
          {rows.map((row, i) => (
            <tr key={i} className="h-[42px] border-b border-borde-fila last:border-none text-[15px]">
              {row.map((cell, j) => (
                <td key={j} className="px-4 text-tinta">{cell}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function Aviso({ estado, children }: { estado: 'error' | 'exito' | 'info', children: React.ReactNode }) {
  if (estado === 'error') {
    return <div className="eti-a inline-flex gap-2 items-center">{children}</div>;
  }
  return <div className="eti-n inline-flex gap-2 items-center">{children}</div>;
}

export function Esqueleto({ className = '' }: React.ComponentPropsWithoutRef<"div">) {
  return <div className={`animate-pulse bg-borde rounded-md ${className}`} />;
}

export function Toast({ children }: React.ComponentPropsWithoutRef<"div">) {
  return <div className="fixed top-4 right-4 bg-tinta text-white px-4 py-2 rounded-md shadow-lg z-50">{children}</div>;
}

export function DialogoConfirmacion({ open, titulo, onClose, onConfirm }: { open: boolean; titulo: string; onClose: () => void; onConfirm: () => void }) {
  if (!open) return null;
  return (
    <div className="fixed inset-0 bg-tinta/50 flex items-center justify-center z-50">
      <Tarjeta className="max-w-md w-full">
        <h3 className="text-[22px] font-display mb-4">{titulo}</h3>
        <div className="flex gap-4 justify-end mt-6">
          <Boton principal={false} onClick={onClose}>Cancelar</Boton>
          <Boton onClick={onConfirm}>Confirmar</Boton>
        </div>
      </Tarjeta>
    </div>
  );
}

export function Selector({ options, value, onChange }: { options: {label: string, value: string}[]; value: string; onChange: (e: React.ChangeEvent<HTMLSelectElement>) => void }) {
  return (
    <select 
      value={value} 
      onChange={onChange}
      className="h-11 rounded-md border border-borde-campo px-3 bg-white text-tinta outline-none focus:border-principal"
    >
      {options.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  );
}
