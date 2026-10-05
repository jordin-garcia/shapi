import { isRouteErrorResponse, useRouteError } from 'react-router';
import { EstadoError } from '@shapi/ui';
import Error404 from './Error-404';

/**
 * Error de una ruta del portal: un 404 muestra "Página no encontrada"; cualquier otro error de render muestra el aviso
 * de error con "Reintentar" en español (11 §4), en vez de la pantalla de React Router en inglés.
 */
export function ErrorRuta() {
  const error = useRouteError();
  if (isRouteErrorResponse(error) && error.status === 404) return <Error404 />;
  return <div className="p-11"><EstadoError reintentar={() => window.location.reload()} /></div>;
}
