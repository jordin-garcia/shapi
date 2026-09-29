import { useRef, useState, type FormEvent } from 'react';
import { Boton } from '@shapi/ui';
import { AvisoError, CampoEtiquetado, Encabezado, MarcoAcceso } from '../modulos/identidad/Formularios';
import { interpretarError, recuperar, type ErrorFormulario } from '../modulos/identidad/useIdentidad';

export default function PaginaA14aRecuperacion() {
  const formulario = useRef<HTMLFormElement>(null);
  const [correo, setCorreo] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<ErrorFormulario | null>(null);
  const [enviado, setEnviado] = useState(false);

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      await recuperar(correo);
      setEnviado(true);
    } catch (causa) {
      setError(interpretarError(causa));
    } finally {
      setEnviando(false);
    }
  }

  const errorCorreo = error?.errores?.correo?.[0];

  return (
    <MarcoAcceso>

      {enviado ? (
        <Encabezado rotulo="Recuperación · paso 2 de 2" titulo="Revise su correo">
          <p className="text-[15px] leading-[1.55] text-tinta-suave m-0">
            Le enviamos un enlace para definir su contraseña. El enlace es de un solo uso y vence en <span className="font-medium text-tinta tabular-nums">60 minutos</span>.
          </p>
        </Encabezado>
      ) : (
        <>
          <Encabezado rotulo="Recuperación · paso 1 de 2" titulo="Recuperar la contraseña">
            <p className="text-[15px] leading-[1.55] text-tinta-suave m-0">
              Escriba su correo y le enviamos un enlace para definir una contraseña nueva.
            </p>
          </Encabezado>

          {error && !errorCorreo && (
            <div className="mt-6">
              <AvisoError mensaje={error.mensaje} reintentar={error.codigo === null ? () => formulario.current?.requestSubmit() : undefined} />
            </div>
          )}

          <form ref={formulario} onSubmit={enviar} noValidate>
            <div className="flex flex-col gap-[7px] mt-8">
              <CampoEtiquetado
                etiqueta="Correo electrónico"
                type="email"
                value={correo}
                onChange={(e) => setCorreo(e.target.value)}
                error={errorCorreo}
                autoComplete="email"
              />
            </div>

            <div className="mt-6">
              <Boton type="submit" className="w-full" deshabilitado={enviando}>Enviar el enlace</Boton>
            </div>

            <div className="bg-fondo border border-borde rounded-base py-[14px] px-4 mt-6">
              <p className="text-sm leading-[1.5] text-tinta-suave m-0">
                El enlace es de un solo uso y vence <span className="text-tinta font-medium tabular-nums">60 minutos</span> después de enviarse.
              </p>
            </div>
          </form>
        </>
      )}
    </MarcoAcceso>
  );
}
