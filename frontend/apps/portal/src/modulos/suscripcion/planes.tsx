import { useMutation, useQuery } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/planes';
import type { components as componentesSuscripciones, paths as rutasSuscripciones } from '@shapi/api/suscripciones';
import { Aviso, Boton, EstadoCargando, EstadoError } from '@shapi/ui';
import { useNavigate } from 'react-router';
import { useSesionConsumidor } from '../sesion/useSesionConsumidor';

const clientePlanes = crearCliente<paths>(window.location.origin);
const clienteSuscripciones = crearCliente<rutasSuscripciones>(window.location.origin);

export type PlanApi = components['schemas']['PlanApi'];
export type Contratacion = componentesSuscripciones['schemas']['Contratacion'];
export const clavePlanesPortal = ['portal', 'planes'] as const;

export function usePlanesPortal() {
  return useQuery({
    queryKey: clavePlanesPortal,
    queryFn: async ({ signal }) => {
      const { data, response } = await clientePlanes.GET('/api/portal/planes', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron cargar los planes.');
      return data.filter(plan => plan.activo);
    }, retry: false, staleTime: 5 * 60 * 1000,
  });
}

export function dinero(valor: number) {
  return `Q ${valor.toLocaleString('es-GT', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}
export function numero(valor: number) { return valor.toLocaleString('es-GT'); }
export function textoCuota(plan: PlanApi) { return `${numero(plan.cuotaLlamadas)} llamadas al mes`; }
export function textoLimite(plan: PlanApi) { return `${numero(plan.limiteMinuto)} peticiones por minuto`; }

export function PlanVacio({ nombrePortal = 'esta API' }: { nombrePortal?: string }) {
  return <section className="mt-8 rounded-base border border-borde bg-panel p-20 text-center"><div className="mx-auto flex max-w-[500px] flex-col items-center gap-6"><div aria-hidden="true" className="flex size-10 items-center justify-center rounded-base border-2 border-dashed border-[#C9D2E1] text-[#C9D2E1]">≡</div><div><h2 className="font-display text-[22px] leading-[1.3]">Todavía no hay planes para esta API</h2><p className="mt-3 text-[15px] leading-[1.55] text-tinta-suave">Cuando {nombrePortal} publique sus planes, podrá contratarlos desde aquí.</p></div></div></section>;
}

export function PlanCard({ plan, contratar }: { plan: PlanApi; contratar: (plan: PlanApi) => void }) {
  return <article className="flex flex-col gap-5 rounded-base border border-borde bg-panel p-6"><h2 className="font-display text-[21px] leading-[1.3]">{plan.nombre}</h2><p className="text-[14px] leading-[1.5] text-tinta-suave">{plan.descripcion}</p><div className="flex flex-col gap-1 border-t border-borde pt-5"><span className="font-display text-[28px] leading-none text-tinta">{dinero(plan.precio)}</span><span className="text-[13px] text-tinta-suave">Vigencia de {plan.vigenciaDias} días</span></div><div className="flex flex-col gap-3 text-[13px] text-tinta-suave"><span className="rounded-full bg-[#F4F6FA] px-3 py-2">{textoCuota(plan)}</span><span className="rounded-full bg-[#F4F6FA] px-3 py-2">{textoLimite(plan)}</span></div><Boton type="button" className="mt-auto w-full" onClick={() => contratar(plan)}>Contratar</Boton></article>;
}

export function useContratacionPortal() {
  return useMutation({
    mutationFn: async ({ planId, tarjeta }: { planId: string; tarjeta?: componentesSuscripciones['schemas']['Tarjeta'] }) => {
      const { data, response } = await clienteSuscripciones.POST('/api/portal/suscripciones', { body: tarjeta ? { planId, tarjeta } : { planId } });
      if (!response.ok || !data) throw new Error('No se pudo completar la contratación.');
      return data;
    },
  });
}

export function useElegirPlan() {
  const navegar = useNavigate();
  const sesion = useSesionConsumidor({ enabled: false });
  const contratacion = useContratacionPortal();
  async function elegir(plan: PlanApi, alContratar: (plan: PlanApi, datos: Contratacion) => void, alAviso: (mensaje: string) => void) {
    const actual = sesion.data ?? (await sesion.refetch()).data;
    if (!actual) { await navegar('/entrar'); return; }
    if (!actual.correoVerificado) { alAviso('Verifique su correo antes de contratar un plan.'); return; }
    if (!plan.esGratuito) { await navegar(`/contratar/${plan.id}`); return; }
    try { alContratar(plan, await contratacion.mutateAsync({ planId: plan.id })); }
    catch (error) { alAviso(error instanceof ErrorApi ? error.titulo : 'No se pudo completar la contratación.'); }
  }
  return { elegir, contratacion };
}

export function PlanesError({ reintentar }: { reintentar: () => void }) { return <EstadoError mensaje="No se pudieron cargar los planes." reintentar={reintentar} />; }
export function PlanesCargando() { return <EstadoCargando />; }
export function ErrorContratacion({ mensaje }: { mensaje: string }) { return <Aviso estado="error">{mensaje}</Aviso>; }
