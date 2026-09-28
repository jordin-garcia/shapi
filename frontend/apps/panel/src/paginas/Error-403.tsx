import { Link } from 'react-router';
import { Boton, EstadoError, EstadoSinPermiso } from '@shapi/ui';
import { useSesion } from '../modulos/sesion/useSesion';
import { useCerrarSesion } from '../modulos/sesion/useCerrarSesion';

/**
 * "No tiene permiso para ver esta página" (11 §4). Si la persona entró al área de otro rol (panel o administración),
 * no hay barra lateral: se le ofrece ir a su panel, según su destino de 10 §1, o cerrar sesión.
 */
export default function PaginaError403({ conSalida = false }: { conSalida?: boolean }) {
  const { data } = useSesion();
  const salir = useCerrarSesion();
  if (!conSalida) return <EstadoSinPermiso />;
  return (
    <div>
      <EstadoSinPermiso />
      <div className="px-8 flex flex-col gap-4 items-start">
        <div className="flex gap-3">
          {data && <Link to={data.destino} className="h-[46px] rounded-base px-6 text-[15px] font-medium inline-flex items-center bg-principal text-white hover:bg-principal-hover">Ir a su panel</Link>}
          <Boton principal={false} onClick={() => salir.mutate()} disabled={salir.isPending}>Cerrar sesión</Boton>
        </div>
        {salir.isError && <EstadoError mensaje="No se pudo cerrar la sesión." reintentar={() => salir.mutate()} />}
      </div>
    </div>
  );
}
