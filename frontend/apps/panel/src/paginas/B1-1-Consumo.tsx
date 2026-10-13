import { useState } from 'react';
import { useParams } from 'react-router';
import { Boton, Campo, EstadoCargando, EstadoError, Tabla, Tarjeta } from '@shapi/ui';
import { ErrorApi } from '@shapi/api';
import type { components } from '@shapi/api/consumo';
import { useConsumo } from '../modulos/consumo/useConsumo';

type Datos = components['schemas']['ConsumoApi'];
type Percentil = components['schemas']['Percentil95'];
const meses = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];
const numero = (valor: number) => valor.toLocaleString('en-US');
const latencia = (valor: Percentil) => valor.supera2500 ? '> 2500 ms' : valor.milisegundos === null ? '—' : `${numero(Math.round(valor.milisegundos))} ms`;
const gb = (bytes: number) => `${(bytes / 1e9).toFixed(2)} GB`;
function partes(fecha: string) { const [anio, mes, dia] = fecha.split('-').map(Number); return { anio, mes, dia }; }
function tituloPeriodo(desde: string, hasta: string) {
  const inicio = partes(desde), fin = partes(hasta);
  if (inicio.anio === fin.anio && inicio.mes === fin.mes) return `Del ${inicio.dia} al ${fin.dia} de ${meses[fin.mes - 1]} de ${fin.anio}`;
  return `Del ${inicio.dia} de ${meses[inicio.mes - 1]} de ${inicio.anio} al ${fin.dia} de ${meses[fin.mes - 1]} de ${fin.anio}`;
}

export default function PaginaB11Consumo() {
  const { id = '' } = useParams();
  const [periodo, setPeriodo] = useState<{ desde: string; hasta: string }>();
  const [seleccion, setSeleccion] = useState<{ desde: string; hasta: string }>();
  const [errorPeriodo, setErrorPeriodo] = useState<string>();
  const consulta = useConsumo(id, periodo);
  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje={consulta.error instanceof ErrorApi ? consulta.error.titulo : 'No se pudo cargar el consumo.'} reintentar={() => {
    if (consulta.error instanceof ErrorApi && consulta.error.estado === 400 && periodo) setPeriodo(undefined);
    else void consulta.refetch();
  }} />;
  const datos = consulta.data;
  return <div>
    <div className="flex flex-wrap items-end justify-between gap-6">
      <div className="flex flex-col gap-[10px]">
        <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">{datos.apiNombre}</p>
        <h1 className="font-display text-[32px] leading-[1.2]">Consumo, latencia y errores</h1>
        <p className="text-[15px] leading-[1.55] text-tinta-suave">Datos consolidados por suscripción, ruta y día.</p>
      </div>
      <button type="button" aria-expanded={!!seleccion} className="flex items-center gap-3 rounded-base border border-borde-campo bg-panel px-[14px] py-[11px] text-[15px]" onClick={() => { setErrorPeriodo(undefined); setSeleccion(seleccion ? undefined : { desde: datos.desde, hasta: datos.hasta }); }}>
        {tituloPeriodo(datos.desde, datos.hasta)}
        <svg aria-hidden="true" width="16" height="16" viewBox="0 0 16 16" fill="none"><path d="M4 6 L8 10 L12 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" /></svg>
      </button>
    </div>
    {seleccion && <Tarjeta className="mt-5">
      <form className="flex flex-wrap items-end gap-4" onSubmit={e => {
        e.preventDefault();
        const dias = (Date.parse(seleccion.hasta) - Date.parse(seleccion.desde)) / 86400000 + 1;
        if (!Number.isFinite(dias) || dias < 1 || dias > 366) { setErrorPeriodo('Seleccione un periodo de hasta 366 días.'); return; }
        setErrorPeriodo(undefined); setPeriodo(seleccion); setSeleccion(undefined);
      }}>
        <Campo etiqueta="Desde" type="date" required error={errorPeriodo} value={seleccion.desde} max={seleccion.hasta} onChange={e => setSeleccion({ ...seleccion, desde: e.target.value })} />
        <Campo etiqueta="Hasta" type="date" required value={seleccion.hasta} min={seleccion.desde} onChange={e => setSeleccion({ ...seleccion, hasta: e.target.value })} />
        <Boton type="submit">Aplicar periodo</Boton>
        <Boton principal={false} type="button" onClick={() => setSeleccion(undefined)}>Cancelar</Boton>
      </form>
    </Tarjeta>}
    {datos.peticiones === 0 ? <Tarjeta className="mt-8">
      <div className="flex flex-col items-center gap-6 pt-[88px] pb-24 text-center">
        <svg aria-hidden="true" width="40" height="40" viewBox="0 0 40 40" fill="none"><rect x="1" y="1" width="38" height="38" rx="8" stroke="var(--borde-campo)" strokeWidth="1.5" strokeDasharray="4 4" /><path d="M11 27 V21 M20 27 V16 M29 27 V12" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" /></svg>
        <div className="flex max-w-[520px] flex-col gap-[10px]">
          <h2 className="font-display text-[22px] leading-[1.3]">Todavía no hay consumo de esta API</h2>
          <p className="text-[15px] leading-[1.55] text-tinta-suave">Cuando sus consumidores empiecen a llamar a la API, aquí verá las peticiones por ruta y por día, la latencia, los errores y los datos transferidos del periodo.</p>
        </div>
      </div>
    </Tarjeta> : <Indicadores datos={datos} />}
  </div>;
}

function Indicadores({ datos }: { datos: Datos }) {
  const maximo = Math.max(1, ...datos.dias.map(d => d.peticiones));
  const tarjetas = [
    ['Peticiones a la API', numero(datos.peticiones), `En los ${datos.dias.length} días del periodo`],
    ['Latencia total · percentil 95', latencia(datos.latenciaTotalP95), 'Lo que tarda la petición completa'],
    ['Latencia de la compuerta · percentil 95', latencia(datos.latenciaCompuertaP95), 'Lo que añade Shapi a esa latencia'],
    ['Datos transferidos', gb(datos.bytesEntrada + datos.bytesSalida), `${gb(datos.bytesEntrada)} de entrada · ${gb(datos.bytesSalida)} de salida`],
  ];
  const gruposErrores: { titulo: string; errores: Datos['erroresOrigen'] }[] = [
    { titulo: 'Rechazos de la compuerta · la petición no llegó a su servidor', errores: datos.rechazosCompuerta },
    { titulo: 'Errores de su servidor de origen', errores: datos.erroresOrigen },
  ];
  return <>
    <div className="mt-8 grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-4">
      {tarjetas.map(([etiqueta, cifra, detalle]) => <Tarjeta key={etiqueta} className="flex flex-col gap-[10px]">
        <p className="text-[13px] font-semibold leading-[1.4] text-tinta-suave">{etiqueta}</p>
        <span className="font-display text-[32px] leading-none tracking-[-0.02em] tabular-nums">{cifra}</span>
        <p className="text-[14px] leading-[1.5] text-tinta-suave"><span>{detalle}</span>{etiqueta === 'Peticiones a la API' && <span className="block">{numero(datos.llamadas)} llamadas descontadas</span>}</p>
      </Tarjeta>)}
    </div>
    <Tarjeta className="mt-5">
      <h2 className="font-display text-[21px] leading-[1.3]">Peticiones por día</h2>
      <div className="mt-5 flex items-end gap-3 overflow-x-auto">
        {datos.dias.map(dia => {
          const fecha = partes(dia.fecha), etiqueta = `${fecha.dia} ${meses[fecha.mes - 1].slice(0, 3)}`;
          return <div key={dia.fecha} role="img" aria-label={`${etiqueta}: ${numero(dia.peticiones)} peticiones`} className="flex min-w-[55px] flex-1 flex-col items-center gap-2">
            <span className="text-[13px] leading-[1.2] text-tinta-suave tabular-nums">{numero(dia.peticiones)}</span>
            <div className="w-full rounded-base bg-principal" style={{ height: `${dia.peticiones / maximo * 120}px` }} />
            <span className="text-[13px] leading-[1.2] text-tinta-suave">{etiqueta}</span>
          </div>;
        })}
      </div>
    </Tarjeta>
    <Tarjeta className="mt-5 pb-2">
      <h2 className="mb-5 font-display text-[21px] leading-[1.3]">Consumo y latencia por ruta</h2>
      <Tabla encabezados={['Ruta expuesta', 'Peticiones', '% del total', 'Latencia total p95', 'Latencia de la compuerta p95']} filas={datos.rutas.map(r => [
        <span key="ruta"><span className="mr-3 text-[11px] font-semibold text-tinta-suave">{r.metodo}</span><span className="font-medium">{r.patron}</span></span>,
        numero(r.peticiones), `${r.porcentaje.toFixed(1)} %`, latencia(r.latenciaTotalP95), latencia(r.latenciaCompuertaP95),
      ])} />
    </Tarjeta>
    <Tarjeta className="mt-5 pb-2">
      <h2 className="mb-5 font-display text-[21px] leading-[1.3]">Errores</h2>
      <div className="overflow-x-auto"><table className="w-full border-collapse text-left">
        <thead><tr>{['Código', 'Motivo', 'Peticiones', '% del total'].map(t => <th key={t} className="border-b border-borde pr-4 pb-[10px] text-encabezado uppercase font-semibold text-tinta-suave">{t}</th>)}</tr></thead>
        <tbody>{gruposErrores.map(grupo => <GrupoErrores key={grupo.titulo} {...grupo} />)}</tbody>
      </table></div>
    </Tarjeta>
  </>;
}

function GrupoErrores({ titulo, errores }: { titulo: string; errores: Datos['erroresOrigen'] }) {
  return <><tr><th colSpan={4} scope="rowgroup" className="border-b border-borde-fila pt-5 pb-2 text-[11px] tracking-[.12em] uppercase font-semibold text-tinta-suave">{titulo}</th></tr>
    {errores.map(error => <tr key={error.codigo} className="border-b border-borde-fila text-[15px] tabular-nums"><td className="py-3 pr-4 font-medium">{error.codigo}</td><td className="py-3 pr-4">{error.motivo}</td><td className="py-3 pr-4">{numero(error.peticiones)}</td><td className="py-3">{error.porcentaje.toFixed(2)} %</td></tr>)}
  </>;
}
