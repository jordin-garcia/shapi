import { Link } from 'react-router';

export default function PaginaA23Confirmacion() {
  return <section role="status" className="mx-auto max-w-[720px] rounded-base border border-borde bg-panel p-8 md:p-10">
    <h1 className="font-display text-[32px]">Plan activado</h1>
    <p className="mt-3 text-[15px] leading-relaxed text-tinta-suave">El cobro fue autorizado y su suscripción de plataforma ya está vigente.</p>
    <p className="mt-6 text-sm">La renovación se cobra a esta misma tarjeta al cierre de cada ciclo.</p>
    <Link className="mt-8 inline-flex h-[46px] items-center rounded-base bg-principal px-5 font-medium text-white" to="/panel/suscripcion">Ver suscripción</Link>
  </section>;
}
