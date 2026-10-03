import { useRef, useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { Boton, Campo } from '@shapi/ui';
import { AvisoError, EncabezadoAcceso, MarcoAcceso } from '../modulos/sesion/FormulariosAcceso';
import { interpretarError, recuperarConsumidor, type ErrorFormulario } from '../modulos/sesion/useIdentidadConsumidor';

export default function PaginaA59Recuperacion() {
  const formulario = useRef<HTMLFormElement>(null);
  const [correo, setCorreo] = useState('');
  const [enviado, setEnviado] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<ErrorFormulario | null>(null);

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      await recuperarConsumidor(correo);
      setEnviado(true);
    } catch (causa) {
      setError(interpretarError(causa));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <MarcoAcceso ancho={460}>
      <EncabezadoAcceso
        rotulo={enviado ? 'Recuperación · revise su correo' : 'Recuperación · paso 1 de 2'}
        titulo={enviado ? 'Revise su correo' : 'Recuperar la contraseña'}
      >
        <p className="text-[15px] leading-[1.55] text-tinta-suave">
          {enviado
            ? 'Si el correo tiene una cuenta en este portal, recibirá el enlace.'
            : 'Escriba el correo de su cuenta de este portal y le enviamos un enlace para definir una contraseña nueva.'}
        </p>
      </EncabezadoAcceso>
      {!enviado && (
        <form ref={formulario} onSubmit={enviar} noValidate className="mt-8 flex flex-col gap-5">
          {error && (
            <AvisoError
              mensaje={error.mensaje}
              reintentar={error.codigo === null ? () => formulario.current?.requestSubmit() : undefined}
            />
          )}
          <Campo etiqueta="Correo electrónico" type="email" value={correo} onChange={e => setCorreo(e.target.value)} autoComplete="email" />
          <Boton className="mt-3 w-full" type="submit" deshabilitado={enviando}>
            Enviar el enlace
          </Boton>
          <p className="text-center text-sm">
            <Link className="text-[var(--marca-principal)]" to="/entrar">Volver a entrar</Link>
          </p>
        </form>
      )}
      <div className="mt-6 rounded-base border border-borde bg-fondo px-4 py-[14px] text-sm text-tinta-suave">
        Si el correo tiene una cuenta en este portal, recibirá el enlace. Es de un solo uso y vence{' '}
        <span className="font-medium text-tinta">60 minutos</span> después de enviarse.
      </div>
    </MarcoAcceso>
  );
}
