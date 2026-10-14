import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/pagos';
import { EstadoCargando, EstadoError } from '@shapi/ui';

const cliente = crearCliente<paths>(window.location.origin);
type Pago = components['schemas']['Pago'];

const meses = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

function fecha(valor: string) {
  const fecha = new Date(valor);
  const partes = new Intl.DateTimeFormat('en-CA', {
    day: 'numeric', month: 'numeric', year: 'numeric', timeZone: 'America/Guatemala',
  }).formatToParts(fecha);
  const parte = (tipo: Intl.DateTimeFormatPartTypes) => Number(partes.find(p => p.type === tipo)?.value);
  return `${parte('day')} ${meses[parte('month') - 1]} ${parte('year')}`;
}

function fechaFinDePeriodo(valor: string) {
  return fecha(new Date(new Date(valor).getTime() - 24 * 60 * 60 * 1000).toISOString());
}

function dinero(monto: number) {
  return `Q ${monto.toLocaleString('es-GT', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function concepto(pago: Pago) {
  return pago.concepto === 'cambioPlan' ? 'Cambio de plan de plataforma' : 'Suscripción de plataforma';
}

function descripcion(pago: Pago) {
  if (pago.concepto === 'cambioPlan' || !pago.periodoInicio || !pago.periodoFin) return pago.descripcion;
  const nombrePlan = pago.descripcion
    .replace(/^Contratación del plan /i, '')
    .replace(/^Renovación · /i, '')
    .replace(/^Reactivación · /i, '');
  return `${nombrePlan} · ${fecha(pago.periodoInicio)} – ${fechaFinDePeriodo(pago.periodoFin)}`;
}

function medioPago(pago: Pago) {
  if (!pago.medioPago) return 'Sin tarjeta registrada';
  const marcas = { visa: 'Visa', mastercard: 'Mastercard', americanExpress: 'American Express' };
  return `${marcas[pago.medioPago.marca]} •••• ${pago.medioPago.ultimos4}`;
}

function estado(pago: Pago) {
  switch (pago.estado) {
    case 'autorizado': return { texto: 'Pagado', estilo: 'border-[#B6DCC9] bg-[#E4F3EC] text-[#146542]' };
    case 'rechazado': return { texto: 'Rechazado', estilo: 'border-[#EDC3B4] bg-[#FBE9E3] text-[#8E3315]' };
    case 'revertido': return { texto: 'Revertido', estilo: 'border-borde bg-fondo text-tinta-suave' };
  }
}

export default function PaginaB13Pagos() {
  const consulta = useQuery({
    queryKey: ['pagos-plataforma'],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/pagos', { params: { query: { pagina: 1, tamano: 20 } }, signal });
      if (!response.ok || !data) throw new Error('No se pudo cargar el historial de pagos.');
      return data;
    },
    retry: false,
  });

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudo cargar el historial de pagos." reintentar={() => void consulta.refetch()} />;

  return <main className="mx-auto max-w-[1000px]">
    <header className="flex flex-col gap-[10px]">
      <p className="text-[12px] font-medium uppercase tracking-[0.16em] text-tinta-suave">Organización</p>
      <h1 className="font-display text-[32px] leading-tight">Historial de pagos</h1>
      <p className="text-[15px] leading-relaxed text-tinta-suave">Cobros de su suscripción de plataforma. De su tarjeta solo se conservan la marca y los últimos cuatro dígitos.</p>
    </header>

    <section aria-label="Pagos de plataforma" className="mt-8 rounded-base border border-borde bg-panel p-5">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[780px] border-collapse text-left">
          <thead>
            <tr className="border-b border-borde text-[11px] font-semibold uppercase tracking-[0.12em] text-tinta-suave">
              <th className="w-[124px] pb-[10px] pr-4">Fecha</th>
              <th className="pb-[10px] pr-4">Concepto</th>
              <th className="w-[124px] pb-[10px] pr-4">Monto</th>
              <th className="w-[196px] pb-[10px] pr-4">Medio de pago</th>
              <th className="w-[116px] pb-[10px]">Estado</th>
            </tr>
          </thead>
          {consulta.data.elementos.length > 0 && <tbody>
            {consulta.data.elementos.map((pago, indice) => {
              const etiqueta = estado(pago);
              const ultimaFila = indice === consulta.data.elementos.length - 1;
              return <tr key={pago.id} className="text-[15px] text-[#2B3547]">
                <td className={`py-3 pr-4 align-middle tabular-nums ${ultimaFila ? '' : 'border-b border-[#EBEFF5]'}`}>{fecha(pago.fecha)}</td>
                <td className={`py-3 pr-4 align-middle ${ultimaFila ? '' : 'border-b border-[#EBEFF5]'}`}>
                  <span className="block font-medium leading-[1.4] text-tinta">{concepto(pago)}</span>
                  <span className="block text-sm leading-[1.4] text-tinta-suave">{descripcion(pago)}</span>
                </td>
                <td className={`py-3 pr-4 align-middle tabular-nums ${ultimaFila ? '' : 'border-b border-[#EBEFF5]'}`}>{dinero(pago.monto)}</td>
                <td className={`py-3 pr-4 align-middle tabular-nums ${ultimaFila ? '' : 'border-b border-[#EBEFF5]'}`}>{medioPago(pago)}</td>
                <td className={`py-3 align-middle ${ultimaFila ? '' : 'border-b border-[#EBEFF5]'}`}>
                  <span className={`inline-block whitespace-nowrap rounded-base border px-[10px] py-[5px] text-xs font-semibold uppercase tracking-wide ${etiqueta.estilo}`}>{etiqueta.texto}</span>
                </td>
              </tr>;
            })}
          </tbody>}
        </table>
      </div>

      {consulta.data.elementos.length === 0 && <div className="flex flex-col items-center gap-6 px-4 pb-[52px] pt-16 text-center">
        <svg width="40" height="40" viewBox="0 0 40 40" fill="none" aria-hidden="true">
          <rect x="1" y="1" width="38" height="38" rx="8" stroke="#C9D2E1" strokeWidth="1.5" strokeDasharray="4 4" />
          <rect x="11" y="14.5" width="18" height="12" rx="2.5" stroke="#3B6FF0" strokeWidth="1.5" />
          <path d="M11 18.5H29M14 23H18" stroke="#3B6FF0" strokeWidth="1.5" strokeLinecap="round" />
        </svg>
        <div className="flex max-w-[520px] flex-col gap-[10px]">
          <h2 className="font-display text-[22px] leading-tight">Todavía no tiene pagos</h2>
          <p className="text-[15px] leading-relaxed text-tinta-suave">Su organización está en el plan Prueba, que no se cobra. Cuando contrate un plan de pago, aquí aparecerá cada cobro con su concepto, su monto, su tarjeta y su estado.</p>
        </div>
      </div>}
    </section>
  </main>;
}
