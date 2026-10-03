import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router';
import { Boton, Campo, EstadoCargando } from '@shapi/ui';
import { AvisoError, EncabezadoAcceso, MarcoAcceso, RotuloPortal } from '../modulos/sesion/FormulariosAcceso';
import {
  interpretarError,
  reenviarVerificacionConsumidor,
  useIrAlDestinoConsumidor,
  verificarCorreoConsumidor,
  type ErrorFormulario,
} from '../modulos/sesion/useIdentidadConsumidor';

const MENSAJE_REENVIO = 'Si su correo todavía no está confirmado, le llegará un enlace nuevo en unos minutos.';

export default function PaginaA58Verificacion() {
  const [parametros] = useSearchParams();
  const token = parametros.get('token');
  return token ? <Verificar token={token} /> : <Revisar correo={parametros.get('correo') ?? ''} />;
}

/** Reenvío del enlace de verificación; un fallo de conexión se puede reintentar (11 §4). */
function useReenviar() {
  const [enviando, setEnviando] = useState(false);
  const [enviado, setEnviado] = useState(false);
  const [error, setError] = useState<ErrorFormulario | null>(null);

  async function reenviar(correo: string) {
    setEnviando(true);
    setError(null);
    try {
      await reenviarVerificacionConsumidor(correo);
      setEnviado(true);
    } catch (causa) {
      setError(interpretarError(causa));
    } finally {
      setEnviando(false);
    }
  }

  return { enviando, enviado, error, reenviar };
}

function AvisosReenvio({ enviado, error, reintentar }: { enviado: boolean; error: ErrorFormulario | null; reintentar: () => void }) {
  return (
    <>
      {enviado && <p role="status" className="mt-4 text-sm text-correcto">{MENSAJE_REENVIO}</p>}
      {error && (
        <div className="mt-4">
          <AvisoError mensaje={error.mensaje} reintentar={error.codigo === null ? reintentar : undefined} />
        </div>
      )}
    </>
  );
}

function Revisar({ correo: correoInicial }: { correo: string }) {
  const [correo, setCorreo] = useState(correoInicial);
  const { enviando, enviado, error, reenviar } = useReenviar();
  const enviar = (evento: FormEvent) => {
    evento.preventDefault();
    void reenviar(correo);
  };

  return (
    <MarcoAcceso ancho={460}>
      <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <rect x="2" y="5" width="20" height="14" rx="3" stroke="var(--marca-principal)" strokeWidth="1.5" />
        <path d="M3.5 7.5 12 13.5 20.5 7.5" stroke="var(--marca-principal)" strokeWidth="1.5" />
      </svg>
      <div className="mt-6">
        <EncabezadoAcceso rotulo={<RotuloPortal />} titulo="Revise su correo" />
      </div>
      <p className="mt-5 text-base leading-[1.6] text-tinta-suave">
        Enviamos un enlace de confirmación a <span className="font-medium text-tinta">{correoInicial || 'su correo'}</span>. Vence en{' '}
        <span className="font-medium text-tinta">24 horas</span>.
      </p>
      <div className="mt-6 rounded-base border border-alerta-borde bg-alerta-fondo px-4 py-[14px] text-sm text-alerta">
        Podrá contratar un plan en cuanto confirme su correo.
      </div>
      {correoInicial ? (
        <form onSubmit={enviar} className="mt-7 text-center text-sm text-tinta-suave">
          ¿No le llegó?{' '}
          <button type="submit" disabled={enviando} className="text-[var(--marca-principal)]">
            Enviar el enlace otra vez
          </button>
        </form>
      ) : (
        <form onSubmit={enviar} className="mt-7 flex flex-col gap-4">
          <Campo etiqueta="Correo electrónico" type="email" value={correo} onChange={e => setCorreo(e.target.value)} />
          <Boton type="submit" deshabilitado={enviando}>Enviar el enlace otra vez</Boton>
        </form>
      )}
      <AvisosReenvio enviado={enviado} error={error} reintentar={() => void reenviar(correo)} />
    </MarcoAcceso>
  );
}

function Verificar({ token }: { token: string }) {
  const irAlDestino = useIrAlDestinoConsumidor();
  const [error, setError] = useState<ErrorFormulario | null>(null);
  const ejecutado = useRef(false);
  // El token es de un solo uso: si ya se confirmó y solo falló la consulta del destino, el reintento no lo reenvía.
  const confirmado = useRef(false);

  const verificar = useCallback(async () => {
    setError(null);
    try {
      if (!confirmado.current) {
        await verificarCorreoConsumidor(token);
        confirmado.current = true;
      }
      await irAlDestino();
    } catch (causa) {
      setError(interpretarError(causa));
    }
  }, [token, irAlDestino]);

  useEffect(() => {
    if (ejecutado.current) return;
    ejecutado.current = true;
    void verificar();
  }, [verificar]);

  if (!error) {
    return (
      <MarcoAcceso ancho={460}>
        <EncabezadoAcceso rotulo="Verificación de correo" titulo="Confirmando su correo" />
        <EstadoCargando />
      </MarcoAcceso>
    );
  }
  if (error.codigo === 'token_invalido') return <EnlaceNoValido mensaje={error.mensaje} />;
  return (
    <MarcoAcceso ancho={460}>
      <EncabezadoAcceso rotulo="Verificación de correo" titulo="No se pudo confirmar su correo" />
      <div className="mt-6">
        <AvisoError mensaje={error.mensaje} reintentar={error.codigo === null ? () => void verificar() : undefined} />
      </div>
    </MarcoAcceso>
  );
}

/** El enlace venció o ya se usó: como en A1.2, se puede pedir uno nuevo con el correo (el enlace no lo trae). */
function EnlaceNoValido({ mensaje }: { mensaje: string }) {
  const [correo, setCorreo] = useState('');
  const { enviando, enviado, error, reenviar } = useReenviar();
  const enviar = (evento: FormEvent) => {
    evento.preventDefault();
    void reenviar(correo);
  };

  return (
    <MarcoAcceso ancho={460}>
      <EncabezadoAcceso rotulo="Verificación de correo" titulo="Enlace no válido">
        <p className="text-tinta-suave">{mensaje}</p>
        <p className="text-tinta-suave">Escriba su correo y le enviaremos un enlace nuevo.</p>
      </EncabezadoAcceso>
      <form onSubmit={enviar} noValidate className="mt-8 flex flex-col gap-4">
        <Campo etiqueta="Correo electrónico" type="email" value={correo} onChange={e => setCorreo(e.target.value)} autoComplete="email" />
        <Boton className="w-full" type="submit" deshabilitado={enviando || !correo}>
          Enviar el enlace otra vez
        </Boton>
      </form>
      <AvisosReenvio enviado={enviado} error={error} reintentar={() => void reenviar(correo)} />
    </MarcoAcceso>
  );
}
