import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { paths, components } from '@shapi/api/administracion';
import { Boton, EstadoCargando, EstadoError, Selector, Toast } from '@shapi/ui';
import { CampoEtiquetado, AvisoError } from '../modulos/identidad/Formularios';
import { useSesion } from '../modulos/sesion/useSesion';

const cliente = crearCliente<paths>(window.location.origin);
type Cuenta = components['schemas']['CuentaPlataforma'];
const opciones = [{ etiqueta: 'Administrador', valor: 'administrador' }, { etiqueta: 'Soporte', valor: 'soporte' }];
const clave = ['admin', 'cuentas'] as const;

export default function PaginaA65Cuentas() {
  const cache = useQueryClient();
  const { data: sesion } = useSesion();
  const consulta = useQuery({ queryKey: clave, queryFn: async () => {
    const respuesta = await cliente.GET('/api/admin/cuentas');
    if (!respuesta.data) throw new Error('No se pudieron consultar las cuentas.');
    return respuesta.data;
  } });
  const [nombre, setNombre] = useState('');
  const [correo, setCorreo] = useState('');
  const [rol, setRol] = useState<Cuenta['rol']>('soporte');
  const [aviso, setAviso] = useState('');
  const crear = useMutation({
    mutationFn: async () => {
      const { response } = await cliente.POST('/api/admin/cuentas', { body: { nombre, correo, rol }, params: { header: { 'X-Requested-With': 'shapi' } } });
      if (!response.ok) throw new Error('No se pudo crear la cuenta.');
    },
    onSuccess: async () => {
      setNombre(''); setCorreo(''); setRol('soporte');
      setAviso('Cuenta creada. Le enviamos un enlace para definir su contraseña.');
      await cache.invalidateQueries({ queryKey: clave });
    },
  });
  const cambiar = useMutation({
    mutationFn: async (cuenta: Cuenta) => {
      const ruta = cuenta.estado === 'activa' ? '/api/admin/cuentas/{id}/desactivar' : '/api/admin/cuentas/{id}/activar';
      const { response } = await cliente.POST(ruta, { params: { path: { id: cuenta.id }, header: { 'X-Requested-With': 'shapi' } } });
      if (!response.ok) throw new Error('No se pudo cambiar el estado de la cuenta.');
    },
    onSuccess: async () => {
      setAviso('Estado de la cuenta actualizado.');
      await cache.invalidateQueries({ queryKey: clave });
    },
  });
  function enviar(e: FormEvent) { e.preventDefault(); setAviso(''); crear.mutate(); }
  const error = crear.error instanceof ErrorApi ? crear.error : null;
  const propia = (cuenta: Cuenta) => cuenta.correo === sesion?.correo;
  const vacia = consulta.data?.length === 0 || (consulta.data?.length === 1 && propia(consulta.data[0]));

  return <div>
    {aviso && <Toast key={aviso}>{aviso}</Toast>}
    <header className="flex flex-col gap-2.5">
      <p className="text-etiqueta uppercase tracking-[.16em] text-tinta-suave">Sistema</p>
      <h1 className="font-display text-[32px] leading-[1.2]">Cuentas de administración y soporte</h1>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">Personal de Shapi con acceso a la plataforma y el rol de cada persona.</p>
    </header>
    <div className="mt-8 grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_440px]">
      <section className="rounded-base border border-borde bg-panel p-5" aria-label="Cuentas de plataforma">
        {consulta.isPending && <EstadoCargando />}
        {consulta.isError && <EstadoError reintentar={() => void consulta.refetch()} />}
        {cambiar.isError && <AvisoError mensaje={cambiar.error instanceof ErrorApi ? cambiar.error.titulo : 'No se pudo cambiar el estado de la cuenta.'} reintentar={() => cambiar.variables && cambiar.mutate(cambiar.variables)} />}
        {consulta.data && <>
          <div className="overflow-auto"><table className="w-full border-collapse text-left text-[15px]">
            <thead><tr>{['Persona', 'Rol', 'Estado', 'Acción'].map(t => <th key={t} className={`border-b border-borde pb-2.5 text-encabezado uppercase text-tinta-suave ${t === 'Acción' ? 'text-right' : 'pr-4'}`}>{t}</th>)}</tr></thead>
            <tbody>{consulta.data.map(cuenta => <tr key={cuenta.id} className="border-b border-borde-fila last:border-b-0">
              <td className="py-3 pr-4"><div className="flex flex-col gap-0.5"><span className="font-medium">{cuenta.nombre} {propia(cuenta) && <span className="font-normal text-tinta-suave">(usted)</span>}</span><span className="text-[14px] text-tinta-suave">{cuenta.correo}</span></div></td>
              <td className="py-3 pr-4">{propia(cuenta) ? (cuenta.rol === 'administrador' ? 'Administrador' : 'Soporte') : <Selector aria-label={`Rol de ${cuenta.nombre}`} className="w-[158px]" opciones={opciones} value={cuenta.rol} disabled />}</td>
              <td className="py-3 pr-4"><span className={`inline-block rounded-base border px-2.5 py-1 text-[12px] font-semibold uppercase tracking-[.06em] ${cuenta.estado === 'activa' ? 'border-correcto-borde bg-correcto-fondo text-correcto-base' : 'border-borde bg-fondo text-tinta-suave'}`}>{cuenta.estado === 'activa' ? 'Activa' : 'Desactivada'}</span></td>
              <td className="py-3 text-right">{sesion && !propia(cuenta) && <button type="button" disabled={cambiar.isPending} onClick={() => { setAviso(''); cambiar.mutate(cuenta); }} aria-label={`${cuenta.estado === 'activa' ? 'Desactivar' : 'Activar'} ${cuenta.nombre}`} className="font-medium text-principal hover:underline disabled:opacity-50">{cuenta.estado === 'activa' ? 'Desactivar' : 'Activar'}</button>}</td>
            </tr>)}</tbody>
          </table></div>
          {vacia && <div className="flex flex-col items-center gap-2.5 px-4 py-12 text-center">
            <svg width="40" height="40" viewBox="0 0 40 40" fill="none" aria-hidden="true" className="mb-3 text-principal"><rect x="1" y="1" width="38" height="38" rx="8" stroke="currentColor" strokeDasharray="4 4" /><circle cx="20" cy="15" r="4" stroke="currentColor" /><path d="M12 29v-3a8 8 0 0 1 16 0v3" stroke="currentColor" /></svg>
            <h2 className="font-display text-[22px] leading-[1.3]">Todavía no ha creado ninguna cuenta</h2>
            <p className="max-w-[520px] text-[15px] leading-[1.55] text-tinta-suave">Su cuenta se creó en la siembra inicial del sistema. Las cuentas de administración y de soporte que cree aparecerán aquí con su rol.</p>
          </div>}
        </>}
      </section>
      <form onSubmit={enviar} noValidate className="rounded-base border border-borde bg-panel p-5">
        <h2 className="font-display text-[21px] leading-[1.3]">Crear cuenta</h2>
        {crear.isError && !error?.errores && <div className="mt-5"><AvisoError mensaje={error?.titulo ?? 'No se pudo crear la cuenta.'} reintentar={() => crear.mutate()} /></div>}
        <div className="mt-5 flex flex-col gap-[18px] border-t border-borde-fila pt-5">
          <CampoEtiquetado etiqueta="Nombre" placeholder="Nombre y apellido" value={nombre} onChange={e => setNombre(e.target.value)} error={error?.errores?.nombre?.[0]} />
          <CampoEtiquetado etiqueta="Correo" type="email" placeholder="nombre@shapi.localhost" value={correo} onChange={e => setCorreo(e.target.value)} error={error?.errores?.correo?.[0]} />
          <div className="flex flex-col gap-[7px]"><label htmlFor="rol-cuenta" className="text-[13px] font-semibold">Rol</label><Selector id="rol-cuenta" opciones={opciones} value={rol} onChange={e => setRol(e.target.value as Cuenta['rol'])} />{error?.errores?.rol?.[0] && <p className="text-sm text-alerta">{error.errores.rol[0]}</p>}</div>
        </div>
        <p className="mt-5 text-[14px] leading-[1.5] text-tinta-suave">Le enviaremos un enlace a ese correo para que defina su contraseña. Estas cuentas no se crean desde el registro público.</p>
        <Boton type="submit" className="mt-6 w-full" deshabilitado={crear.isPending}>Crear cuenta</Boton>
      </form>
    </div>
  </div>;
}
