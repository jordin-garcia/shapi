import { useRef, useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { Boton, Campo } from '@shapi/ui';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';
import { AvisoError, EncabezadoAcceso, MarcoAcceso, RotuloPortal } from '../modulos/sesion/FormulariosAcceso';
import { interpretarError, registrarConsumidor, type ErrorFormulario, type RegistroConsumidor } from '../modulos/sesion/useIdentidadConsumidor';

export default function PaginaA53Registro() {
  const marca = useMarcaPortal();
  const navegar = useNavigate();
  const formulario = useRef<HTMLFormElement>(null);
  const [datos, setDatos] = useState<RegistroConsumidor>({ nombre: '', correo: '', nombreEmpresa: '', contrasena: '' });
  const [error, setError] = useState<ErrorFormulario | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      await registrarConsumidor(datos);
      await navegar(`/verificar-correo?correo=${encodeURIComponent(datos.correo)}`);
    } catch (causa) {
      setError(interpretarError(causa));
    } finally {
      setEnviando(false);
    }
  }

  const cambiar = (campo: keyof RegistroConsumidor) => (e: ChangeEvent<HTMLInputElement>) => setDatos({ ...datos, [campo]: e.target.value });
  const errorDe = (campo: keyof RegistroConsumidor) => error?.errores[campo]?.[0];
  const porCampo = Object.keys(error?.errores ?? {}).length > 0;

  return (
    <MarcoAcceso>
      <EncabezadoAcceso rotulo={<RotuloPortal />} titulo="Crear una cuenta">
        <p className="text-[15px] leading-[1.55] text-tinta-suave">
          Su cuenta pertenece al portal de {marca.nombrePortal}. Con ella contrata sus planes y obtiene sus claves de la API de {marca.nombreApi}.
        </p>
      </EncabezadoAcceso>
      {error && !porCampo && (
        <div className="mt-6">
          <AvisoError
            mensaje={error.mensaje}
            reintentar={error.codigo === null ? () => formulario.current?.requestSubmit() : undefined}
          />
        </div>
      )}
      <form ref={formulario} onSubmit={enviar} noValidate className="mt-8 flex flex-col gap-5">
        <Campo etiqueta="Nombre" value={datos.nombre} onChange={cambiar('nombre')} error={errorDe('nombre')} autoComplete="name" />
        <Campo
          etiqueta="Correo electrónico"
          type="email"
          value={datos.correo}
          onChange={cambiar('correo')}
          error={errorDe('correo')}
          autoComplete="email"
        />
        <Campo
          etiqueta="Nombre de la empresa"
          value={datos.nombreEmpresa}
          onChange={cambiar('nombreEmpresa')}
          error={errorDe('nombreEmpresa')}
          autoComplete="organization"
        />
        <Campo
          etiqueta="Contraseña"
          type="password"
          value={datos.contrasena}
          onChange={cambiar('contrasena')}
          error={errorDe('contrasena')}
          autoComplete="new-password"
        />
        <Boton className="mt-3 w-full" type="submit" deshabilitado={enviando}>
          Crear cuenta
        </Boton>
        <p className="text-center text-sm text-tinta-suave">
          ¿Ya tiene una cuenta? <Link className="text-[var(--marca-principal)]" to="/entrar">Entrar</Link>
        </p>
      </form>
    </MarcoAcceso>
  );
}
