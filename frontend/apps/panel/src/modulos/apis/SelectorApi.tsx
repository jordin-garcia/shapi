import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import { useNavigate, useParams, useLocation } from 'react-router';
import { Selector } from '@shapi/ui';

import type { paths } from '@shapi/api/apis';

const cliente = crearCliente<paths>('/api');

export function SelectorApi() {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();

  const { data } = useQuery({
    queryKey: ['apis'],
    queryFn: async () => {
      // Mientras DC-04 no exista, manejar error sin fallar o devolver vacío
      try {
        const { data, error } = await cliente.GET('/api/apis');
        if (error) return { elementos: [] };
        return data;
      } catch {
        return { elementos: [] };
      }
    },
    initialData: { elementos: [] }
  });

  const apis = data?.elementos || [];
  
  if (apis.length === 0) {
    return (
      <div className="flex items-center justify-between gap-2 border border-[#2A3550] rounded-lg px-3 py-2 text-sm font-medium text-[#E8EDF7] bg-[#0C1220]">
        <span className="truncate">Sin APIs</span>
      </div>
    );
  }

  const options = apis.map(api => ({ label: api.nombre || 'Sin nombre', value: api.id || '' }));
  
  return (
    <Selector 
      options={options} 
      value={id || ''} 
      onChange={(e) => {
        const newId = e.target.value;
        // Reemplazar el :id actual en la URL por el nuevo
        if (id && newId) {
          navigate(location.pathname.replace(id, newId));
        }
      }} 
    />
  );
}
