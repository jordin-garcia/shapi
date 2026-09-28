import { useQuery } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import { useNavigate, useParams, useLocation } from 'react-router';
import { EstadoCargando, EstadoError, Selector } from '@shapi/ui';
import type { paths } from '@shapi/api/apis';

const cliente = crearCliente<paths>(window.location.origin);

export function SelectorApi() {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const { data, isPending, error, refetch } = useQuery({
    queryKey: ['apis'],
    queryFn: async ({ signal }) => {
      try {
        const { data, response } = await cliente.GET('/api/apis', { signal });
        if (response.status === 404 || response.status === 501) return { elementos: [] };
        if (!response.ok || !data) throw new Error('No se pudieron cargar las APIs');
        return data;
      } catch (error) {
        if (error instanceof ErrorApi && [404, 501].includes(error.estado)) return { elementos: [] };
        throw error;
      }
    },
    retry: false,
  });
  if (isPending) return <EstadoCargando />;
  if (error) return <EstadoError reintentar={() => void refetch()} />;
  const apis = data?.elementos || [];
  if (!apis.length) return <div className="border border-borde-campo rounded-base px-3 py-[9px] text-[14px] leading-[1.5] text-tinta bg-panel">Sin APIs</div>;
  return <Selector aria-label="API" className="w-full" value={id || ''} opciones={[
    { etiqueta: 'Seleccione una API', valor: '', deshabilitada: true },
    ...apis.flatMap(api => api.id ? [{ etiqueta: api.nombre ?? api.id, valor: api.id }] : []),
  ]} onChange={evento => {
    const nuevoId = evento.target.value;
    if (!nuevoId) return;
    const segmentos = location.pathname.split('/');
    if (id) segmentos[3] = encodeURIComponent(nuevoId);
    const destino = id ? segmentos.join('/') : `/panel/apis/${encodeURIComponent(nuevoId)}/especificacion`;
    void navigate(destino + location.search + location.hash);
  }} />;
}
