import { useState, type FormEvent } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/organizaciones';
import { Aviso, Boton, EstadoCargando } from '@shapi/ui';
import { CampoEtiquetado, Encabezado, MarcoAcceso } from '../modulos/identidad/Formularios';

const cliente = crearCliente<paths>(window.location.origin);
type Invitacion = components['schemas']['InvitacionMiembro'];

export default function PaginaA82Invitacion() {
  const [parametros] = useSearchParams();
  const token = parametros.get('token') ?? '';
  const [nombre, setNombre] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [error, setError] = useState('');
  const [aceptada, setAceptada] = useState(false);

  const consulta = useQuery({
    queryKey: ['invitacion-miembro', token],
    enabled: token.length > 0,
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/invitaciones/{token}', { params: { path: { token } }, signal });
      if (!response.ok || !data) throw new Error('La invitación venció, ya se usó o no existe.');
      return data;
    },
    retry: false,
  });

  const aceptar = useMutation({
    mutationFn: async (cuerpo: components['schemas']['PeticionAceptarInvitacionMiembro']) => {
      const { response } = await cliente.POST('/api/invitaciones/{token}/aceptar', { params: { path: { token } }, body: cuerpo });
      if (!response.ok) throw new Error('No se pudo aceptar la invitación.');
    },
    onSuccess: () => { setError(''); setAceptada(true); },
    onError: (causa: Error) => {
      setError(causa instanceof ErrorApi && causa.codigo === 'correo_en_otra_organizacion'
        ? 'Este correo ya pertenece a otra organización.'
        : 'No se pudo aceptar la invitación. Revise los datos e inténtelo de nuevo.');
    },
  });

  if (!token || consulta.isError) {
    return (
      <MarcoAcceso>
        <Encabezado rotulo="Invitación" titulo="El enlace ya no sirve">
          <p className="text-[15px] leading-relaxed text-tinta-suave">La invitación venció, ya se usó o no pertenece a este portal.</p>
        </Encabezado>
        <Link className="mt-7 inline-block text-principal hover:underline" to="/entrar">Ir a entrar</Link>
      </MarcoAcceso>
    );
  }
  if (consulta.isPending) return <MarcoAcceso><Encabezado rotulo="Invitación" titulo="Cargando invitación" /><EstadoCargando /></MarcoAcceso>;
  const invitacion = consulta.data as Invitacion;

  if (aceptada) {
    return (
      <MarcoAcceso>
        <Encabezado rotulo="Invitación" titulo="Ya es parte de la organización">
          <p className="text-[15px] leading-relaxed text-tinta-suave">Se aceptó la invitación a {invitacion.organizacion}. Entre con su correo y la contraseña que acaba de crear.</p>
        </Encabezado>
        <Link className="mt-7 inline-block text-principal hover:underline" to="/entrar">Entrar a Shapi</Link>
      </MarcoAcceso>
    );
  }

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    setError('');
    aceptar.mutate({ nombre: nombre.trim(), contrasena });
  }

  const vencimiento = new Intl.DateTimeFormat('es-GT', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'America/Guatemala' }).format(new Date(invitacion.expiraEn));
  const rol = invitacion.rol === 'editor' ? 'editor' : 'lector';

  return (
    <MarcoAcceso>
      <Encabezado rotulo={`Invitación de ${invitacion.organizacion}`} titulo={`Unirse a ${invitacion.organizacion}`}>
        <p className="text-[15px] leading-relaxed text-tinta-suave">{invitacion.nombrePropietario} lo invitó a administrar las APIs de su organización en Shapi con el rol de <strong className="font-medium text-tinta">{rol}</strong>.</p>
      </Encabezado>
      <form onSubmit={enviar} noValidate>
        <div className="mt-8 flex flex-col gap-5">
          <div className="flex flex-col gap-2">
            <label className="text-[13px] font-semibold" htmlFor="correo-invitacion">Correo electrónico</label>
            <input id="correo-invitacion" value={invitacion.correo} disabled className="rounded-lg border border-borde bg-fondo px-3.5 py-3 text-[15px] text-tinta-suave" />
            <p className="m-0 text-[13px] leading-relaxed text-tinta-suave">Es el correo al que llegó la invitación.</p>
          </div>
          <CampoEtiquetado etiqueta="Nombre" value={nombre} onChange={evento => setNombre(evento.target.value)} autoComplete="name" />
          <CampoEtiquetado etiqueta="Contraseña" type="password" value={contrasena} onChange={evento => setContrasena(evento.target.value)} autoComplete="new-password" />
        </div>
        {error && <div className="mt-6"><Aviso estado="error">{error}</Aviso></div>}
        <div className="mt-8 flex flex-col gap-5">
          <Boton type="submit" className="w-full" deshabilitado={aceptar.isPending || !nombre.trim() || contrasena.length < 10}>
            {aceptar.isPending ? 'Aceptando…' : 'Aceptar la invitación'}
          </Boton>
          <p className="m-0 text-center text-sm text-tinta-suave">La invitación vence el {vencimiento}.</p>
        </div>
      </form>
    </MarcoAcceso>
  );
}
