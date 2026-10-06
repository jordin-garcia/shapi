import { Link } from 'react-router';
import type { components } from '@shapi/api/suscripciones';

type Contratacion = components['schemas']['ResultadoContratacionPlataforma'];
const fecha = (valor: string) => new Intl.DateTimeFormat('es-GT', {
  day: 'numeric', month: 'short', year: 'numeric', timeZone: 'America/Guatemala',
}).format(new Date(valor));

export default function PaginaA23Confirmacion({ contratacion }: { contratacion: Contratacion }) {
  return <section role="status" className="mx-auto max-w-[720px] rounded-base border border-borde bg-panel p-8 md:p-10">
    <h1 className="font-display text-[32px]">Plan activado</h1>
    <p className="mt-3 text-[15px] leading-relaxed text-tinta-suave">El cobro fue autorizado y su suscripción de plataforma ya está vigente.</p>
    <dl className="mt-8 space-y-4 border-t border-borde pt-6 text-sm">
      <div className="flex justify-between gap-4"><dt>Plan</dt><dd>{contratacion.plan.nombre} · Activa</dd></div>
      <div className="flex justify-between gap-4"><dt>Precio</dt><dd>{'Q ' + contratacion.plan.precio.toLocaleString('es-GT', { minimumFractionDigits: 2 })}</dd></div>
      <div className="flex justify-between gap-4"><dt>Vigencia</dt><dd>{contratacion.plan.vigenciaDias} días</dd></div>
      <div className="flex justify-between gap-4"><dt>Periodo activo</dt><dd>{fecha(contratacion.periodo.inicio)} – {fecha(contratacion.periodo.fin)}</dd></div>
      <div className="flex justify-between gap-4"><dt>Próxima renovación automática</dt><dd>{new Intl.DateTimeFormat('es-GT', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'America/Guatemala' }).format(new Date(contratacion.proximaRenovacion))}</dd></div>
      <div className="flex justify-between gap-4"><dt>Medio de pago</dt><dd>{contratacion.tarjetaEnmascarada}</dd></div>
    </dl>
    <p className="mt-6 border-t border-borde pt-6 text-sm leading-relaxed text-tinta-suave">La renovación se cobra a esta misma tarjeta al cierre de cada ciclo.</p>
    <Link className="mt-6 inline-flex h-[46px] items-center rounded-base bg-principal px-5 font-medium text-white" to="/panel/apis">Ir a mis APIs</Link>
  </section>;
}
