import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import { crearCliente } from '@shapi/api';
import type { paths as pathsSoporte } from '@shapi/api/soporte';
import type { paths as pathsApis } from '@shapi/api/apis';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { claseBoton, claseCampo, claseEtiqueta, EtiquetaEstado } from '../modulos/soporte/ComponentesSoporte';
import { fechaCorta } from '../modulos/soporte/formato';

const soporte = crearCliente<pathsSoporte>(window.location.origin);
const apis = crearCliente<pathsApis>(window.location.origin);

export default function PaginaA71Casos() {
  const queryClient = useQueryClient();
  const [formulario, setFormulario] = useState({ asunto: '', apiId: '', descripcion: '' });
  const consulta = useQuery({
    queryKey: ['casos'],
    queryFn: async ({ signal }) => {
      const { data, response } = await soporte.GET('/api/casos', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron consultar los casos');
      return data;
    }, retry: false,
  });
  const consultaApis = useQuery({
    queryKey: ['apis', 'opciones-caso'],
    queryFn: async ({ signal }) => {
      const { data, response } = await apis.GET('/api/apis', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron consultar las APIs');
      return data.elementos;
    }, retry: false,
  });
  const abrir = useMutation({
    mutationFn: async () => {
      const { data, response } = await soporte.POST('/api/casos', {
        body: { asunto: formulario.asunto, apiId: formulario.apiId || null, descripcion: formulario.descripcion },
      });
      if (!response.ok || !data) throw new Error('No se pudo abrir el caso');
      return data;
    },
    onSuccess: async () => {
      setFormulario({ asunto: '', apiId: '', descripcion: '' });
      await queryClient.invalidateQueries({ queryKey: ['casos'] });
    },
  });

  return <main>
    <header className="flex flex-col gap-[10px]">
      <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">Organización</p>
      <h1 className="font-display text-[32px] leading-[1.2]">Casos de soporte</h1>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">Escríbale al equipo de soporte de Shapi. Le avisaremos por correo cada vez que le respondan.</p>
    </header>

    <section className="mt-8 rounded-base border border-borde bg-panel p-5">
      {consulta.isPending && <EstadoCargando />}
      {consulta.isError && <EstadoError reintentar={() => void consulta.refetch()} />}
      {consulta.data && <table className="w-full border-collapse text-left">
        <thead><tr>{['Caso', 'Asunto', 'Estado', 'Acción'].map(titulo => <th key={titulo} className="border-b border-borde pb-[10px] pr-4 text-encabezado uppercase text-tinta-suave">{titulo}</th>)}</tr></thead>
        <tbody>{consulta.data.map(caso => <tr key={caso.numero} className="border-b border-borde-fila last:border-b-0">
          <td className="py-3 pr-4 font-medium tabular-nums">CAS-{caso.numero}</td>
          <td className="py-3 pr-4"><p className="font-medium">{caso.asunto}</p><p className="text-[14px] text-tinta-suave">{caso.apiNombre ? `${caso.apiNombre} · ` : ''}{fechaCorta(caso.creadoEn)} · {caso.respuestas} {caso.respuestas === 1 ? 'respuesta' : 'respuestas'}</p></td>
          <td className="py-3 pr-4"><EtiquetaEstado estado={caso.estado} /></td>
          <td className="py-3"><Link className="font-medium text-principal hover:underline" to={`/panel/soporte/${caso.numero}`}>Abrir</Link></td>
        </tr>)}</tbody>
      </table>}
    </section>

    <form className="mt-8 rounded-base border border-borde bg-panel p-5" onSubmit={evento => { evento.preventDefault(); abrir.mutate(); }}>
      <h2 className="font-display text-[22px]">Abrir un caso</h2>
      <div className="mt-5 grid grid-cols-2 gap-4">
        <label className={claseEtiqueta}>Asunto<input required maxLength={120} className={claseCampo} placeholder="Resuma el caso en una línea" value={formulario.asunto} onChange={e => setFormulario({ ...formulario, asunto: e.target.value })} /></label>
        <label className={claseEtiqueta}>API afectada <span className="font-normal text-tinta-suave">(opcional)</span>
          <select className={claseCampo} value={formulario.apiId} onChange={e => setFormulario({ ...formulario, apiId: e.target.value })}>
            <option value="">Ninguna</option>{consultaApis.data?.map(api => <option key={api.id} value={api.id}>{api.nombre}</option>)}
          </select>
        </label>
        <label className={`${claseEtiqueta} col-span-2`}>Descripción<textarea required maxLength={4000} rows={4} className={claseCampo} placeholder="Qué ocurrió, desde cuándo y qué intentó" value={formulario.descripcion} onChange={e => setFormulario({ ...formulario, descripcion: e.target.value })} /></label>
      </div>
      <div className="mt-5 flex justify-end"><button className={claseBoton} disabled={abrir.isPending}>Abrir caso</button></div>
    </form>
  </main>;
}
