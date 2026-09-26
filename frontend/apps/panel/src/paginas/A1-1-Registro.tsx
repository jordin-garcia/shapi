import { useRef, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { Boton } from '@shapi/ui';
import { AvisoError, CampoEtiquetado, Encabezado, MarcoAcceso } from '../modulos/identidad/Formularios';
import { interpretarError, registrar, type DatosRegistro, type ErrorFormulario } from '../modulos/identidad/useIdentidad';

const CAMPOS = ['nombre', 'correo', 'organizacion', 'contrasena'] as const;

// A1.1 · Registro del proveedor (RF-01).
export default function PaginaA11Registro() {
  const navegar = useNavigate();
  const formulario = useRef<HTMLFormElement>(null);
  const [datos, setDatos] = useState<DatosRegistro>({ nombre: '', correo: '', organizacion: '', contrasena: '' });
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<ErrorFormulario | null>(null);

  const cambiar = (campo: keyof DatosRegistro) => (e: { target: { value: string } }) =>
    setDatos(actual => ({ ...actual, [campo]: e.target.value }));

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    setError(null);
    try {
      await registrar(datos);
      await navegar(`/verificar-correo?correo=${encodeURIComponent(datos.correo)}`);
    } catch (causa) {
      const interpretado = interpretarError(causa);
      // El correo repetido se muestra debajo del campo de correo, como los errores de validación.
      if (interpretado.codigo === 'correo_ya_registrado') interpretado.errores = { correo: [interpretado.mensaje] };
      setError(interpretado);
    } finally {
      setEnviando(false);
    }
  }

  const errorDe = (campo: (typeof CAMPOS)[number]) => error?.errores[campo]?.[0];
  const hayErrorPorCampo = CAMPOS.some(campo => errorDe(campo));

  return (
    <MarcoAcceso>
      <Encabezado rotulo="Cuenta de proveedor" titulo="Crear una cuenta">
        <p className="text-[15px] leading-[1.55] text-tinta-suave m-0">
          Con estos datos se crea su organización en Shapi. Queda en el plan Prueba, sin costo y sin pedirle tarjeta.
        </p>
      </Encabezado>

      {error && !hayErrorPorCampo && (
        <div className="mt-6">
          <AvisoError mensaje={error.mensaje} reintentar={error.codigo === null ? () => formulario.current?.requestSubmit() : undefined} />
        </div>
      )}

      <form ref={formulario} onSubmit={enviar} noValidate>
        <div className="flex flex-col gap-5 mt-8">
          <CampoEtiquetado etiqueta="Nombre" value={datos.nombre} onChange={cambiar('nombre')} error={errorDe('nombre')} autoComplete="name" />
          <CampoEtiquetado etiqueta="Correo electrónico" type="email" value={datos.correo} onChange={cambiar('correo')} error={errorDe('correo')} autoComplete="email" />
          <CampoEtiquetado etiqueta="Nombre de la organización" value={datos.organizacion} onChange={cambiar('organizacion')} error={errorDe('organizacion')} autoComplete="organization" />
          <CampoEtiquetado etiqueta="Contraseña" type="password" value={datos.contrasena} onChange={cambiar('contrasena')} error={errorDe('contrasena')}
            autoComplete="new-password" className={datos.contrasena ? 'tracking-[0.18em]' : ''} />
        </div>

        <div className="flex flex-col gap-5 mt-8">
          <Boton type="submit" deshabilitado={enviando} className="w-full">Crear cuenta</Boton>
          <p className="text-sm text-tinta-suave text-center m-0">
            ¿Ya tiene una cuenta? <Link to="/entrar" className="text-principal hover:text-principal-hover no-underline hover:underline">Entrar</Link>
          </p>
        </div>
      </form>
    </MarcoAcceso>
  );
}
