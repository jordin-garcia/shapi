import { Link } from 'react-router';
import { EstadoCargando, EstadoError, Tarjeta } from '@shapi/ui';
import type { components } from '@shapi/api/consumo';
import { useConsumoConsumidor } from '../modulos/consumo/useConsumoConsumidor';

const meses = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
const numero = (valor: number) => valor.toLocaleString('en-US');
function fecha(valor: string) {
  const [anio, mes, dia] = valor.split('-').map(Number);
  return `${dia} ${meses[mes - 1]} ${anio}`;
}

export default function Consumo() {
  const consulta = useConsumoConsumidor();
  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudo cargar el consumo." reintentar={() => void consulta.refetch()} />;

  return <div>
    <div className="flex flex-col gap-[10px]">
      <p className="text-etiqueta font-medium uppercase tracking-[.16em] text-tinta-suave">Mi cuenta</p>
      <h1 className="font-display text-[32px] leading-[1.2]">Consumo del ciclo</h1>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">Datos consolidados por suscripción, ruta y día.</p>
    </div>
    {consulta.data ? <Indicadores datos={consulta.data} /> : <Vacio />}
  </div>;
}

function Indicadores({ datos }: { datos: components['schemas']['ConsumoCiclo'] }) {
  const porcentajeBarra = Math.min(100, Math.max(0, datos.porcentaje));
  return <>
    <div className="mt-8 rounded-base border border-borde bg-panel p-6">
      <div className="flex flex-wrap items-end justify-between gap-6">
        <div className="flex flex-col gap-[10px]">
          <p className="text-[13px] font-semibold leading-[1.4] text-tinta-suave">Ciclo en curso</p>
          <h2 className="font-display text-[22px] leading-[1.3]">{fecha(datos.inicio)} – {fecha(datos.fin)}</h2>
        </div>
        <div className="flex flex-col items-end gap-[6px]">
          <span className="font-display text-[32px] leading-none tracking-[-.02em] tabular-nums">{numero(datos.llamadas)}</span>
          <span className="text-[14px] leading-[1.5] text-tinta-suave">de {numero(datos.cuota)} llamadas de su cuota</span>
        </div>
      </div>
      <div className="mt-6 flex flex-col gap-3 border-t border-borde-fila pt-6">
        <div role="progressbar" aria-label="Cuota consumida" aria-valuemin={0} aria-valuemax={100} aria-valuenow={porcentajeBarra} className="h-2 overflow-hidden rounded-base bg-borde-fila">
          <div className="h-2 rounded-base bg-[var(--marca-principal)]" style={{ width: `${porcentajeBarra}%` }} />
        </div>
        <div className="flex flex-wrap items-baseline justify-between gap-6 text-[14px] text-tinta-suave">
          <span>{numero(Math.round(datos.porcentaje))} % de su cuota mensual</span>
          <span>Le quedan <span className="ml-2 text-[15px] font-medium text-tinta tabular-nums">{numero(datos.restante)} llamadas</span></span>
        </div>
      </div>
    </div>
    <Tarjeta className="mt-5">
      <h2 id="consumo-por-ruta" className="font-display text-[21px] leading-[1.3]">Consumo por ruta</h2>
      <div className="mt-5 overflow-x-auto">
        <table aria-labelledby="consumo-por-ruta" className="w-full border-collapse text-left">
          <thead><tr>{['Ruta', 'Peticiones', 'Descuenta por petición', 'Llamadas descontadas', '% del consumo'].map(titulo => <th key={titulo} scope="col" className="border-b border-borde pr-4 pb-[10px] text-[11px] font-semibold uppercase tracking-[.12em] text-tinta-suave last:pr-0">{titulo}</th>)}</tr></thead>
          <tbody>
            {datos.rutas.map(ruta => <tr key={ruta.rutaId ?? 'sin-ruta'} className="border-b border-borde-fila text-[15px] leading-[1.6] text-[#2B3547] tabular-nums [&>td]:py-3 [&>td]:pr-4 [&>td:last-child]:pr-0">
              <td><span className="inline-block w-11 text-[11px] font-semibold tracking-[.06em] text-tinta-suave">{ruta.metodo}</span><span className="font-medium text-tinta">{ruta.patron}</span></td>
              <td>{numero(ruta.peticiones)}</td>
              <td>{ruta.pesoLlamadas === null ? '—' : `${numero(ruta.pesoLlamadas)} ${ruta.pesoLlamadas === 1 ? 'llamada' : 'llamadas'}`}</td>
              <td>{numero(ruta.llamadas)}</td>
              <td>{ruta.porcentaje.toFixed(1)} %</td>
            </tr>)}
            <tr className="border-t border-borde text-[15px] font-medium leading-[1.6] text-tinta tabular-nums [&>td]:py-3 [&>td]:pr-4 [&>td:last-child]:pr-0">
              <td>Total del ciclo</td><td>{numero(datos.peticiones)}</td><td></td><td>{numero(datos.llamadasConsolidadas)}</td><td>{datos.llamadasConsolidadas > 0 ? '100 %' : '0 %'}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </Tarjeta>
  </>;
}

function Vacio() {
  return <Tarjeta className="mt-8">
    <div className="flex flex-col items-center gap-6 pt-[88px] pb-24 text-center">
      <svg aria-hidden="true" width="40" height="40" viewBox="0 0 40 40" fill="none"><rect x="1" y="1" width="38" height="38" rx="8" stroke="var(--borde-campo)" strokeWidth="1.5" strokeDasharray="4 4" /><path d="M11 27 V21 M20 27 V16 M29 27 V12" stroke="var(--marca-principal)" strokeWidth="1.5" strokeLinecap="round" /></svg>
      <div className="flex max-w-[560px] flex-col gap-[10px]">
        <h2 className="font-display text-[22px] leading-[1.3]">Todavía no tiene un plan contratado</h2>
        <p className="text-[15px] leading-[1.55] text-tinta-suave">En cuanto contrate un plan de esta API verá aquí el consumo de cada ciclo, desglosado por ruta: las peticiones que hizo y las llamadas que descontaron de su cuota.</p>
      </div>
      <Link to="/planes" className="rounded-base bg-principal px-6 py-[13px] text-[15px] font-medium leading-[1.3] text-white hover:bg-principal-hover hover:text-white hover:no-underline">Ver los planes</Link>
    </div>
  </Tarjeta>;
}
