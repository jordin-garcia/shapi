import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/planes';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { Link } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
type Plan = components['schemas']['PlanPlataforma'];
const dinero = (monto: number) => 'Q ' + monto.toLocaleString('es-GT', { minimumFractionDigits: 2 });

export default function PaginaA01Inicio() {
  const consulta = useQuery({
    queryKey: ['planes-plataforma'],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/planes-plataforma', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron cargar los planes.');
      return data;
    },
    retry: false,
  });

  const planAnual = consulta.data?.find(plan => plan.nombre === 'Escala anual');
  const visibles = consulta.data?.filter(plan => plan.nombre !== 'Escala anual') ?? [];

  return (
    <div className="dark min-h-screen bg-fondo text-tinta overflow-x-hidden">
      <div className="mx-auto max-w-[1440px]">
        {/* Encabezado y héroe */}
        <div className="relative">
          <div
            aria-hidden="true"
            className="pointer-events-none absolute inset-0"
            style={{
              backgroundImage:
                'linear-gradient(to right, #121B2E 1px, transparent 1px), linear-gradient(to bottom, #121B2E 1px, transparent 1px)',
              backgroundSize: '64px 64px',
              WebkitMaskImage: 'radial-gradient(ellipse 78% 62% at 50% 26%, #000 35%, transparent 100%)',
              maskImage: 'radial-gradient(ellipse 78% 62% at 50% 26%, #000 35%, transparent 100%)',
            }}
          />
          <div
            aria-hidden="true"
            className="pointer-events-none absolute inset-0"
            style={{
              background: 'radial-gradient(ellipse 56% 42% at 50% 8%, rgba(59,111,240,0.30), transparent 68%)',
            }}
          />

          <div className="relative">
            <div className="flex h-[76px] items-center justify-between border-b border-borde-barra px-8 lg:px-20">
              <div className="flex items-center gap-[11px]">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <rect x="1" y="1" width="22" height="22" rx="6" stroke="var(--principal)" strokeWidth="1.5" />
                  <path d="M6 8.5 H14" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
                  <path d="M6 15.5 H12" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
                  <path d="M6 12 H22" stroke="#3B6FF0" strokeWidth="2.5" strokeLinecap="round" />
                </svg>
                <span className="font-display text-[20px] font-medium tracking-[-0.02em] text-tinta">Shapi</span>
              </div>
              <div className="flex items-center gap-[30px]">
                <a href="#planes" className="text-[14.5px] text-tinta-navegacion hover:underline">
                  Precios
                </a>
                <Link to="/entrar" className="text-[14.5px] text-tinta-navegacion hover:underline">
                  Entrar
                </Link>
                <Link
                  to="/registro"
                  className="rounded-base bg-tinta px-[18px] py-[9px] text-[14.5px] font-medium text-fondo hover:no-underline"
                >
                  Crear cuenta
                </Link>
              </div>
            </div>

            <div className="flex flex-col items-center px-8 pt-[84px] text-center lg:px-20">
              <p className="text-etiqueta font-medium uppercase tracking-[0.16em] text-tinta-rotulo">
                Plataforma como servicio
              </p>
              <h1
                className="mt-[22px] font-display text-[124px] font-light leading-none tracking-[-0.045em] text-transparent bg-clip-text"
                style={{
                  backgroundImage: 'linear-gradient(180deg, #FFFFFF 22%, #93AEE8 96%)',
                }}
              >
                Shapi
              </h1>
              <p className="mt-[26px] max-w-[52ch] text-[20px] leading-[1.55] text-tinta-suave">
                Publique su API, controle quién la usa y cobre por ella. Sin modificar el servidor que ya tiene.
              </p>
              <div className="mt-[34px] flex items-center gap-[14px]">
                <Link
                  to="/registro"
                  className="rounded-base bg-tinta px-[26px] py-[13px] text-[15px] font-medium text-fondo hover:no-underline"
                >
                  Crear cuenta
                </Link>
                <a
                  href="#planes"
                  className="rounded-base border border-borde-campo px-[25px] py-[12px] text-[15px] font-medium text-tinta hover:no-underline"
                >
                  Ver precios
                </a>
              </div>

              <div className="mt-16 w-full max-w-[1040px] pb-24">
                <svg viewBox="0 0 1040 420" width="100%" className="block" aria-hidden="true">
                  <defs>
                    <linearGradient id="v4a" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor="#3B6FF0" stopOpacity="0.55" />
                      <stop offset="100%" stopColor="#3B6FF0" stopOpacity="0" />
                    </linearGradient>
                    <radialGradient id="v4b" cx="50%" cy="50%" r="50%">
                      <stop offset="0%" stopColor="#7FA6FF" stopOpacity="0.55" />
                      <stop offset="100%" stopColor="#7FA6FF" stopOpacity="0" />
                    </radialGradient>
                  </defs>
                  <g stroke="#18233A" strokeWidth="1">
                    <path d="M0 340 H1040" />
                    <path d="M0 300 H1040" />
                    <path d="M0 268 H1040" />
                    <path d="M0 244 H1040" />
                    <path d="M0 226 H1040" />
                    <path d="M0 212 H1040" />
                    <path d="M520 200 L60 420" />
                    <path d="M520 200 L220 420" />
                    <path d="M520 200 L380 420" />
                    <path d="M520 200 L520 420" />
                    <path d="M520 200 L660 420" />
                    <path d="M520 200 L820 420" />
                    <path d="M520 200 L980 420" />
                  </g>
                  <ellipse cx="520" cy="200" rx="300" ry="140" fill="url(#v4b)" />
                  <g stroke="#2C3B5C" strokeWidth="1" fill="none">
                    <ellipse cx="520" cy="150" rx="230" ry="76" />
                    <ellipse cx="520" cy="150" rx="160" ry="52" />
                    <ellipse cx="520" cy="150" rx="92" ry="30" />
                    <path d="M520 46 V254" />
                    <path d="M290 150 H750" />
                  </g>
                  <g fill="#3B6FF0">
                    <circle cx="520" cy="150" r="7" />
                    <circle cx="750" cy="150" r="4" />
                    <circle cx="290" cy="150" r="4" />
                    <circle cx="520" cy="46" r="4" />
                    <circle cx="520" cy="254" r="4" />
                  </g>
                  <g fill="#7FA6FF">
                    <circle cx="612" cy="129" r="3" />
                    <circle cx="428" cy="171" r="3" />
                    <circle cx="680" cy="176" r="3" />
                    <circle cx="360" cy="124" r="3" />
                  </g>
                  <rect x="380" y="286" width="280" height="8" rx="4" fill="url(#v4a)" />
                </svg>
              </div>
            </div>
          </div>
        </div>

        {/* Cómo funciona */}
        <section className="flex flex-col items-center px-8 pt-24 text-center lg:px-20">
          <p className="text-etiqueta font-medium uppercase tracking-[0.16em] text-tinta-rotulo">Cómo funciona</p>
          <h2 className="mt-[18px] max-w-[20ch] font-display text-[44px] font-light leading-[1.15] tracking-[-0.03em]">
            Tres pasos para publicar y cobrar por su API.
          </h2>
          <p className="mt-[18px] max-w-[60ch] text-[17px] leading-[1.6] text-tinta-suave">
            Usted conecta su servidor, sube su especificación OpenAPI y define los planes. Shapi se encarga del resto.
          </p>

          <div className="mt-[52px] grid w-full grid-cols-1 gap-6 text-left md:grid-cols-3">
            <div className="flex flex-col gap-[18px] rounded-base border border-borde bg-panel p-7">
              <svg viewBox="0 0 320 150" width="100%" className="block" aria-hidden="true">
                <rect x="8" y="46" width="86" height="58" rx="6" fill="#111A2C" stroke="#26334D" />
                <g stroke="#3D4C6B" strokeWidth="1">
                  <path d="M20 62 H82" />
                  <path d="M20 75 H70" />
                  <path d="M20 88 H76" />
                </g>
                <path d="M102 75 H196" stroke="#3B6FF0" strokeWidth="2" strokeDasharray="5 5" />
                <circle cx="196" cy="75" r="4" fill="#7FA6FF" />
                <rect x="212" y="30" width="90" height="90" rx="8" fill="#0E1729" stroke="#3B6FF0" />
                <g stroke="#7FA6FF" strokeWidth="1.5" strokeLinecap="round">
                  <path d="M228 52 H286" />
                  <path d="M228 75 H286" />
                  <path d="M228 98 H286" />
                </g>
              </svg>
              <div className="flex flex-col gap-[9px]">
                <span className="tabular-nums text-[12px] font-semibold tracking-[0.14em] text-principal">01</span>
                <h3 className="font-display text-[21px] font-normal leading-[1.3] tracking-[-0.03em]">
                  Conectar el servidor de origen
                </h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Indique la dirección del servidor que ya tiene en funcionamiento. Shapi no lo modifica: solamente enruta el tráfico hacia él.
                </p>
              </div>
            </div>

            <div className="flex flex-col gap-[18px] rounded-base border border-borde bg-panel p-7">
              <svg viewBox="0 0 320 150" width="100%" className="block" aria-hidden="true">
                <rect x="8" y="24" width="104" height="102" rx="6" fill="#111A2C" stroke="#26334D" />
                <g stroke="#3D4C6B" strokeWidth="1">
                  <path d="M24 46 H96" />
                  <path d="M24 60 H84" />
                  <path d="M24 74 H96" />
                  <path d="M24 88 H72" />
                  <path d="M24 102 H90" />
                </g>
                <g stroke="#3B6FF0" strokeWidth="1.5" strokeDasharray="4 4">
                  <path d="M120 46 H176" />
                  <path d="M120 75 H176" />
                  <path d="M120 104 H176" />
                </g>
                <g>
                  <rect x="188" y="32" width="114" height="26" rx="5" fill="#0E1729" stroke="#3B6FF0" />
                  <rect x="188" y="62" width="114" height="26" rx="5" fill="#0E1729" stroke="#3B6FF0" />
                  <rect x="188" y="92" width="114" height="26" rx="5" fill="#0B111F" stroke="#26334D" />
                </g>
                <g fill="#7FA6FF">
                  <circle cx="200" cy="45" r="3" />
                  <circle cx="200" cy="75" r="3" />
                </g>
                <circle cx="200" cy="105" r="3" fill="#3D4C6B" />
              </svg>
              <div className="flex flex-col gap-[9px]">
                <span className="tabular-nums text-[12px] font-semibold tracking-[0.14em] text-principal">02</span>
                <h3 className="font-display text-[21px] font-normal leading-[1.3] tracking-[-0.03em]">
                  Cargar la especificación OpenAPI
                </h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Suba el archivo. Shapi extrae las rutas y usted decide cuáles quedan expuestas y cuáles permanecen ocultas.
                </p>
              </div>
            </div>

            <div className="flex flex-col gap-[18px] rounded-base border border-borde bg-panel p-7">
              <svg viewBox="0 0 320 150" width="100%" className="block" aria-hidden="true">
                <rect x="14" y="30" width="88" height="90" rx="6" fill="#0E1729" stroke="#26334D" />
                <rect x="116" y="30" width="88" height="90" rx="6" fill="#0E1729" stroke="#3B6FF0" />
                <rect x="218" y="30" width="88" height="90" rx="6" fill="#0E1729" stroke="#26334D" />
                <g stroke="#3D4C6B" strokeWidth="1">
                  <path d="M28 52 H74" />
                  <path d="M28 96 H88" />
                  <path d="M232 52 H278" />
                  <path d="M232 96 H292" />
                </g>
                <g stroke="#7FA6FF" strokeWidth="1">
                  <path d="M130 52 H176" />
                  <path d="M130 96 H190" />
                </g>
                <rect x="28" y="66" width="42" height="14" rx="3" fill="#1B2740" />
                <rect x="130" y="66" width="56" height="14" rx="3" fill="#3B6FF0" />
                <rect x="232" y="66" width="36" height="14" rx="3" fill="#1B2740" />
              </svg>
              <div className="flex flex-col gap-[9px]">
                <span className="tabular-nums text-[12px] font-semibold tracking-[0.14em] text-principal">03</span>
                <h3 className="font-display text-[21px] font-normal leading-[1.3] tracking-[-0.03em]">
                  Definir los planes
                </h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Nombre, descripción, precio, vigencia, cuota mensual y límite por minuto para cada plan de su API.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Qué recibe el proveedor */}
        <section className="flex flex-col items-center px-8 pt-[104px] text-center lg:px-20">
          <p className="text-etiqueta font-medium uppercase tracking-[0.16em] text-tinta-rotulo">
            Qué recibe el proveedor
          </p>
          <h2 className="mt-[18px] max-w-[22ch] font-display text-[44px] font-light leading-[1.15] tracking-[-0.03em]">
            Todo lo que hace falta para operar una API de paga.
          </h2>

          <div className="mt-[52px] grid w-full grid-cols-1 gap-6 text-left md:grid-cols-2">
            <div className="flex items-start gap-[26px] rounded-base border border-borde bg-panel p-[30px]">
              <svg width="132" height="132" viewBox="0 0 132 132" className="shrink-0" aria-hidden="true">
                <rect x="6" y="18" width="120" height="96" rx="8" fill="#0E1729" stroke="#26334D" />
                <path d="M6 40 H126" stroke="#26334D" />
                <circle cx="22" cy="29" r="4" fill="#3B6FF0" />
                <rect x="34" y="25" width="40" height="8" rx="4" fill="#1B2740" />
                <g stroke="#3D4C6B" strokeWidth="1">
                  <path d="M22 58 H82" />
                  <path d="M22 72 H108" />
                  <path d="M22 86 H70" />
                </g>
                <rect x="22" y="96" width="46" height="10" rx="5" fill="#3B6FF0" />
              </svg>
              <div className="flex flex-col gap-[10px]">
                <h3 className="font-display text-[22px] font-normal tracking-[-0.03em]">Un portal con su propia marca</h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Su logotipo, su color principal y sus textos. La documentación se genera desde su especificación OpenAPI e incluye consola de pruebas.
                </p>
              </div>
            </div>

            <div className="flex items-start gap-[26px] rounded-base border border-borde bg-panel p-[30px]">
              <svg width="132" height="132" viewBox="0 0 132 132" className="shrink-0" aria-hidden="true">
                <circle cx="42" cy="52" r="22" fill="none" stroke="#3B6FF0" strokeWidth="2" />
                <circle cx="42" cy="52" r="8" fill="#0E1729" stroke="#7FA6FF" strokeWidth="1.5" />
                <path d="M60 60 L104 96" stroke="#3B6FF0" strokeWidth="2" strokeLinecap="round" />
                <path d="M90 84 L82 94" stroke="#3B6FF0" strokeWidth="2" strokeLinecap="round" />
                <path d="M100 92 L92 102" stroke="#3B6FF0" strokeWidth="2" strokeLinecap="round" />
                <circle cx="86" cy="34" r="18" fill="none" stroke="#26334D" strokeWidth="1.5" strokeDasharray="4 5" />
                <path d="M22 96 H70" stroke="#26334D" strokeWidth="1.5" />
              </svg>
              <div className="flex flex-col gap-[10px]">
                <h3 className="font-display text-[22px] font-normal tracking-[-0.03em]">Claves de acceso</h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Claves de producción y de pruebas, emitidas al activarse cada suscripción. Sus consumidores las rotan desde su portal y usted puede revocarlas en cualquier momento.
                </p>
              </div>
            </div>

            <div className="flex items-start gap-[26px] rounded-base border border-borde bg-panel p-[30px]">
              <svg width="132" height="132" viewBox="0 0 132 132" className="shrink-0" aria-hidden="true">
                <g stroke="#26334D" strokeWidth="1.5">
                  <path d="M14 30 V102" />
                  <path d="M38 30 V102" />
                  <path d="M62 30 V102" />
                  <path d="M86 30 V102" />
                  <path d="M110 30 V102" />
                </g>
                <path d="M6 52 H126" stroke="#3B6FF0" strokeWidth="2" />
                <circle cx="14" cy="52" r="4" fill="#7FA6FF" />
                <circle cx="38" cy="52" r="4" fill="#7FA6FF" />
                <circle cx="62" cy="52" r="4" fill="#7FA6FF" />
                <path d="M6 80 H62" stroke="#C2481F" strokeWidth="2" />
                <circle cx="62" cy="80" r="4" fill="#F08A5F" />
                <path d="M70 80 H126" stroke="#1B2740" strokeWidth="2" strokeDasharray="4 4" />
              </svg>
              <div className="flex flex-col gap-[10px]">
                <h3 className="font-display text-[22px] font-normal tracking-[-0.03em]">
                  Cuotas aplicadas en cada petición
                </h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  La compuerta verifica la clave, la suscripción, el límite por minuto y la cuota antes de que la petición llegue a su servidor.
                </p>
              </div>
            </div>

            <div className="flex items-start gap-[26px] rounded-base border border-borde bg-panel p-[30px]">
              <svg width="132" height="132" viewBox="0 0 132 132" className="shrink-0" aria-hidden="true">
                <rect x="6" y="18" width="120" height="96" rx="8" fill="#0E1729" stroke="#26334D" />
                <g stroke="#1B2740" strokeWidth="1">
                  <path d="M6 42 H126" />
                  <path d="M6 66 H126" />
                  <path d="M6 90 H126" />
                </g>
                <rect x="20" y="28" width="54" height="10" rx="3" fill="#1B2740" />
                <rect x="20" y="52" width="82" height="10" rx="3" fill="#3B6FF0" />
                <rect x="20" y="76" width="38" height="10" rx="3" fill="#1B2740" />
                <rect x="20" y="98" width="66" height="10" rx="3" fill="#1F8A5B" />
              </svg>
              <div className="flex flex-col gap-[10px]">
                <h3 className="font-display text-[22px] font-normal tracking-[-0.03em]">Registro de consumo y pagos</h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Consumo consolidado por suscripción, ruta y día, e historial de pagos de la organización y de cada consumidor.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Planes */}
        <section id="planes" aria-label="Planes" className="relative px-8 pt-[104px] lg:px-20">
          <div
            aria-hidden="true"
            className="pointer-events-none absolute inset-0"
            style={{
              background: 'radial-gradient(ellipse 50% 40% at 50% 40%, rgba(59,111,240,0.14), transparent 70%)',
            }}
          />
          <div className="relative flex flex-col items-center text-center">
            <p className="text-etiqueta font-medium uppercase tracking-[0.16em] text-tinta-rotulo">Planes</p>
            <h2 className="mt-[18px] font-display text-[44px] font-light leading-[1.15] tracking-[-0.03em]">
              Los planes de Shapi.
            </h2>
            <p className="mt-[18px] max-w-[56ch] text-[17px] leading-[1.6] text-tinta-suave">
              Precios en quetzales. La vigencia se cuenta desde la contratación del plan.
            </p>

            <div className="mt-[52px] w-full text-left">
              {consulta.isPending ? (
                <EstadoCargando />
              ) : consulta.isError ? (
                <EstadoError
                  mensaje="No se pudieron cargar los planes."
                  reintentar={() => void consulta.refetch()}
                />
              ) : (
                <div className="grid grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-4">
                  {visibles.map((plan: Plan) => {
                    const destacado = plan.nombre === 'Producto';
                    const esEscala = plan.nombre.startsWith('Escala');
                    return (
                      <article
                        key={plan.id}
                        className={`flex flex-col gap-5 rounded-base border p-[26px] ${
                          destacado ? 'border-principal bg-fondo-activo' : 'border-borde bg-panel'
                        }`}
                      >
                        <div className="flex flex-col gap-2">
                          <h3
                            className={`font-display text-[22px] font-normal tracking-[-0.03em] ${
                              destacado ? 'text-white' : 'text-tinta'
                            }`}
                          >
                            {esEscala ? 'Escala' : plan.nombre}
                          </h3>
                          <p className="min-h-[42px] text-[14px] leading-[1.5] text-tinta-suave">
                            {plan.descripcion}
                          </p>
                        </div>

                        <div className="flex flex-col gap-1 border-t border-borde pt-5">
                          <span
                            className={`tabular-nums font-display text-[32px] font-normal tracking-[-0.03em] ${
                              destacado ? 'text-white' : 'text-tinta'
                            }`}
                          >
                            {dinero(plan.precio)}
                          </span>
                          <span className="text-[13px] text-tinta-rotulo">
                            {plan.nombre === 'Escala mensual'
                              ? `Mensual · vigencia de ${plan.vigenciaDias} días`
                              : `Vigencia de ${plan.vigenciaDias} días`}
                          </span>
                          {plan.nombre === 'Escala mensual' && planAnual && (
                            <span className="mt-[6px] tabular-nums text-[13px] text-tinta-navegacion">
                              o {dinero(planAnual.precio)} anual · {planAnual.vigenciaDias} días y{' '}
                              {planAnual.cuotaPeticiones.toLocaleString('es-GT')} peticiones
                            </span>
                          )}
                        </div>

                        <ul className="flex flex-col gap-[11px] text-[14.5px] text-tinta-navegacion">
                          <li>
                            {plan.maxApis == null
                              ? 'APIs ilimitadas'
                              : `${plan.maxApis} ${plan.maxApis === 1 ? 'API' : 'APIs'}`}
                          </li>
                          <li className="tabular-nums">
                            {plan.cuotaPeticiones.toLocaleString('es-GT')} peticiones
                          </li>
                          <li>
                            {plan.maxMiembros == null
                              ? 'Miembros ilimitados'
                              : `${plan.maxMiembros} ${plan.maxMiembros === 1 ? 'miembro' : 'miembros'}`}
                          </li>
                          {plan.dominioPropio && <li>Dominio propio</li>}
                          {plan.esPrueba && <li>Prueba gratis por {plan.vigenciaDias} días</li>}
                        </ul>
                      </article>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        </section>

        {/* Pie */}
        <footer className="mt-[104px] flex items-start justify-between gap-16 border-t border-borde-barra px-8 pt-11 pb-[60px] lg:px-20">
          <div className="flex items-center gap-[11px]">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <rect x="1" y="1" width="22" height="22" rx="6" stroke="var(--tinta-suave)" strokeWidth="1.5" />
              <path d="M6 8.5 H14" stroke="var(--tinta-suave)" strokeWidth="1.5" strokeLinecap="round" />
              <path d="M6 15.5 H12" stroke="var(--tinta-suave)" strokeWidth="1.5" strokeLinecap="round" />
              <path d="M6 12 H22" stroke="var(--tinta-suave)" strokeWidth="2.5" strokeLinecap="round" />
            </svg>
            <span className="font-display text-[17px] font-medium tracking-[-0.02em] text-tinta-rotulo">Shapi</span>
          </div>
          <p className="text-right text-[14px] text-tinta-rotulo">
            Shapi es un proyecto académico de la Universidad Rafael Landívar, Facultad de Ingeniería.
          </p>
        </footer>
      </div>
    </div>
  );
}
