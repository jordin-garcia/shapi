import { useRef, useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/apis';
import { Aviso, EstadoCargando, EstadoError, Etiqueta, Tabla, Tarjeta } from '@shapi/ui';
import { Link, useParams } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
type EspecificacionCargada = components['schemas']['EspecificacionCargada'];

function tamano(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  return `${Math.round(bytes / 1024)} KB`;
}

export default function PaginaA33Especificacion() {
  const { id = '' } = useParams();
  const selector = useRef<HTMLInputElement>(null);
  const [archivo, setArchivo] = useState<File>();
  const [resultado, setResultado] = useState<EspecificacionCargada>();
  const [error, setError] = useState<string>();
  const [arrastrando, setArrastrando] = useState(false);

  const consulta = useQuery({
    queryKey: ['rutas-api', id],
    queryFn: async () => {
      const { data, response } = await cliente.GET('/api/apis/{id}/rutas', { params: { path: { id } } });
      if (!response.ok || !data) throw new Error('No se pudo consultar la API.');
      return data;
    },
    enabled: Boolean(id),
  });

  const carga = useMutation({
    mutationFn: async (seleccionado: File) => {
      const { data, response } = await cliente.PUT('/api/apis/{id}/especificacion', {
        params: { path: { id }, header: { 'X-Requested-With': 'shapi' } },
        body: { archivo: seleccionado as unknown as string },
        bodySerializer: () => {
          const formulario = new FormData();
          formulario.set('archivo', seleccionado);
          return formulario;
        },
      });
      if (!response.ok || !data) throw new Error('No se pudo cargar la especificación.');
      return data;
    },
    onSuccess: data => {
      setResultado(data);
      setError(undefined);
    },
    onError: fallo => {
      if (fallo instanceof ErrorApi) {
        const ubicacion = typeof fallo.detalle?.ubicacion === 'string' ? fallo.detalle.ubicacion : undefined;
        const mensaje = typeof fallo.detalle?.mensaje === 'string' ? fallo.detalle.mensaje : undefined;
        const tecnico = typeof fallo.detalle?.detalleTecnico === 'string' ? `Detalle técnico: ${fallo.detalle.detalleTecnico}` : undefined;
        setError([fallo.titulo, ubicacion, mensaje, tecnico].filter(Boolean).join(' '));
        return;
      }
      setError('No se pudo cargar la especificación. Inténtelo de nuevo.');
    },
  });

  function seleccionar(seleccionado?: File) {
    if (!seleccionado) return;
    if (seleccionado.size > 2 * 1024 * 1024) {
      setError('El archivo no puede superar 2 MB.');
      return;
    }
    setArchivo(seleccionado);
    setResultado(undefined);
    setError(undefined);
    carga.mutate(seleccionado);
  }

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudo cargar la API." reintentar={() => void consulta.refetch()} />;

  const apiNombre = resultado?.apiNombre ?? consulta.data?.apiNombre ?? 'API';
  const rutas = resultado?.rutas ?? consulta.data?.elementos ?? [];
  // Al volver a A3.3 se muestra la especificación que ya está cargada. El nombre del archivo no se guarda: va el título.
  const cargada = archivo ? undefined : consulta.data?.especificacion ?? undefined;

  return <div>
    <header className="flex flex-col gap-[10px]">
      <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">{apiNombre}</p>
      <h1 className="font-display text-[32px] leading-[1.2]">Cargar especificación OpenAPI</h1>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">Shapi lee el archivo y extrae de él las rutas de su API.</p>
    </header>

    <div className="mt-8 grid grid-cols-1 items-start gap-6 xl:grid-cols-2">
      <Tarjeta className="flex flex-col">
        <h2 className="font-display text-[21px] leading-[1.3]">Archivo de especificación</h2>
        <div
          className={`mt-5 flex flex-col items-center gap-3 rounded-base border border-dashed p-8 text-center ${arrastrando ? 'border-principal bg-principal-claro' : 'border-borde-campo bg-fondo'}`}
          onDragOver={evento => { evento.preventDefault(); setArrastrando(true); }}
          onDragLeave={() => setArrastrando(false)}
          onDrop={evento => {
            evento.preventDefault();
            setArrastrando(false);
            seleccionar(evento.dataTransfer.files[0]);
          }}
        >
          <span aria-hidden="true" className="text-[28px] leading-none text-principal">↑</span>
          <p className="text-[15px]">Arrastre aquí su archivo o{' '}
            <button type="button" className="text-principal hover:underline" onClick={() => selector.current?.click()}>elíjalo en su equipo</button>
          </p>
          <p className="text-[13px] text-tinta-suave">Especificación OpenAPI en formato .yaml o .json</p>
          <input
            ref={selector}
            className="sr-only"
            aria-label="Elegir archivo OpenAPI"
            type="file"
            accept=".yaml,.yml,.json,application/json,application/yaml,text/yaml"
            onChange={evento => seleccionar(evento.target.files?.[0])}
          />
        </div>

        {cargada && <div className="mt-4 flex items-center justify-between gap-4 rounded-base border border-borde px-4 py-[14px]">
          <div>
            <p className="text-[15px] font-medium">{cargada.titulo}</p>
            <p className="text-[13px] text-tinta-suave">OpenAPI {cargada.versionOpenApi} · {tamano(cargada.tamanoBytes)}</p>
          </div>
          <Etiqueta estado="correcto">Cargado</Etiqueta>
        </div>}

        {archivo && <div className="mt-4 flex items-center justify-between gap-4 rounded-base border border-borde px-4 py-[14px]">
          <div>
            <p className="text-[15px] font-medium">{archivo.name}</p>
            <p className="text-[13px] text-tinta-suave">
              {resultado ? `OpenAPI ${resultado.versionOpenApi}` : 'Validando OpenAPI'} · {tamano(archivo.size)}
            </p>
          </div>
          {resultado && <Etiqueta estado="correcto">Cargado</Etiqueta>}
        </div>}

        {carga.isPending && <div className="mt-4"><Aviso estado="neutro">Validando y extrayendo rutas…</Aviso></div>}
        {error && <div className="mt-4"><Aviso estado="error">{error}</Aviso></div>}

        <div className="mt-5 flex flex-col gap-[14px] border-t border-borde-fila pt-5">
          <div className="flex justify-between gap-5 text-[14px]">
            <span className="text-tinta-suave">Rutas encontradas</span>
            <span className="font-medium tabular-nums">{rutas.length}</span>
          </div>
          <p className="text-[14px] leading-[1.5] text-tinta-suave">Las rutas nuevas quedan ocultas hasta que usted las exponga. Si vuelve a cargar el archivo, las rutas que ya existían conservan su configuración.</p>
        </div>
      </Tarjeta>

      <Tarjeta className="flex flex-col">
        <h2 className="font-display text-[21px] leading-[1.3]">Rutas extraídas</h2>
        <div className="mt-5">
          <Tabla encabezados={['Método', 'Ruta']} filas={rutas.map(ruta => [
            <span key="metodo" className="text-[12px] font-semibold tracking-[0.06em] text-tinta-suave">{ruta.metodo}</span>,
            ruta.patron,
          ])} />
        </div>
        {rutas.length > 0 && <div className="mt-6 flex justify-end">
          <Link className="inline-flex h-[46px] items-center rounded-base bg-principal px-6 text-[15px] font-medium text-white hover:bg-principal-hover hover:no-underline" to={`/panel/apis/${encodeURIComponent(id)}/rutas`}>
            Continuar a la selección de rutas
          </Link>
        </div>}
      </Tarjeta>
    </div>
  </div>;
}
