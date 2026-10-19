import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { paths } from '@shapi/api/consumo';
import { EstadoCargando, EstadoError, EstadoSinPermiso, Etiqueta, Tarjeta } from '@shapi/ui';
import { useSesion } from '../modulos/sesion/useSesion';

const cliente = crearCliente<paths>(window.location.origin);
const numero = (valor: number) => valor.toLocaleString('en-US');
const dinero = (valor: number) => `Q ${valor.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
const meses = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
function ciclo(inicio: string, fin: string) {
  const [anio, mes, dia] = inicio.split('-').map(Number);
  const [anioFin, mesFin, diaFin] = fin.split('-').map(Number);
  return `${dia} ${meses[mes - 1]}${anio !== anioFin ? ` ${anio}` : ''} – ${diaFin} ${meses[mesFin - 1]} ${anioFin}`;
}
const encabezados = ['Consumidor', 'Plan', 'Ciclo en curso', 'Consumo del ciclo', 'Uso', 'Facturación', 'Cobro'];
const anchos = ['', 'w-[106px]', 'w-[184px]', 'w-[186px]', 'w-[82px]', 'w-[116px]', 'w-[104px]'];

export default function PaginaB12Consumidores() {
  const { id = '' } = useParams();
  const sesion = useSesion();
  const consulta = useQuery({
    queryKey: ['consumo-consumidores', sesion.data?.organizacionId, id],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/apis/{apiId}/consumidores', { params: { path: { apiId: id } }, signal });
      if (!response.ok || !data) throw new Error('No se pudo cargar el consumo de los consumidores.');
      return data;
    },
    enabled: !!sesion.data && !!id,
    retry: false,
  });
  if (sesion.isError) return <EstadoError mensaje="No se pudo consultar la sesión." reintentar={() => void sesion.refetch()} />;
  if (!sesion.isPending && !sesion.data) return <EstadoSinPermiso />;
  if (sesion.isPending || consulta.isPending) return <EstadoCargando />;
  if (consulta.error instanceof ErrorApi && consulta.error.estado === 403) return <EstadoSinPermiso />;
  if (consulta.isError) return <EstadoError mensaje={consulta.error instanceof ErrorApi ? consulta.error.titulo : 'No se pudo cargar el consumo de los consumidores.'} reintentar={() => void consulta.refetch()} />;
  const datos = consulta.data;
  const puedeInvitar = sesion.data?.rol === 'propietario' || sesion.data?.rol === 'editor';
  return <div>
    <div className="flex flex-col items-start justify-between gap-6 lg:flex-row lg:items-end">
      <div className="flex min-w-0 flex-1 flex-col gap-[10px]">
        <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">{datos.apiNombre}</p>
        <h1 className="font-display text-[32px] leading-[1.2]">Consumo y facturación por consumidor</h1>
        <p className="text-[15px] leading-[1.55] text-tinta-suave">Cada consumidor con el consumo de su ciclo en curso y lo que se le facturó por su plan. Consolidado por suscripción, ruta y día.</p>
      </div>
      {puedeInvitar && <Link to={`/panel/apis/${id}/consumidores/invitar`} className="inline-flex h-[46px] shrink-0 items-center justify-center rounded-base bg-principal px-6 text-[15px] font-medium text-white hover:bg-principal-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-principal">Invitar consumidores</Link>}
    </div>
    <Tarjeta className="mt-8">
      <div className="overflow-x-auto"><table className="w-full border-collapse text-left">
        <thead><tr>{encabezados.map((texto, i) => <th key={texto} scope="col" className={`border-b border-borde pb-[10px] pr-4 text-encabezado font-semibold uppercase text-tinta-suave last:pr-0 ${anchos[i]}`}>{texto}</th>)}</tr></thead>
        <tbody>{datos.elementos.map(fila => <tr key={fila.suscripcionId} className="border-b border-borde-fila last:border-0 text-[15px] tabular-nums">
          <td className="py-3 pr-4 font-medium">{fila.consumidor}</td>
          <td className="py-3 pr-4">{fila.plan}</td>
          <td className="whitespace-nowrap py-3 pr-4 text-tinta-suave">{ciclo(fila.inicio, fila.fin)}</td>
          <td className="whitespace-nowrap py-3 pr-4">{numero(fila.llamadas)} de {numero(fila.cuota)}</td>
          <td className="whitespace-nowrap py-3 pr-4">{numero(Math.round(fila.porcentaje))} %</td>
          <td className="whitespace-nowrap py-3 pr-4">{dinero(fila.facturacion)}</td>
          <td className="py-3"><Etiqueta estado={fila.ultimoCobro === 'autorizado' ? 'correcto' : fila.ultimoCobro === 'rechazado' ? 'alerta' : 'neutro'}>
            {fila.ultimoCobro === 'autorizado' ? 'Pagado' : fila.ultimoCobro === 'rechazado' ? 'Rechazado' : fila.ultimoCobro === 'revertido' ? 'Revertido' : 'Sin cobros'}
          </Etiqueta></td>
        </tr>)}</tbody>
      </table></div>
      {datos.elementos.length === 0 ? <div className="flex flex-col items-center gap-6 pt-16 pb-[52px] text-center">
        <svg aria-hidden="true" width="40" height="40" viewBox="0 0 40 40" fill="none"><rect x="1" y="1" width="38" height="38" rx="8" stroke="var(--borde-campo)" strokeWidth="1.5" strokeDasharray="4 4" /><circle cx="20" cy="16.5" r="4" stroke="var(--principal)" strokeWidth="1.5" /><path d="M12.5 28 C12.5 23.9 15.9 22 20 22 C24.1 22 27.5 23.9 27.5 28" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" /></svg>
        <div className="flex max-w-[520px] flex-col gap-[10px]">
          <h2 className="font-display text-[22px] leading-[1.3]">Todavía no tiene consumidores</h2>
          <p className="text-[15px] leading-[1.55] text-tinta-suave">Cuando alguien contrate un plan de esta API desde su portal, aquí aparecerá con el consumo de su ciclo y lo que se le facturó.</p>
        </div>
      </div> : <div className="mt-5 flex flex-wrap items-center justify-between gap-4 border-t border-borde pt-5 text-[14px] leading-[1.5] text-tinta-suave">
        <span>{datos.total} {datos.total === 1 ? 'consumidor' : 'consumidores'} con suscripción {datos.activas === datos.total ? 'activa' : 'vigente'}</span>
        <span>Facturación de los ciclos en curso <span className="ml-2 font-medium text-tinta">{dinero(datos.facturacion)}</span></span>
      </div>}
    </Tarjeta>
  </div>;
}
