import { useState } from 'react';
import { Aviso, Boton, EstadoCargando, EstadoError, Etiqueta } from '@shapi/ui';
import { Link } from 'react-router';
import { useMarcaPortal } from '../modulos/configuracion/useConfiguracionPortal';
import {
  ApiProveedorNoDisponible,
  mensajeError,
  useClavesConsumidor,
  useEmitirClave,
  useRevocarClave,
  useRotarClave,
  useSuscripcionConsumidor,
  type Clave,
  type ClaveEmitida,
} from '../modulos/claves/claves';
import RotarClave from './B2-4-RotarClave';
import ClaveNueva from './B2-5-ClaveNueva';
import RevocarClave from './B2-6-RevocarClave';

const DIA = 86_400_000;
const mesesCortos = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
const mesesLargos = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];
function partesFecha(valor: string | Date) {
  const partes = new Intl.DateTimeFormat('en-CA', {
    day: 'numeric', month: 'numeric', year: 'numeric', timeZone: 'America/Guatemala',
  }).formatToParts(typeof valor === 'string' ? new Date(valor) : valor);
  const numero = (tipo: Intl.DateTimeFormatPartTypes) => Number(partes.find(parte => parte.type === tipo)!.value);
  return { dia: numero('day'), mes: numero('month'), anio: numero('year') };
}
const fecha = (valor: string | Date) => { const dato = partesFecha(valor); return `${dato.dia} ${mesesCortos[dato.mes - 1]} ${dato.anio}`; };
const fechaLarga = (valor: string | Date) => { const dato = partesFecha(valor); return `${dato.dia} de ${mesesLargos[dato.mes - 1]} de ${dato.anio}`; };
const dinero = (valor: number) => `Q ${valor.toLocaleString('es-GT', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
const nombreTipo = (tipo: Clave['tipo']) => tipo === 'produccion' ? 'producción' : 'pruebas';
const nombreEstado = (estado: Clave['estado']) => estado === 'activa' ? 'Activa' : estado === 'rotada' ? 'Rotada' : 'Revocada';

type Paso = { accion: 'rotar' | 'revocar'; clave: Clave };

export default function Suscripcion() {
  const marca = useMarcaPortal();
  const consulta = useSuscripcionConsumidor();
  const claves = useClavesConsumidor(Boolean(consulta.data));
  const rotar = useRotarClave();
  const revocar = useRevocarClave();
  const emitir = useEmitirClave();
  const [paso, setPaso] = useState<Paso>();
  const [nueva, setNueva] = useState<{ clave: ClaveEmitida; anterior?: Clave }>();
  const [error, setError] = useState<string>();
  const [ahora] = useState(() => Date.now());

  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.error instanceof ApiProveedorNoDisponible) return <NoDisponible nombreApi={marca.nombreApi} />;
  if (consulta.isError) return <EstadoError mensaje="No se pudo cargar la suscripción." reintentar={() => void consulta.refetch()} />;
  if (!consulta.data) return <Vacia nombreApi={marca.nombreApi} />;
  if (claves.isPending) return <EstadoCargando />;
  if (claves.isError || !claves.data) return <EstadoError mensaje="No se pudieron cargar las claves." reintentar={() => void claves.refetch()} />;

  const suscripcion = consulta.data;
  if (nueva) return <ClaveNueva clave={nueva.clave} anterior={nueva.anterior} terminar={() => setNueva(undefined)} />;
  if (paso?.accion === 'rotar') return <RotarClave
    api={marca.nombreApi} plan={suscripcion.plan.nombre} clave={paso.clave} procesando={rotar.isPending} error={error}
    cancelar={() => { setPaso(undefined); setError(undefined); }}
    confirmar={() => void (async () => {
      setError(undefined);
      try {
        const resultado = await rotar.mutateAsync(paso.clave.id);
        setPaso(undefined);
        setNueva({ clave: resultado, anterior: resultado.anterior });
      } catch (causa) { setError(mensajeError(causa, 'No se pudo rotar la clave.')); }
    })()}
  />;
  if (paso?.accion === 'revocar') return <RevocarClave
    api={marca.nombreApi} clave={paso.clave} procesando={revocar.isPending} error={error}
    cancelar={() => { setPaso(undefined); setError(undefined); }}
    confirmar={() => void (async () => {
      setError(undefined);
      try { await revocar.mutateAsync(paso.clave.id); setPaso(undefined); }
      catch (causa) { setError(mensajeError(causa, 'No se pudo revocar la clave.')); }
    })()}
  />;

  const enGracia = suscripcion.estado === 'en_gracia';
  const inicioGracia = new Date(suscripcion.proximaRenovacion);
  const finGracia = new Date(inicioGracia.getTime() + 7 * DIA);
  const diasRestantes = Math.max(0, Math.ceil((finGracia.getTime() - ahora) / DIA));
  const finPeriodo = new Date(new Date(suscripcion.periodo.fin).getTime() - DIA);
  const vigencia = Math.max(1, Math.round((new Date(suscripcion.periodo.fin).getTime() - new Date(suscripcion.periodo.inicio).getTime()) / DIA));

  async function emitirNueva(clave: Clave) {
    setError(undefined);
    try { setNueva({ clave: await emitir.mutateAsync(clave.tipo) }); }
    catch (causa) { setError(mensajeError(causa, 'No se pudo emitir la clave.')); }
  }

  return <section>
    <header className="flex flex-wrap items-end justify-between gap-6">
      <div>
        <p className="text-etiqueta uppercase tracking-[.14em] text-[var(--marca-principal)]">Mi cuenta</p>
        <h1 className="mt-2 font-display text-[32px] leading-[1.2]">Suscripción y claves</h1>
        <p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">Su plan de {marca.nombreApi}, su vigencia y sus claves.</p>
      </div>
      {enGracia
        ? <Boton type="button" onClick={() => setError('El pago estará disponible cuando se integre el endpoint de reactivación de EM-10.')}>Pagar con otra tarjeta</Boton>
        : <Link className="inline-flex h-[46px] items-center rounded-base border border-borde-campo bg-panel px-6 text-[15px] font-medium text-tinta" to={`/cuenta/cambiar-plan/${suscripcion.plan.id}`}>Cambiar de plan</Link>}
    </header>

    {enGracia && <section className="mt-8 rounded-base border border-alerta-borde bg-alerta-fondo p-5">
      <h2 className="font-display text-[21px] leading-[1.3] text-alerta">Quedan {diasRestantes} días antes de que sus claves dejen de responder</h2>
      <p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">El cobro de la renovación fue rechazado el {fechaLarga(inicioGracia)}. Si el {fechaLarga(finGracia)} no hay un cobro autorizado, su suscripción se suspende y la compuerta empieza a rechazar sus peticiones con el código 403.</p>
      <p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">Mientras dure el periodo de gracia sus claves siguen funcionando con normalidad.</p>
    </section>}
    {error && <div className="mt-5"><Aviso estado="error">{error}</Aviso></div>}

    <div className={`grid grid-cols-1 gap-6 ${enGracia ? 'mt-5' : 'mt-8'} lg:grid-cols-2 lg:items-start`}>
      <section className="rounded-base border border-borde bg-panel p-5">
        <h2 className="font-display text-[21px] leading-[1.3]">Su suscripción</h2>
        <div className="mt-5 flex items-end justify-between gap-6 border-t border-borde pt-5">
          <div><p className="text-[12px] font-medium uppercase tracking-[.14em] text-tinta-suave">Plan de la API</p><div className="mt-2 flex items-center gap-3"><h3 className="font-display text-[22px]">{suscripcion.plan.nombre}</h3><Etiqueta estado={enGracia ? 'alerta' : 'correcto'}>{enGracia ? 'En gracia' : 'Activa'}</Etiqueta></div></div>
          <div className="text-right"><p className="font-display text-[24px]">{dinero(suscripcion.plan.precio)}</p><p className="mt-1 text-[14px] text-tinta-suave">Vigencia de {vigencia} días</p></div>
        </div>
        <dl className="mt-5 flex flex-col gap-3.5 border-t border-borde pt-5 text-[14px]">
          <Fila nombre={enGracia ? 'Periodo vencido' : 'Periodo activo'} valor={`${fecha(suscripcion.periodo.inicio)} – ${fecha(finPeriodo)}`} />
          {enGracia
            ? <><Fila nombre="Cobro rechazado" valor={fechaLarga(inicioGracia)} /><Fila nombre="Periodo de gracia" valor={`${fecha(inicioGracia)} – ${fecha(finGracia)}`} /><Fila nombre="Suspensión" valor={fechaLarga(finGracia)} /></>
            : <Fila nombre="Renovación automática" valor={fechaLarga(suscripcion.proximaRenovacion)} />}
          <Fila nombre="Tarjeta registrada" valor={suscripcion.tarjetaEnmascarada ?? 'Sin tarjeta registrada'} />
        </dl>
        <p className="mt-5 border-t border-borde pt-5 text-[14px] leading-[1.5] text-tinta-suave">{enGracia ? 'Al autorizar el cobro con otra tarjeta su ciclo se renueva y el periodo de gracia termina.' : 'La renovación se cobra a esta misma tarjeta al cierre de cada ciclo. Sus claves responden con normalidad.'}</p>
      </section>

      <section className="rounded-base border border-borde bg-panel p-5">
        <h2 className="font-display text-[21px] leading-[1.3]">Sus claves</h2>
        <p className="mt-1.5 text-[14px] leading-[1.5] text-tinta-suave">Por seguridad solo se muestran sus últimos cuatro caracteres. Rote una clave si la perdió o si cree que se filtró: la anterior sigue funcionando 24 horas.</p>
        <div className="mt-5 flex flex-col gap-5 border-t border-borde pt-5">
          {claves.data.map(clave => <ClaveEnmascarada key={clave.id} clave={clave}
            rotar={() => { setError(undefined); setPaso({ accion: 'rotar', clave }); }}
            revocar={() => { setError(undefined); setPaso({ accion: 'revocar', clave }); }}
            emitir={() => void emitirNueva(clave)} procesando={emitir.isPending} />)}
        </div>
        <p className="mt-5 border-t border-borde pt-5 text-[14px] leading-[1.5] text-tinta-suave">Una clave revocada, por usted o por su proveedor, se rechaza con el código 401. Si revoca una, podrá emitir otra.</p>
      </section>
    </div>
  </section>;
}

function ClaveEnmascarada({ clave, rotar, revocar, emitir, procesando }: {
  clave: Clave;
  rotar: () => void;
  revocar: () => void;
  emitir: () => void;
  procesando: boolean;
}) {
  const activa = clave.estado === 'activa';
  return <article data-testid={`clave-${clave.tipo}-${clave.estado}`} className="flex flex-col gap-2">
    <div className="flex items-center justify-between gap-3"><span className="text-[13px] font-semibold">Clave de {nombreTipo(clave.tipo)}</span><Etiqueta estado={activa ? 'correcto' : 'neutro'}>{nombreEstado(clave.estado)}</Etiqueta></div>
    <div className="flex flex-wrap items-center gap-3">
      <div className="min-w-[220px] flex-1 rounded-base border border-borde bg-[#F4F6FA] px-3.5 py-2.5 font-mono text-[14px]">{clave.claveEnmascarada}</div>
      {activa && <><Boton type="button" principal={false} onClick={rotar}>Rotar</Boton><Boton type="button" principal={false} onClick={revocar}>Revocar</Boton></>}
      {clave.estado === 'revocada' && <Boton type="button" principal={false} deshabilitado={procesando} onClick={emitir}>{procesando ? 'Emitiendo…' : 'Emitir una clave nueva'}</Boton>}
    </div>
    {clave.estado === 'rotada' && clave.expiraEn && <p className="text-[13px] text-tinta-suave">Funciona hasta el {fechaLarga(clave.expiraEn)}.</p>}
  </article>;
}

function Fila({ nombre, valor }: { nombre: string; valor: string }) {
  return <div className="flex justify-between gap-5"><dt className="text-tinta-suave">{nombre}</dt><dd className="text-right font-medium">{valor}</dd></div>;
}

function Vacia({ nombreApi }: { nombreApi: string }) {
  return <section>
    <p className="text-etiqueta uppercase tracking-[.14em] text-[var(--marca-principal)]">Mi cuenta</p>
    <h1 className="mt-2 font-display text-[32px] leading-[1.2]">Suscripción y claves</h1>
    <p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">Su plan de {nombreApi}, su vigencia y sus claves.</p>
    <div className="mt-8 rounded-base border border-borde bg-panel p-5"><div className="flex flex-col items-center gap-6 py-20 text-center"><div aria-hidden="true" className="flex size-10 items-center justify-center rounded-base border border-dashed border-borde-campo text-[var(--marca-principal)]">⌕</div><div className="max-w-[560px]"><h2 className="font-display text-[22px]">Todavía no tiene una suscripción</h2><p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">Al contratar un plan de esta API recibirá su clave de producción y su clave de pruebas, y aquí verá su vigencia y la fecha de su renovación automática.</p></div><Link className="inline-flex h-[46px] items-center rounded-base bg-principal px-6 text-[15px] font-medium text-white" to="/planes">Ver los planes</Link></div></div>
  </section>;
}

function NoDisponible({ nombreApi }: { nombreApi: string }) {
  return <section>
    <p className="text-etiqueta uppercase tracking-[.14em] text-[var(--marca-principal)]">Mi cuenta</p>
    <h1 className="mt-2 font-display text-[32px] leading-[1.2]">Suscripción y claves</h1>
    <p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">Su plan de {nombreApi}, su vigencia y sus claves.</p>
    <div className="mt-8 rounded-base border border-alerta-borde bg-alerta-fondo p-5"><h2 className="font-display text-[21px] text-alerta">La API no está disponible temporalmente</h2><p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">Las peticiones a {nombreApi} se están rechazando con el código 403. No es un problema de sus claves ni de su suscripción.</p><p className="mt-2 text-[15px] leading-[1.55] text-tinta-suave">Su suscripción sigue vigente, sus claves se conservan y su historial de pagos queda intacto. El servicio se restablece sin que usted tenga que hacer nada.</p></div>
  </section>;
}
