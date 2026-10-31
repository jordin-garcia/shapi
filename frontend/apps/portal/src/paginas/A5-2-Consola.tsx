import { useState, type FormEvent } from 'react';
import { Aviso, Boton, EstadoCargando, EstadoError, Selector } from '@shapi/ui';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';
import {
  CLAVE_PRUEBAS_SESION,
  enviarACompuerta,
  tipoEnEspanol,
  valoresDeEjemplo,
  type ResultadoCompuerta,
  type RutaConsola,
} from '../modulos/consola/clienteCompuerta';
import { useRutasConsola } from '../modulos/consola/useRutasConsola';

function identificador(ruta: RutaConsola) { return `${ruta.metodo} ${ruta.patron}`; }

function CampoParametro({ parametro, valor, cambiar, error }: {
  parametro: RutaConsola['parametros'][number]; valor: string;
  cambiar: (valor: string) => void; error?: string;
}) {
  const id = `parametro-${parametro.nombre}`;
  const idError = `${id}-error`;
  return <div className="flex flex-col gap-[7px]">
    <div className="flex items-baseline justify-between gap-2">
      <label htmlFor={id} className="text-[13px] font-semibold text-tinta">{parametro.nombre}</label>
      <span className="text-[13px] text-tinta-suave">{tipoEnEspanol(parametro.tipo)} · {parametro.obligatorio ? 'obligatorio' : 'opcional'}</span>
    </div>
    <input
      id={id}
      value={valor}
      onChange={evento => cambiar(evento.target.value)}
      inputMode={['number', 'integer'].includes(parametro.tipo) ? 'decimal' : undefined}
      aria-invalid={error ? true : undefined}
      aria-describedby={error ? idError : undefined}
      className={`h-12 rounded-base border bg-panel px-[14px] text-[15px] text-tinta outline-none focus:ring-[3px] focus:ring-anillo-foco ${error ? 'border-alerta' : 'border-borde-campo focus:border-principal'}`}
    />
    {error && <span id={idError} className="text-sm text-alerta">{error}</span>}
  </div>;
}

function PanelRespuesta({ resultado }: { resultado?: ResultadoCompuerta }) {
  return <section className="overflow-hidden rounded-base border border-borde bg-panel">
    <header className="flex items-center justify-between border-b border-borde bg-fondo px-5 py-3.5">
      <h2 className="text-[12px] font-medium uppercase tracking-[.16em] text-tinta-suave">Respuesta</h2>
      {resultado && <span className={`text-[14px] font-semibold ${resultado.estado < 400 ? 'text-correcto' : 'text-alerta'}`}>{resultado.estado}</span>}
    </header>
    {!resultado
      ? <p className="px-5 py-7 text-[14px] text-tinta-suave">Envíe una petición para revisar la respuesta.</p>
      : <>
        {resultado.error && <div className="border-b border-borde px-5 py-4">
          <p className="font-mono text-[13px] font-semibold text-alerta">{resultado.error.codigo}</p>
          <p className="mt-1 text-[14px] text-tinta">{resultado.error.mensaje}</p>
        </div>}
        <pre className="overflow-auto whitespace-pre-wrap px-5 py-[18px] font-mono text-[14px] leading-[1.7] text-[#2B3547]">{resultado.cuerpo}</pre>
        {resultado.cabeceras.length > 0 && <div className="border-t border-borde px-5 py-4">
          <h3 className="text-[12px] font-medium uppercase tracking-[.14em] text-tinta-suave">Límites y cuota</h3>
          <dl className="mt-3 grid grid-cols-[max-content_minmax(0,1fr)] gap-x-5 gap-y-2 text-[13px]">
            {resultado.cabeceras.map(cabecera => <div className="contents" key={cabecera.nombre}>
              <dt className="font-mono text-tinta-suave">{cabecera.nombre}</dt>
              <dd className="min-w-0 break-all text-tinta">{cabecera.valor}</dd>
            </div>)}
          </dl>
        </div>}
      </>}
  </section>;
}

function FormularioConsola({ ruta, rutas, seleccionar }: {
  ruta: RutaConsola; rutas: RutaConsola[]; seleccionar: (id: string) => void;
}) {
  const marca = useMarcaPortal();
  const [valores, setValores] = useState(() => valoresDeEjemplo(ruta));
  const [clave, setClave] = useState(() => sessionStorage.getItem(CLAVE_PRUEBAS_SESION) ?? '');
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [resultado, setResultado] = useState<ResultadoCompuerta>();
  const [errorEnvio, setErrorEnvio] = useState<string>();
  const [enviando, setEnviando] = useState(false);

  function cambiarClave(valor: string) {
    setClave(valor);
    if (valor) sessionStorage.setItem(CLAVE_PRUEBAS_SESION, valor);
    else sessionStorage.removeItem(CLAVE_PRUEBAS_SESION);
  }

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    const siguientes = Object.fromEntries(ruta.parametros
      .filter(parametro => parametro.obligatorio && !valores[parametro.nombre]?.trim())
      .map(parametro => [parametro.nombre, 'Este parámetro es obligatorio.']));
    if (!clave.trim()) siguientes.clave = 'Pegue su clave de pruebas.';
    setErrores(siguientes);
    if (Object.keys(siguientes).length > 0) return;
    setEnviando(true); setErrorEnvio(undefined); setResultado(undefined);
    try { setResultado(await enviarACompuerta(ruta, valores, clave, marca.hostApi)); }
    catch { setErrorEnvio('No se pudo enviar la petición. Revise su conexión e intente de nuevo.'); }
    finally { setEnviando(false); }
  }

  return <div className="mt-7 grid grid-cols-1 items-start gap-6 xl:grid-cols-2">
    <form onSubmit={enviar} className="rounded-base border border-borde bg-panel p-5">
      <h2 className="font-display text-[21px] leading-[1.3]">Petición</h2>
      <div className="mt-5 flex flex-col gap-[18px] border-t border-borde pt-5">
        <div className="flex flex-col gap-[7px]">
          <label htmlFor="ruta-consola" className="text-[13px] font-semibold text-tinta">Ruta</label>
          <Selector
            id="ruta-consola"
            value={identificador(ruta)}
            onChange={evento => seleccionar(evento.target.value)}
            opciones={rutas.map(item => ({ etiqueta: identificador(item), valor: identificador(item) }))}
          />
        </div>
        {ruta.parametros.length === 0
          ? <p className="text-[14px] text-tinta-suave">Esta ruta no recibe parámetros.</p>
          : <div className="grid grid-cols-1 gap-x-4 gap-y-[18px] md:grid-cols-2">
            {ruta.parametros.map(parametro => <CampoParametro
              key={parametro.nombre}
              parametro={parametro}
              valor={valores[parametro.nombre] ?? ''}
              cambiar={valor => setValores(actuales => ({ ...actuales, [parametro.nombre]: valor }))}
              error={errores[parametro.nombre]}
            />)}
          </div>}
      </div>
      <div className="mt-5 flex flex-col gap-5 border-t border-borde pt-5 lg:flex-row lg:items-end lg:justify-between">
        <div className="min-w-0 flex-1">
          <label htmlFor="clave-pruebas" className="text-[13px] font-semibold text-tinta">Su clave de pruebas</label>
          <input
            id="clave-pruebas" type="password" autoComplete="off" value={clave}
            onChange={evento => cambiarClave(evento.target.value)}
            aria-invalid={errores.clave ? true : undefined}
            aria-describedby="ayuda-clave"
            className={`mt-[7px] h-12 w-full rounded-base border bg-panel px-[14px] font-mono text-[15px] text-tinta outline-none focus:ring-[3px] focus:ring-anillo-foco ${errores.clave ? 'border-alerta' : 'border-borde-campo focus:border-principal'}`}
          />
          {errores.clave && <p className="mt-1 text-sm text-alerta">{errores.clave}</p>}
          <p id="ayuda-clave" className="mt-[7px] text-[13px] leading-[1.5] text-tinta-suave">Péguela aquí. Se recuerda solo mientras esta pestaña esté abierta y no descuenta de su cuota.</p>
        </div>
        <Boton type="submit" deshabilitado={enviando} className="shrink-0">{enviando ? 'Enviando…' : 'Enviar petición'}</Boton>
      </div>
      {errorEnvio && <div className="mt-5"><Aviso estado="error">{errorEnvio}</Aviso></div>}
    </form>
    <PanelRespuesta resultado={resultado} />
  </div>;
}

export default function Consola() {
  const marca = useMarcaPortal();
  const consulta = useRutasConsola();
  const [rutaElegida, setRutaElegida] = useState('');
  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudieron cargar las rutas expuestas." reintentar={() => void consulta.refetch()} />;
  if (consulta.data.length === 0) return <section className="p-11"><h1 className="font-display text-[32px]">Consola de pruebas</h1><p className="mt-3 text-tinta-suave">No hay rutas expuestas para probar.</p></section>;
  const ruta = consulta.data.find(item => identificador(item) === rutaElegida) ?? consulta.data[0];

  // JG-30: la barra lateral, el encabezado y la sesión los pone LayoutPortal, igual que en la documentación (mockup A5 Consola).
  return <section className="min-w-0 px-11 pb-11 pt-10">
    <header>
      <p className="text-[12px] font-medium uppercase tracking-[.16em] text-[var(--marca-principal)]">{marca.nombreApi}</p>
      <h1 className="mt-2 font-display text-[32px] leading-[1.2]">Consola de pruebas</h1>
      <p className="mt-3 text-[15px] leading-[1.55] text-tinta-suave">Complete los parámetros de una ruta, envíe la petición y revise la respuesta.</p>
    </header>
    <FormularioConsola key={identificador(ruta)} ruta={ruta} rutas={consulta.data} seleccionar={setRutaElegida} />
  </section>;
}
