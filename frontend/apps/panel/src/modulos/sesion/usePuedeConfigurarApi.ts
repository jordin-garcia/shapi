import { useSesion } from './useSesion';

/** RF-07: solo el propietario y el editor modifican APIs, rutas, planes, marca y dominios. */
export function usePuedeConfigurarApi() {
  const { data: sesion } = useSesion();
  return sesion?.rol === 'propietario' || sesion?.rol === 'editor';
}
