import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import { Aviso, Boton, Campo, EstadoCargando, EstadoError, Selector, Tabla, Tarjeta } from '@shapi/ui';
import type { components, paths } from '@shapi/api/organizaciones';

const cliente = crearCliente<paths>(window.location.origin);
type ListaMiembros = components['schemas']['ListaMiembros'];
type Miembro = components['schemas']['Miembro'];

export default function PaginaA42Miembros() {
  const cache = useQueryClient();
  const [correo, setCorreo] = useState('');
  const [rol, setRol] = useState<'editor' | 'lector'>('editor');
  const [error, setError] = useState('');
  const [quitar, setQuitar] = useState<Miembro | null>(null);
  const consulta = useQuery({
    queryKey: ['miembros'],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/miembros', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron cargar los miembros.');
      return data;
    },
    retry: false,
  });

  const refrescar = () => cache.invalidateQueries({ queryKey: ['miembros'] });
  const invitar = useMutation({
    mutationFn: async (cuerpo: components['schemas']['PeticionInvitarMiembro']) => {
      const { response } = await cliente.POST('/api/miembros/invitaciones', { body: cuerpo });
      if (!response.ok) throw new Error('No se pudo enviar la invitación.');
    },
    onSuccess: async () => { setCorreo(''); setError(''); await refrescar(); },
    onError: () => setError('No se pudo enviar la invitación. Revise el correo y el límite de su plan.'),
  });
  const cambiarRol = useMutation({
    mutationFn: async ({ id, rol: nuevoRol }: { id: string; rol: 'editor' | 'lector' }) => {
      const { response } = await cliente.PUT('/api/miembros/{id}/rol', { params: { path: { id } }, body: { rol: nuevoRol } });
      if (!response.ok) throw new Error('No se pudo cambiar el rol.');
    },
    onSuccess: () => { setError(''); void refrescar(); },
    onError: () => setError('No se pudo cambiar el rol. Inténtelo de nuevo.'),
  });
  const quitarMiembro = useMutation({
    mutationFn: async (id: string) => {
      const { response } = await cliente.DELETE('/api/miembros/{id}', { params: { path: { id } } });
      if (!response.ok) throw new Error('No se pudo quitar al miembro.');
    },
    onSuccess: async () => { setQuitar(null); setError(''); await refrescar(); },
    onError: () => setError('No se pudo quitar al miembro. Inténtelo de nuevo.'),
  });

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudieron cargar los miembros." reintentar={() => void consulta.refetch()} />;
  const datos = consulta.data as ListaMiembros;
  const plan = datos.plan.maxMiembros == null ? 'Miembros ilimitados' : datos.plan.maxMiembros.toLocaleString('es-GT');
  const descripcion = `Personas de ${datos.organizacion} con acceso a Shapi y el rol de cada una. Su plan ${datos.plan.nombre} permite ${plan}${datos.plan.maxMiembros == null ? '' : ' miembros'}; tiene ${datos.total.toLocaleString('es-GT')}.`;
  const limiteAlcanzado = datos.plan.maxMiembros != null && datos.total >= datos.plan.maxMiembros;

  if (quitar) {
    return (
      <div className="mx-auto w-full max-w-[600px] rounded-lg border border-borde bg-white p-8 sm:p-10">
        <p className="m-0 text-xs font-medium uppercase tracking-[0.16em] text-tinta-suave">Miembros y roles</p>
        <h1 className="mt-2 font-display text-[32px] leading-tight">Quitar a {quitar.nombre}</h1>
        <p className="mt-2 text-[15px] leading-relaxed text-tinta-suave">{quitar.nombre} dejará de tener acceso a {datos.organizacion} en Shapi.</p>
        <dl className="mt-8 flex flex-col gap-3 border-t border-borde pt-6 text-sm">
          <FilaDetalle etiqueta="Persona" valor={quitar.nombre} />
          <FilaDetalle etiqueta="Correo" valor={quitar.correo} />
          <FilaDetalle etiqueta="Rol" valor={quitar.rol === 'editor' ? 'Editor' : 'Lector'} />
        </dl>
        {error && <div className="mt-6"><Aviso estado="error">{error}</Aviso></div>}
        <div className="mt-8 flex flex-col gap-4 border-t border-borde pt-6">
          <Boton type="button" deshabilitado={quitarMiembro.isPending} onClick={() => quitarMiembro.mutate(quitar.id)}>Quitar miembro</Boton>
          <button type="button" className="text-sm text-principal hover:underline" onClick={() => setQuitar(null)}>Cancelar</button>
        </div>
      </div>
    );
  }

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    setError('');
    invitar.mutate({ correo: correo.trim(), rol });
  }

  const filas = datos.elementos.map(miembro => [
    <div key="persona" className="flex min-w-0 flex-col gap-0.5">
      <span className="font-medium">{miembro.nombre}{miembro.esActual ? ' (usted)' : ''}</span>
      <span className="text-sm text-tinta-suave">{miembro.correo}</span>
    </div>,
    miembro.rol === 'propietario'
      ? <span key="rol">Propietario</span>
      : <Selector key="rol" aria-label={`Rol de ${miembro.nombre}`} value={miembro.rol}
          onChange={evento => cambiarRol.mutate({ id: miembro.id, rol: evento.target.value as 'editor' | 'lector' })}
          opciones={[{ etiqueta: 'Editor', valor: 'editor' }, { etiqueta: 'Lector', valor: 'lector' }]} />,
    miembro.esActual ? null : <button key="accion" type="button" className="text-right font-medium text-principal hover:underline"
      onClick={() => { setError(''); setQuitar(miembro); }}>Quitar</button>,
  ]);

  return (
    <div>
      <div className="flex flex-col gap-2.5">
        <p className="m-0 text-xs font-medium uppercase tracking-[0.16em] text-tinta-suave">Organización</p>
        <h1 className="m-0 font-display text-[32px] leading-tight">Miembros y roles</h1>
        <p className="m-0 max-w-4xl text-[15px] leading-relaxed text-tinta-suave">{descripcion}</p>
      </div>

      <div className="mt-8 grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_440px]">
        <Tarjeta className="p-5">
          <Tabla encabezados={['Persona', 'Rol', 'Acción']} filas={filas} />
          {datos.elementos.length === 1 && datos.invitacionesPendientes === 0 && (
            <div className="flex flex-col items-center gap-6 px-4 pb-9 pt-13 text-center">
              <svg aria-hidden="true" width="40" height="40" viewBox="0 0 40 40" fill="none">
                <rect x="1" y="1" width="38" height="38" rx="8" stroke="var(--borde-campo)" strokeWidth="1.5" strokeDasharray="4 4" />
                <path d="M20 13 V27 M13 20 H27" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
              </svg>
              <div className="flex max-w-[440px] flex-col gap-2.5">
                <h2 className="m-0 font-display text-[22px]">Todavía no ha invitado a nadie</h2>
                <p className="m-0 text-[15px] leading-relaxed text-tinta-suave">Por ahora usted es la única persona de su organización. Las personas que invite aparecerán aquí con su rol.</p>
              </div>
            </div>
          )}
        </Tarjeta>

        <Tarjeta className="flex flex-col p-5">
          <h2 className="m-0 font-display text-[21px]">Invitar miembro</h2>
          <form onSubmit={enviar}>
            <div className="mt-5 flex flex-col gap-4 border-t border-borde pt-5">
              <Campo etiqueta="Correo" type="email" placeholder="nombre@enviosxelaju.com" value={correo} onChange={evento => setCorreo(evento.target.value)} required />
              <Selector aria-label="Rol" value={rol} onChange={evento => setRol(evento.target.value as 'editor' | 'lector')}
                opciones={[{ etiqueta: 'Editor', valor: 'editor' }, { etiqueta: 'Lector', valor: 'lector' }]} />
            </div>
            <p className="mb-0 mt-5 text-sm leading-relaxed text-tinta-suave">Le enviaremos un enlace a ese correo para unirse a {datos.organizacion}.</p>
            {error && <div className="mt-4"><Aviso estado="error">{error}</Aviso></div>}
            <Boton type="submit" className="mt-6 w-full" deshabilitado={invitar.isPending || limiteAlcanzado}>
              {invitar.isPending ? 'Enviando…' : 'Enviar invitación'}
            </Boton>
            {limiteAlcanzado && <p className="mb-0 mt-3 text-sm text-tinta-suave">Su plan llegó al límite de miembros.</p>}
          </form>
        </Tarjeta>
      </div>
    </div>
  );
}

function FilaDetalle({ etiqueta, valor }: { etiqueta: string; valor: string }) {
  return <div className="flex items-center justify-between gap-6"><dt className="text-tinta-suave">{etiqueta}</dt><dd className="m-0 text-right font-medium">{valor}</dd></div>;
}
