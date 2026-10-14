import type { components } from '@shapi/api/portal';

export type RutaConsola = components['schemas']['RutaDocumentada'];

export const CLAVE_PRUEBAS_SESION = 'shapi:portal:clave-pruebas';

const CABECERAS_VISIBLES = [
  'X-Shapi-Plan',
  'X-RateLimit-Limit',
  'X-RateLimit-Remaining',
  'X-RateLimit-Reset',
  'X-Cuota-Limite',
  'X-Cuota-Restante',
  'X-Cuota-Reinicio',
] as const;

export interface ResultadoCompuerta {
  estado: number;
  cuerpo: string;
  cabeceras: { nombre: string; valor: string }[];
  error?: { codigo: string; mensaje: string };
}

function valorTipado(valor: string, tipo: string): unknown {
  if (tipo === 'number' || tipo === 'integer') {
    const numero = Number(valor);
    return Number.isNaN(numero) ? valor : numero;
  }
  if (tipo === 'boolean') return valor.toLowerCase() === 'true';
  if (tipo === 'array' || tipo === 'object') {
    try { return JSON.parse(valor) as unknown; } catch { return valor; }
  }
  return valor;
}

function textoParaUrl(valor: unknown): string {
  return typeof valor === 'string' ? valor : JSON.stringify(valor);
}

function prepararPeticion(ruta: RutaConsola, valores: Record<string, string>, clave: string) {
  const usadosEnRuta = new Set<string>();
  let urlTexto = ruta.urlCompleta.replace(/\{([^}]+)\}/g, (_coincidencia, nombre: string) => {
    usadosEnRuta.add(nombre);
    return encodeURIComponent(valores[nombre] ?? '');
  });
  const parametros = Object.fromEntries(ruta.parametros
    .filter(parametro => !usadosEnRuta.has(parametro.nombre) && valores[parametro.nombre]?.trim())
    .map(parametro => [parametro.nombre, valorTipado(valores[parametro.nombre], parametro.tipo)]));
  const metodo = ruta.metodo.toUpperCase();
  const llevaCuerpo = !['GET', 'HEAD', 'OPTIONS'].includes(metodo);
  const headers: Record<string, string> = { Accept: 'application/json', 'X-Api-Key': clave };
  let body: string | undefined;

  if (llevaCuerpo) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(parametros);
  } else {
    const url = new URL(urlTexto);
    Object.entries(parametros).forEach(([nombre, valor]) => url.searchParams.set(nombre, textoParaUrl(valor)));
    urlTexto = url.toString();
  }

  return { url: urlTexto, init: { method: metodo, headers, body } satisfies RequestInit };
}

function leerError(cuerpo: unknown): ResultadoCompuerta['error'] {
  if (!cuerpo || typeof cuerpo !== 'object' || !('error' in cuerpo)) return undefined;
  const error = (cuerpo as { error?: unknown }).error;
  if (!error || typeof error !== 'object') return undefined;
  const datos = error as { codigo?: unknown; mensaje?: unknown };
  if (typeof datos.codigo !== 'string' || typeof datos.mensaje !== 'string') return undefined;
  return { codigo: datos.codigo, mensaje: datos.mensaje };
}

function decodificarPlan(valor: string): string {
  try { return decodeURIComponent(valor); } catch { return valor; }
}

export async function enviarACompuerta(
  ruta: RutaConsola,
  valores: Record<string, string>,
  clave: string,
): Promise<ResultadoCompuerta> {
  const peticion = prepararPeticion(ruta, valores, clave);
  const respuesta = await fetch(peticion.url, peticion.init);
  const texto = await respuesta.text();
  let cuerpo: unknown = texto;
  if (texto) {
    try { cuerpo = JSON.parse(texto) as unknown; } catch { /* El origen puede devolver texto. */ }
  }
  const cabeceras = CABECERAS_VISIBLES.flatMap(nombre => {
    const valor = respuesta.headers.get(nombre);
    if (valor === null) return [];
    return [{ nombre, valor: nombre === 'X-Shapi-Plan' ? decodificarPlan(valor) : valor }];
  });

  return {
    estado: respuesta.status,
    cuerpo: texto ? (typeof cuerpo === 'string' ? cuerpo : JSON.stringify(cuerpo, null, 2)) : '(sin cuerpo)',
    cabeceras,
    error: leerError(cuerpo),
  };
}

export function valoresDeEjemplo(ruta: RutaConsola): Record<string, string> {
  const ejemplo = ruta.ejemploPeticion;
  const objeto = ejemplo && typeof ejemplo === 'object' && !Array.isArray(ejemplo)
    ? ejemplo as Record<string, unknown>
    : {};
  return Object.fromEntries(ruta.parametros.map(parametro => {
    const valor = objeto[parametro.nombre];
    return [parametro.nombre, valor === undefined || valor === null
      ? ''
      : typeof valor === 'string' ? valor : JSON.stringify(valor)];
  }));
}

export function tipoEnEspanol(tipo: string): string {
  const tipos: Record<string, string> = {
    string: 'texto', number: 'número', integer: 'número', boolean: 'booleano',
    array: 'lista', object: 'objeto',
  };
  return tipos[tipo] ?? tipo;
}
