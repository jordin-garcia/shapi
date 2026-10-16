import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/pagos';
import { EstadoCargando, EstadoError, Toast } from '@shapi/ui';
import PaginaA63bRevertirPago from './A6-3b-RevertirPago';

const cliente = crearCliente<paths>(window.location.origin);
type Pago = components['schemas']['PagoPlataformaAdministracion'];

async function listar() {
  const { data, response } = await cliente.GET('/api/admin/pagos');
  if (!response.ok || !data) throw new Error('No se pudieron cargar los pagos de plataforma.');
  return data;
}
function dinero(monto: number) { return `Q ${monto.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`; }
function fecha(valor: string) { return new Intl.DateTimeFormat('es-GT', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'America/Guatemala' }).format(new Date(valor)); }

export default function PaginaA63Pagos() {
  const cache = useQueryClient();
  const consulta = useQuery({ queryKey: ['admin', 'pagos'], queryFn: listar, retry: false });
  const [seleccionado, setSeleccionado] = useState<Pago | null>(null);
  const [aviso, setAviso] = useState(false);
  const reversion = useMutation({
    mutationFn: async (pago: Pago) => {
      const { response } = await cliente.POST('/api/admin/pagos/{id}/revertir', { params: { path: { id: pago.id } } });
      if (!response.ok) throw new Error('No se pudo revertir el pago.');
    },
    onSuccess: async () => { setSeleccionado(null); setAviso(true); await cache.invalidateQueries({ queryKey: ['admin', 'pagos'] }); },
  });
  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudieron cargar los pagos de plataforma." reintentar={() => void consulta.refetch()} />;
  if (seleccionado) return <PaginaA63bRevertirPago pago={seleccionado} cancelar={() => { setSeleccionado(null); reversion.reset(); }} confirmar={() => reversion.mutate(seleccionado)} guardando={reversion.isPending} error={reversion.isError} />;
  return <div>
    {aviso && <Toast key="reversion">Pago revertido.</Toast>}
    <header className="flex flex-col gap-2.5"><p className="text-etiqueta uppercase tracking-[.16em] text-tinta-suave">Plataforma</p><h1 className="font-display text-[32px] leading-[1.2]">Pagos de la plataforma</h1><p className="text-[15px] leading-[1.55] text-tinta-suave">Cobros de las suscripciones de plataforma. De cada tarjeta solo se conservan su marca y sus últimos cuatro dígitos.</p></header>
    {reversion.isError && <div className="mt-5"><EstadoError mensaje="No se pudo revertir el pago." reintentar={() => seleccionado && reversion.mutate(seleccionado)} /></div>}
    <section aria-label="Pagos de plataforma" className="mt-8 rounded-base border border-borde bg-panel p-5"><div className="w-full overflow-auto"><table className="w-full border-collapse text-left">
      <thead><tr>{['Fecha', 'Organización', 'Plan', 'Monto', 'Tarjeta', 'Estado', 'Acción'].map(t => <th key={t} className="border-b border-borde pb-2.5 pr-4 text-encabezado uppercase text-tinta-suave">{t}</th>)}</tr></thead>
      <tbody>{consulta.data.map(pago => <tr key={pago.id} className="border-b border-borde-fila last:border-b-0">
        <td className="whitespace-nowrap py-3 pr-4">{fecha(pago.fecha)}</td><td className="py-3 pr-4 font-medium">{pago.organizacion}</td><td className="py-3 pr-4">{pago.plan}</td><td className="whitespace-nowrap py-3 pr-4">{dinero(pago.monto)}</td><td className="whitespace-nowrap py-3 pr-4">{pago.tarjeta ?? '—'}</td>
        <td className="py-3 pr-4"><span className={`inline-block rounded-base border px-2.5 py-1 text-[12px] font-semibold uppercase ${pago.estado === 'pagado' ? 'border-[#B6DCC9] bg-[#E4F3EC] text-[#146542]' : pago.estado === 'rechazado' ? 'border-[#EDC3B4] bg-[#FBE9E3] text-[#8E3315]' : 'border-borde bg-fondo text-tinta-suave'}`}>{pago.estado === 'pagado' ? 'Pagado' : pago.estado === 'rechazado' ? 'Rechazado' : 'Revertido'}</span></td>
        <td className="py-3 text-right">{pago.estado === 'pagado' && <button type="button" aria-label={`Revertir pago de ${pago.organizacion}`} onClick={() => setSeleccionado(pago)} className="font-medium text-principal hover:underline">Revertir</button>}</td>
      </tr>)}</tbody>
    </table></div>{consulta.data.length === 0 && <div className="flex flex-col items-center gap-6 px-4 pb-11 pt-16 text-center"><svg aria-hidden="true" width="40" height="40" viewBox="0 0 40 40" fill="none"><rect x="1" y="1" width="38" height="38" rx="8" stroke="#C9D2E1" strokeWidth="1.5" strokeDasharray="4 4" /><path d="M11 18.5H29M14 23H18" stroke="#3B6FF0" strokeWidth="1.5" strokeLinecap="round" /></svg><div className="max-w-[500px]"><h2 className="font-display text-[22px]">Todavía no hay pagos registrados</h2><p className="mt-2 text-[15px] text-tinta-suave">Cuando una organización contrate un plan de plataforma, su cobro aparecerá aquí con su monto, su tarjeta y su estado.</p></div></div>}</section>
  </div>;
}
