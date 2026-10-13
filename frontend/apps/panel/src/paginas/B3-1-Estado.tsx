import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/sistema';
import { EstadoCargando, EstadoError } from '@shapi/ui';

const cliente = crearCliente<paths>(window.location.origin);
type Componente = components['schemas']['EstadoComponente'];
const catalogo: Record<Componente['id'], { nombre: string; descripcion: string }> = {
  compuerta: { nombre: 'Compuerta de APIs', descripcion: 'Valida la clave, aplica límites y cuotas y traslada la petición al proveedor.' },
  api_control: { nombre: 'API de control', descripcion: 'Sirve el panel, los portales de marca blanca y la administración.' },
  trabajador: { nombre: 'Trabajador en segundo plano', descripcion: 'Consolida el consumo, renueva suscripciones y envía los correos.' },
  redis: { nombre: 'Caché (Redis)', descripcion: 'Claves, suscripciones y contadores que consulta la compuerta.' },
  postgres: { nombre: 'Base de datos (PostgreSQL)', descripcion: 'Organizaciones, planes, pagos y consumo consolidado.' },
  pasarela: { nombre: 'Pasarela de pagos simulada', descripcion: 'Autoriza los cobros de las suscripciones de plataforma y de las APIs.' },
  correo: { nombre: 'Envío de correo', descripcion: 'Entrega los correos de verificación, invitación y avisos de cobro.' },
};
const etiquetas = { en_servicio: 'En servicio', degradado: 'Degradado', fuera_de_servicio: 'Fuera de servicio' };
const meses = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];
function fechaHora(valor: string) {
  const partes = new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Guatemala', year: 'numeric', month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).formatToParts(new Date(valor));
  const obtener = (tipo: Intl.DateTimeFormatPartTypes) => partes.find(p => p.type === tipo)!.value;
  return { fecha: `${Number(obtener('day'))} de ${meses[Number(obtener('month')) - 1]} de ${obtener('year')}`, hora: `${obtener('hour')}:${obtener('minute')}` };
}
function Aviso({ componente, compuertaSana }: { componente: Componente; compuertaSana: boolean }) {
  const correo = componente.id === 'correo';
  const desde = componente.incidenciaDesde ? fechaHora(componente.incidenciaDesde) : null;
  return <div role="alert" className="mt-8 flex items-start gap-4 rounded-base border border-[#EDC3B4] bg-[#FBE9E3] p-5">
    <svg aria-hidden="true" className="mt-[2px] shrink-0" width="24" height="24" viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="11" stroke="#C2481F" strokeWidth="1.5" /><path d="M12 6.5 V13 M12 17 V17" stroke="#C2481F" strokeWidth="2" strokeLinecap="round" /></svg>
    <div className="flex flex-col gap-2">
      <h2 className="font-display text-[21px] leading-[1.3] text-[#8E3315]">{correo ? 'El envío de correo' : catalogo[componente.id].nombre} está {componente.estado === 'degradado' ? 'degradado' : 'fuera de servicio'}</h2>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">{correo ? <>{desde ? `Desde las ${desde.hora} del ${desde.fecha} los` : 'Los'} correos de verificación de cuenta y de invitación al equipo pueden tardar en llegar.{compuertaSana && ' La compuerta no está afectada: las APIs de los proveedores siguen respondiendo con normalidad.'}</> : 'El componente requiere atención. El estado se volverá a comprobar automáticamente.'}</p>
    </div>
  </div>;
}

export default function PaginaB31Estado() {
  const consulta = useQuery({
    queryKey: ['estado-componentes'],
    queryFn: async ({ signal }) => {
      const { data } = await cliente.GET('/api/admin/estado', { signal });
      if (!data) throw new Error('No se pudo consultar el estado de los componentes');
      return data;
    },
    refetchInterval: 30_000,
    retry: false,
  });
  const datos = consulta.data;
  const comprobacion = datos ? fechaHora(datos.comprobadoEn) : null;
  const incidencias = datos?.componentes.filter(c => c.estado !== 'en_servicio') ?? [];
  const compuertaSana = datos?.componentes.find(c => c.id === 'compuerta')?.estado === 'en_servicio';
  const redisCaido = datos?.componentes.find(c => c.id === 'redis')?.estado === 'fuera_de_servicio';
  return <main>
    <header className="flex flex-col gap-[10px]">
      <p className="text-etiqueta font-medium uppercase tracking-[0.16em] text-tinta-suave">Sistema</p>
      <h1 className="font-display text-[32px] leading-[1.2]">Estado de los componentes</h1>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">Estado de cada componente de la plataforma.{comprobacion && ` Comprobado el ${comprobacion.fecha} a las ${comprobacion.hora}.`}</p>
    </header>
    {consulta.isPending && <div className="mt-8"><EstadoCargando /></div>}
    {consulta.isError && <div className="mt-8"><EstadoError reintentar={() => void consulta.refetch()} /></div>}
    {datos && <>
      {incidencias.map(c => <Aviso key={c.id} componente={c} compuertaSana={compuertaSana} />)}
      <section aria-label="Latencia de la compuerta" className={`${incidencias.length ? 'mt-5' : 'mt-8'} flex items-center justify-between gap-8 rounded-base border border-borde bg-panel p-5`}>
        <div className="flex shrink-0 flex-col gap-[10px]">
          <p className="text-[13px] font-semibold leading-[1.4] text-tinta-suave">Latencia añadida por la compuerta · percentil 95</p>
          <span className="font-display text-[32px] leading-none tracking-[-0.02em] tabular-nums">{datos.latenciaSupera2500 ? '> 2500 ms' : datos.latenciaP95Ms === null ? '—' : `${Math.round(datos.latenciaP95Ms)} ms`}</span>
          {datos.latenciaP95Ms === null && !datos.latenciaSupera2500 && <p className="text-[13px] text-tinta-suave">{redisCaido ? 'Latencia no disponible' : 'Sin peticiones en la última hora'}</p>}
        </div>
        <p className="max-w-[560px] text-[15px] leading-[1.55] text-tinta-suave">Es lo que la compuerta suma a cada petición, sin contar lo que tarda el servidor del proveedor. Es la misma cifra que el proveedor ve en el consumo de su API.</p>
      </section>
      <section aria-label="Componentes" className="mt-5 rounded-base border border-borde bg-panel px-5 pb-2 pt-5">
        <h2 className="font-display text-[21px] leading-[1.3]">Componentes</h2>
        <div className="overflow-auto"><table className="mt-5 w-full border-collapse text-left">
          <thead><tr>{['Componente', 'Qué hace', 'Estado'].map((texto, i) => <th key={texto} className={`${i === 0 ? 'w-[260px]' : i === 2 ? 'w-[150px]' : ''} border-b border-borde pb-[10px] pr-4 text-encabezado uppercase text-tinta-suave last:pr-0`}>{texto}</th>)}</tr></thead>
          <tbody>{datos.componentes.map(c => <tr key={c.id} className="border-b border-borde-fila last:border-b-0">
            <td className="py-3 pr-4 text-[15px] font-medium leading-[1.4]">{catalogo[c.id].nombre}</td>
            <td className="py-3 pr-4 text-[15px] text-[#2B3547]">{catalogo[c.id].descripcion}</td>
            <td className="py-3"><span className={`inline-block whitespace-nowrap rounded-base border px-[10px] py-[5px] text-[12px] font-semibold uppercase leading-[1.2] tracking-[0.06em] ${c.estado === 'en_servicio' ? 'border-[#B6DCC9] bg-[#E4F3EC] text-[#146542]' : 'border-[#EDC3B4] bg-[#FBE9E3] text-[#8E3315]'}`}>{etiquetas[c.estado]}</span></td>
          </tr>)}</tbody>
        </table></div>
      </section>
    </>}
  </main>;
}
