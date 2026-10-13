import { useState } from 'react';
import { Boton, Etiqueta } from '@shapi/ui';
import type { Clave, ClaveEmitida } from '../modulos/claves/claves';

const mesesCortos = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
function fechaHora(valor: string) {
  const partes = new Intl.DateTimeFormat('en-CA', {
    day: 'numeric', month: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit',
    hourCycle: 'h23', timeZone: 'America/Guatemala',
  }).formatToParts(new Date(valor));
  const numero = (tipo: Intl.DateTimeFormatPartTypes) => Number(partes.find(parte => parte.type === tipo)!.value);
  return `${numero('day')} ${mesesCortos[numero('month') - 1]} ${numero('year')} a las ${String(numero('hour')).padStart(2, '0')}:${String(numero('minute')).padStart(2, '0')}`;
}
const tipoTitulo = (tipo: ClaveEmitida['tipo']) => tipo === 'produccion' ? 'producción' : 'pruebas';

export default function ClaveNueva({ clave, anterior, terminar }: {
  clave: ClaveEmitida;
  anterior?: Clave;
  terminar: () => void;
}) {
  const [copiada, setCopiada] = useState(false);
  async function copiar() {
    try { await navigator.clipboard.writeText(clave.clave); setCopiada(true); } catch { setCopiada(false); }
  }
  return <section className="mx-auto max-w-[640px] rounded-base border border-borde bg-panel p-10">
    <header>
      <p className="text-etiqueta uppercase tracking-[.14em] text-[var(--marca-principal)]">Suscripción y claves</p>
      <h1 className="mt-2 font-display text-[32px] leading-[1.2]">Su clave nueva de {tipoTitulo(clave.tipo)}</h1>
      {anterior?.expiraEn && <p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">La clave anterior ({anterior.claveEnmascarada}) sigue funcionando hasta el {fechaHora(anterior.expiraEn)}.</p>}
    </header>
    <div className="mt-7 flex flex-col gap-2 border-t border-borde pt-6">
      <div className="flex items-center justify-between gap-3"><span className="text-[13px] font-semibold capitalize">{tipoTitulo(clave.tipo)}</span><Etiqueta estado="correcto">Activa</Etiqueta></div>
      <div className="flex items-center gap-3"><div className="min-w-0 flex-1 overflow-hidden text-ellipsis whitespace-nowrap rounded-base border border-borde bg-[#F4F6FA] px-3.5 py-2.5 font-mono text-[14px]">{clave.clave}</div><Boton type="button" principal={false} onClick={() => void copiar()}>{copiada ? 'Copiada' : 'Copiar'}</Boton></div>
    </div>
    <div className="mt-5 rounded-base border border-alerta-borde bg-alerta-fondo px-4 py-3 text-[14px] leading-[1.5] text-alerta"><strong>Cópiela ahora.</strong> Por seguridad no se volverá a mostrar. Si la pierde, puede rotarla otra vez.</div>
    <div className="mt-6 border-t border-borde pt-6"><Boton type="button" onClick={terminar}>Ya la guardé</Boton></div>
  </section>;
}
