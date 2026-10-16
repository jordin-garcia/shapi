import type { components } from '@shapi/api/pagos';
import { Boton } from '@shapi/ui';

type Pago = components['schemas']['PagoPlataformaAdministracion'];

function dinero(monto: number) { return `Q ${monto.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`; }
function fecha(valor: string) { return new Intl.DateTimeFormat('es-GT', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'America/Guatemala' }).format(new Date(valor)); }

export default function PaginaA63bRevertirPago({ pago, cancelar, confirmar, guardando, error }: { pago: Pago; cancelar: () => void; confirmar: () => void; guardando: boolean; error: boolean }) {
  return <div><header className="flex flex-col gap-2.5"><p className="text-etiqueta uppercase tracking-[.16em] text-tinta-suave">Pagos de la plataforma</p><h1 className="font-display text-[32px] leading-[1.2]">Revertir un pago</h1><p className="text-[15px] leading-[1.55] text-tinta-suave">El monto se devuelve a la tarjeta y el pago queda registrado como <strong className="font-semibold text-tinta">revertido</strong>.</p><p className="text-[15px] leading-[1.55] text-tinta-suave">Si ese pago cubría el ciclo vigente, la suscripción de la organización entra en periodo de gracia y, terminado ese periodo, se suspende: sus APIs dejan de responder y sus consumidores reciben el código 403.</p></header>
    <section className="mt-8 max-w-2xl rounded-base border border-borde bg-panel p-5"><h2 className="font-display text-[21px]">Detalle del pago</h2><dl className="mt-4 divide-y divide-borde-fila border-t border-borde-fila">{[['Organización', pago.organizacion], ['Plan', pago.plan], ['Fecha', fecha(pago.fecha)], ['Monto', dinero(pago.monto)], ['Tarjeta', pago.tarjeta ?? '—']].map(([etiqueta, valor]) => <div key={etiqueta} className="flex justify-between gap-4 py-3"><dt className="text-tinta-suave">{etiqueta}</dt><dd className="font-medium">{valor}</dd></div>)}</dl>
    {error && <p role="alert" className="mt-4 text-sm text-red-700">No se pudo revertir el pago. Inténtelo de nuevo.</p>}<div className="mt-5 flex flex-col gap-3"><Boton type="button" disabled={guardando} onClick={confirmar}>{guardando ? 'Revirtiendo…' : 'Revertir pago'}</Boton><button type="button" onClick={cancelar} className="text-center text-sm text-principal hover:underline">Cancelar</button></div></section>
  </div>;
}
