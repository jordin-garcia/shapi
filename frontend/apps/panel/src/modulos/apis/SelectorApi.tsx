import { useQuery } from '@tanstack/react-query';
import { crearCliente, ProblemDetailsError } from '@shapi/api';
import { useNavigate, useParams, useLocation } from 'react-router';
import { EstadoCargando, EstadoError } from '@shapi/ui';
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
        if (error instanceof ProblemDetailsError && [404, 501].includes(error.status ?? 0)) return { elementos: [] };
        throw error;
      }
    },
    retry: false,
  });
  if (isPending) return <EstadoCargando />;
  if (error) return <EstadoError reintentar={() => void refetch()} />;
  const apis = data?.elementos || [];
  if (!apis.length) return <div className="border border-[#2A3550] rounded-lg px-3 py-2 text-sm text-[#E8EDF7] bg-[#0C1220]">Sin APIs</div>;
  return <select aria-label="API" value={id || ''} className="w-full border border-[#2A3550] rounded-lg px-3 py-2 text-sm font-medium text-[#E8EDF7] bg-[#0C1220]" onChange={event => {
    const nuevoId = event.target.value;
    if (!nuevoId) return;
    const segmentos = location.pathname.split('/');
    if (id) segmentos[3] = encodeURIComponent(nuevoId);
    const destino = id ? segmentos.join('/') : `/panel/apis/${encodeURIComponent(nuevoId)}/especificacion`;
    void navigate(destino + location.search + location.hash);
  }}>
    <option value="" disabled>Seleccione una API</option>
    {apis.map(api => <option key={api.id} value={api.id}>{api.nombre}</option>)}
  </select>;
}
