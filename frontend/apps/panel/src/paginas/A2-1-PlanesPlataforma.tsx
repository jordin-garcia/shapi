import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/planes';
import type { paths as pathsSuscripciones } from '@shapi/api/suscripciones';
import { Boton, EstadoCargando, EstadoError } from '@shapi/ui';
import { Link } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
const clienteSuscripciones = crearCliente<pathsSuscripciones>(window.location.origin);
type Plan = components['schemas']['PlanPlataforma'];
const dinero = (monto: number) => 'Q ' + monto.toLocaleString('es-GT', { minimumFractionDigits: 2 });

export default function PaginaA21PlanesPlataforma() {
  const consulta = useQuery({
    queryKey: ['planes-plataforma'],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/planes-plataforma', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron cargar los planes.');
      return data;
    },
    retry: false,
  });
  const actual = useQuery({
    queryKey: ['suscripcion-plataforma'],
    queryFn: async ({ signal }) => {
      try {
        const { data, response } = await clienteSuscripciones.GET('/api/suscripcion', { signal });
        return response.ok ? (data ?? null) : null;
      } catch {
        return null;
      }
    },
    retry: false,
  });
  if (consulta.isPending || actual.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudieron cargar los planes." reintentar={() => void consulta.refetch()} />;
  const planAnual = consulta.data.find(plan => plan.nombre === 'Escala anual');
  const visibles = consulta.data.filter(plan => plan.nombre !== 'Escala anual');
  const cambiar = actual.data?.estado === 'activa' && actual.data.plan.precio > 0;
  const rutaPlan = (id: string) => `/panel/suscripcion/${cambiar ? 'cambiar' : 'contratar'}/${id}`;
  return <main>
    <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">Planes de plataforma</p>
    <h1 className="mt-2 font-display text-[44px] leading-[1.15]">Los planes de Shapi.</h1>
    <p className="mt-3 text-[16px] leading-[1.6] text-tinta-suave">Precios en quetzales. La vigencia se cuenta desde la contratación del plan.</p>
    <section aria-label="Planes de plataforma" className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-4">
      {visibles.map((plan: Plan) => <article key={plan.id} className={'flex flex-col gap-5 rounded-base border p-6 ' + (plan.nombre === 'Producto' ? 'border-principal bg-white' : 'border-borde bg-panel')}>
        <div>
          {plan.nombre === 'Producto' && <p className="text-etiqueta uppercase tracking-[0.16em] text-principal">Recomendado</p>}
          <h2 className="mt-2 font-display text-[21px]">{plan.nombre.startsWith('Escala') ? 'Escala' : plan.nombre}</h2>
          <p className="mt-2 min-h-10 text-sm text-tinta-suave">{plan.descripcion}</p>
        </div>
        <div className="border-t border-borde pt-4">
          <p className="font-display text-[30px]">{dinero(plan.precio)}</p>
          <p className="text-sm text-tinta-suave">{plan.nombre === 'Escala mensual' ? 'Mensual · vigencia de 30 días' : 'Vigencia de ' + plan.vigenciaDias + ' días'}</p>
          {plan.nombre === 'Escala mensual' && planAnual && <p className="mt-1 text-sm text-tinta-suave">o {dinero(planAnual.precio)} anual · {planAnual.vigenciaDias} días</p>}
        </div>
        <ul className="flex-1 space-y-2 text-sm">
          <li>{plan.maxApis == null ? 'APIs ilimitadas' : plan.maxApis + (plan.maxApis === 1 ? ' API' : ' APIs')}</li>
          <li>{plan.cuotaPeticiones.toLocaleString('es-GT')} peticiones</li>
          <li>{plan.maxMiembros == null ? 'Miembros ilimitados' : plan.maxMiembros + (plan.maxMiembros === 1 ? ' miembro' : ' miembros')}</li>
          {plan.dominioPropio && <li>Dominio propio</li>}
          {plan.esPrueba && <li>Prueba gratis por {plan.vigenciaDias} días</li>}
        </ul>
        {plan.id === actual.data?.plan.id
          ? <Boton disabled className="w-full">Plan actual</Boton>
          : plan.esPrueba
            ? <Boton disabled className="w-full">Prueba</Boton>
          : plan.nombre === 'Escala mensual' && planAnual
            ? <div className="mt-auto flex flex-col gap-3">
              <Link className="inline-flex h-[46px] items-center justify-center rounded-base bg-principal px-4 text-sm font-medium text-white" to={rutaPlan(plan.id)}>{cambiar ? 'Cambiar mensual' : 'Contratar mensual'}</Link>
              <Link className="inline-flex h-[46px] items-center justify-center rounded-base border border-borde-campo px-4 text-sm font-medium text-tinta" to={rutaPlan(planAnual.id)}>{cambiar ? 'Cambiar anual' : 'Contratar anual'}</Link>
            </div>
          : <Link className="inline-flex h-[46px] items-center justify-center rounded-base bg-principal px-5 font-medium text-white hover:bg-principal-hover" to={rutaPlan(plan.id)}>{cambiar ? 'Cambiar plan' : 'Contratar'}</Link>}
      </article>)}
    </section>
  </main>;
}
