import { isRouteErrorResponse, useRouteError } from 'react-router';
import { EstadoError } from '@shapi/ui';
import { pagina } from '../modulos/navegador';
import PaginaError404 from './Error-404';

/**
 * Error de una ruta: un 404 muestra "Página no encontrada"; cualquier otro error (por ejemplo, una página diferida que
 * no se pudo descargar) muestra el aviso de error con "Reintentar", que vuelve a cargar la página (11 §4).
 */
export function ErrorRuta() {
  const error = useRouteError();
  if (isRouteErrorResponse(error) && error.status === 404) return <PaginaError404 />;
  return <div className="p-8"><EstadoError reintentar={() => pagina.recargar()} /></div>;
}
