import { useState } from 'react';
import { useNavigate, Link } from 'react-router';
import { Boton, Campo } from '@shapi/ui';
import { useEntrar } from '../modulos/identidad/useIdentidad';
import { clienteSesion } from '../modulos/sesion/useSesion';

export default function PaginaA13Sesion() {
  const navigate = useNavigate();
  const entrar = useEntrar();

  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [errorVisible, setErrorVisible] = useState('');

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorVisible('');
    try {
      await entrar.mutateAsync({ correo, contrasena });
      
      // Obtener la sesión para leer el destino
      const { data, response } = await clienteSesion.GET('/api/auth/sesion');
      if (response.ok && data) {
        navigate(data.destino || '/panel/apis');
      } else {
        navigate('/panel');
      }
    } catch (error: unknown) {
      if (error && typeof error === 'object' && 'details' in error) {
        const err = error as any;
        if (err.details && err.details.titulo) {
          setErrorVisible(err.details.titulo);
        } else {
          setErrorVisible(err.message);
        }
      } else {
        setErrorVisible(String(error));
      }
    }
  };

  return (
    <div className="flex-grow flex items-center justify-center py-11 px-20">
      <div className="w-[460px] bg-white border border-[var(--borde)] rounded-base p-10">
        <div className="flex flex-col gap-2">
          <p className="text-xs tracking-[.16em] uppercase text-tinta-suave font-medium m-0">Acceso</p>
          <h1 className="font-display text-[32px] leading-[1.2] m-0 tracking-[-0.02em]">Entrar a Shapi</h1>
        </div>

        {errorVisible && (
          <div className="mt-6 p-4 bg-alerta/10 text-alerta rounded-base text-sm">
            {errorVisible}
          </div>
        )}

        <form onSubmit={onSubmit} className="flex flex-col gap-5 mt-8">
          <div className="flex flex-col gap-[7px]">
            <span className="text-[13px] font-semibold text-tinta">Correo electrónico</span>
            <Campo
              type="email"
              value={correo}
              onChange={(e) => setCorreo(e.target.value)}
              autoComplete="email"
              required
            />
          </div>
          <div className="flex flex-col gap-[7px]">
            <span className="text-[13px] font-semibold text-tinta">Contraseña</span>
            <Campo
              type="password"
              value={contrasena}
              onChange={(e) => setContrasena(e.target.value)}
              className={contrasena ? 'tracking-[0.18em]' : ''}
              autoComplete="current-password"
              required
            />
          </div>
          
          <div className="flex justify-end mt-1">
            <Link to="/recuperar" className="text-sm text-principal hover:text-principal-hover no-underline hover:underline">
              ¿Olvidó su contraseña?
            </Link>
          </div>

          <div className="mt-2">
            <Boton type="submit" deshabilitado={entrar.isPending} className="w-full">
              Entrar
            </Boton>
          </div>
        </form>
      </div>
    </div>
  );
}
