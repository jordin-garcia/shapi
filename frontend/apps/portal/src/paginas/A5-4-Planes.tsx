import { useState } from 'react';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';
import { PlanCard, PlanVacio, useElegirPlan, usePlanesPortal, type Contratacion, type PlanApi } from '../modulos/suscripcion/planes';
import Confirmacion from './A5-4b-Confirmacion';

export default function Planes() {
  const marca = useMarcaPortal();
  const consulta = usePlanesPortal();
  const { elegir } = useElegirPlan();
  const [aviso, setAviso] = useState<string>();
  const [resultado, setResultado] = useState<{ plan: PlanApi; datos: Contratacion }>();
  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudieron cargar los planes." reintentar={() => void consulta.refetch()} />;
  if (resultado) return <Confirmacion plan={resultado.plan} contratacion={resultado.datos} />;
  const contratar = (plan: PlanApi) => { setAviso(undefined); void elegir(plan, (seleccion, datos) => setResultado({ plan: seleccion, datos }), setAviso); };
  return <section className="mx-auto max-w-[1120px] px-11 py-11"><header><p className="text-[12px] font-medium uppercase tracking-[.16em] text-[var(--marca-principal)]">{marca.nombreApi}</p><h1 className="mt-2 font-display text-[32px] leading-[1.2]">Planes de la API</h1><p className="mt-3 text-[15px] leading-[1.55] text-tinta-suave">Al contratar un plan recibe sus claves de producción y de pruebas.</p></header>{aviso && <div className="mt-6 rounded-base border border-[#EDC3B4] bg-[#FBE9E3] px-4 py-3 text-[14px] text-[#8E3315]" role="alert">{aviso}</div>}{consulta.data.length === 0 ? <PlanVacio nombrePortal={marca.nombrePortal} /> : <div className="mt-8 grid grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-3">{consulta.data.map(plan => <PlanCard key={plan.id} plan={plan} contratar={contratar} />)}</div>}</section>;
}
