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
  const parametrosDeRuta = ruta.parametros.filter(parametro =>
    parametro.ubicacion === 'ruta' || ruta.patron.includes(`{${parametro.nombre}}`));
  const usadosEnRuta = new Set(parametrosDeRuta.map(parametro => parametro.nombre));
  let urlTexto = ruta.urlCompleta.replace(/\{([^}]+)\}/g, (_coincidencia, nombre: string) => {
    return encodeURIComponent(valores[nombre] ?? '');
  });
  const metodo = ruta.metodo.toUpperCase();
  const headers: Record<string, string> = { Accept: 'application/json', 'X-Api-Key': clave };
  ruta.parametros
    .filter(parametro => parametro.ubicacion === 'cabecera' && valores[parametro.nombre]?.trim())
    .forEach(parametro => { headers[parametro.nombre] = valores[parametro.nombre]; });

  const url = new URL(urlTexto);
  ruta.parametros
    .filter(parametro => !usadosEnRuta.has(parametro.nombre)
      && parametro.ubicacion === 'consulta' && valores[parametro.nombre]?.trim())
    .forEach(parametro => url.searchParams.set(
      parametro.nombre,
      textoParaUrl(valorTipado(valores[parametro.nombre], parametro.tipo)),
    ));
  urlTexto = url.toString();

  const parametrosCuerpo = ruta.parametros.filter(parametro => parametro.ubicacion === 'cuerpo');
  let body: string | undefined;
  if (parametrosCuerpo.length > 0) {
    headers['Content-Type'] = 'application/json';
    const cuerpoCompleto = parametrosCuerpo.length === 1
      && parametrosCuerpo[0].representaCuerpoCompleto;
    const cuerpo = cuerpoCompleto
      ? valorTipado(valores.cuerpo, parametrosCuerpo[0].tipo)
      : Object.fromEntries(parametrosCuerpo
        .filter(parametro => valores[parametro.nombre]?.trim())
        .map(parametro => [parametro.nombre, valorTipado(valores[parametro.nombre], parametro.tipo)]));
    body = JSON.stringify(cuerpo);
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
    const valor = parametro.representaCuerpoCompleto
      ? ejemplo
      : objeto[parametro.nombre];
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
