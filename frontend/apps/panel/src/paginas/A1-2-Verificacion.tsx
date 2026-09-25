import { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router';
import { useVerificarCorreo, useReenviarVerificacion } from '../modulos/identidad/useIdentidad';
import { useSesion } from '../modulos/sesion/useSesion';
import { Boton } from '@shapi/ui';

export default function PaginaA12Verificacion() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token');
  const correo = searchParams.get('correo') || '';

  const verificar = useVerificarCorreo();
  const reenviar = useReenviarVerificacion();
  const { data: sesion } = useSesion();

  const [estado, setEstado] = useState<'pendiente' | 'verificando' | 'exito' | 'error'>(
    token ? 'verificando' : 'pendiente'
  );
  const [mensajeReenvio, setMensajeReenvio] = useState('');

  useEffect(() => {
    if (token && estado === 'verificando') {
      verificar.mutate(token, {
        onSuccess: () => {
          setEstado('exito');
        },
        onError: () => {
          setEstado('error');
        },
      });
    }
  }, [token, estado, verificar]);

  // Redirigir al destino sugerido si hay sesión y correo verificado (exito de la mutacion)
  useEffect(() => {
    if (estado === 'exito' && sesion) {
      const temporizador = setTimeout(() => {
        navigate(sesion.destino || '/panel/apis');
      }, 1500);
      return () => clearTimeout(temporizador);
    }
  }, [estado, sesion, navigate]);

  const handleReenviar = () => {
    if (!correo) return;
    setMensajeReenvio('');
    reenviar.mutate(correo, {
      onSuccess: () => setMensajeReenvio('Se ha enviado un nuevo enlace a su correo.'),
      onError: () => setMensajeReenvio('No se pudo reenviar el enlace. Intente más tarde.')
    });
  };

  if (estado === 'verificando') {
    return (
      <div className="flex-grow flex items-center justify-center py-11 px-20">
        <div className="w-[520px] bg-white border border-[var(--borde)] rounded-base p-10 text-center">
          <p className="text-tinta-suave text-lg">Verificando su correo...</p>
        </div>
      </div>
    );
  }

  if (estado === 'exito') {
    return (
      <div className="flex-grow flex items-center justify-center py-11 px-20">
        <div className="w-[520px] bg-white border border-[var(--borde)] rounded-base p-10 text-center">
          <svg className="mx-auto text-correcto w-12 h-12 mb-4" viewBox="0 0 24 24" fill="none">
            <circle cx="12" cy="12" r="9.25" stroke="currentColor" strokeWidth="1.5"></circle>
            <path d="M8 12.5 L11 15.5 L16 9" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"></path>
          </svg>
          <h1 className="font-display text-[28px] m-0 mb-2">Correo verificado</h1>
          <p className="text-tinta-suave">Su correo ha sido confirmado exitosamente. Redirigiendo...</p>
        </div>
      </div>
    );
  }

  if (estado === 'error') {
    return (
      <div className="flex-grow flex items-center justify-center py-11 px-20">
        <div className="w-[520px] bg-white border border-[var(--borde)] rounded-base p-10">
          <h1 className="font-display text-[28px] m-0 mb-4 text-alerta">Enlace no válido</h1>
          <p className="text-tinta-suave mb-6">El enlace de verificación es inválido o ha expirado. Por favor, solicite uno nuevo.</p>
          {correo ? (
             <div className="flex flex-col gap-3">
               <Boton onClick={handleReenviar} deshabilitado={reenviar.isPending}>Enviar el enlace otra vez</Boton>
               {mensajeReenvio && <p className="text-sm text-tinta-suave mt-2">{mensajeReenvio}</p>}
             </div>
          ) : (
            <p className="text-sm text-tinta-suave">Vuelva a iniciar sesión para solicitar otro enlace.</p>
          )}
        </div>
      </div>
    );
  }

  // Estado pendiente (aviso para revisar el correo)
  return (
    <div className="flex-grow flex items-center justify-center py-11 px-20">
      <div className="w-[520px] bg-white border border-[var(--borde)] rounded-base p-10">
        <div className="flex flex-col gap-6">
          <svg width="44" height="44" viewBox="0 0 24 24" fill="none">
            <rect x="2" y="5" width="20" height="14" rx="3" stroke="#3B6FF0" strokeWidth="1.5"></rect>
            <path d="M3.5 7.5 L12 13.5 L20.5 7.5" stroke="#3B6FF0" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"></path>
          </svg>

          <div className="flex flex-col gap-2">
            <p className="text-xs tracking-[.16em] uppercase text-tinta-suave font-medium m-0">Verificación de correo</p>
            <h1 className="font-display text-[32px] leading-[1.2] m-0">Revise su correo</h1>
          </div>

          <p className="text-[16px] leading-[1.6] text-tinta-suave m-0">
            Enviamos un enlace de confirmación a <span className="text-tinta font-medium">{correo || 'su cuenta'}</span>. Abra ese enlace para confirmar su cuenta. Vence en <span className="tabular-nums text-tinta font-medium">24 horas</span>. ¿No le llegó?{' '}
            <button
              onClick={handleReenviar}
              disabled={reenviar.isPending || !correo}
              className="text-principal hover:text-principal-hover underline bg-transparent border-0 p-0 cursor-pointer disabled:opacity-50"
            >
              Enviar el enlace otra vez
            </button>.
          </p>
          {mensajeReenvio && <p className="text-sm text-correcto m-0">{mensajeReenvio}</p>}

          <p className="text-[15px] leading-[1.55] text-tinta-suave m-0">
            Su organización ya quedó en el plan <span className="text-tinta font-medium">Prueba</span>: 1 API y 10,000 peticiones, sin costo por 30 días.
          </p>

          <div className="bg-[#FBE9E3] border border-[#EDC3B4] rounded-base py-4 px-[18px] flex gap-3">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" className="flex-shrink-0 mt-[1px]">
              <circle cx="12" cy="12" r="9.25" stroke="#8E3315" strokeWidth="1.5"></circle>
              <path d="M12 7.25 V13" stroke="#8E3315" strokeWidth="1.8" strokeLinecap="round"></path>
              <path d="M12 16.4 V16.5" stroke="#8E3315" strokeWidth="2" strokeLinecap="round"></path>
            </svg>
            <p className="text-sm leading-[1.5] text-[#8E3315] m-0">
              No podrá publicar APIs mientras su correo no esté confirmado.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
