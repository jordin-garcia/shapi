import http from 'node:http';
import https from 'node:https';

interface OpcionesEsperaCorreo {
  baseUrl?: string;
  contiene?: string;
  intervaloMs?: number;
  tiempoLimiteMs?: number;
}

interface ResumenMensaje {
  ID?: string;
  To?: unknown;
}

interface ListaMensajes {
  messages?: ResumenMensaje[];
}

interface DetalleMensaje {
  HTML?: string;
  Text?: string;
}

const esperar = (milisegundos: number) => new Promise((resolver) => setTimeout(resolver, milisegundos));

function resolverLocal(_host: string, opciones: { all?: boolean }, callback: (...argumentos: unknown[]) => void) {
  if (opciones.all) {
    callback(null, [{ address: '127.0.0.1', family: 4 }]);
    return;
  }
  callback(null, '127.0.0.1', 4);
}

function leerJson<T>(url: URL): Promise<T> {
  const cliente = url.protocol === 'https:' ? https : http;
  return new Promise<T>((resolver, rechazar) => {
    const peticion = cliente.get(url, {
      rejectUnauthorized: false,
      lookup: resolverLocal,
      timeout: 5_000,
      ...(url.protocol === 'https:' ? { maxVersion: 'TLSv1.2' as const } : {}),
    }, (respuesta) => {
      const fragmentos: Buffer[] = [];
      respuesta.on('data', (fragmento: Buffer) => fragmentos.push(fragmento));
      respuesta.on('end', () => {
        const cuerpo = Buffer.concat(fragmentos).toString('utf8');
        if (!respuesta.statusCode || respuesta.statusCode < 200 || respuesta.statusCode >= 300) {
          rechazar(new Error(`Mailpit respondio ${respuesta.statusCode ?? 'sin estado'} en ${url.pathname}.`));
          return;
        }
        try {
          resolver(JSON.parse(cuerpo) as T);
        } catch (error) {
          rechazar(new Error(`Mailpit devolvio JSON invalido en ${url.pathname}.`, { cause: error }));
        }
      });
    });
    peticion.on('timeout', () => peticion.destroy(new Error(`Tiempo agotado al consultar ${url}.`)));
    peticion.on('error', rechazar);
  });
}

function extraerEnlaces(detalle: DetalleMensaje): string[] {
  const contenido = `${detalle.HTML ?? ''}\n${detalle.Text ?? ''}`;
  const enlaces = new Set<string>();
  for (const coincidencia of contenido.matchAll(/https?:\/\/[^\s<>'"]+/gu)) {
    enlaces.add(coincidencia[0].replaceAll('&amp;', '&').replace(/[).,;]+$/u, ''));
  }
  return [...enlaces];
}

/**
 * Espera el correo mas reciente de un destinatario en Mailpit y devuelve el enlace solicitado.
 * Consulta la lista publica de Mailpit para que el mismo ayudante sirva en local y en CI.
 */
export async function esperarEnlaceCorreo(
  destinatario: string,
  opciones: OpcionesEsperaCorreo = {},
): Promise<string> {
  const baseUrl = opciones.baseUrl ?? 'https://correo.shapi.localhost';
  const contiene = opciones.contiene ?? '';
  const intervaloMs = opciones.intervaloMs ?? 500;
  const tiempoLimiteMs = opciones.tiempoLimiteMs ?? 30_000;
  const limite = Date.now() + tiempoLimiteMs;
  let ultimoError: unknown;

  while (Date.now() < limite) {
    try {
      const lista = await leerJson<ListaMensajes>(new URL('/api/v1/messages', baseUrl));
      const mensajes = lista.messages ?? [];
      const mensaje = mensajes.find((actual) =>
        JSON.stringify(actual.To ?? '').toLocaleLowerCase().includes(destinatario.toLocaleLowerCase()));

      if (mensaje?.ID) {
        const detalle = await leerJson<DetalleMensaje>(
          new URL(`/api/v1/message/${encodeURIComponent(mensaje.ID)}`, baseUrl),
        );
        const enlace = extraerEnlaces(detalle).find((actual) => actual.includes(contiene));
        if (enlace) return enlace;
      }
    } catch (error) {
      ultimoError = error;
    }
    await esperar(intervaloMs);
  }

  const detalle = ultimoError instanceof Error ? ` Ultimo error: ${ultimoError.message}` : '';
  throw new Error(`No llego a Mailpit un enlace para ${destinatario} en ${tiempoLimiteMs} ms.${detalle}`);
}
