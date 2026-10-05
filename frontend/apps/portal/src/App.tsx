import { RouterProvider, type RouterProviderProps } from 'react-router';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { ApiNoDisponible, ProveedorConfiguracionPortal, useConfiguracionPortal } from './modulos/configuracion/useConfiguracionPortal';
import { coloresMarca } from './modulos/configuracion/coloresMarca';
import { router } from './rutas';

export default function App({ enrutador = router }: { enrutador?: RouterProviderProps['router'] }) {
  const consulta = useConfiguracionPortal();

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.error instanceof ApiNoDisponible) {
    return (
      <main className="min-h-screen bg-fondo flex items-center justify-center p-11">
        <div className="max-w-lg text-center">
          <h1 className="font-display text-titulo text-tinta">API no disponible</h1>
          <p className="mt-3 text-tinta-suave">La API no existe o no está publicada.</p>
        </div>
      </main>
    );
  }
  if (consulta.isError || !consulta.data) {
    return <EstadoError reintentar={() => void consulta.refetch()} />;
  }

  return (
    <div style={coloresMarca(consulta.data.colorPrincipal)} className="min-h-screen bg-fondo text-tinta">
      <ProveedorConfiguracionPortal configuracion={consulta.data}>
        <RouterProvider router={enrutador} />
      </ProveedorConfiguracionPortal>
    </div>
  );
}
