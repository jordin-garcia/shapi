import { useRef, useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { Boton } from '@shapi/ui';
import { AvisoError, CampoEtiquetado, Encabezado, MarcoAcceso } from '../modulos/identidad/Formularios';
import { entrar, interpretarError, useIrAlDestino, type ErrorFormulario } from '../modulos/identidad/useIdentidad';

// A1.3 · Inicio de sesión del personal (RF-04). Después de entrar, cada rol va a su destino (10 §1).
export default function PaginaA13Sesion() {
  const irAlDestino = useIrAlDestino();
  const formulario = useRef<HTMLFormElement>(null);
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<ErrorFormulario | null>(null);

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      await entrar(correo, contrasena);
      await irAlDestino();
    } catch (causa) {
      // Credenciales incorrectas, cuenta bloqueada o desactivada: se muestra el mensaje genérico de la API (10 §1).
      setError(interpretarError(causa));
      setEnviando(false);
    }
  }

  return (
    <MarcoAcceso ancho={460}>
      <Encabezado rotulo="Acceso" titulo="Entrar a Shapi" />

      {error && (
        <div className="mt-6">
          <AvisoError mensaje={error.mensaje} reintentar={error.codigo === null ? () => formulario.current?.requestSubmit() : undefined} />
        </div>
      )}

      <form ref={formulario} onSubmit={enviar} noValidate>
        <div className="flex flex-col gap-5 mt-8">
          <CampoEtiquetado etiqueta="Correo electrónico" type="email" value={correo} onChange={e => setCorreo(e.target.value)} autoComplete="email" />
          <CampoEtiquetado etiqueta="Contraseña" type="password" value={contrasena} onChange={e => setContrasena(e.target.value)}
            autoComplete="current-password" style={contrasena ? { letterSpacing: '0.18em' } : undefined} />
          <div className="flex justify-end">
            <Link to="/recuperar" className="text-sm text-principal hover:text-principal-hover no-underline hover:underline">¿Olvidó su contraseña?</Link>
          </div>
        </div>

        <div className="mt-7">
          <Boton type="submit" deshabilitado={enviando} className="w-full">Entrar</Boton>
        </div>
      </form>
    </MarcoAcceso>
  );
}
