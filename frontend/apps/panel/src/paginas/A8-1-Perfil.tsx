import { useState, useRef, type FormEvent } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Boton, Campo, Toast } from '@shapi/ui';
import { editarPerfil, cambiarContrasena, interpretarError, type ErrorFormulario } from '../modulos/identidad/useIdentidad';
import { AvisoError } from '../modulos/identidad/Formularios';
import { useSesion, claveSesion } from '../modulos/sesion/useSesion';
import { useCerrarSesion } from '../modulos/sesion/useCerrarSesion';

function capitalize(s: string) {
  if (!s) return s;
  return s.charAt(0).toUpperCase() + s.slice(1);
}

/** Un error sin errores por campo se muestra arriba del formulario; si fue de red o del servidor, con «Reintentar». */
function sinErroresDeCampo(error: ErrorFormulario | null) {
  return error !== null && Object.keys(error.errores).length === 0;
}

export default function PaginaA81Perfil() {
  const { data: sesion } = useSesion();
  const cache = useQueryClient();
  const salir = useCerrarSesion();

  const formPerfil = useRef<HTMLFormElement>(null);
  const [nombre, setNombre] = useState(sesion?.nombre ?? '');
  const [enviandoPerfil, setEnviandoPerfil] = useState(false);
  const [errorPerfil, setErrorPerfil] = useState<ErrorFormulario | null>(null);

  const formContrasena = useRef<HTMLFormElement>(null);
  const [contrasenaActual, setContrasenaActual] = useState('');
  const [contrasenaNueva, setContrasenaNueva] = useState('');
  const [enviandoContrasena, setEnviandoContrasena] = useState(false);
  const [errorContrasena, setErrorContrasena] = useState<ErrorFormulario | null>(null);

  // 11 §4: la confirmación es un aviso breve de 4 s arriba a la derecha. La clave lo vuelve a montar en cada aviso.
  const [aviso, setAviso] = useState<{ texto: string; clave: number } | null>(null);
  const avisar = (texto: string) => setAviso({ texto, clave: Date.now() });

  if (!sesion) return null;

  async function guardarPerfil(e: FormEvent) {
    e.preventDefault();
    setEnviandoPerfil(true);
    setErrorPerfil(null);
    try {
      await editarPerfil(nombre);
      avisar('Nombre actualizado.');
      cache.invalidateQueries({ queryKey: claveSesion });
    } catch (causa) {
      setErrorPerfil(interpretarError(causa));
    } finally {
      setEnviandoPerfil(false);
    }
  }

  async function guardarContrasena(e: FormEvent) {
    e.preventDefault();
    setEnviandoContrasena(true);
    setErrorContrasena(null);
    try {
      await cambiarContrasena(contrasenaActual, contrasenaNueva);
      avisar('Contraseña actualizada. Se cerraron sus sesiones en otros equipos.');
      setContrasenaActual('');
      setContrasenaNueva('');
    } catch (causa) {
      setErrorContrasena(interpretarError(causa));
    } finally {
      setEnviandoContrasena(false);
    }
  }

  const errNom = errorPerfil?.errores?.nombre?.[0];
  const errContAct = errorContrasena?.errores?.contrasenaActual?.[0];
  const errContNue = errorContrasena?.errores?.contrasenaNueva?.[0];

  return (
    <div className="flex flex-col gap-[32px]">
      {aviso && <Toast key={aviso.clave}>{aviso.texto}</Toast>}

      <div className="flex flex-col gap-[10px]">
        <p className="text-xs tracking-[.16em] uppercase text-tinta-suave font-medium m-0">Cuenta</p>
        <h1 className="text-[32px] font-sora leading-[1.2] m-0 tracking-[-0.02em]">Mi perfil</h1>
        <p className="text-[15px] leading-[1.55] text-tinta-suave m-0">Sus datos de acceso a Shapi.</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 items-start">
        {/* Datos Personales */}
        <div className="bg-white border border-borde rounded-lg p-5 flex flex-col">
          <h2 className="font-sora text-[21px] leading-[1.3] m-0">Datos personales</h2>

          <form ref={formPerfil} onSubmit={guardarPerfil} noValidate>
            <div className="flex flex-col gap-[18px] border-t border-borde-fila mt-5 pt-5">

              {errorPerfil && sinErroresDeCampo(errorPerfil) && (
                <AvisoError mensaje={errorPerfil.mensaje} reintentar={errorPerfil.codigo === null ? () => formPerfil.current?.requestSubmit() : undefined} />
              )}

              <div className="flex flex-col gap-[7px]">
                <label htmlFor="nombre" className="text-[13px] font-semibold text-tinta">Nombre</label>
                <Campo id="nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} error={errNom} />
              </div>

              <div className="flex flex-col gap-[7px]">
                <label className="text-[13px] font-semibold text-tinta">Correo electrónico</label>
                <div className="border border-borde-inactivo rounded-lg py-[11px] px-[14px] text-[15px] leading-[1.5] box-border border-solid bg-fondo text-tinta-suave">
                  {sesion.correo}
                </div>
                <p className="text-[13px] leading-[1.5] text-tinta-suave m-0">Para cambiar su correo, abra un caso de soporte.</p>
              </div>
            </div>

            <div className="flex flex-col gap-[14px] border-t border-borde-fila mt-5 pt-5">
              <div className="flex items-baseline justify-between gap-5">
                <span className="text-[14px] text-tinta-suave">Organización</span>
                <span className="text-[15px] text-tinta font-medium tabular-nums">{sesion.nombreOrganizacion}</span>
              </div>
              <div className="flex items-baseline justify-between gap-5">
                <span className="text-[14px] text-tinta-suave">Rol</span>
                <span className="text-[15px] text-tinta font-medium tabular-nums">{capitalize(sesion.rol)}</span>
              </div>
            </div>

            <div className="flex mt-6">
              <Boton type="submit" deshabilitado={enviandoPerfil}>Guardar cambios</Boton>
            </div>
          </form>
        </div>

        {/* Contraseña */}
        <div className="bg-white border border-borde rounded-lg p-5 flex flex-col">
          <h2 className="font-sora text-[21px] leading-[1.3] m-0">Contraseña</h2>

          <form ref={formContrasena} onSubmit={guardarContrasena} noValidate>
            <div className="flex flex-col gap-[18px] border-t border-borde-fila mt-5 pt-5">
              {errorContrasena && sinErroresDeCampo(errorContrasena) && (
                <AvisoError mensaje={errorContrasena.mensaje} reintentar={errorContrasena.codigo === null ? () => formContrasena.current?.requestSubmit() : undefined} />
              )}

              <div className="flex flex-col gap-[7px]">
                <label htmlFor="contrasenaActual" className="text-[13px] font-semibold text-tinta">Contraseña actual</label>
                <Campo id="contrasenaActual" type="password" value={contrasenaActual} onChange={(e) => setContrasenaActual(e.target.value)} error={errContAct} className="tracking-[0.18em]" />
              </div>

              <div className="flex flex-col gap-[7px]">
                <label htmlFor="contrasenaNueva" className="text-[13px] font-semibold text-tinta">Contraseña nueva</label>
                <Campo id="contrasenaNueva" type="password" value={contrasenaNueva} onChange={(e) => setContrasenaNueva(e.target.value)} error={errContNue} className="tracking-[0.18em]" />
                <p className="text-[13px] leading-[1.5] text-tinta-suave m-0">Al menos 10 caracteres. Al cambiarla se cierran sus sesiones en otros equipos.</p>
              </div>
            </div>

            <div className="flex mt-6">
              <Boton type="submit" deshabilitado={enviandoContrasena}>Cambiar contraseña</Boton>
            </div>
          </form>

          <div className="flex items-center justify-between gap-4 border-t border-borde-fila mt-6 pt-5">
            <p className="text-[14px] leading-[1.5] text-tinta-suave m-0">Su sesión vence tras 8 horas sin actividad.</p>
            <Boton onClick={() => salir.mutate()} deshabilitado={salir.isPending} principal={false}>Cerrar sesión</Boton>
          </div>
        </div>

      </div>
    </div>
  );
}
