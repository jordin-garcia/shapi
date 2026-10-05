import type { QueryClient } from '@tanstack/react-query';
import { claveConfiguracionPortal } from '../configuracion/useConfiguracionPortal';
import { claveSesionConsumidor } from './useSesionConsumidor';

function esClave(clave: readonly unknown[], esperada: readonly unknown[]) {
  return clave.length === esperada.length && esperada.every((parte, indice) => clave[indice] === parte);
}

/** Lo que no es del consumidor: la configuración pública del portal y la consulta de la sesión, que maneja quien llama. */
function esDelConsumidor(clave: readonly unknown[]) {
  return !esClave(clave, claveConfiguracionPortal) && !esClave(clave, claveSesionConsumidor);
}

/**
 * Borra de la caché los datos del consumidor (claves, pagos, consumo…), para que otro consumidor en el mismo navegador
 * no los vea primero (H-76 y H-110 de la auditoría del 3 oct). Se llama al cerrar sesión, cuando la sesión venció y al
 * entrar.
 */
export async function limpiarCacheConsumidor(cache: QueryClient) {
  const predicate = (consulta: { queryKey: readonly unknown[] }) => esDelConsumidor(consulta.queryKey);
  await cache.cancelQueries({ predicate });
  cache.removeQueries({ predicate });
}
