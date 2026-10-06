import { Link } from 'react-router';

export default function PaginaA24Rechazo({ monto, mensaje }: { monto: string; mensaje: string }) {
  return <section role="alert" className="mx-auto max-w-[720px] rounded-base border border-[#D66B62] bg-[#FFF2F0] p-8 md:p-10">
    <h1 className="font-display text-[32px]">Tarjeta rechazada</h1>
    <p className="mt-3 text-[15px] leading-relaxed">{mensaje || 'No se autorizó el cobro de ' + monto + ', así que su plan vigente no cambia.'}</p>
    <Link className="mt-6 inline-flex h-[46px] items-center rounded-base bg-principal px-5 font-medium text-white" to="/panel/suscripcion/planes">Elegir otra tarjeta</Link>
  </section>;
}
