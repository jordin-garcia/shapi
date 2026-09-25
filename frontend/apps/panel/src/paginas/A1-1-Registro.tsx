import { useState } from 'react';
import { useNavigate, Link } from 'react-router';
import { Boton, Campo } from '@shapi/ui';
import { useRegistro } from '../modulos/identidad/useIdentidad';

export default function PaginaA11Registro() {
  const navigate = useNavigate();
  const registro = useRegistro();

  const [nombre, setNombre] = useState('');
  const [correo, setCorreo] = useState('');
  const [organizacion, setOrganizacion] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [errores, setErrores] = useState<Record<string, string[]>>({});

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrores({});
    try {
      await registro.mutateAsync({
        nombre,
        correo,
        organizacion,
        contrasena,
      });
      navigate(`/verificar-correo?correo=${encodeURIComponent(correo)}`);
    } catch (error: any) {
      if (error.details && error.details.errores) {
        setErrores(error.details.errores);
      } else {
        setErrores({ general: [error.message] });
      }
    }
  };

  return (
    <div className="flex-grow flex items-center justify-center py-11 px-20">
      <div className="w-[520px] bg-white border border-[var(--borde)] rounded-base p-10">
        <div className="flex flex-col gap-2">
          <p className="text-xs tracking-[.16em] uppercase text-tinta-suave font-medium m-0">Cuenta de proveedor</p>
          <h1 className="font-display text-[32px] leading-[1.2] m-0 tracking-[-0.02em]">Crear una cuenta</h1>
          <p className="text-[15px] leading-[1.55] text-tinta-suave m-0">
            Con estos datos se crea su organización en Shapi. Queda en el plan Prueba, sin costo y sin pedirle tarjeta.
          </p>
        </div>

        {errores.general && (
          <div className="mt-6 p-4 bg-alerta/10 text-alerta rounded-base text-sm">
            {errores.general[0]}
          </div>
        )}

        <form onSubmit={onSubmit} className="flex flex-col gap-5 mt-8">
          <div className="flex flex-col gap-[7px]">
            <span className="text-[13px] font-semibold text-tinta">Nombre</span>
            <Campo
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              error={errores.nombre?.[0]}
              autoComplete="name"
              required
            />
          </div>
          <div className="flex flex-col gap-[7px]">
            <span className="text-[13px] font-semibold text-tinta">Correo electrónico</span>
            <Campo
              type="email"
              value={correo}
              onChange={(e) => setCorreo(e.target.value)}
              error={errores.correo?.[0]}
              autoComplete="email"
              required
            />
          </div>
          <div className="flex flex-col gap-[7px]">
            <span className="text-[13px] font-semibold text-tinta">Nombre de la organización</span>
            <Campo
              value={organizacion}
              onChange={(e) => setOrganizacion(e.target.value)}
              error={errores.organizacion?.[0]}
              required
            />
          </div>
          <div className="flex flex-col gap-[7px]">
            <span className="text-[13px] font-semibold text-tinta">Contraseña</span>
            <Campo
              type="password"
              value={contrasena}
              onChange={(e) => setContrasena(e.target.value)}
              error={errores.contrasena?.[0]}
              className={contrasena ? 'tracking-[0.18em]' : ''}
              autoComplete="new-password"
              required
            />
          </div>

          <div className="flex flex-col gap-5 mt-3">
            <Boton type="submit" deshabilitado={registro.isPending} className="w-full">
              Crear cuenta
            </Boton>
            <p className="text-sm text-tinta-suave text-center m-0">
              ¿Ya tiene una cuenta? <Link to="/entrar" className="text-principal hover:text-principal-hover no-underline hover:underline">Entrar</Link>
            </p>
          </div>
        </form>
      </div>
    </div>
  );
}
