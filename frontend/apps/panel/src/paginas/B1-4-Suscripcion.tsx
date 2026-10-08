import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { paths } from '@shapi/api/suscripciones';
import { Aviso, Boton, Campo, EstadoCargando, EstadoError } from '@shapi/ui';
import { Link } from 'react-router';
import { useSesion } from '../modulos/sesion/useSesion';

const cliente = crearCliente<paths>(window.location.origin);
const fecha = (value: string) => new Intl.DateTimeFormat('es-GT', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'America/Guatemala' }).format(new Date(value));
const dinero = (monto: number) => 'Q ' + monto.toLocaleString('es-GT', { minimumFractionDigits: 2 });

export default function PaginaB14Suscripcion() {
  const cache = useQueryClient();
  const sesion = useSesion();
  const puedeGestionar = sesion.data?.rol === 'propietario';
  const [mostrarPago, setMostrarPago] = useState(false);
  const [numero, setNumero] = useState('');
  const [mes, setMes] = useState('');
  const [anio, setAnio] = useState('');
  const [cvv, setCvv] = useState('');
  const [titular, setTitular] = useState('');
  const [error, setError] = useState('');
  const consulta = useQuery({
    queryKey: ['suscripcion-plataforma'],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/suscripcion', { signal });
      if (!response.ok || !data) throw new Error('No se pudo cargar la suscripción.');
      return data;
    },
    retry: false,
  });
  const pagar = useMutation({
    mutationFn: async () => {
      const { data, response } = await cliente.POST('/api/suscripcion/pagar', {
        body: { usarRegistrada: false, tarjeta: { numero, mesVencimiento: mes, anioVencimiento: anio, cvv, titular } },
      });
      if (!response.ok || !data) throw new Error('No se pudo reactivar la suscripción.');
      return data;
    },
    onSuccess: async () => { setMostrarPago(false); await cache.invalidateQueries({ queryKey: ['suscripcion-plataforma'] }); },
    onError: e => setError(e instanceof ErrorApi ? e.titulo : 'No se autorizó el cobro. Revise los datos de la tarjeta.'),
  });
  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudo cargar la suscripción." reintentar={() => void consulta.refetch()} />;
  const s = consulta.data;
  const gratuito = s.plan.esPrueba || s.plan.precio === 0;
  const gracia = s.estado === 'en_gracia';
  const suspendida = s.estado === 'suspendida';
  const estado = gracia ? 'En gracia' : suspendida ? 'Suspendida' : 'Activa';
  return <main className="mx-auto max-w-[880px]">
    <h1 className="font-display text-[32px]">Suscripción de plataforma</h1>
    <p className="mt-3 text-[15px] leading-relaxed text-tinta-suave">Su plan de Shapi, su vigencia y la tarjeta con la que se renueva.</p>
    {gracia && <section className="mt-6 rounded-base border border-[#D9A42E] bg-[#FFF9E8] p-5">
      <h2 className="font-display text-[22px]">Quedan {s.diasRestantes || 0} días antes de que sus APIs dejen de responder</h2>
      <p className="mt-3 text-sm leading-relaxed">{gratuito
        ? 'Su plan gratuito o de Prueba venció. Contrate un plan de pago antes de que termine el periodo de gracia para que sus APIs sigan respondiendo.'
        : 'El cobro de la renovación fue rechazado' + (s.ultimoRechazoEn ? ' el ' + fecha(s.ultimoRechazoEn) : '') + '. Si el ' + (s.graciaHasta ? fecha(s.graciaHasta) : fecha(s.proximaRenovacion)) + ' no hay un cobro autorizado, su suscripción se suspende y la compuerta empieza a rechazar las peticiones de sus consumidores. Mientras dure el periodo de gracia sus APIs siguen respondiendo con normalidad.'}</p>
    </section>}
    {suspendida && <section className="mt-6 rounded-base border border-[#D66B62] bg-[#FFF2F0] p-5">
      <h2 className="font-display text-[22px]">Su tráfico está detenido</h2>
      <p className="mt-3 text-sm leading-relaxed">Desde el {s.graciaHasta ? fecha(s.graciaHasta) : fecha(s.proximaRenovacion)} sus APIs dejaron de responder: la compuerta rechaza con 403 las peticiones de sus consumidores. Sus claves, las suscripciones de sus consumidores y su historial se conservan. {gratuito ? 'Contrate un plan de pago para restablecer el tráfico.' : 'El tráfico se restablece en cuanto se autorice el cobro.'}</p>
    </section>}
    <section className="mt-8 rounded-base border border-borde bg-panel p-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-etiqueta uppercase tracking-[0.14em] text-tinta-suave">Plan de plataforma</p>
          <div className="mt-2 flex items-center gap-3"><h2 className="font-display text-[25px]">{s.plan.nombre}</h2><span className="rounded-full border border-borde px-3 py-1 text-xs font-medium">{estado}</span></div>
        </div>
        <div className="text-right"><p className="font-display text-[25px]">{dinero(s.plan.precio)}</p><p className="text-sm text-tinta-suave">Vigencia de {s.plan.vigenciaDias} días</p></div>
      </div>
      <p className="mt-5 text-sm">{gracia || suspendida ? 'Periodo vencido' : 'Periodo activo'}</p>
      <p className="mt-1 text-[15px]">{fecha(s.periodo.inicio || s.proximaRenovacion)} – {fecha(s.periodo.fin || s.proximaRenovacion)}</p>
      <div className="mt-5 grid gap-4 border-t border-borde pt-5 sm:grid-cols-2">
        <div><p className="text-sm text-tinta-suave">{gratuito ? 'Fin del periodo' : 'Renovación automática'}</p><p className="mt-1">{fecha(s.proximaRenovacion)}</p></div>
        <div><p className="text-sm text-tinta-suave">Tarjeta registrada</p><p className="mt-1">{s.tarjetaEnmascarada || 'Sin tarjeta registrada'}</p></div>
      </div>
      {gracia && <p className="mt-5 text-sm">Periodo de gracia hasta {s.graciaHasta ? fecha(s.graciaHasta) : '—'} · {s.diasRestantes || 0} días restantes</p>}
      {(gracia || suspendida) && <div className="mt-5 grid gap-3 border-t border-borde pt-5 text-sm sm:grid-cols-2">
        {!gratuito && <p><span className="text-tinta-suave">Cobro rechazado</span><br />{s.ultimoRechazoEn ? fecha(s.ultimoRechazoEn) : '—'}</p>}
        <p><span className="text-tinta-suave">Periodo de gracia</span><br />{fecha(s.proximaRenovacion)} – {s.graciaHasta ? fecha(new Date(new Date(s.graciaHasta).getTime() - 86400000).toISOString()) : '—'}</p>
        {suspendida && <p><span className="text-tinta-suave">Suspendida desde</span><br />{s.graciaHasta ? fecha(s.graciaHasta) : '—'}</p>}
        {suspendida && <p><span className="text-tinta-suave">Consumidores afectados</span><br />{s.consumidoresAfectados ?? 0} consumidores</p>}
      </div>}
      {s.estado === 'activa' && <p className="mt-5 text-sm leading-relaxed text-tinta-suave">{gratuito ? 'Sus APIs responden con los límites del plan vigente.' : 'La renovación se cobra a esta misma tarjeta al cierre de cada ciclo. Sus APIs responden con normalidad.'}</p>}
      {s.cambioProgramado && <Aviso estado="neutro">A partir del {fecha(s.cambioProgramado.efectivoDesde || s.proximaRenovacion)} su plan será {s.cambioProgramado.nombre}.</Aviso>}
      {puedeGestionar && (gracia || suspendida) && (gratuito
        ? <Link className="mt-6 inline-flex h-[46px] items-center justify-center rounded-base bg-principal px-6 text-[15px] font-medium text-white" to="/panel/suscripcion/planes">Contratar un plan de pago</Link>
        : <Boton className="mt-6" onClick={() => { setError(''); setMostrarPago(true); }}>Pagar con otra tarjeta</Boton>)}
    </section>
    {error && <div className="mt-5"><Aviso estado="error">{error}</Aviso></div>}
    {puedeGestionar && mostrarPago && !gratuito && <form className="mt-6 rounded-base border border-borde bg-panel p-6" onSubmit={e => { e.preventDefault(); pagar.mutate(); }}>
      <h2 className="font-display text-[21px]">Datos de la tarjeta</h2>
      <p className="mt-2 text-sm text-tinta-suave">Al autorizar el cobro la suscripción se reactiva y comienza un ciclo nuevo desde hoy.</p>
      <div className="mt-5 grid gap-4 sm:grid-cols-2">
        <Campo etiqueta="Número de tarjeta" autoComplete="cc-number" value={numero} onChange={e => setNumero(e.target.value)} required />
        <Campo etiqueta="Titular de la tarjeta" autoComplete="cc-name" value={titular} onChange={e => setTitular(e.target.value)} required />
        <Campo etiqueta="Mes de vencimiento" autoComplete="cc-exp-month" value={mes} onChange={e => setMes(e.target.value)} required />
        <Campo etiqueta="Año de vencimiento" autoComplete="cc-exp-year" value={anio} onChange={e => setAnio(e.target.value)} required />
        <Campo etiqueta="Código de seguridad" autoComplete="cc-csc" value={cvv} onChange={e => setCvv(e.target.value)} required />
      </div>
      <Boton type="submit" className="mt-5" deshabilitado={pagar.isPending}>{pagar.isPending ? 'Procesando…' : 'Pagar y reactivar'}</Boton>
    </form>}
    <div className="mt-6 flex flex-wrap gap-4 text-sm">
      {puedeGestionar && <Link className="font-medium text-principal" to="/panel/suscripcion/planes">Cambiar plan</Link>}
      {puedeGestionar && s.cambioProgramado && <button className="font-medium text-principal underline" onClick={async () => {
        const { response } = await cliente.DELETE('/api/suscripcion/cambio-programado');
        if (!response.ok) setError('No se pudo cancelar el cambio programado.');
        else await cache.invalidateQueries({ queryKey: ['suscripcion-plataforma'] });
      }}>Cancelar cambio programado</button>}
    </div>
  </main>;
}
