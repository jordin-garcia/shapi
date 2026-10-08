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

export function Conversacion({ caso, proveedor }: { caso: Caso; proveedor: boolean }) {
  return <section aria-label="Conversación" className="mt-6 rounded-base border border-borde bg-panel p-5">
    <h2 className="font-display text-[20px] leading-[1.3]">Conversación</h2>
    <div className="mt-4 flex flex-col gap-4">
      {caso.mensajes.map((mensaje: Mensaje) => {
        const propio = proveedor ? !mensaje.esPersonalPlataforma : mensaje.esPersonalPlataforma;
        return <article key={mensaje.id} className={propio
          ? 'ml-auto w-[82%] rounded-base border border-[#C8D7F8] bg-[#F3F7FF] p-4'
          : 'mr-auto w-[82%] rounded-base border border-borde bg-fondo p-4'}>
          <p className="text-[14px] font-semibold">{mensaje.autor}
            {propio && proveedor ? <span className="font-normal text-tinta-suave"> (usted)</span> : null}
          </p>
          <p className="text-[13px] text-tinta-suave">
            {mensaje.esPersonalPlataforma ? (proveedor ? 'Soporte de Shapi · ' : `${titulo(mensaje.rol)} · `) : (!proveedor ? `${titulo(mensaje.rol)} · ${caso.organizacion} · ` : '')}
            {fechaHora(mensaje.creadoEn)}
          </p>
          <p className="mt-3 whitespace-pre-wrap text-[15px] leading-[1.6] text-[#2B3547]">{mensaje.cuerpo}</p>
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
