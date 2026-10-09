import { useState, type FormEvent } from 'react';
import { Boton, EstadoError } from '@shapi/ui';
import type { components } from '@shapi/api/administracion';

type Organizacion = components['schemas']['OrganizacionAdministracion'];

function tituloEstado(estado: Organizacion['estado']) {
  return estado === 'en_gracia' ? 'En gracia' : estado === 'suspendida' ? 'Suspendida' : 'Activa';
}

export default function PaginaA62bSuspender({ organizacion, cancelar, confirmar, guardando, error }: {
  organizacion: Organizacion;
  cancelar: () => void;
  confirmar: (motivo: string) => void;
  guardando: boolean;
  error: boolean;
}) {
  const [motivo, setMotivo] = useState('');

  function enviar(evento: FormEvent) {
    evento.preventDefault();
    if (motivo.trim()) confirmar(motivo.trim());
  }

  return <div className="mx-auto w-full max-w-[682px] rounded-base border border-borde bg-panel p-10">
    <div className="flex flex-col gap-5">
      <svg width="32" height="32" viewBox="0 0 32 32" fill="none" aria-hidden="true">
        <circle cx="16" cy="16" r="15" stroke="#C2481F" strokeWidth="1.5" />
        <path d="M16 9.5 V18" stroke="#C2481F" strokeWidth="2" strokeLinecap="round" />
        <path d="M16 22.5 V22.5" stroke="#C2481F" strokeWidth="2.5" strokeLinecap="round" />
      </svg>
      <div className="flex flex-col gap-2">
        <p className="text-etiqueta uppercase tracking-[.16em] text-tinta-suave">Organizaciones</p>
        <h1 className="font-display text-[32px] leading-[1.2]">Suspender organización</h1>
        <p className="mt-0.5 text-[15px] leading-[1.55] text-tinta-suave">Al suspender la organización, <strong className="font-semibold text-tinta">sus APIs publicadas dejan de responder</strong>. Desde ese momento la compuerta rechaza con el código 403 todas las peticiones de sus consumidores, y el portal les avisa que la API no está disponible temporalmente.</p>
        <p className="mt-0.5 text-[15px] leading-[1.55] text-tinta-suave">Sus claves, sus suscripciones y su historial se conservan. El tráfico se restablece al reactivar la organización.</p>
      </div>
    </div>

    <dl className="mt-8 flex flex-col gap-3.5 border-t border-borde-fila pt-6">
      <Fila titulo="Organización" valor={organizacion.nombre} />
      <Fila titulo="Propietario" valor={organizacion.propietarioCorreo} />
      <Fila titulo="Plan de plataforma" valor={organizacion.plan} />
      <Fila titulo="Estado" valor={tituloEstado(organizacion.estado)} />
      <Fila titulo="APIs publicadas" valor={String(organizacion.numeroApisPublicadas)} />
      <Fila titulo="Consumidores afectados" valor={String(organizacion.numeroConsumidores)} />
    </dl>

    <form onSubmit={enviar} className="mt-6 flex flex-col gap-3.5 border-t border-borde-fila pt-6">
      <label className="flex flex-col gap-1.5 text-[13px] font-semibold">
        Motivo administrativo
        <textarea aria-label="Motivo administrativo" required value={motivo} onChange={evento => setMotivo(evento.target.value)} rows={3} className="rounded-base border border-borde-campo px-3.5 py-2.5 text-[15px] font-normal" />
      </label>
      {error && <EstadoError reintentar={() => confirmar(motivo.trim())} />}
      <Boton type="submit" disabled={guardando || !motivo.trim()}>{guardando ? 'Suspendiendo…' : 'Suspender organización'}</Boton>
      <button type="button" onClick={cancelar} className="text-[14px] text-principal hover:underline">Cancelar</button>
    </form>
  </div>;
}

function Fila({ titulo, valor }: { titulo: string; valor: string }) {
  return <div className="flex items-baseline justify-between gap-6">
    <dt className="text-[14px] text-tinta-suave">{titulo}</dt>
    <dd className="text-[15px] font-medium tabular-nums">{valor}</dd>
  </div>;
}
