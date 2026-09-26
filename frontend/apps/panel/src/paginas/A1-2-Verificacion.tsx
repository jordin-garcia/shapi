import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router';
import { Boton, EstadoCargando } from '@shapi/ui';
import { AvisoError, CampoEtiquetado, Encabezado, MarcoAcceso } from '../modulos/identidad/Formularios';
import { interpretarError, reenviarVerificacion, useIrAlDestino, verificarCorreo, type ErrorFormulario } from '../modulos/identidad/useIdentidad';

// La API responde igual exista o no la cuenta (no revela qué correos están registrados), así que el aviso tampoco.
const ENLACE_REENVIADO = 'Si su correo todavía no está confirmado, le llegará un enlace nuevo en unos minutos.';

// A1.2 · Verificación de correo (RF-02). Sin token es el aviso "Revise su correo" que se muestra al registrarse;
// con `?token=` es el destino del enlace del correo (10 §1).
export default function PaginaA12Verificacion() {
  const [parametros] = useSearchParams();
  const token = parametros.get('token');
  return token ? <VerificarEnlace key={token} token={token} /> : <RevisarCorreo correo={parametros.get('correo') ?? ''} />;
}

function useReenviar() {
  const [estado, setEstado] = useState<{ enviando: boolean; enviado: boolean; error: ErrorFormulario | null }>({ enviando: false, enviado: false, error: null });
  const reenviar = async (correo: string) => {
    setEstado({ enviando: true, enviado: false, error: null });
    try {
      await reenviarVerificacion(correo);
      setEstado({ enviando: false, enviado: true, error: null });
    } catch (causa) {
      setEstado({ enviando: false, enviado: false, error: interpretarError(causa) });
    }
  };
  return { ...estado, reenviar };
}

function RevisarCorreo({ correo }: { correo: string }) {
  const { enviando, enviado, error, reenviar } = useReenviar();
  return (
    <MarcoAcceso>
      <div className="flex flex-col gap-6">
        <svg width="44" height="44" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <rect x="2" y="5" width="20" height="14" rx="3" stroke="#3B6FF0" strokeWidth="1.5" />
          <path d="M3.5 7.5 L12 13.5 L20.5 7.5" stroke="#3B6FF0" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
        </svg>

        <Encabezado rotulo="Verificación de correo" titulo="Revise su correo" />

        <p className="text-base leading-[1.6] text-tinta-suave m-0">
          Enviamos un enlace de confirmación a <span className="text-tinta font-medium">{correo || 'su correo'}</span>. Abra ese enlace para confirmar su cuenta.
          Vence en <span className="tabular-nums text-tinta font-medium">24 horas</span>.
          {correo && <> ¿No le llegó?{' '}
            <button type="button" onClick={() => void reenviar(correo)} disabled={enviando}
              className="text-principal hover:text-principal-hover hover:underline bg-transparent border-0 p-0 cursor-pointer disabled:opacity-50">
              Enviar el enlace otra vez
            </button>.</>}
        </p>
        {enviado && <p role="status" className="text-sm text-correcto m-0">{ENLACE_REENVIADO}</p>}
        {error && <AvisoError mensaje={error.mensaje} reintentar={error.codigo === null ? () => void reenviar(correo) : undefined} />}

        <p className="text-[15px] leading-[1.55] text-tinta-suave m-0">
          Su organización ya quedó en el plan <span className="text-tinta font-medium">Prueba</span>: 1 API y 10,000 peticiones, sin costo por 30 días.
        </p>

        <div className="bg-alerta-fondo border border-alerta-borde rounded-base py-4 px-[18px] flex gap-3">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" className="shrink-0 mt-px" aria-hidden="true">
            <circle cx="12" cy="12" r="9.25" stroke="#8E3315" strokeWidth="1.5" />
            <path d="M12 7.25 V13" stroke="#8E3315" strokeWidth="1.8" strokeLinecap="round" />
            <path d="M12 16.4 V16.5" stroke="#8E3315" strokeWidth="2" strokeLinecap="round" />
          </svg>
          <p className="text-sm leading-[1.5] text-alerta m-0">No podrá publicar APIs mientras su correo no esté confirmado.</p>
        </div>
      </div>
    </MarcoAcceso>
  );
}

function VerificarEnlace({ token }: { token: string }) {
  const irAlDestino = useIrAlDestino();
  const [error, setError] = useState<ErrorFormulario | null>(null);
  // El enlace es de un solo uso: se verifica una sola vez por token, aunque el efecto se ejecute dos veces (StrictMode).
  const verificado = useRef<string | null>(null);
  // Si la verificación funcionó y lo que falló fue consultar la sesión, "Reintentar" no vuelve a enviar el token.
  const confirmado = useRef(false);

  const verificar = useCallback(async () => {
    setError(null);
    try {
      if (!confirmado.current) {
        await verificarCorreo(token);
        confirmado.current = true;
      }
      await irAlDestino();
    } catch (causa) {
      setError(interpretarError(causa));
    }
  }, [token, irAlDestino]);

  useEffect(() => {
    if (verificado.current === token) return;
    verificado.current = token;
    void verificar();
  }, [token, verificar]);

  if (!error) {
    return <MarcoAcceso><Encabezado rotulo="Verificación de correo" titulo="Confirmando su correo" /><EstadoCargando /></MarcoAcceso>;
  }
  if (error.codigo === 'token_invalido') return <EnlaceNoValido mensaje={error.mensaje} />;
  return (
    <MarcoAcceso>
      <Encabezado rotulo="Verificación de correo" titulo="No se pudo confirmar su correo" />
      <div className="mt-6">
        <AvisoError mensaje={error.mensaje} reintentar={error.codigo === null ? () => void verificar() : undefined} />
      </div>
    </MarcoAcceso>
  );
}

// El enlace del correo no trae la dirección, así que para reenviarlo se le pide a la persona.
function EnlaceNoValido({ mensaje }: { mensaje: string }) {
  const [correo, setCorreo] = useState('');
  const { enviando, enviado, error, reenviar } = useReenviar();
  const enviar = (e: FormEvent) => { e.preventDefault(); void reenviar(correo); };

  return (
    <MarcoAcceso>
      <Encabezado rotulo="Verificación de correo" titulo="Enlace no válido">
        <p className="text-[15px] leading-[1.55] text-tinta-suave m-0">{mensaje}</p>
        <p className="text-[15px] leading-[1.55] text-tinta-suave m-0">Escriba su correo y le enviaremos un enlace nuevo.</p>
      </Encabezado>
      <form onSubmit={enviar} noValidate className="flex flex-col gap-5 mt-8">
        <CampoEtiquetado etiqueta="Correo electrónico" type="email" value={correo} onChange={e => setCorreo(e.target.value)} autoComplete="email" />
        <Boton type="submit" deshabilitado={enviando || !correo} className="w-full">Enviar el enlace otra vez</Boton>
        {enviado && <p role="status" className="text-sm text-correcto m-0">{ENLACE_REENVIADO}</p>}
        {error && <AvisoError mensaje={error.mensaje} reintentar={error.codigo === null ? () => void reenviar(correo) : undefined} />}
      </form>
    </MarcoAcceso>
  );
}
