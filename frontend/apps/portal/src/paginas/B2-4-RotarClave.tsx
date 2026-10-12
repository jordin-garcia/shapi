import { useState } from 'react';
import { Aviso, Boton } from '@shapi/ui';
import type { Clave } from '../modulos/claves/claves';

const fechaHora = (valor: Date) => new Intl.DateTimeFormat('es-GT', {
  day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  hourCycle: 'h23', timeZone: 'America/Guatemala',
}).format(valor);

const nombreTipo = (tipo: Clave['tipo']) => tipo === 'produccion' ? 'Producción' : 'Pruebas';

export default function RotarClave({ api, plan, clave, procesando, error, confirmar, cancelar }: {
  api: string;
  plan: string;
  clave: Clave;
  procesando: boolean;
  error?: string;
  confirmar: () => void;
  cancelar: () => void;
}) {
  const [expira] = useState(() => new Date(Date.now() + 24 * 60 * 60 * 1000));
  return <section className="mx-auto max-w-[600px] rounded-base border border-borde bg-panel p-10">
    <div aria-hidden="true" className="flex size-8 items-center justify-center rounded-full border border-[var(--marca-principal)] text-[var(--marca-principal)]">!</div>
    <header className="mt-5">
      <p className="text-etiqueta uppercase tracking-[.14em] text-[var(--marca-principal)]">Suscripción y claves</p>
      <h1 className="mt-2 font-display text-[32px] leading-[1.2]">Rotar clave</h1>
      <p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">Se emite una clave nueva de {nombreTipo(clave.tipo).toLowerCase()}. La actual <strong className="font-semibold text-tinta">sigue funcionando 24 horas</strong> para que la reemplace en su sistema sin interrumpir el servicio.</p>
    </header>
    {error && <div className="mt-5"><Aviso estado="error">{error}</Aviso></div>}
    <dl className="mt-8 flex flex-col gap-3.5 border-t border-borde pt-6 text-[14px]">
      <Fila nombre="API" valor={api} />
      <Fila nombre="Plan" valor={plan} />
      <Fila nombre="Tipo" valor={nombreTipo(clave.tipo)} />
      <Fila nombre="Clave actual" valor={clave.claveEnmascarada} mono />
      <Fila nombre="La clave actual deja de funcionar" valor={fechaHora(expira)} />
    </dl>
    <div className="mt-6 flex flex-col gap-3.5 border-t border-borde pt-6">
      <Boton type="button" deshabilitado={procesando} onClick={confirmar}>{procesando ? 'Rotando…' : 'Rotar clave'}</Boton>
      <button type="button" className="text-[14px] text-[var(--marca-principal)]" onClick={cancelar}>Cancelar</button>
    </div>
  </section>;
}

function Fila({ nombre, valor, mono = false }: { nombre: string; valor: string; mono?: boolean }) {
  return <div className="flex justify-between gap-5"><dt className="text-tinta-suave">{nombre}</dt><dd className={`text-right font-medium ${mono ? 'font-mono' : ''}`}>{valor}</dd></div>;
}
