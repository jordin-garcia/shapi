import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { paths } from '@shapi/api/apis';
import { EstadoCargando, EstadoError, Etiqueta, Tabla } from '@shapi/ui';
import { Link } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);

function EnlaceRegistro() {
  return <Link to="/panel/apis/nueva" className="h-[46px] rounded-base bg-principal px-6 text-[15px] font-medium text-white inline-flex items-center justify-center hover:bg-principal-hover">Registrar una API</Link>;
}

export default function PaginaA31Apis() {
  const consulta = useQuery({
    queryKey: ['apis'],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/apis', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron cargar las APIs.');
      return data;
    },
    retry: false,
  });

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) {
    return <EstadoError mensaje="No se pudieron cargar las APIs." reintentar={() => void consulta.refetch()} />;
  }

  const { elementos, total, planNombre, maxApis } = consulta.data;
  const uso = maxApis == null
    ? <>Usa {total} APIs de su plan {planNombre}, sin límite de APIs.</>
    : <>Usa <span className="font-medium text-tinta tabular-nums">{total} de {maxApis}</span> APIs de su plan {planNombre}.</>;

  return <div>
    <div className="flex items-end justify-between gap-6">
      <div className="flex flex-col gap-[10px]">
        <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">Publicación</p>
        <h1 className="font-display text-[32px] leading-[1.2]">APIs de la organización</h1>
        <p className="text-[15px] leading-[1.55] text-tinta-suave">
          {elementos.length === 0
            ? <>Publique o despublique cada API. Para configurarla, entre a su nombre. {maxApis == null ? `Su plan ${planNombre} no limita la cantidad de APIs.` : <>Su plan {planNombre} incluye <span className="font-medium text-tinta tabular-nums">{maxApis}</span> {maxApis === 1 ? 'API' : 'APIs'}.</>}</>
            : <>Publique o despublique cada API. Para configurarla, entre a su nombre. {uso}</>}
        </p>
      </div>
      {elementos.length > 0 && <EnlaceRegistro />}
    </div>

    <section className="mt-8 rounded-base border border-borde bg-panel p-5" aria-label="APIs registradas">
      <Tabla
        encabezados={elementos.length === 0 ? ['API', 'Estado', 'Acción'] : ['API', 'Subdominio', 'Estado', 'Acción']}
        filas={elementos.map(api => {
          const estado = api.estado === 'publicada' ? 'Publicada' : api.estado === 'despublicada' ? 'Despublicada' : 'Borrador';
          return [
            <Link key="nombre" className="font-medium text-tinta hover:text-principal" to={`/panel/apis/${encodeURIComponent(api.id)}/especificacion`}>{api.nombre}</Link>,
            <span key="subdominio">{api.subdominio}</span>,
            <Etiqueta key="estado" estado={api.estado === 'publicada' ? 'correcto' : 'neutro'}>{estado}</Etiqueta>,
            <span key="accion" className="font-medium text-principal">{api.estado === 'publicada' ? 'Despublicar' : 'Publicar'}</span>,
          ];
        })}
      />

      {elementos.length === 0 && <div className="flex flex-col items-center gap-6 px-4 pb-11 pt-16 text-center">
        <svg aria-hidden="true" width="40" height="40" viewBox="0 0 40 40" fill="none">
          <rect x="1" y="1" width="38" height="38" rx="8" stroke="var(--borde-campo)" strokeWidth="1.5" strokeDasharray="4 4" />
          <path d="M20 13 V27 M13 20 H27" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
        </svg>
        <div className="flex max-w-[480px] flex-col gap-[10px]">
          <h2 className="font-display text-[22px] leading-[1.3]">Todavía no tiene APIs registradas</h2>
          <p className="text-[15px] leading-[1.55] text-tinta-suave">Registre su primera API con la URL de su servidor de origen. Después cargará su especificación OpenAPI y elegirá qué rutas exponer.</p>
        </div>
        <EnlaceRegistro />
      </div>}
    </section>
  </div>;
}
