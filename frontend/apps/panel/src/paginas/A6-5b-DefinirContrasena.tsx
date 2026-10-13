import { useRef, useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { paths } from '@shapi/api/administracion';
import { Boton } from '@shapi/ui';
import { AvisoError, CampoEtiquetado, Encabezado, MarcoAcceso } from '../modulos/identidad/Formularios';
import { useIrAlDestino } from '../modulos/identidad/useIdentidad';

const cliente = crearCliente<paths>(window.location.origin);
export default function PaginaA65bDefinirContrasena() {
  const [parametros] = useSearchParams();
  const token = parametros.get('token') ?? '';
  const irAlDestino = useIrAlDestino();
  const formulario = useRef<HTMLFormElement>(null);
  const [contrasena, setContrasena] = useState('');
  const [guardando, setGuardando] = useState(false);
  const [definida, setDefinida] = useState(false);
  const [error, setError] = useState<unknown>(null);
  async function enviar(e: FormEvent) {
    e.preventDefault(); setGuardando(true); setError(null);
    try {
      if (!definida) {
        const { response } = await cliente.POST('/api/auth/definir-contrasena', { body: { token, contrasena }, params: { header: { 'X-Requested-With': 'shapi' } } });
        if (!response.ok) throw new Error('No se pudo definir la contraseña.');
        setDefinida(true);
      }
      await irAlDestino();
    } catch (causa) { setError(causa); setGuardando(false); }
  }
  const invalido = !token || (error instanceof ErrorApi && error.codigo === 'token_invalido');
  const errorCampo = error instanceof ErrorApi ? error.errores?.contrasena?.[0] : undefined;
  return <MarcoAcceso>
    {invalido ? <Encabezado rotulo="Cuenta de plataforma · enlace no válido" titulo="El enlace ya no sirve"><p className="text-[15px] leading-[1.55] text-tinta-suave">El enlace venció o ya se usó. Comuníquese con el administrador de Shapi.</p></Encabezado> : <>
      <Encabezado rotulo="Cuenta de plataforma" titulo="Definir la contraseña"><p className="text-[15px] leading-[1.55] text-tinta-suave">Defina una contraseña para su cuenta de administración o soporte.</p></Encabezado>
      {!!error && !errorCampo && <div className="mt-6"><AvisoError mensaje={error instanceof ErrorApi ? error.titulo : 'No se pudo completar la solicitud.'} reintentar={() => formulario.current?.requestSubmit()} /></div>}
      <form ref={formulario} onSubmit={enviar} noValidate>
        <div className="mt-8"><CampoEtiquetado etiqueta="Contraseña nueva" type="password" autoComplete="new-password" value={contrasena} onChange={e => setContrasena(e.target.value)} error={errorCampo} /></div>
        <Boton type="submit" className="mt-6 w-full" deshabilitado={guardando}>Guardar la contraseña</Boton>
        <div className="mt-6 rounded-base border border-borde bg-fondo px-4 py-[14px]"><p className="text-sm leading-[1.5] text-tinta-suave">Este enlace es de un solo uso: al guardar la contraseña deja de servir. Vence <span className="font-medium text-tinta">7 días</span> después de haberse enviado. Al guardarla se inicia su sesión.</p></div>
      </form>
    </>}
  </MarcoAcceso>;
}
