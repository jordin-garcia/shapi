import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/sistema';
import { Boton, EstadoCargando, EstadoError } from '@shapi/ui';

const cliente = crearCliente<paths>(window.location.origin);
const meses = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];
const mesesCortos = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
const zonaGuatemala = 'America/Guatemala';

type Entrada = components['schemas']['EntradaBitacora'];

function partesGuatemala(fecha: Date) {
  const partes = new Intl.DateTimeFormat('en-CA', {
    timeZone: zonaGuatemala, year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
  }).formatToParts(fecha);
  const valor = (tipo: Intl.DateTimeFormatPartTypes) => partes.find(parte => parte.type === tipo)!.value;
  return { ano: Number(valor('year')), mes: Number(valor('month')), dia: Number(valor('day')), hora: valor('hour'), minuto: valor('minute') };
}

function hoyGuatemala() {
  const { ano, mes, dia } = partesGuatemala(new Date());
  return `${ano}-${String(mes).padStart(2, '0')}-${String(dia).padStart(2, '0')}`;
}

function sumarDias(fecha: string, dias: number) {
  const actual = new Date(`${fecha}T12:00:00Z`);
  actual.setUTCDate(actual.getUTCDate() + dias);
  return actual.toISOString().slice(0, 10);
}

function leerDia(fecha: string) {
  const [ano, mes, dia] = fecha.split('-').map(Number);
  return { ano, mes, dia };
}

function etiquetaPeriodo(desde: string, hasta: string) {
  const inicio = leerDia(desde);
  const fin = leerDia(hasta);
  if (inicio.ano === fin.ano && inicio.mes === fin.mes) {
    return `Del ${inicio.dia} al ${fin.dia} de ${meses[fin.mes - 1]} de ${fin.ano}`;
  }
  return `Del ${inicio.dia} de ${meses[inicio.mes - 1]} de ${inicio.ano} al ${fin.dia} de ${meses[fin.mes - 1]} de ${fin.ano}`;
}

function fechaYHora(valor: string) {
  const fecha = partesGuatemala(new Date(valor));
  return { fecha: `${fecha.dia} ${mesesCortos[fecha.mes - 1]} ${fecha.ano}`, hora: `${fecha.hora}:${fecha.minuto}` };
}

function tituloRol(rol: Entrada['actor']['rol']) {
  return rol.charAt(0).toUpperCase() + rol.slice(1);
}

function EstadoVacio() {
  return <div className="flex flex-col items-center gap-6 py-16 px-4 text-center">
    <svg aria-hidden="true" width="40" height="40" viewBox="0 0 40 40" fill="none">
      <rect x="1" y="1" width="38" height="38" rx="8" stroke="var(--borde-campo)" strokeWidth="1.5" strokeDasharray="4 4" />
      <path d="M13 15 H27 M13 20 H27 M13 25 H21" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
    <div className="flex max-w-[560px] flex-col gap-[10px]">
      <h2 className="font-display text-[22px] leading-[1.3]">Todavía no hay acciones registradas</h2>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">En este periodo nadie suspendió una organización, revirtió un cobro, cambió una cuenta de plataforma ni rotó una clave. Cuando ocurra, aquí aparecerá quién lo hizo, qué hizo y cuándo.</p>
    </div>
  </div>;
}

export default function PaginaB32Bitacora() {
  const hastaInicial = hoyGuatemala();
  const desdeInicial = sumarDias(hastaInicial, -6);
  const [periodo, setPeriodo] = useState({ desde: desdeInicial, hasta: hastaInicial });
  const [borrador, setBorrador] = useState(periodo);
  const [selectorAbierto, setSelectorAbierto] = useState(false);
  const consulta = useQuery({
    queryKey: ['bitacora', periodo.desde, periodo.hasta],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/admin/bitacora', {
        signal,
        params: { query: { desde: periodo.desde, hasta: periodo.hasta, pagina: 1, tamano: 20 } },
      });
      if (!response.ok || !data) throw new Error('No se pudo consultar la bitácora');
      return data;
    },
    retry: false,
  });

  function aplicarPeriodo() {
    if (!borrador.desde || !borrador.hasta || borrador.desde > borrador.hasta) return;
    setPeriodo(borrador);
    setSelectorAbierto(false);
  }

  return <main>
    <header className="flex items-end justify-between gap-6">
      <div className="flex flex-col gap-[10px]">
        <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">Sistema</p>
        <h1 className="font-display text-[32px] leading-[1.2]">Bitácora de acciones sensibles</h1>
        <p className="text-[15px] leading-[1.55] text-tinta-suave">Quedan registradas las acciones que cambian el acceso, el cobro o el estado de una organización.</p>
      </div>

      <div className="relative w-[300px] shrink-0">
        <button type="button" aria-label="Seleccionar periodo" aria-expanded={selectorAbierto} onClick={() => setSelectorAbierto(abierto => !abierto)}
          className="flex w-full items-center justify-between gap-3 rounded-base border border-borde-campo bg-panel px-[14px] py-[11px] text-left text-[15px] leading-[1.5] tabular-nums">
          <span>{etiquetaPeriodo(periodo.desde, periodo.hasta)}</span>
          <svg aria-hidden="true" width="16" height="16" viewBox="0 0 16 16" fill="none" className="shrink-0 text-tinta-suave"><path d="M4 6 L8 10 L12 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
        </button>
        {selectorAbierto && <div className="absolute right-0 z-10 mt-2 w-[360px] rounded-base border border-borde bg-panel p-4 shadow-lg">
          <div className="grid grid-cols-2 gap-3">
            <label className="flex flex-col gap-[6px] text-[13px] font-semibold">Desde
              <input aria-label="Desde" type="date" value={borrador.desde} max={borrador.hasta} onChange={evento => setBorrador(actual => ({ ...actual, desde: evento.target.value }))} className="h-12 rounded-base border border-borde-campo px-3 text-[15px]" />
            </label>
            <label className="flex flex-col gap-[6px] text-[13px] font-semibold">Hasta
              <input aria-label="Hasta" type="date" value={borrador.hasta} min={borrador.desde} onChange={evento => setBorrador(actual => ({ ...actual, hasta: evento.target.value }))} className="h-12 rounded-base border border-borde-campo px-3 text-[15px]" />
            </label>
          </div>
          <div className="mt-4 flex justify-end"><Boton type="button" onClick={aplicarPeriodo}>Aplicar</Boton></div>
        </div>}
      </div>
    </header>

    <section className="mt-8 rounded-base border border-borde bg-panel p-5" aria-label="Acciones sensibles">
      {consulta.isPending && <EstadoCargando />}
      {consulta.isError && <EstadoError reintentar={() => void consulta.refetch()} />}
      {consulta.data && <>
        <div className="w-full overflow-auto">
          <table className="w-full border-collapse text-left">
            <thead><tr>
              <th className="w-[150px] border-b border-borde pb-[10px] pr-4 text-encabezado uppercase text-tinta-suave">Fecha</th>
              <th className="w-[300px] border-b border-borde pb-[10px] pr-4 text-encabezado uppercase text-tinta-suave">Usuario</th>
              <th className="border-b border-borde pb-[10px] text-encabezado uppercase text-tinta-suave">Acción</th>
            </tr></thead>
            {consulta.data.elementos.length > 0 && <tbody>{consulta.data.elementos.map((entrada, indice) => {
              const fecha = fechaYHora(entrada.fecha);
              return <tr key={`${entrada.fecha}-${entrada.accion}-${indice}`} className="border-b border-borde-fila last:border-b-0">
                <td className="py-3 pr-4 align-middle tabular-nums"><div className="flex flex-col gap-[2px]"><span className="text-[15px] font-medium leading-[1.4]">{fecha.fecha}</span><span className="text-[14px] leading-[1.4] text-tinta-suave">{fecha.hora}</span></div></td>
                <td className="py-3 pr-4 align-middle"><div className="flex flex-col gap-[2px]"><span className="text-[15px] font-medium leading-[1.4]">{entrada.actor.nombre}</span><span className="text-[14px] leading-[1.4] text-tinta-suave">{tituloRol(entrada.actor.rol)}{entrada.actor.organizacion ? ` · ${entrada.actor.organizacion}` : ''}</span></div></td>
                <td className="py-3 align-middle text-[15px] text-[#2B3547]">{entrada.descripcion}</td>
              </tr>;
            })}</tbody>}
          </table>
        </div>
        {consulta.data.elementos.length === 0 && <EstadoVacio />}
      </>}
    </section>
  </main>;
}
