import { useRef, useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { Boton, Campo } from '@shapi/ui';
import { AvisoError, EncabezadoAcceso, MarcoAcceso, RotuloPortal } from '../modulos/sesion/FormulariosAcceso';
import { entrarConsumidor, interpretarError, useIrAlDestinoConsumidor, type ErrorFormulario } from '../modulos/sesion/useIdentidadConsumidor';

export default function PaginaA57Acceso() {
  const irAlDestino = useIrAlDestinoConsumidor();
  const formulario = useRef<HTMLFormElement>(null);
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [error, setError] = useState<ErrorFormulario | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      await entrarConsumidor(correo, contrasena);
      await irAlDestino();
    } catch (causa) {
      setError(interpretarError(causa));
      setEnviando(false);
    }
  }

  return (
    <MarcoAcceso ancho={460}>
      <EncabezadoAcceso rotulo={<RotuloPortal />} titulo="Entrar">
        <p className="text-[15px] leading-[1.55] text-tinta-suave">Use el correo y la contraseña de su cuenta de este portal.</p>
      </EncabezadoAcceso>
      {error && (
        <div className="mt-6">
          <AvisoError
            mensaje={error.mensaje}
            reintentar={error.codigo === null ? () => formulario.current?.requestSubmit() : undefined}
          />
        </div>
      )}
      <form ref={formulario} onSubmit={enviar} noValidate className="mt-8 flex flex-col gap-5">
        <Campo etiqueta="Correo electrónico" type="email" value={correo} onChange={e => setCorreo(e.target.value)} autoComplete="email" />
        <div>
          <div className="mb-[6px] flex items-baseline justify-between">
            <span className="text-[13px] font-semibold">Contraseña</span>
            <Link className="text-sm text-[var(--marca-principal)]" to="/recuperar">¿Olvidó su contraseña?</Link>
          </div>
          <Campo
            aria-label="Contraseña"
            type="password"
            value={contrasena}
            onChange={e => setContrasena(e.target.value)}
            autoComplete="current-password"
          />
        </div>
        <Boton className="mt-3 w-full" type="submit" deshabilitado={enviando}>
          Entrar
        </Boton>
        <p className="text-center text-sm text-tinta-suave">
          ¿No tiene una cuenta? <Link className="text-[var(--marca-principal)]" to="/registro">Crear cuenta</Link>
        </p>
      </form>
    </MarcoAcceso>
  );
}
