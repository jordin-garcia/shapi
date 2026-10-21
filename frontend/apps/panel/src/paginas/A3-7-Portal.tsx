import { useRef, useState, type CSSProperties } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/apis';
import { Aviso, Boton, EstadoCargando, EstadoError, Tarjeta } from '@shapi/ui';
import { useParams } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
type PersonalizacionPortal = components['schemas']['PersonalizacionPortal'];
const CLASE_CAMPO_BASE = 'w-full rounded-base border bg-panel px-[14px] py-[11px] text-[15px] leading-[1.5] text-tinta outline-none focus:ring-[3px] focus:ring-anillo-foco';

function claseCampo(tieneError: boolean) {
  return `${CLASE_CAMPO_BASE} ${tieneError ? 'border-alerta focus:border-alerta' : 'border-borde-campo focus:border-principal'}`;
}

function iniciales(nombre: string) {
  return nombre.split(/\s+/).filter(Boolean).slice(0, 2).map(parte => parte[0]?.toUpperCase()).join('');
}

function dinero(valor: number) {
  return `Q ${valor.toLocaleString('es-GT', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function numero(valor: number) {
  return valor.toLocaleString('es-GT');
}

function textoPeso(peso: number) {
  return `Descuenta ${peso} ${peso === 1 ? 'llamada' : 'llamadas'} de su cuota`;
}

function Codigo({ valor }: { valor: unknown }) {
  if (valor === null || valor === undefined) {
    return <p className="p-5 text-[14px] text-tinta-suave">Sin ejemplo.</p>;
  }
  return (
    <pre className="overflow-auto whitespace-pre-wrap px-5 py-[18px] font-mono text-[14px] leading-[1.7] tabular-nums text-[#2B3547]">
      {JSON.stringify(valor, null, 2)}
    </pre>
  );
}

export default function PaginaA37Portal() {
  const { id = '' } = useParams();
  const cache = useQueryClient();
  const selectorLogo = useRef<HTMLInputElement>(null);

  const [nombreEditado, setNombreEditado] = useState<string>();
  const [colorEditado, setColorEditado] = useState<string>();
  const [bienvenidaEditada, setBienvenidaEditada] = useState<string>();
  const [nombreArchivoLogo, setNombreArchivoLogo] = useState<string>();
  const [logoVistaPrevia, setLogoVistaPrevia] = useState<string>();
  const [versionLogo, setVersionLogo] = useState(0);
  const [mensaje, setMensaje] = useState<string>();
  const [error, setError] = useState<string>();
  const [erroresCampos, setErroresCampos] = useState<Record<string, string[]>>({});

  const consulta = useQuery({
    queryKey: ['portal-api', id],
    queryFn: async () => {
      const { data, response } = await cliente.GET('/api/apis/{id}/portal', { params: { path: { id } } });
      if (!response.ok || !data) throw new Error('No se pudo cargar la personalización del portal.');
      return data;
    },
    enabled: Boolean(id),
  });

  const guardar = useMutation({
    mutationFn: async (cuerpo: { nombre: string | null; color: string; bienvenida: string | null }) => {
      const { data, response } = await cliente.PUT('/api/apis/{id}/portal', {
        params: { path: { id }, header: { 'X-Requested-With': 'shapi' } },
        body: cuerpo,
      });
      if (!response.ok || !data) throw new Error('No se pudo guardar la personalización.');
      return data;
    },
    onSuccess: (data: PersonalizacionPortal) => {
      cache.setQueryData(['portal-api', id], data);
      setNombreEditado(undefined);
      setColorEditado(undefined);
      setBienvenidaEditada(undefined);
      setErroresCampos({});
      setError(undefined);
      setMensaje('La personalización del portal se guardó.');
    },
    onError: fallo => {
      setMensaje(undefined);
      if (fallo instanceof ErrorApi) {
        if (fallo.errores && Object.keys(fallo.errores).length > 0) {
          setErroresCampos(fallo.errores);
          setError(undefined);
          return;
        }
        setErroresCampos({});
        setError(fallo.titulo);
        return;
      }
      setErroresCampos({});
      setError('No se pudo guardar la personalización. Inténtelo de nuevo.');
    },
  });

  const cargarLogo = useMutation({
    mutationFn: async (archivo: File) => {
      const { data, response } = await cliente.PUT('/api/apis/{id}/portal/logo', {
        params: { path: { id }, header: { 'X-Requested-With': 'shapi' } },
        body: { archivo: archivo as unknown as string },
        bodySerializer: () => {
          const formulario = new FormData();
          formulario.set('archivo', archivo);
          return formulario;
        },
      });
      if (!response.ok || !data) throw new Error('No se pudo cargar el logotipo.');
      return data;
    },
    onSuccess: (data: PersonalizacionPortal, archivo: File) => {
      cache.setQueryData(['portal-api', id], data);
      setNombreArchivoLogo(archivo.name);
      setVersionLogo(actual => actual + 1);
      setError(undefined);
      setMensaje('El logotipo del portal se guardó.');
    },
    onError: fallo => {
      setLogoVistaPrevia(undefined);
      setNombreArchivoLogo(undefined);
      setMensaje(undefined);
      if (fallo instanceof ErrorApi) {
        setError(fallo.titulo);
        return;
      }
      setError('No se pudo cargar el logotipo. Inténtelo de nuevo.');
    },
  });

  function seleccionarLogo(archivo?: File) {
    if (!archivo) return;
    if (archivo.size > 512 * 1024) {
      setMensaje(undefined);
      setError('El logotipo debe ser una imagen PNG o SVG de hasta 512 KB.');
      return;
    }
    const lector = new FileReader();
    lector.onload = () => {
      if (typeof lector.result === 'string') setLogoVistaPrevia(lector.result);
    };
    lector.readAsDataURL(archivo);
    cargarLogo.mutate(archivo);
  }

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudo cargar la personalización del portal." reintentar={() => void consulta.refetch()} />;

  const datos = consulta.data;
  const valorNombre = nombreEditado ?? (datos.nombre ?? datos.apiNombre);
  const valorColor = colorEditado ?? datos.color;
  const valorBienvenida = bienvenidaEditada ?? (datos.bienvenida ?? '');

  const nombreVivo = valorNombre.trim() || datos.apiNombre;
  const colorVivo = /^#[0-9A-Fa-f]{6}$/.test(valorColor.trim()) ? valorColor.trim() : datos.color;
  const bienvenidaViva = valorBienvenida.trim() || `Bienvenido al portal de ${datos.apiNombre}.`;
  const urlLogoVivo = logoVistaPrevia ?? (datos.urlLogo ? `${datos.urlLogo}?v=${versionLogo}` : null);
  const etiquetaArchivoLogo = nombreArchivoLogo ?? (datos.tieneLogo ? 'Logotipo cargado' : 'Sin logotipo personalizado');
  const primeraConEjemplo = datos.rutas.find(ruta => ruta.ejemploPeticion !== null || ruta.ejemploRespuesta !== null);

  return (
    <div>
      <header className="flex flex-col gap-[10px]">
        <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">{datos.apiNombre}</p>
        <h1 className="font-display text-[32px] leading-[1.2]">Personalización del portal</h1>
        <p className="text-[15px] leading-[1.55] text-tinta-suave">Su portal lleva su logotipo, su color y sus textos. Sus consumidores no ven la marca de Shapi.</p>
      </header>

      <div className="mt-7 grid grid-cols-1 items-start gap-6 xl:grid-cols-[400px_minmax(0,1fr)]">
        <Tarjeta className="flex flex-col p-5">
          <h2 className="font-display text-[21px] leading-[1.3]">Marca del portal</h2>

          <div className="mt-5 flex flex-col gap-[18px] border-t border-borde-fila pt-5">
            <div className="flex flex-col gap-[7px]">
              <span className="text-[13px] font-semibold text-tinta">Logotipo</span>
              <div className="flex items-center justify-between gap-[14px] rounded-base border border-borde-campo py-[10px] pl-[10px] pr-[14px]">
                <div className="flex items-center gap-[14px]">
                  {urlLogoVivo
                    ? <img src={urlLogoVivo} alt={nombreVivo} className="size-11 rounded-base object-contain" />
                    : (
                      <div
                        style={{ backgroundColor: colorVivo }}
                        className="flex size-11 items-center justify-center rounded-base font-display text-[17px] font-semibold tracking-[-0.02em] text-white"
                      >
                        {iniciales(nombreVivo)}
                      </div>
                    )}
                  <div className="flex flex-col gap-[1px]">
                    <span className="text-[14px] font-medium text-tinta">{etiquetaArchivoLogo}</span>
                    <span className="text-[13px] text-tinta-suave">PNG o SVG</span>
                  </div>
                </div>
                <button
                  type="button"
                  className="text-[14px] font-medium text-principal hover:underline"
                  onClick={() => selectorLogo.current?.click()}
                >
                  Cambiar
                </button>
                <input
                  ref={selectorLogo}
                  type="file"
                  accept=".png,.svg,image/png,image/svg+xml"
                  aria-label="Cambiar logotipo"
                  className="sr-only"
                  onChange={evento => seleccionarLogo(evento.target.files?.[0])}
                />
              </div>
            </div>

            <div className="flex flex-col gap-[7px]">
              <label htmlFor="portal-color" className="text-[13px] font-semibold text-tinta">Color principal</label>
              <div className={`flex items-center gap-3 rounded-base border bg-panel px-[14px] py-[11px] focus-within:ring-[3px] focus-within:ring-anillo-foco ${erroresCampos.color?.length ? 'border-alerta focus-within:border-alerta' : 'border-borde-campo focus-within:border-principal'}`}>
                <span
                  aria-hidden="true"
                  style={{ backgroundColor: colorVivo }}
                  className="size-5 shrink-0 rounded-[4px]"
                />
                <input
                  id="portal-color"
                  type="text"
                  value={valorColor}
                  aria-invalid={Boolean(erroresCampos.color?.length)}
                  onChange={evento => setColorEditado(evento.target.value)}
                  className="w-full bg-transparent text-[15px] leading-[1.5] tracking-[0.02em] text-tinta tabular-nums outline-none"
                />
              </div>
              {erroresCampos.color?.map(detalle => (
                <p key={detalle} className="text-[13px] text-alerta">{detalle}</p>
              ))}
            </div>

            <div className="flex flex-col gap-[7px]">
              <label htmlFor="portal-nombre" className="text-[13px] font-semibold text-tinta">Nombre del portal</label>
              <input
                id="portal-nombre"
                type="text"
                value={valorNombre}
                aria-invalid={Boolean(erroresCampos.nombre?.length)}
                onChange={evento => setNombreEditado(evento.target.value)}
                className={claseCampo(Boolean(erroresCampos.nombre?.length))}
              />
              {erroresCampos.nombre?.map(detalle => (
                <p key={detalle} className="text-[13px] text-alerta">{detalle}</p>
              ))}
            </div>

            <div className="flex flex-col gap-[7px]">
              <label htmlFor="portal-bienvenida" className="text-[13px] font-semibold text-tinta">Texto de bienvenida</label>
              <textarea
                id="portal-bienvenida"
                rows={3}
                value={valorBienvenida}
                aria-invalid={Boolean(erroresCampos.bienvenida?.length)}
                onChange={evento => setBienvenidaEditada(evento.target.value)}
                className={`${claseCampo(Boolean(erroresCampos.bienvenida?.length))} min-h-[72px] resize-y`}
              />
              {erroresCampos.bienvenida?.map(detalle => (
                <p key={detalle} className="text-[13px] text-alerta">{detalle}</p>
              ))}
            </div>
          </div>

          {mensaje && <div className="mt-4"><Aviso estado="exito">{mensaje}</Aviso></div>}
          {error && <div className="mt-4"><Aviso estado="error">{error}</Aviso></div>}

          <div className="mt-6">
            <Boton
              type="button"
              className="w-full"
              deshabilitado={guardar.isPending}
              onClick={() => guardar.mutate({
                nombre: valorNombre.trim() || null,
                color: valorColor.trim(),
                bienvenida: valorBienvenida.trim() || null,
              })}
            >
              Guardar personalización
            </Boton>
          </div>
        </Tarjeta>

        <Tarjeta className="flex flex-col p-5">
          <div className="flex items-baseline justify-between gap-4">
            <h2 className="font-display text-[21px] leading-[1.3]">Vista previa</h2>
            <a
              href={datos.urlPortal}
              target="_blank"
              rel="noreferrer"
              className="flex items-center gap-[7px] text-[14px] font-medium text-principal hover:underline"
            >
              Abrir mi portal
              <svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
                <path d="M5.5 2.75 H2.75 V11.25 H11.25 V8.5" stroke="#3B6FF0" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                <path d="M8 2.75 H11.25 V6" stroke="#3B6FF0" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                <path d="M11 3 L6.5 7.5" stroke="#3B6FF0" strokeWidth="1.5" strokeLinecap="round" />
              </svg>
            </a>
          </div>

          <div className="mt-5 h-[803px] w-full overflow-hidden rounded-base border border-borde bg-panel">
            <div
              style={{
                '--marca-principal': colorVivo,
                width: '1440px',
                height: '1882px',
                transform: 'scale(0.42569)',
                transformOrigin: 'top left',
              } as CSSProperties}
              className="flex flex-col overflow-hidden bg-panel text-[16px] leading-[1.6] text-tinta"
            >
              <div className="flex h-[76px] shrink-0 items-center justify-between border-b border-borde bg-panel px-20">
                <div className="flex items-center gap-[11px]">
                  {urlLogoVivo
                    ? <img src={urlLogoVivo} alt={nombreVivo} className="size-7 rounded-[6px] object-contain" />
                    : (
                      <div className="flex size-7 items-center justify-center rounded-[6px] bg-[var(--marca-principal)] font-display text-[11px] font-semibold tracking-[-0.02em] text-white">
                        {iniciales(nombreVivo)}
                      </div>
                    )}
                  <span className="font-display text-[20px] font-medium tracking-[-0.03em] text-tinta">{nombreVivo}</span>
                </div>
                <div className="flex items-center gap-8">
                  <span className="text-[15px] text-tinta-suave">Documentación</span>
                  <span className="text-[15px] text-tinta-suave">Planes</span>
                  <span className="text-[15px] font-medium text-tinta">Entrar</span>
                  <div className="rounded-base bg-[var(--marca-principal)] px-6 py-[13px] text-[15px] font-medium leading-[1.3] text-white">Crear cuenta</div>
                </div>
              </div>

              <div className="relative shrink-0 overflow-hidden border-b border-borde p-20">
                <div
                  aria-hidden="true"
                  className="absolute inset-0 opacity-[0.08] [background-image:linear-gradient(var(--marca-principal)_1px,transparent_1px),linear-gradient(90deg,var(--marca-principal)_1px,transparent_1px)] [background-size:48px_48px]"
                />
                <div className="relative grid grid-cols-[minmax(0,1fr)_520px] items-center gap-16">
                  <div className="flex flex-col gap-5">
                    <p className="text-[12px] font-medium uppercase tracking-[0.16em] text-[var(--marca-principal)]">{datos.apiNombre}</p>
                    <p className="font-display text-[44px] font-light leading-[1.15] tracking-[-0.03em] text-tinta">{bienvenidaViva}</p>
                    {datos.descripcionApi && (
                      <p className="max-w-[52ch] text-[20px] leading-[1.55] text-tinta-suave">{datos.descripcionApi}</p>
                    )}
                    <div className="mt-3 flex gap-3">
                      <div className="rounded-base bg-[var(--marca-principal)] px-6 py-[13px] text-[15px] font-medium leading-[1.3] text-white">Crear cuenta</div>
                      <div className="rounded-base border border-borde-campo bg-panel px-[23px] py-3 text-[15px] font-medium leading-[1.3] text-tinta">Ver documentación</div>
                    </div>
                  </div>

                  {primeraConEjemplo && (
                    <div className="overflow-hidden rounded-base border border-borde bg-panel">
                      <div className="flex items-center justify-between border-b border-borde-fila px-5 py-[14px]">
                        <span className="text-[12px] font-medium uppercase tracking-[0.16em] text-tinta-suave">Petición</span>
                        <span className="text-[14px] text-tinta">
                          <span className="mr-2 font-semibold tracking-[0.06em] text-[var(--marca-principal)]">{primeraConEjemplo.metodo}</span>
                          {primeraConEjemplo.patron}
                        </span>
                      </div>
                      <Codigo valor={primeraConEjemplo.ejemploPeticion} />
                      <div className="flex items-center justify-between border-y border-borde-fila bg-fondo px-5 py-[14px]">
                        <span className="text-[12px] font-medium uppercase tracking-[0.16em] text-tinta-suave">Respuesta</span>
                        <span className="text-[14px] font-semibold text-[#1F8A5B]">{primeraConEjemplo.codigoRespuesta ?? '—'}</span>
                      </div>
                      <Codigo valor={primeraConEjemplo.ejemploRespuesta} />
                    </div>
                  )}
                </div>
              </div>

              <div className="shrink-0 px-20 py-16">
                <div className="flex flex-col gap-3">
                  <p className="text-[12px] font-medium uppercase tracking-[0.16em] text-tinta-suave">Documentación</p>
                  <p className="font-display text-[44px] font-light leading-[1.15] tracking-[-0.03em] text-tinta">Rutas disponibles</p>
                </div>
                {datos.rutas.length === 0 ? (
                  <p className="mt-8 text-tinta-suave">No hay rutas publicadas.</p>
                ) : (
                  <div className="mt-10 grid grid-cols-4 gap-5">
                    {datos.rutas.map(ruta => (
                      <div key={`${ruta.metodo}-${ruta.patron}`} className="flex flex-col gap-[14px] rounded-base border border-borde bg-panel p-6">
                        <span className="text-[12px] font-semibold leading-[1.2] tracking-[0.06em] text-[var(--marca-principal)]">{ruta.metodo}</span>
                        <span className="font-display text-[22px] font-normal leading-[1.3] tracking-[-0.02em]">{ruta.patron}</span>
                        <p className="min-h-[63px] text-[14px] leading-[1.5] text-tinta-suave">{ruta.descripcion ?? ruta.resumen}</p>
                        <span className="border-t border-borde-fila pt-[14px] text-[13px] text-tinta-suave">{textoPeso(ruta.pesoLlamadas)}</span>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              <div className="shrink-0 border-y border-borde bg-fondo px-20 py-16">
                <div className="flex flex-col gap-3">
                  <p className="text-[12px] font-medium uppercase tracking-[0.16em] text-tinta-suave">Precios</p>
                  <p className="font-display text-[44px] font-light leading-[1.15] tracking-[-0.03em] text-tinta">Planes de la API</p>
                </div>
                {datos.planes.length === 0 ? (
                  <p className="mt-8 text-tinta-suave">Todavía no hay planes para esta API.</p>
                ) : (
                  <div className="mt-10 grid grid-cols-3 gap-5">
                    {datos.planes.map(plan => (
                      <div key={plan.id} className="flex flex-col gap-5 rounded-base border border-borde bg-panel p-6">
                        <p className="font-display text-[21px] font-normal leading-[1.3] tracking-[-0.02em]">{plan.nombre}</p>
                        <p className="text-[14px] leading-[1.5] text-tinta-suave">{plan.descripcion}</p>
                        <div className="flex flex-col gap-[5px] border-t border-borde-fila pt-5">
                          <span className="font-display text-[32px] font-normal leading-none tracking-[-0.03em] tabular-nums">{dinero(plan.precio)}</span>
                          <span className="text-[13px] text-tinta-suave">Vigencia de {plan.vigenciaDias} días</span>
                        </div>
                        <div className="flex flex-col gap-[11px]">
                          <span className="text-[14.5px] tabular-nums text-[#2B3547]">{numero(plan.cuotaLlamadas)} llamadas al mes</span>
                          <span className="text-[14.5px] tabular-nums text-[#2B3547]">{numero(plan.limiteMinuto)} peticiones por minuto</span>
                        </div>
                        <div className="mt-auto rounded-base bg-[var(--marca-principal)] px-6 py-[13px] text-center text-[15px] font-medium leading-[1.3] text-white">Contratar</div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              <div className="flex flex-grow items-center justify-between px-20 py-8">
                <div className="flex items-center gap-[11px]">
                  <div className="flex size-6 items-center justify-center rounded-[6px] bg-[var(--marca-principal)] font-display text-[10px] font-semibold text-white">
                    {iniciales(datos.nombreOrganizacion)}
                  </div>
                  <span className="text-[14px] text-tinta-suave">{datos.nombreOrganizacion}</span>
                </div>
                <div className="flex gap-8 text-[14px] text-tinta-suave">
                  <span>Documentación</span>
                  <span>Planes</span>
                  <span>Entrar</span>
                </div>
              </div>
            </div>
          </div>

          <p className="mt-[14px] text-[13px] leading-[1.5] text-tinta-suave">
            Página de inicio de su portal en {datos.urlPortal}: es lo primero que ve su cliente al abrir el enlace. Todo su contenido sale de lo que usted ingresó: la marca y los textos de esta pantalla; la descripción, las rutas y el ejemplo, de su especificación; las llamadas por ruta, de la configuración de cada ruta; y los precios, de sus planes de API. La vista previa refleja sus cambios antes de guardarlos; «Abrir mi portal» muestra el portal tal como está guardado.
          </p>
        </Tarjeta>
      </div>
    </div>
  );
}
