import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from 'react-router';
import { crearCliente } from '@shapi/api';
import type { paths } from '@shapi/api/soporte';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { claseBoton, claseCampo, claseEtiqueta, Conversacion, EtiquetaEstado } from '../modulos/soporte/ComponentesSoporte';
import { fechaLarga } from '../modulos/soporte/formato';

const cliente = crearCliente<paths>(window.location.origin);

export default function PaginaA72Caso() {
  const numero = Number(useParams().numero);
  const queryClient = useQueryClient();
  const [cuerpo, setCuerpo] = useState('');
  const consulta = useQuery({
    queryKey: ['caso', numero],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/casos/{numero}', { params: { path: { numero } }, signal });
      if (!response.ok || !data) throw new Error('No se pudo consultar el caso');
      return data;
    }, retry: false,
  });
  const responder = useMutation({
    mutationFn: async () => {
      const { response } = await cliente.POST('/api/casos/{numero}/mensajes', { params: { path: { numero } }, body: { cuerpo } });
      if (!response.ok) throw new Error('No se pudo enviar la respuesta');
    },
    onSuccess: async () => { setCuerpo(''); await queryClient.invalidateQueries({ queryKey: ['caso', numero] }); },
  });

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError || !consulta.data) return <EstadoError reintentar={() => void consulta.refetch()} />;
  const caso = consulta.data;
  return <main>
    <header className="flex items-start justify-between gap-6">
      <div className="flex flex-col gap-[10px]">
        <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">Casos de soporte · CAS-{caso.numero}</p>
        <h1 className="font-display text-[32px] leading-[1.2]">{caso.asunto}</h1>
        <p className="text-[15px] text-tinta-suave">{caso.apiNombre ? `${caso.apiNombre} · ` : ''}Abierto el {fechaLarga(caso.creadoEn)}{caso.asignadoA ? ` · Lo atiende ${caso.asignadoA}` : ''}</p>
      </div>
      <EtiquetaEstado estado={caso.estado} />
    </header>
    <Conversacion caso={caso} proveedor />
    {caso.estado === 'abierto' ? <form className="mt-6 rounded-base border border-borde bg-panel p-5" onSubmit={e => { e.preventDefault(); responder.mutate(); }}>
      <label className={claseEtiqueta}>Su respuesta<textarea className={claseCampo} required rows={4} maxLength={4000} placeholder="Escriba su respuesta para el equipo de soporte" value={cuerpo} onChange={e => setCuerpo(e.target.value)} /></label>
      <div className="mt-4 flex justify-end"><button className={claseBoton} disabled={responder.isPending}>Enviar respuesta</button></div>
    </form> : <p className="mt-6 rounded-base border border-borde bg-fondo p-4 text-tinta-suave">Este caso está cerrado y ya no admite respuestas.</p>}
  </main>;
}
