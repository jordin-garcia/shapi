import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { paths } from '@shapi/api/suscripciones';
import type { components as componentesPlanes, paths as pathsPlanes } from '@shapi/api/planes';
import { Aviso, Boton, Campo, EstadoCargando, EstadoError } from '@shapi/ui';
import { Link, useParams } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
const clientePlanes = crearCliente<pathsPlanes>(window.location.origin);
type Plan = componentesPlanes['schemas']['PlanPlataforma'];
const dinero = (monto: number) => 'Q ' + monto.toLocaleString('es-GT', { minimumFractionDigits: 2 });
const fecha = (value: string) => new Intl.DateTimeFormat('es-GT', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(new Date(value));

export default function PaginaA25CambioPlan() {
  const { plan: planId } = useParams();
  const cache = useQueryClient();
  const [error, setError] = useState('');
  const [usarRegistrada, setUsarRegistrada] = useState(true);
  const [numero, setNumero] = useState('');
  const [mes, setMes] = useState('');
  const [anio, setAnio] = useState('');
  const [cvv, setCvv] = useState('');
  const [titular, setTitular] = useState('');
  const planes = useQuery({
    queryKey: ['planes-plataforma'],
    queryFn: async ({ signal }) => {
      const { data, response } = await clientePlanes.GET('/api/planes-plataforma', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron cargar los planes.');
      return data;
    },
    retry: false,
  });
  const suscripcion = useQuery({
    queryKey: ['suscripcion-plataforma'],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/suscripcion', { signal });
      if (!response.ok || !data) throw new Error('No se pudo cargar la suscripción.');
      return data;
    },
    retry: false,
  });
  const destino = planes.data?.find((p: Plan) => p.id === planId);
  const actual = suscripcion.data;
  const origen = planes.data?.find(p => p.id === actual?.plan.id);
  const dias = actual?.diasRestantesCiclo ?? 0;
  const credito = origen ? Math.round((origen.precio * dias / origen.vigenciaDias + Number.EPSILON) * 100) / 100 : 0;
  const cargo = destino && origen ? (destino.vigenciaDias === origen.vigenciaDias
    ? Math.round((destino.precio * dias / destino.vigenciaDias + Number.EPSILON) * 100) / 100 : destino.precio) : 0;
  const aPagar = Math.max(cargo - credito, 0);
  const cambio = useMutation({
    mutationFn: async () => {
      if (!destino) throw new Error('No se encontró el plan.');
      const { data, response } = await cliente.POST('/api/suscripcion/cambiar', {
        body: usarRegistrada && actual?.tarjetaEnmascarada
          ? { planId: destino.id, usarRegistrada: true }
          : { planId: destino.id, usarRegistrada: false, tarjeta: { numero, mesVencimiento: mes, anioVencimiento: anio, cvv, titular } },
      });
      if (!response.ok || !data) throw new Error('No se pudo cambiar el plan.');
      return data;
    },
    onSuccess: async () => { await cache.invalidateQueries({ queryKey: ['suscripcion-plataforma'] }); },
    onError: fallo => setError(fallo instanceof ErrorApi ? fallo.titulo : 'No se pudo cambiar el plan.'),
  });
  if (planes.isPending || suscripcion.isPending) return <EstadoCargando />;
  if (planes.isError || suscripcion.isError) return <EstadoError mensaje="No se pudo cargar la suscripción." reintentar={() => { void planes.refetch(); void suscripcion.refetch(); }} />;
  if (!destino || !origen || !actual) return <Aviso estado="error">No se encontró el plan o la suscripción de plataforma.</Aviso>;
  const bajada = destino.precio / destino.vigenciaDias <= origen.precio / origen.vigenciaDias;
  return <main className="mx-auto max-w-[920px]">
    <h1 className="font-display text-[32px]">Cambio de plan</h1>
    <p className="mt-3 text-[15px] leading-relaxed text-tinta-suave">El cambio se cobra prorrateado por los días que restan del ciclo en curso.</p>
    {error && <div className="mt-5"><Aviso estado="error">{error}</Aviso></div>}
    {bajada ? <section className="mt-8 rounded-base border border-borde bg-panel p-6">
      <h2 className="font-display text-[22px]">Cambio programado</h2>
      <p className="mt-3">El plan {destino.nombre} empezará el {fecha(actual.proximaRenovacion)}. No se reembolsa el periodo restante.</p>
      <Boton className="mt-6" onClick={() => cambio.mutate()} deshabilitado={cambio.isPending}>Programar cambio de plan</Boton>
    </section> : <section className="mt-8 grid gap-6 lg:grid-cols-2">
      <div className="rounded-base border border-borde bg-panel p-6">
        <h2 className="font-display text-[22px]">{destino.nombre}</h2>
        <p className="mt-2 text-sm text-tinta-suave">{destino.descripcion}</p>
        <p className="mt-5 text-[28px]">{dinero(destino.precio)}</p>
        <p className="text-sm text-tinta-suave">{destino.vigenciaDias} días</p>
      </div>
      <div className="rounded-base border border-borde bg-panel p-6">
        <h2 className="font-display text-[21px]">Diferencia prorrateada</h2>
        <dl className="mt-5 space-y-4 border-t border-borde pt-5 text-sm">
          <div className="flex justify-between gap-4"><dt>Ciclo en curso</dt><dd>{fecha(actual.periodo.inicio || actual.proximaRenovacion)} – {fecha(actual.periodo.fin || actual.proximaRenovacion)}</dd></div>
          <div className="flex justify-between"><dt>Días restantes</dt><dd>{dias} de {origen.vigenciaDias}</dd></div>
          <div className="flex justify-between"><dt>Crédito por {dias} días no usados de {origen.nombre}</dt><dd className="text-[#1F8A5B]">− {dinero(credito)}</dd></div>
          <div className="flex justify-between"><dt>Cargo por {dias} días de {destino.nombre}</dt><dd>{dinero(cargo)}</dd></div>
          <div className="flex justify-between border-t border-borde pt-4 text-base"><dt>A pagar hoy</dt><dd className="font-display text-[25px]">{dinero(aPagar)}</dd></div>
        <div className="flex justify-between"><dt>Tarjeta registrada</dt><dd>{actual.tarjetaEnmascarada || 'No hay tarjeta registrada'}</dd></div>
        </dl>
        <p className="mt-5 text-sm leading-relaxed text-tinta-suave">La diferencia se cobra a esta tarjeta al confirmar. Desde el {fecha(actual.proximaRenovacion)} el plan {destino.nombre} se cobra completo: {dinero(destino.precio)} cada {destino.vigenciaDias} días.</p>
        {aPagar > 0 && <div className="mt-5 space-y-4">
          {actual.tarjetaEnmascarada && <fieldset className="space-y-3 text-sm">
            <legend className="mb-2 font-medium">Forma de pago</legend>
            <label className="flex items-center gap-3"><input type="radio" name="forma-pago" checked={usarRegistrada} onChange={() => setUsarRegistrada(true)} />Tarjeta registrada · {actual.tarjetaEnmascarada}</label>
            <label className="flex items-center gap-3"><input type="radio" name="forma-pago" checked={!usarRegistrada} onChange={() => setUsarRegistrada(false)} />Usar otra tarjeta</label>
          </fieldset>}
          {(!actual.tarjetaEnmascarada || !usarRegistrada) && <>
            <Campo etiqueta="Número de tarjeta" autoComplete="cc-number" value={numero} onChange={e => setNumero(e.target.value)} required />
            <div className="grid grid-cols-2 gap-4">
              <Campo etiqueta="Mes de vencimiento" autoComplete="cc-exp-month" value={mes} onChange={e => setMes(e.target.value)} required />
              <Campo etiqueta="Año de vencimiento" autoComplete="cc-exp-year" value={anio} onChange={e => setAnio(e.target.value)} required />
            </div>
            <Campo etiqueta="Código de seguridad" autoComplete="cc-csc" value={cvv} onChange={e => setCvv(e.target.value)} required />
            <Campo etiqueta="Titular de la tarjeta" autoComplete="cc-name" value={titular} onChange={e => setTitular(e.target.value)} required />
          </>}
        </div>}
        <Boton className="mt-6 w-full" onClick={() => { setError(''); cambio.mutate(); }} deshabilitado={cambio.isPending || (aPagar > 0 && (usarRegistrada ? !actual.tarjetaEnmascarada : !numero || !mes || !anio || !cvv || !titular))}>
          {cambio.isPending ? 'Procesando…' : 'Pagar ' + dinero(aPagar) + ' y cambiar de plan'}
        </Boton>
      </div>
    </section>}
    <Link className="mt-5 inline-block text-sm font-medium text-principal" to="/panel/suscripcion">Volver a la suscripción</Link>
  </main>;
}
