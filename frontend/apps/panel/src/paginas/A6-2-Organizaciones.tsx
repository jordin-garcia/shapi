import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/administracion';
import { EstadoCargando, EstadoError, Toast } from '@shapi/ui';
import { useSesion } from '../modulos/sesion/useSesion';
import PaginaA62bSuspender from './A6-2b-Suspender';

const cliente = crearCliente<paths>(window.location.origin);
type Organizacion = components['schemas']['OrganizacionAdministracion'];

async function listar() {
  const { data, response } = await cliente.GET('/api/admin/organizaciones');
  if (!response.ok || !data) throw new Error('No se pudieron consultar las organizaciones');
  return data;
}

function tituloEstado(estado: Organizacion['estado']) {
  return estado === 'en_gracia' ? 'En gracia' : estado === 'suspendida' ? 'Suspendida' : 'Activa';
}

function ciclo(organizacion: Organizacion) {
  const meses = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
  const partes = (fecha: string) => Object.fromEntries(new Intl.DateTimeFormat('es-GT', {
    day: 'numeric', month: 'numeric', year: 'numeric', timeZone: 'America/Guatemala',
  }).formatToParts(new Date(fecha)).map(parte => [parte.type, parte.value]));
  const desde = partes(organizacion.cicloInicio);
  const hasta = partes(organizacion.cicloFin);
  return `${desde.day} ${meses[Number(desde.month) - 1]} – ${hasta.day} ${meses[Number(hasta.month) - 1]} ${hasta.year}`;
}

export default function PaginaA62Organizaciones() {
  const cache = useQueryClient();
  const { data: sesion } = useSesion();
  const consulta = useQuery({ queryKey: ['admin', 'organizaciones'], queryFn: listar });
  const [seleccionada, setSeleccionada] = useState<Organizacion | null>(null);
  const [aviso, setAviso] = useState(0);
  const mutacion = useMutation({
    mutationFn: async ({ organizacion, accion, motivo }: { organizacion: Organizacion; accion: 'suspender' | 'reactivar'; motivo?: string }) => {
      const parametros = { params: { path: { id: organizacion.id }, header: { 'X-Requested-With': 'shapi' as const } } };
      const respuesta = accion === 'suspender'
        ? await cliente.POST('/api/admin/organizaciones/{id}/suspender', { ...parametros, body: { motivo: motivo! } })
        : await cliente.POST('/api/admin/organizaciones/{id}/reactivar', parametros);
      if (!respuesta.response.ok) throw new Error('No se pudo cambiar el estado de la organización');
      return accion;
    },
    onSuccess: async () => {
      setSeleccionada(null);
      setAviso(actual => actual + 1);
      await cache.invalidateQueries({ queryKey: ['admin', 'organizaciones'] });
    },
  });

  if (seleccionada) {
    return <PaginaA62bSuspender
      organizacion={seleccionada}
      cancelar={() => setSeleccionada(null)}
      confirmar={motivo => mutacion.mutate({ organizacion: seleccionada, accion: 'suspender', motivo })}
      guardando={mutacion.isPending}
      error={mutacion.isError}
    />;
  }

  return <div>
    {aviso > 0 && <Toast key={aviso}>Estado de la organización actualizado.</Toast>}
    <header className="flex flex-col gap-2.5">
      <p className="text-etiqueta uppercase tracking-[.16em] text-tinta-suave">Plataforma</p>
      <h1 className="font-display text-[32px] leading-[1.2]">Organizaciones</h1>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">Organizaciones proveedoras registradas en Shapi y el estado de su suscripción de plataforma.</p>
    </header>

    {mutacion.isError && mutacion.variables?.accion === 'reactivar' &&
      <div className="mt-6"><EstadoError reintentar={() => mutacion.mutate(mutacion.variables!)} /></div>}

    <section className="mt-8 rounded-base border border-borde bg-panel p-5" aria-label="Organizaciones proveedoras">
      {consulta.isPending && <EstadoCargando />}
      {consulta.isError && <EstadoError reintentar={() => void consulta.refetch()} />}
      {consulta.data && <>
        <div className="w-full overflow-auto">
          <table className="w-full border-collapse text-left">
            <thead><tr>
              <th className="border-b border-borde pb-2.5 pr-4 text-encabezado uppercase text-tinta-suave">{consulta.data.length === 0 ? 'Organización' : 'Organización y propietario'}</th>
              <th className="w-[60px] border-b border-borde pb-2.5 pr-4 text-encabezado uppercase text-tinta-suave">APIs</th>
              <th className="w-[106px] border-b border-borde pb-2.5 pr-4 text-encabezado uppercase text-tinta-suave">Plan</th>
              <th className="w-[190px] border-b border-borde pb-2.5 pr-4 text-encabezado uppercase text-tinta-suave">Ciclo vigente</th>
              <th className="w-[110px] border-b border-borde pb-2.5 pr-4 text-encabezado uppercase text-tinta-suave">Estado</th>
              <th className="w-[78px] border-b border-borde pb-2.5 text-right text-encabezado uppercase text-tinta-suave">Acción</th>
            </tr></thead>
            {consulta.data.length > 0 && <tbody>{consulta.data.map(organizacion => <tr key={organizacion.id} className="border-b border-borde-fila last:border-b-0">
              <td className="py-3 pr-4"><div className="flex flex-col"><span className="text-[15px] font-medium">{organizacion.nombre}</span><span className="text-[14px] text-tinta-suave">{organizacion.propietarioCorreo}</span></div></td>
              <td className="py-3 pr-4 text-[15px] tabular-nums">{organizacion.numeroApis}</td>
              <td className="py-3 pr-4 text-[15px]">{organizacion.plan}</td>
              <td className="py-3 pr-4 text-[14px] tabular-nums">{ciclo(organizacion)}</td>
              <td className="py-3 pr-4"><span className={`inline-block rounded-base border px-2.5 py-1 text-[12px] font-semibold uppercase tracking-[.06em] ${organizacion.estado === 'suspendida' ? 'border-[#EDC3B4] bg-[#FBE9E3] text-[#8E3315]' : organizacion.estado === 'en_gracia' ? 'border-borde bg-fondo text-tinta-suave' : 'border-[#B6DCC9] bg-[#E4F3EC] text-[#146542]'}`}>{tituloEstado(organizacion.estado)}</span></td>
              <td className="py-3 text-right">
                {sesion?.rol === 'administrador' && (organizacion.suspendidaAdministrativamente
                  ? <button type="button" aria-label={`Reactivar ${organizacion.nombre}`} onClick={() => mutacion.mutate({ organizacion, accion: 'reactivar' })} className="font-medium text-principal hover:underline">Reactivar</button>
                  : <button type="button" aria-label={`Suspender ${organizacion.nombre}`} onClick={() => { mutacion.reset(); setSeleccionada(organizacion); }} className="font-medium text-principal hover:underline">Suspender</button>)}
              </td>
            </tr>)}</tbody>}
          </table>
        </div>
        {consulta.data.length === 0 && <EstadoVacio />}
      </>}
    </section>
  </div>;
}

function EstadoVacio() {
  return <div className="flex flex-col items-center gap-6 px-4 pb-[52px] pt-16 text-center">
    <svg width="40" height="40" viewBox="0 0 40 40" fill="none" aria-hidden="true">
      <rect x="1" y="1" width="38" height="38" rx="8" stroke="#C9D2E1" strokeWidth="1.5" strokeDasharray="4 4" />
      <path d="M13 27 V15 L20 12 V27 M20 27 V18.5 L27 21 V27 M11 27 H29" stroke="#3B6FF0" strokeWidth="1.5" strokeLinejoin="round" strokeLinecap="round" />
    </svg>
    <div className="flex max-w-[520px] flex-col gap-2.5">
      <h2 className="font-display text-[22px] leading-[1.3]">Todavía no hay organizaciones registradas</h2>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">Las empresas que se registren en Shapi aparecerán aquí con su plan de plataforma y el estado de su suscripción.</p>
    </div>
  </div>;
}
