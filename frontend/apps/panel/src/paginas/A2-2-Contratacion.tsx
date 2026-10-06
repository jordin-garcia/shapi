import { useQuery, useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { paths } from '@shapi/api/suscripciones';
import { Aviso, Boton, Campo, EstadoCargando, EstadoError } from '@shapi/ui';
import { useParams } from 'react-router';
import Confirmacion from './A2-3-Confirmacion';
import Rechazo from './A2-4-Rechazo';

const cliente = crearCliente<paths>(window.location.origin);
const dinero = (monto: number) => 'Q ' + monto.toLocaleString('es-GT', { minimumFractionDigits: 2 });

export default function PaginaA22Contratacion() {
  const { plan: planId } = useParams();
  const [numero, setNumero] = useState('');
  const [mes, setMes] = useState('');
  const [anio, setAnio] = useState('');
  const [cvv, setCvv] = useState('');
  const [titular, setTitular] = useState('');
  const [usarRegistrada, setUsarRegistrada] = useState(true);
  const [resultado, setResultado] = useState<'exito' | 'rechazo'>();
  const [mensaje, setMensaje] = useState('');
  const planes = useQuery({
    queryKey: ['planes-plataforma'],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/planes-plataforma', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron cargar los planes.');
      return data;
    },
    retry: false,
  });
  const plan = planes.data?.find(item => item.id === planId);
  const suscripcion = useQuery({
    queryKey: ['suscripcion-plataforma'],
    queryFn: async ({ signal }) => {
      try {
        const { data, response } = await cliente.GET('/api/suscripcion', { signal });
        return response.ok ? (data ?? null) : null;
      } catch {
        return null;
      }
    },
    retry: false,
  });
  const contratar = useMutation({
    mutationFn: async () => {
      if (!plan) throw new Error('No se encontró el plan.');
      const { data, response } = await cliente.POST('/api/suscripcion/contratar', {
        body: usarRegistrada && suscripcion.data?.tarjetaEnmascarada
          ? { planId: plan.id, usarRegistrada: true }
          : { planId: plan.id, usarRegistrada: false, tarjeta: { numero, mesVencimiento: mes, anioVencimiento: anio, cvv, titular } },
      });
      if (!response.ok || !data) throw new Error('No se pudo contratar el plan.');
      return data;
    },
    onSuccess: () => setResultado('exito'),
    onError: error => {
      setResultado('rechazo');
      setMensaje(error instanceof ErrorApi ? error.titulo : 'No se autorizó el cobro. Revise los datos e inténtelo de nuevo.');
    },
  });
  if (planes.isPending || suscripcion.isPending) return <EstadoCargando />;
  if (planes.isError) return <EstadoError mensaje="No se pudieron cargar los planes." reintentar={() => void planes.refetch()} />;
  if (!plan) return <Aviso estado="error">No se encontró el plan seleccionado.</Aviso>;
  if (resultado === 'exito') return <Confirmacion />;
  if (resultado === 'rechazo') return <Rechazo monto={dinero(plan.precio)} mensaje={mensaje} />;
  return <main className="mx-auto max-w-[760px] rounded-base border border-borde bg-panel p-8 md:p-10">
    <h1 className="font-display text-[32px] leading-tight">Contratación de un plan superior</h1>
    <p className="mt-3 text-[15px] leading-relaxed text-tinta-suave">El cobro se autoriza en el momento y el plan queda activo de inmediato.</p>
    <section className="mt-8 border-y border-borde py-5">
      <h2 className="font-display text-[22px]">{plan.nombre}</h2>
      <p className="mt-1 text-sm text-tinta-suave">{plan.descripcion}</p>
      <p className="mt-4 text-sm">Precio <span className="float-right font-medium">{dinero(plan.precio)} cada {plan.vigenciaDias} días</span></p>
      <p className="mt-2 text-sm">Plan actual <span className="float-right">Prueba · Q 0.00</span></p>
      <p className="mt-5 text-etiqueta uppercase tracking-[0.14em] text-tinta-suave">Límites del plan</p>
      <p className="mt-2 text-sm">{plan.maxApis ?? 'APIs ilimitadas'} APIs · {plan.cuotaPeticiones.toLocaleString('es-GT')} peticiones · {plan.maxMiembros ?? 'Miembros ilimitados'} miembros</p>
    </section>
    <form className="mt-6 space-y-4" onSubmit={event => { event.preventDefault(); contratar.mutate(); }}>
      <h2 className="font-display text-[21px]">Datos de la tarjeta</h2>
      <p className="text-sm leading-relaxed text-tinta-suave">Shapi no guarda el número completo ni el código de seguridad: la pasarela entrega un token con el que se cobran las renovaciones. Usted solo verá la marca y los últimos cuatro dígitos.</p>
      {suscripcion.data?.tarjetaEnmascarada && <fieldset className="space-y-3 text-sm">
        <legend className="mb-3 font-medium">Forma de pago</legend>
        <label className="flex items-center gap-3"><input type="radio" name="tarjeta" checked={usarRegistrada} onChange={() => setUsarRegistrada(true)} />Usar tarjeta registrada · {suscripcion.data.tarjetaEnmascarada}</label>
        <label className="flex items-center gap-3"><input type="radio" name="tarjeta" checked={!usarRegistrada} onChange={() => setUsarRegistrada(false)} />Usar otra tarjeta</label>
      </fieldset>}
      {(!suscripcion.data?.tarjetaEnmascarada || !usarRegistrada) && <>
        <Campo etiqueta="Número de tarjeta" autoComplete="cc-number" inputMode="numeric" value={numero} onChange={event => setNumero(event.target.value)} required />
        <div className="grid grid-cols-2 gap-4">
          <Campo etiqueta="Mes de vencimiento" autoComplete="cc-exp-month" inputMode="numeric" value={mes} onChange={event => setMes(event.target.value)} required />
          <Campo etiqueta="Año de vencimiento" autoComplete="cc-exp-year" inputMode="numeric" value={anio} onChange={event => setAnio(event.target.value)} required />
        </div>
        <Campo etiqueta="Código de seguridad" autoComplete="cc-csc" inputMode="numeric" value={cvv} onChange={event => setCvv(event.target.value)} required />
        <Campo etiqueta="Titular de la tarjeta" autoComplete="cc-name" value={titular} onChange={event => setTitular(event.target.value)} required />
      </>}
      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-borde pt-5">
        <p className="text-sm text-tinta-suave">A pagar hoy <strong className="ml-3 font-display text-[26px] text-tinta">{dinero(plan.precio)}</strong></p>
        <Boton type="submit" deshabilitado={contratar.isPending}>{contratar.isPending ? 'Procesando…' : 'Pagar ' + dinero(plan.precio) + ' y contratar'}</Boton>
      </div>
    </form>
  </main>;
}
