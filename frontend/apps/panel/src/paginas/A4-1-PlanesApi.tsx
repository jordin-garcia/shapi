import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/planes';
import type { paths as apisPaths } from '@shapi/api/apis';
import { Aviso, Boton, Campo, Selector, Tabla, Tarjeta, EstadoCargando, EstadoError } from '@shapi/ui';
import { useParams } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
const clienteApis = crearCliente<apisPaths>(window.location.origin);

type PlanApi = components['schemas']['PlanApi'];

export default function PaginaA41PlanesApi() {
  const { id: apiId } = useParams<{ id: string }>();
  const cache = useQueryClient();
  const [vista, setVista] = useState<'lista' | 'nuevo' | PlanApi>('lista');

  const consulta = useQuery({
    queryKey: ['planes', apiId],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/apis/{apiId}/planes', {
        params: { path: { apiId: apiId! } },
        signal,
      });
      if (!response.ok || !data) throw new Error('No se pudieron cargar los planes.');
      return data;
    },
    retry: false,
  });

  const consultaApis = useQuery({
    queryKey: ['apis'],
    queryFn: async ({ signal }) => {
      const { data, response } = await clienteApis.GET('/api/apis', { signal });
      if (!response.ok || !data) throw new Error('Error al cargar APIs.');
      return data;
    },
    staleTime: 60000,
  });
  const nombreApi = consultaApis.data?.elementos.find(a => a.id === apiId)?.nombre ?? 'API';

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) {
    return <EstadoError mensaje="No se pudieron cargar los planes." reintentar={() => void consulta.refetch()} />;
  }

  const planes = consulta.data;


  if (vista !== 'lista') {
    return (
      <FormularioPlan apiNombre={nombreApi}
        apiId={apiId!}
        planOriginal={vista === 'nuevo' ? undefined : vista}
        alTerminar={() => {
          setVista('lista');
          void cache.invalidateQueries({ queryKey: ['planes', apiId] });
        }}
        alCancelar={() => setVista('lista')}
      />
    );
  }

  return (
    <div>
      <div className="flex items-end justify-between gap-6">
        <div className="flex flex-col gap-[10px]">
          <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">{nombreApi}</p>
          <h1 className="font-display text-[32px] leading-[1.2]">Planes de la API</h1>
          <p className="text-[15px] leading-[1.55] text-tinta-suave">Sus consumidores contratan estos planes desde su portal.</p>
        </div>
        {planes.length > 0 && <Boton type="button" onClick={() => setVista('nuevo')}>Crear un plan</Boton>}
      </div>

      <Tarjeta className="mt-8 pt-0">
        <Tabla
          encabezados={['Plan', 'Descripción', 'Precio', 'Vigencia', 'Cuota mensual', 'Límite por minuto', 'Acción']}
          filas={planes.map(plan => [
            <span key="nombre" className="font-medium text-tinta">{plan.nombre}</span>,
            <span key="desc" className="line-clamp-2" title={plan.descripcion}>{plan.descripcion}</span>,
            <span key="precio">{plan.esGratuito ? 'Gratis' : `Q ${plan.precio.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`}</span>,
            <span key="vigencia">{plan.vigenciaDias} días</span>,
            <span key="cuota">{plan.cuotaLlamadas.toLocaleString('en-US')} llamadas</span>,
            <span key="limite">{plan.limiteMinuto.toLocaleString('en-US')} peticiones</span>,
            <button key="accion" type="button" onClick={() => setVista(plan)} className="font-medium text-principal hover:underline text-right w-full block">Editar</button>
          ])}
        />

        {planes.length === 0 && (
          <div className="flex flex-col items-center gap-6 pb-11 pt-16 text-center">
            <svg aria-hidden="true" width="40" height="40" viewBox="0 0 40 40" fill="none">
              <rect x="1" y="1" width="38" height="38" rx="8" stroke="var(--borde-campo)" strokeWidth="1.5" strokeDasharray="4 4" />
              <path d="M20 13 V27 M13 20 H27" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
            </svg>
            <div className="flex max-w-[500px] flex-col gap-[10px]">
              <h2 className="font-display text-[22px] leading-[1.3]">Todavía no tiene planes para esta API</h2>
              <p className="text-[15px] leading-[1.55] text-tinta-suave">Cree un plan con su precio, vigencia, cuota mensual y límite por minuto. Sus consumidores lo contratarán desde su portal.</p>
            </div>
            <Boton type="button" onClick={() => setVista('nuevo')}>Crear un plan</Boton>
          </div>
        )}
      </Tarjeta>
    </div>
  );
}

function FormularioPlan({ apiId, apiNombre, planOriginal, alTerminar, alCancelar }: { apiId: string, apiNombre: string, planOriginal?: PlanApi, alTerminar: () => void, alCancelar: () => void }) {
  const esNuevo = !planOriginal;
  const [nombre, setNombre] = useState(planOriginal?.nombre ?? '');
  const [descripcion, setDescripcion] = useState(planOriginal?.descripcion ?? '');
  const [precioStr, setPrecioStr] = useState(planOriginal?.precio.toString() ?? '');
  const [esGratuito, setEsGratuito] = useState(planOriginal?.esGratuito ?? false);
  const [vigenciaDias, setVigenciaDias] = useState(planOriginal?.vigenciaDias.toString() ?? '30');
  const [cuotaLlamadas, setCuotaLlamadas] = useState(planOriginal?.cuotaLlamadas.toString() ?? '');
  const [limiteMinuto, setLimiteMinuto] = useState(planOriginal?.limiteMinuto.toString() ?? '');

  const [errorGeneral, setErrorGeneral] = useState<string>();

  const mutacion = useMutation({
    mutationFn: async () => {
      const cuerpo = {
        nombre,
        descripcion,
        precio: esGratuito ? 0 : Number(precioStr) || 0,
        esGratuito,
        vigenciaDias: Number(vigenciaDias) || 0,
        cuotaLlamadas: Number(cuotaLlamadas) || 0,
        limiteMinuto: Number(limiteMinuto) || 0,
      };

      let res;
      if (esNuevo) {
        res = await cliente.POST('/api/apis/{apiId}/planes', {
          params: { path: { apiId } },
          body: cuerpo,
        });
      } else {
        res = await cliente.PUT('/api/apis/{apiId}/planes/{planId}', {
          params: { path: { apiId, planId: planOriginal.id } },
          body: cuerpo,
        });
      }

      if (!res.response.ok || !res.data) {
        throw new Error(esNuevo ? 'No se pudo crear el plan.' : 'No se pudo editar el plan.');
      }
      return res.data;
    },
    onSuccess: alTerminar,
    onError: error => {
      if (error instanceof ErrorApi) {
        setErrorGeneral(error.titulo);
        return;
      }
      setErrorGeneral(esNuevo ? 'No se pudo crear el plan. Inténtelo de nuevo.' : 'No se pudo editar el plan. Inténtelo de nuevo.');
    },
  });

  function enviar(evento: React.FormEvent) {
    evento.preventDefault();
    setErrorGeneral(undefined);
    mutacion.mutate();
  }

  return (
    <div>
      <div className="flex flex-col gap-[10px]">
        <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">{apiNombre}</p>
        <h1 className="font-display text-[32px] leading-[1.2]">{esNuevo ? 'Crear un plan' : 'Editar plan'}</h1>
        <p className="text-[15px] leading-[1.55] text-tinta-suave">Sus consumidores ven estos datos en la sección de planes de su portal.</p>
      </div>

      <form onSubmit={enviar} className="grid grid-cols-1 md:grid-cols-[1fr_440px] gap-6 mt-8 items-start">
        <Tarjeta className="flex flex-col">
          <h2 className="font-display text-[21px] leading-[1.3]">Datos del plan</h2>
          <div className="flex flex-col gap-[18px] border-t border-borde-fila mt-5 pt-5">
            <Campo required etiqueta="Nombre" value={nombre} onChange={e => setNombre(e.target.value)} placeholder="Nombre que verán sus consumidores" />
            <div className="flex flex-col gap-[6px]">
              <label htmlFor="descripcion" className="text-[13px] font-semibold text-tinta">Descripción</label>
              <textarea
                id="descripcion"
                required
                className="min-h-[100px] rounded-base border px-[14px] py-[10px] text-[15px] bg-panel text-tinta outline-none transition-colors placeholder:text-tinta-inactiva focus:ring-[3px] focus:ring-anillo-foco border-borde-campo focus:border-principal"
                value={descripcion}
                onChange={e => setDescripcion(e.target.value)}
                placeholder="Para quién es este plan"
              />
            </div>
            
            <div className="grid grid-cols-2 gap-5">
              <div className="flex flex-col gap-[7px]">
                <label htmlFor="cuotaLlamadas" className="text-[13px] font-semibold text-tinta">Cuota mensual</label>
                <div className="flex items-center gap-3">
                  <input id="cuotaLlamadas" required type="number" min="1" className="flex-grow h-12 rounded-base border border-borde-campo px-[14px] text-[15px] bg-panel outline-none focus:border-principal focus:ring-[3px] focus:ring-anillo-foco tabular-nums" value={cuotaLlamadas} onChange={e => setCuotaLlamadas(e.target.value)} placeholder="Por ejemplo, 1,000" />
                  <span className="text-[14px] text-tinta-suave whitespace-nowrap">llamadas</span>
                </div>
              </div>
              <div className="flex flex-col gap-[7px]">
                <label htmlFor="limiteMinuto" className="text-[13px] font-semibold text-tinta">Límite por minuto</label>
                <div className="flex items-center gap-3">
                  <input id="limiteMinuto" required type="number" min="1" className="flex-grow h-12 rounded-base border border-borde-campo px-[14px] text-[15px] bg-panel outline-none focus:border-principal focus:ring-[3px] focus:ring-anillo-foco tabular-nums" value={limiteMinuto} onChange={e => setLimiteMinuto(e.target.value)} placeholder="Por ejemplo, 10" />
                  <span className="text-[14px] text-tinta-suave whitespace-nowrap">peticiones</span>
                </div>
              </div>
            </div>
          </div>
        </Tarjeta>

        <Tarjeta className="flex flex-col">
          <h2 className="font-display text-[21px] leading-[1.3]">Precio y vigencia</h2>
          <div className="flex flex-col gap-[18px] border-t border-borde-fila mt-5 pt-5">
            
            <label className={`flex items-start gap-3 rounded-base p-[13px] border cursor-pointer ${esGratuito ? 'border-principal ring-[3px] ring-anillo-foco' : 'border-borde-campo'}`}>
              <div className={`mt-[2px] w-[18px] h-[18px] rounded flex items-center justify-center shrink-0 border-[1.5px] ${esGratuito ? 'border-principal bg-principal' : 'border-borde-campo bg-white'}`}>
                {esGratuito && <svg width="12" height="12" viewBox="0 0 12 12" fill="none"><path d="M2.5 6.2 L5 8.5 L9.5 3.5" stroke="#FFFFFF" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" /></svg>}
              </div>
              <input type="checkbox" className="sr-only" checked={esGratuito} onChange={e => setEsGratuito(e.target.checked)} />
              <div className="flex flex-col gap-[2px]">
                <span className="text-[15px] font-medium text-tinta leading-[1.5]">Plan gratuito</span>
                <span className="text-[13px] text-tinta-suave leading-[1.5]">Al contratarlo, a su consumidor no se le pedirá tarjeta.</span>
              </div>
            </label>

            <div className="flex flex-col gap-[7px]">
              <label htmlFor="precio" className={`text-[13px] font-semibold ${esGratuito ? 'text-tinta-inactiva' : 'text-tinta'}`}>Precio</label>
              <div className={`flex gap-2 items-center h-12 rounded-base border px-[14px] text-[15px] outline-none tabular-nums ${esGratuito ? 'border-borde bg-fondo text-tinta-inactiva' : 'border-borde-campo bg-panel focus-within:border-principal focus-within:ring-[3px] focus-within:ring-anillo-foco'}`}>
                <span className={esGratuito ? 'text-tinta-inactiva' : 'text-tinta-suave'}>Q</span>
                <input id="precio" required={!esGratuito} disabled={esGratuito} type="number" step="0.01" min="0" className="flex-grow bg-transparent outline-none" value={esGratuito ? '0.00' : precioStr} onChange={e => setPrecioStr(e.target.value)} />
              </div>
              {esGratuito && <p className="text-[13px] text-tinta-suave leading-[1.5]">Un plan gratuito queda en Q 0.00.</p>}
            </div>

            <div className="flex flex-col gap-[7px]">
              <label className="text-[13px] font-semibold text-tinta">Vigencia</label>
              <Selector 
                value={vigenciaDias} 
                onChange={e => setVigenciaDias(e.target.value)}
                opciones={Array.from(new Set(['30', '365', vigenciaDias])).sort((a, b) => Number(a) - Number(b)).map(v => ({ valor: v, etiqueta: `${v} días` }))} 
              />
            </div>
          </div>

          {errorGeneral && <div className="mt-4"><Aviso estado="error">{errorGeneral}</Aviso></div>}

          <div className="flex flex-col gap-[14px] border-t border-borde-fila mt-6 pt-5">
            <Boton type="submit" deshabilitado={mutacion.isPending}>{mutacion.isPending ? 'Guardando...' : (esNuevo ? 'Crear plan' : 'Guardar plan')}</Boton>
            <p className="text-[14px] text-tinta-suave text-center"><button type="button" onClick={alCancelar} className="hover:underline hover:text-principal">Volver a los planes</button></p>
          </div>
        </Tarjeta>
      </form>
    </div>
  );
}
