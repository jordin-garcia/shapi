import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/organizaciones';
import { Aviso, Boton, Campo, EstadoCargando, EstadoError, Tabla, Tarjeta } from '@shapi/ui';
import { useParams } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
type Invitacion = components['schemas']['InvitacionConsumidor'];

const meses = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

function fecha(valor: string) {
  const partes = new Intl.DateTimeFormat('en-CA', {
    day: 'numeric', month: 'numeric', year: 'numeric', timeZone: 'America/Guatemala',
  }).formatToParts(new Date(valor));
  const parte = (tipo: Intl.DateTimeFormatPartTypes) => Number(partes.find(p => p.type === tipo)?.value);
  return `${parte('day')} ${meses[parte('month') - 1]} ${parte('year')}`;
}

export default function PaginaB15InvitarConsumidores() {
  const { id: apiId } = useParams<{ id: string }>();
  const cache = useQueryClient();
  const [correo, setCorreo] = useState('');
  const [error, setError] = useState('');
  const consulta = useQuery({
    queryKey: ['invitaciones-consumidores', apiId],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/apis/{apiId}/invitaciones', {
        params: { path: { apiId: apiId! } }, signal,
      });
      if (!response.ok || !data) throw new Error('No se pudieron cargar las invitaciones.');
      return data;
    },
    retry: false,
  });
  const refrescar = () => cache.invalidateQueries({ queryKey: ['invitaciones-consumidores', apiId] });
  const invitar = useMutation({
    mutationFn: async (nuevoCorreo: string) => {
      const { response } = await cliente.POST('/api/apis/{apiId}/invitaciones', {
        params: { path: { apiId: apiId! } }, body: { correo: nuevoCorreo },
      });
      if (!response.ok) throw new Error('No se pudo enviar la invitación.');
    },
    onSuccess: async () => { setCorreo(''); setError(''); await refrescar(); },
    onError: causa => setError(causa instanceof ErrorApi && causa.codigo === 'consumidor_existente'
      ? 'Ya existe una cuenta de consumidor con este correo.'
      : causa instanceof ErrorApi && causa.codigo === 'invitacion_pendiente'
        ? 'Ya existe una invitación pendiente para este correo.'
        : 'No se pudo enviar la invitación. Revise el correo e inténtelo de nuevo.'),
  });
  const reenviar = useMutation({
    mutationFn: async (id: string) => {
      const { response } = await cliente.POST('/api/apis/{apiId}/invitaciones/{invitacionId}/reenviar', {
        params: { path: { apiId: apiId!, invitacionId: id } },
      });
      if (!response.ok) throw new Error('No se pudo reenviar la invitación.');
    },
    onSuccess: async () => { setError(''); await refrescar(); },
    onError: () => setError('No se pudo reenviar la invitación. Inténtelo de nuevo.'),
  });
  const cancelar = useMutation({
    mutationFn: async (id: string) => {
      const { response } = await cliente.DELETE('/api/apis/{apiId}/invitaciones/{invitacionId}', {
        params: { path: { apiId: apiId!, invitacionId: id } },
      });
      if (!response.ok) throw new Error('No se pudo cancelar la invitación.');
    },
    onSuccess: async () => { setError(''); await refrescar(); },
    onError: () => setError('No se pudo cancelar la invitación. Inténtelo de nuevo.'),
  });

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudieron cargar las invitaciones." reintentar={() => void consulta.refetch()} />;

  const filas = consulta.data.elementos.map((invitacion: Invitacion) => [
    <span key="correo" className="break-all">{invitacion.correo}</span>,
    <span key="enviada" className="whitespace-nowrap tabular-nums">{fecha(invitacion.enviadaEn)}</span>,
    <span key="vence" className="whitespace-nowrap tabular-nums">{fecha(invitacion.venceEn)}</span>,
    <span key="accion" className="flex justify-end gap-4 whitespace-nowrap">
      <button type="button" aria-label={`Reenviar invitación a ${invitacion.correo}`} disabled={reenviar.isPending || cancelar.isPending}
        onClick={() => { setError(''); reenviar.mutate(invitacion.id); }} className="font-medium text-principal hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-principal disabled:cursor-not-allowed disabled:text-tinta-inactiva">Reenviar</button>
      <button type="button" aria-label={`Cancelar invitación a ${invitacion.correo}`} disabled={reenviar.isPending || cancelar.isPending}
        onClick={() => { setError(''); cancelar.mutate(invitacion.id); }} className="font-medium text-principal hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-principal disabled:cursor-not-allowed disabled:text-tinta-inactiva">Cancelar</button>
    </span>,
  ]);

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    setError('');
    invitar.mutate(correo.trim());
  }

  return (
    <main>
      <header className="flex flex-col gap-2.5">
        <p className="m-0 text-xs font-medium uppercase tracking-[0.16em] text-tinta-suave">{consulta.data.nombreApi}</p>
        <h1 className="m-0 font-display text-[32px] leading-tight">Invitar consumidores</h1>
        <p className="m-0 max-w-4xl text-[15px] leading-relaxed text-tinta-suave">Invite a un cliente a crear su cuenta en su portal. La cuenta creada por invitación queda con el correo ya verificado.</p>
      </header>

      <div className="mt-8 grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_440px]">
        <Tarjeta className="p-5">
          <h2 className="mb-4 mt-0 font-display text-[21px] leading-snug">Invitaciones pendientes</h2>
          {consulta.data.elementos.length > 0 ? <Tabla encabezados={['Correo', 'Enviada', 'Vence', 'Acción']} filas={filas} /> : (
            <div className="border-t border-borde py-8 text-center">
              <h3 className="m-0 font-display text-lg">No tiene invitaciones pendientes</h3>
              <p className="mb-0 mt-2 text-sm leading-relaxed text-tinta-suave">Las invitaciones que envíe aparecerán aquí.</p>
            </div>
          )}
        </Tarjeta>

        <Tarjeta className="flex flex-col p-5">
          <h2 className="m-0 font-display text-[21px] leading-snug">Invitar consumidor</h2>
          <form onSubmit={enviar}>
            <div className="mt-5 border-t border-borde pt-5">
              <Campo etiqueta="Correo" type="email" placeholder="correo@empresa.com" value={correo}
                onChange={evento => setCorreo(evento.target.value)} maxLength={320} required />
            </div>
            <p className="mb-0 mt-5 text-sm leading-relaxed text-tinta-suave">Le enviaremos un enlace al portal de {consulta.data.nombrePortal} para crear su cuenta. El enlace vence en 7 días.</p>
            {error && <div className="mt-4"><Aviso estado="error">{error}</Aviso></div>}
            <Boton type="submit" className="mt-6 w-full" deshabilitado={invitar.isPending || reenviar.isPending || cancelar.isPending}>
              {invitar.isPending ? 'Enviando…' : 'Enviar invitación'}
            </Boton>
          </form>
        </Tarjeta>
      </div>
    </main>
  );
}
