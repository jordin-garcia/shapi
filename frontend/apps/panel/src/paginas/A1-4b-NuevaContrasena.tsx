import { useRef, useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router';
import { Boton } from '@shapi/ui';
import { AvisoError, CampoEtiquetado, Encabezado, MarcoAcceso } from '../modulos/identidad/Formularios';
import { interpretarError, restablecer, useIrAlDestino, type ErrorFormulario } from '../modulos/identidad/useIdentidad';

export default function PaginaA14bNuevaContrasena() {
  const [parametros] = useSearchParams();
  const token = parametros.get('token') ?? '';
  const correo = parametros.get('correo') ?? 'su cuenta';

  const irAlDestino = useIrAlDestino();
  const formulario = useRef<HTMLFormElement>(null);

  const [contrasena, setContrasena] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<ErrorFormulario | null>(null);

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      await restablecer(token, contrasena);
      irAlDestino();
    } catch (causa) {
      setError(interpretarError(causa));
      setEnviando(false);
    }
  }

  const errorContrasena = error?.errores?.contrasena?.[0];

  return (
    <MarcoAcceso>

      <Encabezado rotulo="Recuperación · paso 2 de 2" titulo="Definir la contraseña">
        <p className="text-[15px] leading-[1.55] text-tinta-suave m-0">
          Está definiendo la contraseña de <span className="text-tinta font-medium">{correo}</span>.
        </p>
      </Encabezado>

      {error && !errorContrasena && (
        <div className="mt-6">
          <AvisoError mensaje={error.mensaje} reintentar={error.codigo === null ? () => formulario.current?.requestSubmit() : undefined} />
        </div>
      )}

      <form ref={formulario} onSubmit={enviar} noValidate>
        <div className="flex flex-col gap-[7px] mt-8">
          <CampoEtiquetado
            etiqueta="Contraseña nueva"
            type="password"
            value={contrasena}
            onChange={(e) => setContrasena(e.target.value)}
            error={errorContrasena}
            className="tracking-[0.18em]"
          />
        </div>

        <div className="mt-6">
          <Boton type="submit" className="w-full" deshabilitado={enviando}>Guardar la contraseña</Boton>
        </div>

        <div className="bg-fondo border border-borde rounded-base py-[14px] px-4 mt-6">
          <p className="text-sm leading-[1.5] text-tinta-suave m-0">
            Este enlace es de un solo uso: al guardar la contraseña deja de servir. Vence <span className="text-tinta font-medium tabular-nums">60 minutos</span> después de haberse enviado. Al guardarla se cierran las sesiones abiertas en otros equipos.
          </p>
        </div>
      </form>
    </MarcoAcceso>
  );
}
