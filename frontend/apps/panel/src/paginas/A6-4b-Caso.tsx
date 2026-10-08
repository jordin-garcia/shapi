import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from 'react-router';
import { crearCliente } from '@shapi/api';
import type { paths } from '@shapi/api/soporte';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { claseBoton, claseBotonSecundario, claseCampo, claseEtiqueta, Conversacion, EtiquetaEstado } from '../modulos/soporte/ComponentesSoporte';
import { diaAnterior, fechaCorta, fechaLarga } from '../modulos/soporte/formato';

const cliente = crearCliente<paths>(window.location.origin);

function useAccionCaso(numero: number, ruta: '/api/admin/casos/{numero}/asignar' | '/api/admin/casos/{numero}/cerrar') {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      const { response } = await cliente.POST(ruta, { params: { path: { numero } } });
      if (!response.ok) throw new Error('No se pudo actualizar el caso');
    },
    onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['admin', 'caso', numero] }); },
  });
}

export default function PaginaA64bCaso() {
  const numero = Number(useParams().numero);
  const queryClient = useQueryClient();
  const [cuerpo, setCuerpo] = useState('');
  const caso = useQuery({ queryKey: ['admin', 'caso', numero], queryFn: async ({ signal }) => {
    const { data, response } = await cliente.GET('/api/admin/casos/{numero}', { params: { path: { numero } }, signal });
    if (!response.ok || !data) throw new Error('No se pudo consultar el caso'); return data;
  }, retry: false });
  const organizacion = useQuery({ queryKey: ['admin', 'caso', numero, 'organizacion'], queryFn: async ({ signal }) => {
    const { data, response } = await cliente.GET('/api/admin/casos/{numero}/organizacion', { params: { path: { numero } }, signal });
    if (!response.ok || !data) throw new Error('No se pudo consultar la organización'); return data;
  }, retry: false });
  const asignar = useAccionCaso(numero, '/api/admin/casos/{numero}/asignar');
  const cerrar = useAccionCaso(numero, '/api/admin/casos/{numero}/cerrar');
  const responder = useMutation({ mutationFn: async () => {
    const { response } = await cliente.POST('/api/admin/casos/{numero}/mensajes', { params: { path: { numero } }, body: { cuerpo } }); if (!response.ok) throw new Error('No se pudo responder');
  }, onSuccess: async () => { setCuerpo(''); await queryClient.invalidateQueries({ queryKey: ['admin', 'caso', numero] }); } });

  if (caso.isPending) return <EstadoCargando />;
  if (caso.isError || !caso.data) return <EstadoError reintentar={() => void caso.refetch()} />;
  const detalle = caso.data;
  return <main>
    <header className="flex items-start justify-between gap-6"><div className="flex flex-col gap-[10px]"><p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">Soporte · CAS-{detalle.numero}</p><h1 className="font-display text-[32px]">{detalle.asunto}</h1><p className="text-[15px] text-tinta-suave">{detalle.organizacion} · Abierto por {detalle.creadoPor} el {fechaLarga(detalle.creadoEn)}{detalle.asignadoA ? ` · Atiende ${detalle.asignadoA}` : ''}</p></div><EtiquetaEstado estado={detalle.estado} /></header>
    <div className="mt-8 grid grid-cols-[minmax(0,1fr)_440px] items-start gap-6">
      <section className="rounded-base border border-borde bg-panel p-5">
        <Conversacion caso={detalle} proveedor={false} integrada />
        {detalle.estado === 'abierto' && <form className="mt-5 border-t border-borde pt-5" onSubmit={e => { e.preventDefault(); responder.mutate(); }}>
          <label className={claseEtiqueta}>Respuesta<textarea required maxLength={4000} rows={4} className={claseCampo} placeholder="Escriba la respuesta para la organización" value={cuerpo} onChange={e => setCuerpo(e.target.value)} /></label>
          <div className="mt-6 flex gap-3"><button className={claseBoton} disabled={responder.isPending}>Enviar respuesta</button>{!detalle.asignadoA && <button type="button" className={claseBotonSecundario} onClick={() => asignar.mutate()} disabled={asignar.isPending}>Asignarme el caso</button>}<button type="button" className={claseBotonSecundario} onClick={() => cerrar.mutate()} disabled={cerrar.isPending}>Cerrar el caso</button></div>
        </form>}
      </section>
      <section className="rounded-base border border-borde bg-panel p-5"><h2 className="font-display text-[20px]">Datos de la organización</h2><p className="mt-5 rounded-base border border-borde bg-fondo p-3 text-[14px] text-tinta-suave">Solo lectura. Con el rol de soporte usted consulta los datos de la organización, pero no puede modificarlos.</p>
        {organizacion.isPending && <EstadoCargando />}{organizacion.isError && <EstadoError reintentar={() => void organizacion.refetch()} />}{organizacion.data && <dl className="mt-5 flex flex-col gap-[14px] border-t border-borde pt-5">{[
          ['Organización', organizacion.data.organizacion], ['API afectada', organizacion.data.apiAfectada ?? 'Ninguna'], ['Plan de plataforma', organizacion.data.plan], ['Ciclo vigente', `${fechaCorta(organizacion.data.cicloInicio)} – ${fechaCorta(diaAnterior(organizacion.data.cicloFin))}`],
          ['Estado', organizacion.data.estado === 'en_gracia' ? 'En gracia' : organizacion.data.estado === 'activa' ? 'Activa' : 'Suspendida'], ['APIs', String(organizacion.data.numeroApis)], ['Consumidores', String(organizacion.data.numeroConsumidores)],
          ['Dominio propio', organizacion.data.dominioPropio ?? 'Sin dominio propio'], ['Verificación', organizacion.data.verificacionDominio ? organizacion.data.verificacionDominio.charAt(0).toUpperCase() + organizacion.data.verificacionDominio.slice(1) : 'No aplica'],
        ].map(([clave, valor]) => <div key={clave} className="flex items-baseline justify-between gap-4"><dt className="text-[14px] text-tinta-suave">{clave}</dt><dd className="text-right text-[15px] font-medium">{valor}</dd></div>)}</dl>}
      </section>
    </div>
  </main>;
}
