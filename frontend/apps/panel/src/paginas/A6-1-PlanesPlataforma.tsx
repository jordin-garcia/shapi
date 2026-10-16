import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/planes';
import { Boton, EstadoCargando, EstadoError, Toast } from '@shapi/ui';

const cliente = crearCliente<paths>(window.location.origin);
type Plan = components['schemas']['PlanPlataformaAdministracion'];
type Peticion = components['schemas']['PeticionPlanPlataforma'];

async function listar() {
  const { data, response } = await cliente.GET('/api/admin/planes-plataforma');
  if (!response.ok || !data) throw new Error('No se pudieron cargar los planes de plataforma.');
  return data;
}

function dinero(precio: number) {
  return `Q ${precio.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function limites(plan: Plan) {
  if (plan.maxApis === null && plan.maxMiembros === null) {
    return `APIs y miembros ilimitados · ${plan.cuotaPeticiones.toLocaleString('en-US')} peticiones${plan.dominioPropio ? ' · dominio propio' : ''}`;
  }
  const apis = plan.maxApis === null ? 'APIs ilimitadas' : `${plan.maxApis} ${plan.maxApis === 1 ? 'API' : 'APIs'}`;
  const miembros = plan.maxMiembros === null ? 'miembros ilimitados' : `${plan.maxMiembros} ${plan.maxMiembros === 1 ? 'miembro' : 'miembros'}`;
  return `${apis} · ${plan.cuotaPeticiones.toLocaleString('en-US')} peticiones · ${miembros}${plan.dominioPropio ? ' · dominio propio' : ''}`;
}

export default function PaginaA61PlanesPlataforma() {
  const cache = useQueryClient();
  const consulta = useQuery({ queryKey: ['admin', 'planes-plataforma'], queryFn: listar, retry: false });
  const [planSeleccionado, setPlanSeleccionado] = useState<Plan | null>(null);
  const [crear, setCrear] = useState(false);
  const [aviso, setAviso] = useState('');
  const desactivar = useMutation({
    mutationFn: async (plan: Plan) => {
      const { response } = await cliente.POST('/api/admin/planes-plataforma/{id}/desactivar', { params: { path: { id: plan.id } } });
      if (!response.ok) throw new Error('No se pudo desactivar el plan.');
    },
    onSuccess: async () => { setAviso('Plan desactivado.'); await cache.invalidateQueries({ queryKey: ['admin', 'planes-plataforma'] }); },
  });

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudieron cargar los planes de plataforma." reintentar={() => void consulta.refetch()} />;
  if (crear || planSeleccionado) return <FormularioPlan
    original={planSeleccionado ?? undefined}
    cancelar={() => { setCrear(false); setPlanSeleccionado(null); }}
    guardado={async () => { setCrear(false); setPlanSeleccionado(null); setAviso('Plan guardado.'); await cache.invalidateQueries({ queryKey: ['admin', 'planes-plataforma'] }); }}
    desactivar={planSeleccionado?.activo ? async () => { await desactivar.mutateAsync(planSeleccionado); setPlanSeleccionado(null); } : undefined}
    errorDesactivar={desactivar.isError}
  />;

  return <div>
    {aviso && <Toast key={aviso}>{aviso}</Toast>}
    <header className="flex items-end justify-between gap-6">
      <div className="flex flex-col gap-2.5">
        <p className="text-etiqueta uppercase tracking-[.16em] text-tinta-suave">Plataforma</p>
        <h1 className="font-display text-[32px] leading-[1.2]">Planes de plataforma</h1>
        <p className="text-[15px] leading-[1.55] text-tinta-suave">Los planes que las organizaciones proveedoras contratan en Shapi.</p>
      </div>
      <Boton type="button" onClick={() => setCrear(true)}>Crear un plan</Boton>
    </header>
    {desactivar.isError && <div className="mt-5"><EstadoError mensaje="No se pudo desactivar el plan." reintentar={() => desactivar.mutate(desactivar.variables!)} /></div>}
    <section aria-label="Planes de plataforma" className="mt-8 rounded-base border border-borde bg-panel p-5">
      <div className="w-full overflow-auto"><table className="w-full border-collapse text-left">
        <thead><tr>{['Plan', 'Descripción', 'Precio', 'Vigencia', 'Límites', 'Acción'].map(t => <th key={t} className="border-b border-borde pb-2.5 pr-4 text-encabezado uppercase text-tinta-suave">{t}</th>)}</tr></thead>
        <tbody>{consulta.data.map(plan => <tr key={plan.id} className="border-b border-borde-fila last:border-b-0">
          <td className="py-3 pr-4"><div className="font-medium">{plan.nombre}</div>{!plan.activo && <span className="text-xs text-tinta-suave">Inactivo</span>}</td>
          <td className="py-3 pr-4 text-sm">{plan.descripcion}</td>
          <td className="py-3 pr-4 whitespace-nowrap">{dinero(plan.precio)}</td>
          <td className="py-3 pr-4 whitespace-nowrap">{plan.vigenciaDias} días</td>
          <td className="py-3 pr-4 text-sm">{limites(plan)}</td>
          <td className="py-3 text-right"><button type="button" onClick={() => setPlanSeleccionado(plan)} className="font-medium text-principal hover:underline">Editar</button></td>
        </tr>)}</tbody>
      </table></div>
      {consulta.data.length === 0 && <div className="px-4 py-14 text-center"><h2 className="font-display text-[22px]">Todavía no hay planes de plataforma</h2><p className="mt-2 text-tinta-suave">Cree planes para las organizaciones proveedoras.</p></div>}
    </section>
  </div>;
}

function FormularioPlan({ original, cancelar, guardado, desactivar, errorDesactivar }: { original?: Plan; cancelar: () => void; guardado: () => Promise<void>; desactivar?: () => Promise<void>; errorDesactivar: boolean }) {
  const [nombre, setNombre] = useState(original?.nombre ?? '');
  const [descripcion, setDescripcion] = useState(original?.descripcion ?? '');
  const [apis, setApis] = useState(original?.maxApis?.toString() ?? '');
  const [peticiones, setPeticiones] = useState(original?.cuotaPeticiones.toString() ?? '');
  const [miembros, setMiembros] = useState(original?.maxMiembros?.toString() ?? '');
  const [dominio, setDominio] = useState(original?.dominioPropio ?? false);
  const [precio, setPrecio] = useState(original?.precio.toFixed(2) ?? '');
  const [vigencia, setVigencia] = useState(original?.vigenciaDias.toString() ?? '30');
  const [error, setError] = useState('');
  const mutacion = useMutation({
    mutationFn: async () => {
      const cuerpo: Peticion = { nombre, descripcion, precio: Number(precio), vigenciaDias: Number(vigencia), maxApis: apis ? Number(apis) : null, maxMiembros: miembros ? Number(miembros) : null, cuotaPeticiones: Number(peticiones), dominioPropio: dominio };
      const respuesta = original
        ? await cliente.PUT('/api/admin/planes-plataforma/{id}', { params: { path: { id: original.id } }, body: cuerpo })
        : await cliente.POST('/api/admin/planes-plataforma', { body: cuerpo });
      if (!respuesta.response.ok) throw new Error('Revise los datos e inténtelo de nuevo.');
    },
    onSuccess: guardado,
    onError: e => setError(e instanceof Error ? e.message : 'No se pudo guardar el plan.'),
  });
  const submit = (e: React.FormEvent) => { e.preventDefault(); setError(''); mutacion.mutate(); };
  return <div>
    <header className="flex flex-col gap-2.5"><p className="text-etiqueta uppercase tracking-[.16em] text-tinta-suave">Plataforma</p><h1 className="font-display text-[32px] leading-[1.2]">{original ? 'Editar plan de plataforma' : 'Crear plan de plataforma'}</h1><p className="text-[15px] leading-[1.55] text-tinta-suave">Las organizaciones proveedoras ven estos datos en la página de precios de Shapi.</p></header>
    <form onSubmit={submit} className="mt-8 grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_440px]">
      <section className="rounded-base border border-borde bg-panel p-5">
        <h2 className="font-display text-[21px]">Datos del plan</h2>
        <div className="mt-5 flex flex-col gap-4 border-t border-borde-fila pt-5">
          <Campo label="Nombre" value={nombre} onChange={setNombre} required placeholder="Nombre del plan" />
          <Campo label="Descripción" value={descripcion} onChange={setDescripcion} required multiline placeholder="Para qué sirve este plan" />
        </div>
        <h2 className="mt-6 border-t border-borde-fila pt-5 font-display text-[21px]">Límites del plan</h2>
        <div className="mt-5 grid gap-4 sm:grid-cols-3">
          <Campo label="APIs" value={apis} onChange={setApis} type="number" min="1" placeholder="0" suffix="APIs" />
          <Campo label="Peticiones por ciclo" value={peticiones} onChange={setPeticiones} type="number" min="1" required placeholder="0" suffix="peticiones" />
          <Campo label="Miembros" value={miembros} onChange={setMiembros} type="number" min="1" placeholder="0" suffix="miembros" />
        </div>
        <label className="mt-5 flex items-start gap-3 rounded-base border border-borde p-3.5"><input type="checkbox" checked={dominio} onChange={e => setDominio(e.target.checked)} /><span><span className="block font-medium">Dominio propio</span><span className="text-sm text-tinta-suave">Con este plan la organización puede conectar su propio dominio a las APIs que publique.</span></span></label>
      </section>
      <section className="rounded-base border border-borde bg-panel p-5">
        <h2 className="font-display text-[21px]">Precio y vigencia</h2>
        <div className="mt-5 flex flex-col gap-4 border-t border-borde-fila pt-5">
          <Campo label="Precio" value={precio} onChange={setPrecio} type="number" min="0" step="0.01" prefix="Q" required />
          <label className="flex flex-col gap-1.5 text-sm font-semibold">Vigencia<select className="rounded-base border border-borde bg-white px-3.5 py-3 font-normal" value={vigencia} onChange={e => setVigencia(e.target.value)}>{[30, 60, 90, 180, 365].map(dias => <option key={dias} value={dias}>{dias} días</option>)}</select></label>
        </div>
        {(error || errorDesactivar) && <p role="alert" className="mt-4 text-sm text-red-700">{error || 'No se pudo desactivar el plan.'}</p>}
        <div className="mt-6 flex flex-col gap-3 border-t border-borde-fila pt-5"><Boton type="submit" disabled={mutacion.isPending}>{mutacion.isPending ? 'Guardando…' : original ? 'Guardar plan' : 'Crear plan'}</Boton>
          {original?.activo && desactivar && <Boton type="button" principal={false} onClick={() => void desactivar()}>Desactivar plan</Boton>}
          <button type="button" onClick={cancelar} className="text-center text-sm text-principal hover:underline">Volver a los planes</button></div>
      </section>
    </form>
  </div>;
}

function Campo({ label, value, onChange, type = 'text', min, step, placeholder, required, multiline, prefix, suffix }: { label: string; value: string; onChange: (value: string) => void; type?: string; min?: string; step?: string; placeholder?: string; required?: boolean; multiline?: boolean; prefix?: string; suffix?: string }) {
  const id = `campo-${label.toLowerCase().replaceAll(' ', '-')}`;
  const common = { id, 'aria-label': label, value, onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => onChange(e.target.value), required, placeholder, className: 'w-full rounded-base border border-borde bg-white px-3.5 py-3 text-[15px] font-normal' };
  return <label htmlFor={id} className="flex flex-col gap-1.5 text-[13px] font-semibold">{label}{multiline ? <textarea {...common} rows={3} /> : <span className="flex items-center rounded-base border border-borde bg-white px-3.5"><span>{prefix}</span><input {...common} type={type} min={min} step={step} className="w-full border-0 px-2 py-3 text-[15px] font-normal outline-none" /><span className="text-xs text-tinta-suave">{suffix}</span></span>}</label>;
}
