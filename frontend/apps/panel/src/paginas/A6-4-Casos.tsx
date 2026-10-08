import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import { crearCliente } from '@shapi/api';
import type { paths } from '@shapi/api/soporte';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { claseBoton, claseCampo, claseEtiqueta, EtiquetaEstado } from '../modulos/soporte/ComponentesSoporte';
import { fechaCorta } from '../modulos/soporte/formato';

const cliente = crearCliente<paths>(window.location.origin);

export default function PaginaA64Casos() {
  const queryClient = useQueryClient();
  const [formulario, setFormulario] = useState({ organizacionId: '', apiId: '', asunto: '', descripcion: '' });
  const consulta = useQuery({ queryKey: ['admin', 'casos'], queryFn: async ({ signal }) => {
    const { data, response } = await cliente.GET('/api/admin/casos', { signal });
    if (!response.ok || !data) throw new Error('No se pudieron consultar los casos'); return data;
  }, retry: false });
  const opciones = useQuery({ queryKey: ['admin', 'casos', 'organizaciones'], queryFn: async ({ signal }) => {
    const { data, response } = await cliente.GET('/api/admin/casos/organizaciones', { signal });
    if (!response.ok || !data) throw new Error('No se pudieron consultar las organizaciones'); return data;
  }, retry: false });
  const apis = useMemo(() => opciones.data?.find(o => o.id === formulario.organizacionId)?.apis ?? [], [opciones.data, formulario.organizacionId]);
  const abrir = useMutation({ mutationFn: async () => {
    const { response } = await cliente.POST('/api/admin/casos', { body: { ...formulario, apiId: formulario.apiId || null } });
    if (!response.ok) throw new Error('No se pudo registrar el caso');
  }, onSuccess: async () => {
    setFormulario({ organizacionId: '', apiId: '', asunto: '', descripcion: '' });
    await queryClient.invalidateQueries({ queryKey: ['admin', 'casos'] });
  } });

  return <main>
    <header className="flex flex-col gap-[10px]"><p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">Soporte</p><h1 className="font-display text-[32px]">Casos</h1><p className="text-[15px] text-tinta-suave">Casos que abren las organizaciones proveedoras desde su panel, o que el soporte registra a su nombre, y su estado.</p></header>
    <section className="mt-8 rounded-base border border-borde bg-panel p-5">
      {consulta.isPending && <EstadoCargando />}{consulta.isError && <EstadoError reintentar={() => void consulta.refetch()} />}
      {consulta.data && <>
        <table className="w-full border-collapse text-left"><thead><tr>{['Caso', 'Asunto', 'Estado', 'Acción'].map(x => <th key={x} className="border-b border-borde pb-[10px] pr-4 text-encabezado uppercase text-tinta-suave">{x}</th>)}</tr></thead>
          {consulta.data.length > 0 && <tbody>{consulta.data.map(caso => <tr key={caso.numero} className="border-b border-borde-fila last:border-0"><td className="py-3 pr-4 font-medium">CAS-{caso.numero}</td><td className="py-3 pr-4"><p className="font-medium">{caso.asunto}</p><p className="text-[14px] text-tinta-suave">{caso.organizacion} · {fechaCorta(caso.creadoEn)}</p></td><td className="py-3 pr-4"><EtiquetaEstado estado={caso.estado} /></td><td className="py-3"><Link className="font-medium text-principal" to={`/admin/casos/${caso.numero}`}>Abrir</Link></td></tr>)}</tbody>}
        </table>
        {consulta.data.length === 0 && <div className="flex flex-col items-center gap-3 py-14 text-center"><h2 className="font-display text-[22px]">Todavía no hay casos</h2><p className="max-w-[600px] text-[15px] text-tinta-suave">Los casos que abran las organizaciones o que registre el soporte aparecerán aquí con su organización y su estado.</p></div>}
      </>}
    </section>
    <form className="mt-8 rounded-base border border-borde bg-panel p-5" onSubmit={e => { e.preventDefault(); abrir.mutate(); }}>
      <h2 className="font-display text-[22px]">Registrar un caso</h2>
      <div className="mt-5 grid grid-cols-2 gap-4">
        <label className={claseEtiqueta}>Organización<select required className={claseCampo} value={formulario.organizacionId} onChange={e => setFormulario({ ...formulario, organizacionId: e.target.value, apiId: '' })}><option value="">Seleccione una organización</option>{opciones.data?.map(o => <option key={o.id} value={o.id}>{o.nombre}</option>)}</select></label>
        <label className={claseEtiqueta}>API afectada <span className="font-normal text-tinta-suave">(opcional)</span><select className={claseCampo} value={formulario.apiId} onChange={e => setFormulario({ ...formulario, apiId: e.target.value })}><option value="">Ninguna</option>{apis.map(api => <option key={api.id} value={api.id}>{api.nombre}</option>)}</select></label>
        <label className={`${claseEtiqueta} col-span-2`}>Asunto<input required maxLength={120} className={claseCampo} placeholder="Resuma el caso en una línea" value={formulario.asunto} onChange={e => setFormulario({ ...formulario, asunto: e.target.value })} /></label>
        <label className={`${claseEtiqueta} col-span-2`}>Descripción<textarea required maxLength={4000} rows={4} className={claseCampo} placeholder="Qué reportó la organización y qué se observó" value={formulario.descripcion} onChange={e => setFormulario({ ...formulario, descripcion: e.target.value })} /></label>
      </div>
      <p className="mt-3 text-[14px] text-tinta-suave">Úselo cuando la organización reporte algo por otro medio. El caso queda a nombre de la organización y usted lo atiende.</p>
      <div className="mt-5 flex justify-end"><button className={claseBoton} disabled={abrir.isPending}>Registrar caso</button></div>
    </form>
  </main>;
}
