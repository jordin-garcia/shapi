import type { components } from '@shapi/api/soporte';
import { fechaHora } from './formato';

type Caso = components['schemas']['CasoDetalle'];
type Mensaje = components['schemas']['MensajeCaso'];

export function EtiquetaEstado({ estado }: { estado: 'abierto' | 'cerrado' }) {
  return <span className={estado === 'abierto'
    ? 'inline-flex rounded-base border border-[#B6DCC9] bg-[#E4F3EC] px-[10px] py-[5px] text-[12px] font-semibold uppercase tracking-[0.06em] text-[#146542]'
    : 'inline-flex rounded-base border border-borde bg-fondo px-[10px] py-[5px] text-[12px] font-semibold uppercase tracking-[0.06em] text-tinta-suave'}>
    {estado === 'abierto' ? 'Abierto' : 'Cerrado'}
  </span>;
}

export function Conversacion({ caso, proveedor, integrada = false }: { caso: Caso; proveedor: boolean; integrada?: boolean }) {
  return <section aria-label="Conversación" className={integrada ? '' : 'mt-6 rounded-base border border-borde bg-panel p-5'}>
    <h2 className="font-display text-[20px] leading-[1.3]">Conversación</h2>
    <div className="mt-5 flex flex-col gap-4 border-t border-borde pt-5">
      {caso.mensajes.map((mensaje: Mensaje) => {
        const propio = proveedor && !mensaje.esPersonalPlataforma;
        return <article key={mensaje.id} className={mensaje.esPersonalPlataforma
          ? 'w-full rounded-base border border-borde bg-fondo px-4 py-[14px]'
          : 'w-full'}>
          <p className="text-[14px] text-tinta-suave">
            <span className="font-semibold text-tinta">{mensaje.autor}</span>
            {propio ? ' (usted)' : ''}
            {' · '}
            {mensaje.esPersonalPlataforma ? (proveedor ? 'Soporte de Shapi · ' : `${titulo(mensaje.rol)} · `) : (!proveedor ? `${titulo(mensaje.rol)} · ${caso.organizacion} · ` : '')}
            {fechaHora(mensaje.creadoEn)}
          </p>
          <p className="mt-[6px] whitespace-pre-wrap text-[15px] leading-[1.6] text-[#2B3547]">{mensaje.cuerpo}</p>
        </article>;
      })}
    </div>
  </section>;
}

export const claseCampo = 'w-full rounded-base border border-borde-campo bg-panel px-[14px] py-[11px] text-[15px] leading-[1.5]';
export const claseEtiqueta = 'flex flex-col gap-[6px] text-[13px] font-semibold';
export const claseBoton = 'rounded-base bg-principal px-6 py-[13px] text-[15px] font-medium text-white disabled:cursor-not-allowed disabled:opacity-60';
export const claseBotonSecundario = 'rounded-base border border-borde-campo bg-panel px-[23px] py-3 text-[15px] font-medium disabled:opacity-60';

function titulo(valor: string) {
  return valor.charAt(0).toUpperCase() + valor.slice(1);
}
