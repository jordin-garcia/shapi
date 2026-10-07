import { EstadoCargando, EstadoError } from '@shapi/ui';
import { Link } from 'react-router';
import ReactMarkdown from 'react-markdown';
import rehypeSanitize from 'rehype-sanitize';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';
import { rutaDocumentacion, textoPeso, useDocumentacionPortal } from './A5-1-Documentacion';

function Codigo({ valor }: { valor: unknown }) {
  if (valor === null || valor === undefined) return <p className="p-5 text-[14px] text-tinta-suave">Sin ejemplo.</p>;
  return <pre className="overflow-auto whitespace-pre-wrap p-5 font-mono text-[14px] leading-[1.7] text-[#2B3547]">{JSON.stringify(valor, null, 2)}</pre>;
}

export default function Inicio() {
  const marca = useMarcaPortal();
  const consulta = useDocumentacionPortal();

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError reintentar={() => void consulta.refetch()} />;
  const primeraConEjemplo = consulta.data.rutas.find(ruta => ruta.ejemploPeticion !== null || ruta.ejemploRespuesta !== null);

  return (
    <>
      <section className="relative overflow-hidden border-b border-borde px-20 py-20">
        <div aria-hidden="true" className="absolute inset-0 opacity-[.08] [background-image:linear-gradient(var(--marca-principal)_1px,transparent_1px),linear-gradient(90deg,var(--marca-principal)_1px,transparent_1px)] [background-size:48px_48px]" />
        <div className="relative grid grid-cols-[minmax(0,1fr)_minmax(380px,520px)] items-center gap-16">
          <div>
            <p className="text-[12px] font-medium uppercase tracking-[.16em] text-[var(--marca-principal)]">{marca.nombreApi}</p>
            <h1 className="mt-5 max-w-[18ch] font-display text-[44px] font-light leading-[1.15] tracking-[-.03em] text-tinta">
              {marca.bienvenida ?? `Bienvenido al portal de ${marca.nombreApi}.`}
            </h1>
            {marca.descripcionApi && <p className="mt-5 max-w-[52ch] text-[20px] leading-[1.55] text-tinta-suave">{marca.descripcionApi}</p>}
            <div className="mt-8 flex gap-3">
              <Link to="/registro" className="rounded-base bg-[var(--marca-principal)] px-6 py-[13px] text-[15px] font-medium text-white">Crear cuenta</Link>
              <Link to="/documentacion" className="rounded-base border border-[#C9D2E1] bg-panel px-6 py-3 text-[15px] font-medium text-tinta">Ver documentación</Link>
            </div>
          </div>
          {primeraConEjemplo && (
            <div className="overflow-hidden rounded-base border border-borde bg-panel">
              <div className="flex items-center justify-between border-b border-borde px-5 py-3.5">
                <span className="text-[12px] font-medium uppercase tracking-[.16em] text-tinta-suave">Petición</span>
                <span className="text-[14px]"><strong className="mr-2 text-[var(--marca-principal)]">{primeraConEjemplo.metodo}</strong>{primeraConEjemplo.patron}</span>
              </div>
              <Codigo valor={primeraConEjemplo.ejemploPeticion} />
              <div className="flex items-center justify-between border-y border-borde bg-[#F4F6FA] px-5 py-3.5">
                <span className="text-[12px] font-medium uppercase tracking-[.16em] text-tinta-suave">Respuesta</span>
                <span className="text-[14px] font-semibold text-[#1F8A5B]">{primeraConEjemplo.codigoRespuesta ?? '—'}</span>
              </div>
              <Codigo valor={primeraConEjemplo.ejemploRespuesta} />
            </div>
          )}
        </div>
      </section>

      <section className="px-20 py-16">
        <p className="text-[12px] font-medium uppercase tracking-[.16em] text-tinta-suave">Documentación</p>
        <h2 className="mt-3 font-display text-[44px] font-light tracking-[-.03em] text-tinta">Rutas disponibles</h2>
        {consulta.data.rutas.length === 0
          ? <p className="mt-8 text-tinta-suave">No hay rutas publicadas.</p>
          : (
            <div className="mt-10 grid grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-4">
              {consulta.data.rutas.map(ruta => (
                <Link key={`${ruta.metodo}-${ruta.patron}`} to={rutaDocumentacion(ruta.patron)} className="flex min-h-56 flex-col gap-3.5 rounded-base border border-borde bg-panel p-6 text-tinta hover:no-underline">
                  <span className="text-[12px] font-semibold tracking-[.06em] text-[var(--marca-principal)]">{ruta.metodo}</span>
                  <span className="font-display text-[22px] font-normal tracking-[-.02em]">{ruta.patron}</span>
                  <div className="min-h-[63px] text-[14px] leading-[1.5] text-tinta-suave [&_strong]:font-semibold [&_strong]:text-tinta">
                    {ruta.descripcion
                      ? <ReactMarkdown rehypePlugins={[rehypeSanitize]}>{ruta.descripcion}</ReactMarkdown>
                      : ruta.resumen}
                  </div>
                  <span className="mt-auto border-t border-borde pt-3.5 text-[13px] text-tinta-suave">{textoPeso(ruta.pesoLlamadas)}</span>
                </Link>
              ))}
            </div>
          )}
      </section>

      <section aria-labelledby="planes-api" className="border-y border-borde bg-[#F4F6FA] px-20 py-16">
        <p className="text-[12px] font-medium uppercase tracking-[.16em] text-tinta-suave">Precios</p>
        <h2 id="planes-api" className="mt-3 font-display text-[44px] font-light tracking-[-.03em] text-tinta">Planes de la API</h2>
        <div data-pendiente="DC-09" className="mt-10 min-h-20" />
      </section>
    </>
  );
}
