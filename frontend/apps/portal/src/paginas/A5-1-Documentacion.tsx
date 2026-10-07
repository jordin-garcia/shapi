import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/portal';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { Link, Navigate, useParams } from 'react-router';
import ReactMarkdown from 'react-markdown';
import rehypeSanitize from 'rehype-sanitize';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';

export type RutaDocumentada = components['schemas']['RutaDocumentada'];

const clientePortal = crearCliente<paths>(window.location.origin);
const claveDocumentacion = ['portal', 'documentacion'] as const;

export function useDocumentacionPortal() {
  return useQuery({
    queryKey: claveDocumentacion,
    queryFn: async ({ signal }) => {
      const { data, response } = await clientePortal.GET('/api/portal/documentacion', { signal });
      if (!response.ok || !data) throw new Error('No se pudo consultar la documentación');
      return data;
    },
    staleTime: 5 * 60 * 1000,
  });
}

export function rutaDocumentacion(patron: string) {
  return `/documentacion/${encodeURIComponent(patron)}`;
}

export function textoPeso(peso: number) {
  return `Descuenta ${peso} ${peso === 1 ? 'llamada' : 'llamadas'} de su cuota`;
}

function tipoEnEspanol(tipo: string) {
  const tipos: Record<string, string> = {
    string: 'texto', number: 'número', integer: 'número', boolean: 'booleano', array: 'lista', object: 'objeto',
  };
  return tipos[tipo] ?? tipo;
}

function Codigo({ valor }: { valor: unknown }) {
  if (valor === null || valor === undefined) return null;
  const contenido = typeof valor === 'string' ? valor : JSON.stringify(valor, null, 2);
  return <pre className="overflow-auto whitespace-pre-wrap p-5 font-mono text-[14px] leading-[1.7] text-[#2B3547]">{contenido}</pre>;
}

export default function Documentacion() {
  const marca = useMarcaPortal();
  const consulta = useDocumentacionPortal();
  const { ruta } = useParams();

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError reintentar={() => void consulta.refetch()} />;
  if (consulta.data.rutas.length === 0) {
    return <div className="p-11"><h1 className="font-display text-[32px]">Documentación</h1><p className="mt-3 text-tinta-suave">No hay rutas publicadas.</p></div>;
  }
  if (!ruta) return <Navigate to={rutaDocumentacion(consulta.data.rutas[0].patron)} replace />;

  const patron = decodeURIComponent(ruta);
  const seleccionada = consulta.data.rutas.find(item => item.patron === patron);
  if (!seleccionada) return <Navigate to={rutaDocumentacion(consulta.data.rutas[0].patron)} replace />;

  return (
    <div className="grid min-h-[720px] grid-cols-[272px_minmax(0,1fr)] bg-fondo">
      <aside className="border-r border-borde bg-panel px-4 py-7" aria-label="Rutas documentadas">
        <p className="px-3 text-[11px] font-semibold uppercase tracking-[.14em] text-tinta-suave">Documentación</p>
        <nav className="mt-3 flex flex-col gap-1">
          {consulta.data.rutas.map(item => (
            <Link
              key={`${item.metodo}-${item.patron}`}
              to={rutaDocumentacion(item.patron)}
              aria-current={item.patron === seleccionada.patron ? 'page' : undefined}
              className={`flex items-center gap-3 rounded-base px-3 py-2 text-[14px] ${item.patron === seleccionada.patron
                ? 'bg-[color-mix(in_srgb,var(--marca-principal)_8%,#FFFFFF)] text-[var(--marca-principal)] font-medium'
                : 'text-[#2B3547]'}`}
            >
              <span className="w-12 shrink-0 text-[11px] font-semibold tracking-[.06em] text-[var(--marca-principal)]">{item.metodo}</span>
              <span className="truncate">{item.patron}</span>
            </Link>
          ))}
        </nav>
      </aside>

      <article className="min-w-0 p-11">
        <header className="flex items-end justify-between gap-8">
          <div className="min-w-0">
            <p className="text-[12px] font-medium uppercase tracking-[.16em] text-[var(--marca-principal)]">{marca.nombreApi}</p>
            <div className="mt-2 flex items-baseline gap-4">
              <span className="text-[15px] font-semibold tracking-[.06em] text-[var(--marca-principal)]">{seleccionada.metodo}</span>
              <h1 className="font-display text-[32px] font-normal tracking-[-.02em] text-tinta">{seleccionada.patron}</h1>
            </div>
            {seleccionada.descripcion && (
              <div className="mt-2 text-[15px] leading-[1.55] text-tinta-suave [&_strong]:font-semibold [&_strong]:text-tinta">
                <ReactMarkdown rehypePlugins={[rehypeSanitize]}>{seleccionada.descripcion}</ReactMarkdown>
              </div>
            )}
            <div className="mt-3 flex flex-wrap items-center gap-5 text-[14px] text-tinta-suave">
              <span className="font-mono text-tinta">{seleccionada.urlCompleta}</span>
              <span aria-hidden="true" className="h-4 w-px bg-borde" />
              <span>{textoPeso(seleccionada.pesoLlamadas)}</span>
            </div>
          </div>
          <Link to="/consola" className="shrink-0 rounded-base border border-[#C9D2E1] bg-panel px-6 py-3 text-[15px] font-medium text-tinta">Probar en la consola</Link>
        </header>

        <section className="mt-7 rounded-base border border-borde bg-panel p-5">
          <h2 className="font-display text-[21px] font-normal">Parámetros</h2>
          {seleccionada.parametros.length === 0
            ? <p className="mt-4 text-[14px] text-tinta-suave">Esta ruta no recibe parámetros.</p>
            : (
              <div className="mt-4 overflow-x-auto">
                <table className="w-full text-left text-[14px]">
                  <thead><tr className="border-b border-borde text-[12px] uppercase tracking-[.08em] text-tinta-suave">
                    <th className="px-3 py-3 font-medium">Parámetro</th><th className="px-3 py-3 font-medium">Tipo</th>
                    <th className="px-3 py-3 font-medium">Obligatorio</th><th className="px-3 py-3 font-medium">Descripción</th>
                  </tr></thead>
                  <tbody>{seleccionada.parametros.map(parametro => (
                    <tr key={parametro.nombre} className="border-b border-borde last:border-0">
                      <td className="px-3 py-3 font-medium text-tinta">{parametro.nombre}</td>
                      <td className="px-3 py-3 text-tinta-suave">{tipoEnEspanol(parametro.tipo)}</td>
                      <td className="px-3 py-3 text-tinta-suave">{parametro.obligatorio ? 'Sí' : 'No'}</td>
                      <td className="px-3 py-3 text-tinta-suave">{parametro.descripcion ?? '—'}</td>
                    </tr>
                  ))}</tbody>
                </table>
              </div>
            )}
        </section>

        <div className="mt-6 grid grid-cols-2 gap-6">
          <section className="overflow-hidden rounded-base border border-borde bg-panel">
            <div className="flex items-center justify-between border-b border-borde px-5 py-3.5">
              <h2 className="text-[12px] font-medium uppercase tracking-[.16em] text-tinta-suave">Petición</h2>
              <span className="text-[14px]"><strong className="mr-2 text-[var(--marca-principal)]">{seleccionada.metodo}</strong>{seleccionada.patron}</span>
            </div>
            <Codigo valor={seleccionada.ejemploPeticion} />
          </section>
          <section className="overflow-hidden rounded-base border border-borde bg-panel">
            <div className="flex items-center justify-between border-b border-borde bg-[#F4F6FA] px-5 py-3.5">
              <h2 className="text-[12px] font-medium uppercase tracking-[.16em] text-tinta-suave">Respuesta</h2>
              <span className="text-[14px] font-semibold text-[#1F8A5B]">{seleccionada.codigoRespuesta ?? '—'}</span>
            </div>
            <Codigo valor={seleccionada.ejemploRespuesta} />
          </section>
        </div>
      </article>
    </div>
  );
}
