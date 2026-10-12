import { Aviso, Boton } from '@shapi/ui';
import type { Clave } from '../modulos/claves/claves';

const nombreTipo = (tipo: Clave['tipo']) => tipo === 'produccion' ? 'Producción' : 'Pruebas';

export default function RevocarClave({ api, clave, procesando, error, confirmar, cancelar }: {
  api: string;
  clave: Clave;
  procesando: boolean;
  error?: string;
  confirmar: () => void;
  cancelar: () => void;
}) {
  return <section className="mx-auto max-w-[600px] rounded-base border border-borde bg-panel p-10">
    <div aria-hidden="true" className="flex size-8 items-center justify-center rounded-full border border-alerta-base text-alerta-base">!</div>
    <header className="mt-5">
      <p className="text-etiqueta uppercase tracking-[.14em] text-[var(--marca-principal)]">Suscripción y claves</p>
      <h1 className="mt-2 font-display text-[32px] leading-[1.2]">Revocar clave</h1>
      <p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">La clave <strong className="font-semibold text-tinta">deja de funcionar en menos de 10 segundos</strong> y la compuerta rechaza con el código 401 toda petición que la use. Después podrá emitir una clave nueva.</p>
    </header>
    {error && <div className="mt-5"><Aviso estado="error">{error}</Aviso></div>}
    <dl className="mt-8 flex flex-col gap-3.5 border-t border-borde pt-6 text-[14px]">
      <Fila nombre="API" valor={api} />
      <Fila nombre="Tipo" valor={nombreTipo(clave.tipo)} />
      <Fila nombre="Clave" valor={clave.claveEnmascarada} mono />
    </dl>
    <div className="mt-6 flex flex-col gap-3.5 border-t border-borde pt-6">
      <Boton type="button" deshabilitado={procesando} onClick={confirmar}>{procesando ? 'Revocando…' : 'Revocar clave'}</Boton>
      <button type="button" className="text-[14px] text-[var(--marca-principal)]" onClick={cancelar}>Cancelar</button>
    </div>
  </section>;
}

function Fila({ nombre, valor, mono = false }: { nombre: string; valor: string; mono?: boolean }) {
  return <div className="flex justify-between gap-5"><dt className="text-tinta-suave">{nombre}</dt><dd className={`text-right font-medium ${mono ? 'font-mono' : ''}`}>{valor}</dd></div>;
}
