import { Link } from 'react-router';
import type { components } from '@shapi/api/suscripciones';

type Suscripcion = components['schemas']['SuscripcionPlataforma'];

export default function PaginaA24Rechazo({ planId, plan, monto, tarjeta, suscripcion }: {
  planId: string;
  plan: string;
  monto: string;
  tarjeta: string;
  suscripcion: Suscripcion | null;
}) {
  const planActual = suscripcion?.plan;
  const periodoVigente = planActual ? `Sus APIs y sus claves siguen funcionando con los límites del plan ${planActual.nombre}.` : 'Su plan vigente y sus límites siguen sin cambios.';
  return <section role="alert" className="mx-auto max-w-[720px] rounded-base border border-[#D66B62] bg-[#FFF2F0] p-8 md:p-10">
    <h1 className="font-display text-[32px]">Tarjeta rechazada</h1>
    <p className="mt-3 text-[15px] leading-relaxed">No se autorizó el cobro de {monto}, así que <strong>la suscripción del plan {plan} no se activó</strong>. Puede intentar con otra tarjeta.</p>
    <dl className="mt-8 space-y-4 border-t border-borde pt-6 text-sm">
      <div className="flex justify-between gap-4"><dt>Plan que intentó contratar</dt><dd>{plan} · Rechazado</dd></div>
      <div className="flex justify-between gap-4"><dt>Monto</dt><dd>{monto}</dd></div>
      <div className="flex justify-between gap-4"><dt>Tarjeta utilizada</dt><dd>{tarjeta}</dd></div>
      <div className="flex justify-between gap-4"><dt>Plan vigente de su organización</dt><dd>{planActual ? `${planActual.nombre} · Q ${planActual.precio.toLocaleString('es-GT', { minimumFractionDigits: 2 })} · Sin cambios` : 'Sin cambios'}</dd></div>
    </dl>
    <p className="mt-6 border-t border-borde pt-6 text-sm leading-relaxed text-tinta-suave">{periodoVigente}</p>
    <div className="mt-6 flex flex-col gap-3">
      <Link className="inline-flex h-[46px] items-center justify-center rounded-base bg-principal px-5 font-medium text-white" to={'/panel/suscripcion/contratar/' + planId}>Usar otra tarjeta</Link>
      <Link className="text-center text-sm text-principal" to="/panel/suscripcion/planes">Volver a los planes</Link>
    </div>
  </section>;
}
