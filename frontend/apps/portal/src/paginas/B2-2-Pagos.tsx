import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/pagos';
import { EstadoCargando, EstadoError, Etiqueta } from '@shapi/ui';
import { Link } from 'react-router';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';
import { dinero } from '../modulos/suscripcion/planes';

const cliente = crearCliente<paths>(window.location.origin);
const meses = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
const DIA = 86_400_000;
type Pago = components['schemas']['Pago'];

function fecha(valor: string | Date) {
  const partes = new Intl.DateTimeFormat('en-CA', {
    day: 'numeric', month: 'numeric', year: 'numeric', timeZone: 'America/Guatemala',
  }).formatToParts(typeof valor === 'string' ? new Date(valor) : valor);
  const numero = (tipo: Intl.DateTimeFormatPartTypes) => Number(partes.find(parte => parte.type === tipo)!.value);
  return `${numero('day')} ${meses[numero('month') - 1]} ${numero('year')}`;
}

function periodo(pago: Pago) {
  if (!pago.periodoInicio || !pago.periodoFin) return null;
  return `${fecha(pago.periodoInicio)} – ${fecha(new Date(new Date(pago.periodoFin).getTime() - DIA))}`;
}

function medio(pago: Pago) {
  if (!pago.medioPago) return '—';
  const marcas = { visa: 'Visa', mastercard: 'Mastercard', americanExpress: 'American Express' } as const;
  return `${marcas[pago.medioPago.marca]} •••• ${pago.medioPago.ultimos4}`;
}

function estado(pago: Pago) {
  if (pago.estado === 'autorizado') return <Etiqueta estado="correcto">Pagado</Etiqueta>;
  if (pago.estado === 'rechazado') return <Etiqueta estado="alerta">Rechazado</Etiqueta>;
  return <Etiqueta estado="neutro">Revertido</Etiqueta>;
}

export default function Pagos() {
  const marca = useMarcaPortal();
  const consulta = useQuery({
    queryKey: ['portal', 'pagos', 1],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/portal/pagos', {
        params: { query: { pagina: 1, tamano: 20 } }, signal,
      });
      if (!response.ok || !data) throw new Error('No se pudieron cargar los pagos.');
      return data;
    },
    retry: false,
  });

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudieron cargar los pagos." reintentar={() => void consulta.refetch()} />;

  return <section>
    <header>
      <p className="text-etiqueta uppercase tracking-[.14em] text-[var(--marca-principal)]">Mi cuenta</p>
      <h1 className="mt-2 font-display text-[32px] leading-[1.2]">Historial de pagos</h1>
      <p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">Los cobros de su suscripción a la {marca.nombreApi}.</p>
    </header>

    {consulta.data.elementos.length === 0 ? <section className="mt-8 rounded-base border border-borde bg-panel p-5">
      <div className="flex flex-col items-center gap-6 px-4 py-20 text-center">
        <div aria-hidden="true" className="flex size-10 items-center justify-center rounded-base border-2 border-dashed border-borde-campo text-[var(--marca-principal)]">▭</div>
        <div className="max-w-[560px]">
          <h2 className="font-display text-[22px] leading-[1.3]">Todavía no tiene pagos</h2>
          <p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">Cuando contrate un plan de esta API, aquí aparecerá cada cobro con su fecha, su concepto, su monto, su tarjeta y su estado.</p>
        </div>
        <Link className="inline-flex h-[46px] items-center rounded-base bg-principal px-6 text-[15px] font-medium text-white" to="/planes">Ver los planes</Link>
      </div>
    </section> : <section className="mt-8 rounded-base border border-borde bg-panel p-5 pb-2">
      <h2 className="font-display text-[21px] leading-[1.3]">Sus pagos</h2>
      <div className="mt-5 overflow-x-auto">
        <table aria-label="Sus pagos" className="w-full min-w-[760px] border-collapse text-left">
          <thead><tr>{['Fecha', 'Concepto', 'Monto', 'Medio de pago', 'Estado'].map(titulo => <th key={titulo} className="border-b border-borde pb-2.5 pr-4 text-encabezado font-semibold uppercase tracking-[.12em] text-tinta-suave">{titulo}</th>)}</tr></thead>
          <tbody>{consulta.data.elementos.map(pago => <tr key={pago.id} className="border-b border-borde-fila last:border-0">
            <td className="py-3 pr-4 text-[15px] text-[#2B3547]">{fecha(pago.fecha)}</td>
            <td className="py-3 pr-4"><span className="block text-[15px] font-medium text-tinta">{pago.descripcion}</span>{periodo(pago) && <span className="block text-[14px] text-tinta-suave">{periodo(pago)}</span>}</td>
            <td className="py-3 pr-4 text-[15px] tabular-nums text-[#2B3547]">{dinero(pago.monto)}</td>
            <td className="py-3 pr-4 text-[15px] text-[#2B3547]">{medio(pago)}</td>
            <td className="py-3 text-[15px]">{estado(pago)}</td>
          </tr>)}</tbody>
        </table>
      </div>
    </section>}
  </section>;
}
