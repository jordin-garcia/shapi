import { useRef, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { Boton, Campo, EstadoCargando } from '@shapi/ui';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';
import { AvisoError, EncabezadoAcceso, MarcoAcceso, RotuloPortal } from '../modulos/sesion/FormulariosAcceso';
import {
  aceptarInvitacion,
  consultarInvitacion,
  interpretarError,
  useIrAlDestinoConsumidor,
  type ErrorFormulario,
} from '../modulos/sesion/useIdentidadConsumidor';

export default function PaginaA53bInvitacion() {
  const [parametros] = useSearchParams();
  const token = parametros.get('token') ?? '';
  const marca = useMarcaPortal();
  const irAlDestino = useIrAlDestinoConsumidor();
  const formulario = useRef<HTMLFormElement>(null);
  const invitacion = useQuery({
    queryKey: ['portal', 'invitacion', token],
    queryFn: () => consultarInvitacion(token),
    enabled: token.length > 0,
    retry: false,
  });
  const [nombre, setNombre] = useState('');
  const [nombreEmpresa, setNombreEmpresa] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [error, setError] = useState<ErrorFormulario | null>(null);
  const [enviando, setEnviando] = useState(false);
  // El token es de un solo uso: si la invitación ya se aceptó y solo falló la consulta del destino, el reintento no la
  // vuelve a aceptar (el backend respondería token_invalido aunque la cuenta ya exista).
  const aceptada = useRef(false);
  // Con errores por campo, cada uno se muestra en su campo y no hace falta el aviso general (como en A5.3).
  const porCampo = Object.keys(error?.errores ?? {}).length > 0;

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      if (!aceptada.current) {
        await aceptarInvitacion(token, { nombre, nombreEmpresa, contrasena });
        aceptada.current = true;
      }
      await irAlDestino();
    } catch (causa) {
      setError(interpretarError(causa));
      setEnviando(false);
    }
  }

  if (invitacion.isPending && token) {
    return (
      <MarcoAcceso>
        <EstadoCargando />
      </MarcoAcceso>
    );
  }
  if (!token) return <InvitacionNoValida />;
  if (invitacion.isError) {
    const interpretado = interpretarError(invitacion.error);
    if (interpretado.codigo === 'token_invalido') return <InvitacionNoValida />;
    return (
      <MarcoAcceso>
        <EncabezadoAcceso rotulo="Invitación" titulo="No se pudo consultar la invitación" />
        <div className="mt-6">
          <AvisoError mensaje={interpretado.mensaje} reintentar={() => void invitacion.refetch()} />
        </div>
      </MarcoAcceso>
    );
  }
  // La invitación pudo usarse o vencer mientras se llenaba el formulario (por ejemplo, en otra pestaña).
  if (!invitacion.data || error?.codigo === 'token_invalido') return <InvitacionNoValida />;

  return (
    <MarcoAcceso>
      <EncabezadoAcceso rotulo={<RotuloPortal invitacion />} titulo="Crear una cuenta">
        <p className="text-[15px] leading-[1.55] text-tinta-suave">
          {marca.nombrePortal} lo invitó a su portal. Su cuenta pertenece a ese portal: con ella contrata planes y obtiene claves de su API.
        </p>
      </EncabezadoAcceso>
      {error && !porCampo && error.codigo !== 'correo_ya_registrado' && (
        <div className="mt-6">
          <AvisoError
            mensaje={error.mensaje}
            reintentar={error.codigo === null ? () => formulario.current?.requestSubmit() : undefined}
          />
        </div>
      )}
      {error?.codigo === 'correo_ya_registrado' && (
        // CU-11 2a: el correo ya tiene una cuenta en este portal; se ofrece entrar o recuperar la contraseña.
        <div className="mt-6">
          <AvisoError mensaje={error.mensaje} />
          <p className="mt-3 flex gap-6 text-sm">
            <Link className="font-medium text-[var(--marca-principal)]" to="/entrar">Entrar</Link>
            <Link className="font-medium text-[var(--marca-principal)]" to="/recuperar">Recuperar la contraseña</Link>
          </p>
        </div>
      )}
      <form ref={formulario} onSubmit={enviar} noValidate className="mt-8 flex flex-col gap-5">
        <Campo
          etiqueta="Nombre"
          value={nombre}
          onChange={e => setNombre(e.target.value)}
          error={error?.errores.nombre?.[0]}
          autoComplete="name"
        />
        <Campo
          etiqueta="Correo electrónico"
          value={invitacion.data.correo}
          readOnly
          aria-describedby="correo-invitado"
          className="[&_input]:border-borde-inactivo [&_input]:bg-fondo [&_input]:text-tinta-suave"
        />
        <span id="correo-invitado" className="-mt-4 text-[13px] text-tinta-suave">Es el correo al que llegó la invitación.</span>
        <Campo
          etiqueta="Nombre de la empresa"
          value={nombreEmpresa}
          onChange={e => setNombreEmpresa(e.target.value)}
          error={error?.errores.nombreEmpresa?.[0]}
          autoComplete="organization"
        />
        <Campo
          etiqueta="Contraseña"
          type="password"
          value={contrasena}
          onChange={e => setContrasena(e.target.value)}
          error={error?.errores.contrasena?.[0]}
          autoComplete="new-password"
        />
        <Boton className="mt-3 w-full" type="submit" deshabilitado={enviando}>
          Aceptar la invitación y crear la cuenta
        </Boton>
        <p className="text-center text-sm text-tinta-suave">
          ¿Ya tiene una cuenta? <Link className="text-[var(--marca-principal)]" to="/entrar">Entrar</Link>
        </p>
      </form>
    </MarcoAcceso>
  );
}

function InvitacionNoValida() {
  return (
    <MarcoAcceso>
      <EncabezadoAcceso rotulo="Invitación" titulo="El enlace ya no sirve">
        <p className="text-tinta-suave">La invitación venció, ya se usó o no pertenece a este portal.</p>
      </EncabezadoAcceso>
    </MarcoAcceso>
  );
}
