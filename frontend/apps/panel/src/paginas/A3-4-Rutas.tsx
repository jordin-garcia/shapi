import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/apis';
import { Aviso, Boton, EstadoCargando, EstadoError, Tarjeta } from '@shapi/ui';
import { useParams } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
type Ruta = components['schemas']['RutaAdministrada'];

export default function PaginaA34Rutas() {
  const { id = '' } = useParams();
  const cache = useQueryClient();
  const [cambios, setCambios] = useState<Record<string, boolean>>({});
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
  const rutas: Ruta[] = (consulta.data?.elementos ?? []).map(ruta => ({
    ...ruta,
    expuesta: cambios[ruta.id] ?? ruta.expuesta,
  }));

  const guardar = useMutation({
    mutationFn: async () => {
      const { data, response } = await cliente.PUT('/api/apis/{id}/rutas/exposicion', {
        params: { path: { id }, header: { 'X-Requested-With': 'shapi' } },
        body: rutas.map(ruta => ({ rutaId: ruta.id, expuesta: ruta.expuesta })),
      });
      if (!response.ok || !data) throw new Error('No se pudieron guardar las rutas.');
      return data;
    },
    onSuccess: async data => {
      cache.setQueryData(['rutas-api', id], data);
      setCambios({});
      setMensaje('Las rutas expuestas se guardaron.');
      setError(undefined);
    },
    onError: fallo => {
      setMensaje(undefined);
      setError(fallo instanceof ErrorApi ? fallo.titulo : 'No se pudieron guardar las rutas. Inténtelo de nuevo.');
    },
  });

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudieron cargar las rutas." reintentar={() => void consulta.refetch()} />;

  const expuestas = rutas.filter(ruta => ruta.expuesta).length;
  const ocultas = rutas.length - expuestas;

  return <div>
    <header className="flex flex-col gap-[10px]">
      <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">{consulta.data.apiNombre}</p>
      <h1 className="font-display text-[32px] leading-[1.2]">Rutas expuestas</h1>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">Marque cada ruta de su especificación como expuesta u oculta.</p>
    </header>

    <div className="mt-8 grid grid-cols-1 items-start gap-6 xl:grid-cols-[minmax(0,1fr)_400px]">
      <Tarjeta className="pb-2">
        <div className="w-full overflow-auto">
          <table className="w-full border-collapse text-left">
            <thead><tr>
              {['Método', 'Ruta', 'Resumen', 'Estado'].map(titulo => <th key={titulo} className="border-b border-borde pb-[10px] pr-4 text-encabezado font-semibold uppercase text-tinta-suave">{titulo}</th>)}
            </tr></thead>
            <tbody>{rutas.map(ruta => <tr key={ruta.id} className="border-b border-borde-fila">
              <td className="py-3 pr-4 text-[12px] font-semibold tracking-[0.06em] text-tinta-suave">{ruta.metodo}</td>
              <td className="py-3 pr-4 text-[15px] text-tinta">{ruta.patron}</td>
              <td className="py-3 pr-4 text-[14px] text-tinta-suave">{ruta.resumen ?? '—'}</td>
              <td className="py-3 text-[14px] text-tinta">
                <div className="flex gap-5">
                  <label className="flex items-center gap-2">
                    <input type="radio" name={`ruta-${ruta.id}`} checked={ruta.expuesta} onChange={() => setCambios(actuales => ({ ...actuales, [ruta.id]: true }))} aria-label={`Exponer ${ruta.metodo} ${ruta.patron}`} />
                    Expuesta
                  </label>
                  <label className="flex items-center gap-2">
                    <input type="radio" name={`ruta-${ruta.id}`} checked={!ruta.expuesta} onChange={() => setCambios(actuales => ({ ...actuales, [ruta.id]: false }))} aria-label={`Ocultar ${ruta.metodo} ${ruta.patron}`} />
                    Oculta
                  </label>
                </div>
              </td>
            </tr>)}</tbody>
          </table>
        </div>
      </Tarjeta>

      <Tarjeta className="flex flex-col">
        <h2 className="font-display text-[21px] leading-[1.3]">Resumen</h2>
        <div className="mt-5 flex flex-col gap-[14px] border-t border-borde-fila pt-5">
          <div className="flex justify-between gap-5 text-[14px]"><span className="text-tinta-suave">Rutas expuestas</span><span className="font-medium tabular-nums">{expuestas}</span></div>
          <div className="flex justify-between gap-5 text-[14px]"><span className="text-tinta-suave">Rutas ocultas</span><span className="font-medium tabular-nums">{ocultas}</span></div>
        </div>
        <p className="mt-5 text-[14px] leading-[1.5] text-tinta-suave">Solo las rutas expuestas quedan disponibles para sus consumidores.</p>
        {mensaje && <div className="mt-5"><Aviso estado="exito">{mensaje}</Aviso></div>}
        {error && <div className="mt-5"><Aviso estado="error">{error}</Aviso></div>}
        <Boton className="mt-6" type="button" deshabilitado={guardar.isPending || rutas.length === 0} onClick={() => guardar.mutate()}>
          {guardar.isPending ? 'Guardando…' : 'Guardar rutas expuestas'}
        </Boton>
      </Tarjeta>
    </div>
  </div>;
}
