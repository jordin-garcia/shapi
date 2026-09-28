import { Navigate } from 'react-router';
import { useSesion } from './useSesion';

/** Ruta índice de /panel y /admin: lleva al destino del rol (10 §1). La sesión ya la consultó RequiereSesion. */
export function IrAlDestino() {
  const { data } = useSesion();
  return data ? <Navigate to={data.destino} replace /> : null;
}
