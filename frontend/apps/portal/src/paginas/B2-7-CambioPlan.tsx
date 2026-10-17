import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState, type ChangeEvent } from 'react';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/suscripciones';
import { Aviso, Boton, Campo, EstadoCargando, EstadoError } from '@shapi/ui';
import { Link, useNavigate, useParams } from 'react-router';
import { claveSuscripcionConsumidor, useSuscripcionConsumidor, type SuscripcionActual, type Tarjeta } from '../modulos/claves/claves';
import { dinero, numero, usePlanesPortal, type PlanApi } from '../modulos/suscripcion/planes';
import ClaveNueva from './B2-5-ClaveNueva';

const cliente = crearCliente<paths>(window.location.origin);
const DIA = 86_400_000;
const meses = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
const mesesLargos = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];

function redondear(valor: number) { return Math.round((valor + Number.EPSILON) * 100) / 100; }
function fecha(valor: string | Date) {
  const partes = new Intl.DateTimeFormat('en-CA', {
    day: 'numeric', month: 'numeric', year: 'numeric', timeZone: 'America/Guatemala',
  }).formatToParts(typeof valor === 'string' ? new Date(valor) : valor);
  const numeroParte = (tipo: Intl.DateTimeFormatPartTypes) => Number(partes.find(parte => parte.type === tipo)!.value);
  return `${numeroParte('day')} ${meses[numeroParte('month') - 1]} ${numeroParte('year')}`;
}
function fechaLarga(valor: string | Date) {
  const partes = new Intl.DateTimeFormat('en-CA', {
    day: 'numeric', month: 'numeric', year: 'numeric', timeZone: 'America/Guatemala',
  }).formatToParts(typeof valor === 'string' ? new Date(valor) : valor);
  const numeroParte = (tipo: Intl.DateTimeFormatPartTypes) => Number(partes.find(parte => parte.type === tipo)!.value);
  return `${numeroParte('day')} de ${mesesLargos[numeroParte('month') - 1]} de ${numeroParte('year')}`;
}

function TarjetaPlan({ plan, tipo, cuota, limite }: { plan: Pick<PlanApi, 'id' | 'nombre' | 'precio'> & Partial<Pick<PlanApi, 'vigenciaDias'>>; tipo: string; cuota: number; limite: number }) {
  return <article className={`rounded-base border bg-panel p-5 ${tipo === 'Plan destino' ? 'border-[var(--marca-principal)]' : 'border-borde'}`}>
    <div className="flex items-center justify-between gap-3"><h2 className="font-display text-[22px] leading-[1.3]">{plan.nombre}</h2><span className="eti-n">{tipo}</span></div>
    <p className="mt-4 font-display text-[32px] leading-none tabular-nums">{dinero(plan.precio)}</p>
    <p className="mt-3 text-[14px] text-tinta-suave">Vigencia de {plan.vigenciaDias} días</p>
    <div className="mt-4 flex flex-col gap-2 border-t border-borde-fila pt-4 text-[14.5px] text-[#2B3547]"><span>{numero(cuota)} llamadas al mes</span><span>{numero(limite)} peticiones por minuto</span></div>
  </article>;
}

export default function CambioPlan() {
  const { plan: planId } = useParams();
  const navegar = useNavigate();
  const cache = useQueryClient();
  const planes = usePlanesPortal();
  const suscripcion = useSuscripcionConsumidor();
  const [error, setError] = useState<string>();
  const [tarjeta, setTarjeta] = useState<Tarjeta>({ numero: '', titular: '', mesVencimiento: '', anioVencimiento: '', cvv: '' });
  const [clavesNuevas, setClavesNuevas] = useState<components['schemas']['ClaveEmitida'][]>([]);
  const [ahora] = useState(() => Date.now());
  const destino = planes.data?.find(plan => plan.id === planId);
  const actual = suscripcion.data;
  const inicio = actual ? new Date(actual.periodo.inicio) : new Date();
  const renovacion = actual ? new Date(actual.proximaRenovacion) : new Date();
  const vigenciaActual = Math.max(1, Math.round((renovacion.getTime() - inicio.getTime()) / DIA));
  const dias = actual ? Math.max(0, Math.ceil((renovacion.getTime() - ahora) / DIA)) : 0;
  const desdeGratuito = Boolean(actual && actual.plan.precio <= 0);
  const bajada = Boolean(actual && destino && destino.precio / destino.vigenciaDias <= actual.plan.precio / vigenciaActual);
  const credito = actual && !desdeGratuito ? redondear(actual.plan.precio * dias / vigenciaActual) : 0;
  const cargo = actual && destino
    ? (desdeGratuito || destino.vigenciaDias !== vigenciaActual ? destino.precio : redondear(destino.precio * dias / destino.vigenciaDias))
    : 0;
  const aPagar = Math.max(cargo - credito, 0);
  const requiereTarjeta = !bajada && aPagar > 0 && !actual?.tarjetaEnmascarada;

  const cambio = useMutation({
    mutationFn: async () => {
      if (!destino) throw new Error('No se encontró el plan de destino.');
      const { data, response } = await cliente.POST('/api/portal/suscripcion/cambiar', {
        body: requiereTarjeta ? { planId: destino.id, tarjeta } : { planId: destino.id },
      });
      if (!response.ok || !data) throw new Error('No se pudo cambiar el plan.');
      return data;
    },
    onSuccess: async resultado => {
      await Promise.all([
        cache.invalidateQueries({ queryKey: claveSuscripcionConsumidor }),
        cache.invalidateQueries({ queryKey: ['portal', 'pagos'] }),
      ]);
      if ('claves' in resultado && resultado.claves.length > 0) {
        setClavesNuevas(resultado.claves);
        return;
      }
      await navegar('/cuenta/suscripcion');
    },
    onError: causa => setError(causa instanceof ErrorApi ? causa.titulo : 'No se pudo cambiar el plan.'),
  });

  if (planes.isPending || suscripcion.isPending) return <EstadoCargando />;
  if (planes.isError || suscripcion.isError) return <EstadoError mensaje="No se pudo cargar la información del cambio de plan." reintentar={() => { void planes.refetch(); void suscripcion.refetch(); }} />;
  if (!actual) return <Aviso estado="error">No tiene una suscripción vigente para cambiar de plan.</Aviso>;
  if (clavesNuevas[0]) return <ClaveNueva key={clavesNuevas[0].id} clave={clavesNuevas[0]} terminar={() => {
    if (clavesNuevas.length > 1) setClavesNuevas(clavesNuevas.slice(1));
    else void navegar('/cuenta/suscripcion');
  }} />;
  if (planId === actual.plan.id) return <SeleccionDestino actual={actual} planes={planes.data} elegir={id => void navegar(`/cuenta/cambiar-plan/${id}`)} />;
  if (!destino) return <Aviso estado="error">No se encontró el plan de destino.</Aviso>;

  const origen = { ...actual.plan, vigenciaDias: vigenciaActual };
  const tarjetaCompleta = Object.values(tarjeta).every(valor => valor.trim());
  const cambiarTarjeta = (campo: keyof Tarjeta) => (evento: ChangeEvent<HTMLInputElement>) => setTarjeta(valor => ({ ...valor, [campo]: evento.target.value }));

  return <section>
    <header>
      <p className="text-etiqueta uppercase tracking-[.14em] text-[var(--marca-principal)]">Mi cuenta</p>
      <h1 className="mt-2 font-display text-[32px] leading-[1.2]">Cambio de plan</h1>
      <p className="mt-2 max-w-[1050px] text-[15px] leading-[1.55] text-tinta-suave">Al subir de plan se cobra la diferencia por los días que restan del ciclo y los nuevos límites aplican de inmediato. Si elige un plan menor, el cambio se aplica en su próxima renovación.</p>
    </header>
    {error && <div className="mt-5"><Aviso estado="error">{error}</Aviso></div>}
    <div className="mt-8 grid grid-cols-1 items-start gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_400px]">
      <TarjetaPlan plan={origen} tipo="Plan actual" cuota={actual.cuotaLlamadas} limite={actual.limiteMinuto} />
      <TarjetaPlan plan={destino} tipo="Plan destino" cuota={destino.cuotaLlamadas} limite={destino.limiteMinuto} />
      {bajada ? <section className="rounded-base border border-borde bg-panel p-5">
          <h2 className="font-display text-[21px]">Cambio programado</h2>
          <p className="mt-4 text-[15px] leading-[1.55] text-tinta-suave">El plan {destino.nombre} se aplicará en su próxima renovación, el {fecha(actual.proximaRenovacion)}. No se cobrará ni se reembolsará nada hoy.</p>
          <Boton className="mt-6 w-full" onClick={() => { setError(undefined); cambio.mutate(); }} deshabilitado={cambio.isPending}>{cambio.isPending ? 'Procesando…' : 'Programar cambio de plan'}</Boton>
        </section> : <section className="rounded-base border border-borde bg-panel p-5">
          <h2 className="font-display text-[21px]">Diferencia prorrateada</h2>
          <dl className="mt-5 flex flex-col gap-3.5 border-t border-borde-fila pt-5 text-[14px]">
            <Fila nombre="Ciclo en curso" valor={`${fecha(actual.periodo.inicio)} – ${fecha(actual.periodo.fin)}`} />
            <Fila nombre="Días restantes" valor={`${dias} de ${vigenciaActual}`} />
            <Fila nombre={`Crédito por ${dias} días de ${actual.plan.nombre}`} valor={`− ${dinero(credito)}`} />
            <Fila nombre={!desdeGratuito && destino.vigenciaDias === vigenciaActual ? `Cargo por ${dias} días de ${destino.nombre}` : `Cargo por el ciclo completo de ${destino.nombre}`} valor={dinero(cargo)} />
            <div className="mt-1 flex items-baseline justify-between gap-6 border-t border-borde-fila pt-5"><dt className="font-semibold text-tinta">A pagar hoy</dt><dd className="font-display text-[26px] tabular-nums">{dinero(aPagar)}</dd></div>
            <Fila nombre="Tarjeta registrada" valor={actual.tarjetaEnmascarada ?? 'Sin tarjeta registrada'} />
          </dl>
          <p className="mt-4 text-[14px] leading-[1.5] text-tinta-suave">{!desdeGratuito && destino.vigenciaDias === vigenciaActual
            ? `Desde el ${fechaLarga(actual.proximaRenovacion)} el plan ${destino.nombre} se cobra completo: ${dinero(destino.precio)} cada ${destino.vigenciaDias} días.`
            : `El plan ${destino.nombre} inicia un ciclo nuevo hoy y después se cobra ${dinero(destino.precio)} cada ${destino.vigenciaDias} días.`}</p>
          {requiereTarjeta && <div className="mt-5 space-y-4 border-t border-borde-fila pt-5">
            <Campo etiqueta="Número de tarjeta" value={tarjeta.numero} onChange={cambiarTarjeta('numero')} autoComplete="cc-number" />
            <Campo etiqueta="Nombre del titular" value={tarjeta.titular} onChange={cambiarTarjeta('titular')} autoComplete="cc-name" />
            <div className="grid grid-cols-2 gap-3"><Campo etiqueta="Mes" value={tarjeta.mesVencimiento} onChange={cambiarTarjeta('mesVencimiento')} autoComplete="cc-exp-month" /><Campo etiqueta="Año" value={tarjeta.anioVencimiento} onChange={cambiarTarjeta('anioVencimiento')} autoComplete="cc-exp-year" /></div>
            <Campo etiqueta="Código de seguridad" value={tarjeta.cvv} onChange={cambiarTarjeta('cvv')} autoComplete="cc-csc" />
          </div>}
          <Boton className="mt-5 w-full" onClick={() => { setError(undefined); cambio.mutate(); }} deshabilitado={cambio.isPending || (requiereTarjeta && !tarjetaCompleta)}>{cambio.isPending ? 'Procesando…' : `Pagar ${dinero(aPagar)} y cambiar de plan`}</Boton>
        </section>}
    </div>
    <Link className="mt-5 inline-block text-[14px] font-medium text-[var(--marca-principal)]" to={`/cuenta/cambiar-plan/${actual.plan.id}`}>Elegir otro plan</Link>
  </section>;
}

function Fila({ nombre, valor }: { nombre: string; valor: string }) {
  return <div className="flex items-baseline justify-between gap-6"><dt className="text-tinta-suave">{nombre}</dt><dd className="text-right font-medium tabular-nums text-tinta">{valor}</dd></div>;
}

function SeleccionDestino({ actual, planes, elegir }: {
  actual: SuscripcionActual;
  planes: PlanApi[];
  elegir: (id: string) => void;
}) {
  const inicio = new Date(actual.periodo.inicio);
  const renovacion = new Date(actual.proximaRenovacion);
  const vigencia = Math.max(1, Math.round((renovacion.getTime() - inicio.getTime()) / DIA));
  const disponibles = planes.filter(plan => plan.id !== actual.plan.id);
  return <section>
    <header><p className="text-etiqueta uppercase tracking-[.14em] text-[var(--marca-principal)]">Mi cuenta</p><h1 className="mt-2 font-display text-[32px] leading-[1.2]">Cambio de plan</h1><p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">Elija el plan al que desea cambiar su suscripción.</p></header>
    <div className="mt-8 grid grid-cols-1 items-start gap-5 md:grid-cols-2 xl:grid-cols-3">
      <TarjetaPlan plan={{ ...actual.plan, vigenciaDias: vigencia }} tipo="Plan actual" cuota={actual.cuotaLlamadas} limite={actual.limiteMinuto} />
      {disponibles.map(plan => <article key={plan.id} className="rounded-base border border-borde bg-panel p-5"><h2 className="font-display text-[22px]">{plan.nombre}</h2><p className="mt-3 text-[14px] text-tinta-suave">{plan.descripcion}</p><p className="mt-5 font-display text-[28px]">{dinero(plan.precio)}</p><p className="text-[14px] text-tinta-suave">Vigencia de {plan.vigenciaDias} días</p><div className="mt-4 space-y-2 border-t border-borde-fila pt-4 text-[14px]"><p>{numero(plan.cuotaLlamadas)} llamadas al mes</p><p>{numero(plan.limiteMinuto)} peticiones por minuto</p></div><Boton className="mt-5 w-full" onClick={() => elegir(plan.id)}>Elegir {plan.nombre}</Boton></article>)}
    </div>
    {disponibles.length === 0 && <div className="mt-8"><Aviso estado="neutro">No hay otro plan disponible en este momento.</Aviso></div>}
  </section>;
}
