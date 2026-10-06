import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/apis';
import { Aviso, Boton, EstadoCargando, EstadoError, Selector, Tarjeta } from '@shapi/ui';
import { useParams } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
type Ruta = components['schemas']['RutaAdministrada'];
type Edicion = { limiteMinuto: string; cacheSegundos: string; pesoLlamadas: string };

const opcionesCache = [
  { etiqueta: 'Desactivada', valor: '0' },
  { etiqueta: '30 segundos', valor: '30' },
  { etiqueta: '1 minuto', valor: '60' },
  { etiqueta: '5 minutos', valor: '300' },
  { etiqueta: '15 minutos', valor: '900' },
  { etiqueta: '1 hora', valor: '3600' },
  { etiqueta: '24 horas', valor: '86400' },
];

function desdeRuta(ruta: Ruta): Edicion {
  return {
    limiteMinuto: ruta.limiteMinuto?.toString() ?? '',
    cacheSegundos: ruta.cacheSegundos.toString(),
    pesoLlamadas: ruta.pesoLlamadas.toString(),
  };
}

const claseCampo = 'h-10 rounded-base border border-borde-campo bg-panel px-3 text-[14px] text-tinta outline-none focus:border-principal focus:ring-[3px] focus:ring-anillo-foco disabled:border-borde-inactivo disabled:bg-fondo disabled:text-tinta-inactiva';

export default function PaginaA35ConfigRutas() {
  const { id = '' } = useParams();
  const cache = useQueryClient();
  const [ediciones, setEdiciones] = useState<Record<string, Edicion>>({});
  const [mensaje, setMensaje] = useState<string>();
  const [error, setError] = useState<string>();
  const consulta = useQuery({
    queryKey: ['rutas-api', id],
    queryFn: async () => {
      const { data, response } = await cliente.GET('/api/apis/{id}/rutas', { params: { path: { id } } });
      if (!response.ok || !data) throw new Error('No se pudieron cargar las rutas.');
      return data;
    },
    enabled: Boolean(id),
  });

  const rutas = (consulta.data?.elementos ?? []).filter(ruta => ruta.expuesta);
  const guardar = useMutation({
    mutationFn: async () => {
      const { data, response } = await cliente.PUT('/api/apis/{id}/configuracion-rutas', {
        params: { path: { id }, header: { 'X-Requested-With': 'shapi' } },
        body: rutas.map(ruta => {
          const edicion = ediciones[ruta.id] ?? desdeRuta(ruta);
          return {
            rutaId: ruta.id,
            limiteMinuto: edicion.limiteMinuto === '' ? null : Number(edicion.limiteMinuto),
            cacheSegundos: ruta.metodo === 'GET' ? Number(edicion.cacheSegundos) : 0,
            pesoLlamadas: Number(edicion.pesoLlamadas),
          };
        }),
      });
      if (!response.ok || !data) throw new Error('No se pudo guardar la configuración.');
      return data;
    },
    onSuccess: data => {
      cache.setQueryData(['rutas-api', id], data);
      setEdiciones(Object.fromEntries(data.elementos.map(ruta => [ruta.id, desdeRuta(ruta)])));
      setMensaje('La configuración por ruta se guardó.');
      setError(undefined);
    },
    onError: fallo => {
      setMensaje(undefined);
      setError(fallo instanceof ErrorApi ? fallo.titulo : 'No se pudo guardar la configuración. Inténtelo de nuevo.');
    },
  });

  function cambiar(ruta: Ruta, campo: keyof Edicion, valor: string) {
    setEdiciones(actuales => ({ ...actuales, [ruta.id]: { ...(actuales[ruta.id] ?? desdeRuta(ruta)), [campo]: valor } }));
    setMensaje(undefined);
    setError(undefined);
  }

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudieron cargar las rutas." reintentar={() => void consulta.refetch()} />;

  return <div>
    <header className="flex flex-col gap-[10px]">
      <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">{consulta.data.apiNombre}</p>
      <h1 className="font-display text-[32px] leading-[1.2]">Configuración por ruta</h1>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">Defina el límite por minuto, la caché y el peso en llamadas de cada ruta expuesta.</p>
    </header>
    <Tarjeta className="mt-8">
      <div className="w-full overflow-auto">
        <table className="w-full min-w-[940px] border-collapse text-left">
          <thead><tr>{['Método', 'Ruta', 'Límite por minuto', 'Caché', 'Peso en llamadas'].map(titulo => <th key={titulo} className="border-b border-borde pb-[10px] pr-4 text-encabezado font-semibold uppercase text-tinta-suave">{titulo}</th>)}</tr></thead>
          <tbody>{rutas.map(ruta => {
            const edicion = ediciones[ruta.id] ?? desdeRuta(ruta);
            return <tr key={ruta.id} className="border-b border-borde-fila">
              <td className="py-3 pr-4 text-[12px] font-semibold tracking-[0.06em] text-tinta-suave">{ruta.metodo}</td>
              <td className="py-3 pr-4 text-[15px]">{ruta.patron}</td>
              <td className="py-3 pr-4"><label className="flex items-center gap-3"><input aria-label={`Límite por minuto de ${ruta.metodo} ${ruta.patron}`} className={`${claseCampo} w-[110px]`} type="number" min="1" value={edicion.limiteMinuto} onChange={evento => cambiar(ruta, 'limiteMinuto', evento.target.value)} /><span className="text-[13px] text-tinta-suave">peticiones</span></label></td>
              <td className="py-3 pr-4"><Selector aria-label={`Caché de ${ruta.metodo} ${ruta.patron}`} className="w-[200px]" value={ruta.metodo === 'GET' ? edicion.cacheSegundos : '0'} disabled={ruta.metodo !== 'GET'} opciones={opcionesCache} onChange={evento => cambiar(ruta, 'cacheSegundos', evento.target.value)} /></td>
              <td className="py-3"><label className="flex items-center gap-3"><input aria-label={`Peso en llamadas de ${ruta.metodo} ${ruta.patron}`} className={`${claseCampo} w-[72px]`} type="number" min="1" max="1000" value={edicion.pesoLlamadas} onChange={evento => cambiar(ruta, 'pesoLlamadas', evento.target.value)} /><span className="text-[13px] text-tinta-suave">{edicion.pesoLlamadas === '1' ? 'llamada' : 'llamadas'} por petición</span></label></td>
            </tr>;
          })}</tbody>
        </table>
      </div>
      {rutas.length === 0 && <Aviso estado="neutro">Exponga al menos una ruta antes de configurarla.</Aviso>}
      <div className="mt-2 flex items-center justify-between gap-8 border-t border-borde pt-5">
        <p className="max-w-[72ch] text-[14px] leading-[1.5] text-tinta-suave">El peso indica cuántas llamadas descuenta de la cuota del consumidor cada petición a esa ruta. El límite por minuto se cuenta en peticiones de cada consumidor y se aplica además del límite de su plan. La caché solo está disponible en rutas GET.</p>
        <Boton className="shrink-0" type="button" deshabilitado={guardar.isPending || rutas.length === 0} onClick={() => guardar.mutate()}>{guardar.isPending ? 'Guardando…' : 'Guardar configuración'}</Boton>
      </div>
      {mensaje && <div className="mt-4"><Aviso estado="exito">{mensaje}</Aviso></div>}
      {error && <div className="mt-4"><Aviso estado="error">{error}</Aviso></div>}
    </Tarjeta>
  </div>;
}
