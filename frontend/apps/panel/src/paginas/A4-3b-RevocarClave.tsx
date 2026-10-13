import { useMutation } from '@tanstack/react-query';
import { Boton, EstadoError, Tarjeta } from '@shapi/ui';
import { clienteClaves, mensajeClaves, tipoClave, type SeleccionClave } from '../modulos/claves/claves';

export default function RevocarClave({ apiId, seleccion, alCancelar, alRevocar }: {
  apiId: string; seleccion: SeleccionClave; alCancelar: () => void; alRevocar: () => Promise<void>;
}) {
  const { consumidor, clave } = seleccion;
  const revocacion = useMutation({
    mutationFn: async () => {
      const { data, response } = await clienteClaves.POST('/api/apis/{apiId}/claves/{claveId}/revocar', { params: { path: { apiId, claveId: clave.id }, header: { 'X-Requested-With': 'shapi' } } });
      if (!response.ok || !data) throw new Error('No se pudo revocar la clave.');
      return data;
    },
    onSuccess: alRevocar,
  });
  return <div className="flex min-h-full items-center justify-center">
    <Tarjeta className="w-full max-w-[600px] !p-10">
      <div className="flex flex-col gap-5">
        <svg aria-hidden="true" width="32" height="32" viewBox="0 0 32 32" fill="none"><circle cx="16" cy="16" r="15" stroke="var(--alerta-base)" strokeWidth="1.5" /><path d="M16 9.5 V18 M16 22.5 V22.5" stroke="var(--alerta-base)" strokeWidth="2" strokeLinecap="round" /></svg>
        <div className="flex flex-col gap-2">
          <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">Claves de sus consumidores</p>
          <h1 className="font-display text-[32px] leading-[1.2]">Revocar clave</h1>
          <p className="mt-0.5 text-[15px] leading-[1.55] text-tinta-suave">La clave <strong className="font-semibold text-tinta">deja de funcionar en menos de 10 segundos</strong>. Desde ese momento, la compuerta rechaza con el código 401 toda petición que la use.</p>
        </div>
      </div>
      <dl className="mt-8 flex flex-col gap-[14px] border-t border-borde-fila pt-6">
        {Object.entries({ Consumidor: consumidor.consumidor, Plan: consumidor.plan, Tipo: tipoClave(clave), Clave: clave.claveEnmascarada }).map(([rotulo, valor]) => <div key={rotulo} className="flex items-center justify-between gap-6"><dt className="text-[14px] text-tinta-suave">{rotulo}</dt><dd className="text-[15px] font-medium tabular-nums text-tinta">{valor}</dd></div>)}
      </dl>
      {revocacion.isError && <div className="mt-6"><EstadoError mensaje={mensajeClaves(revocacion.error, 'No se pudo revocar la clave. Inténtelo de nuevo.')} reintentar={() => revocacion.mutate()} /></div>}
      <div className="mt-6 flex flex-col gap-[14px] border-t border-borde-fila pt-6">
        <Boton type="button" deshabilitado={revocacion.isPending} onClick={() => revocacion.mutate()}>{revocacion.isPending ? 'Revocando...' : 'Revocar clave'}</Boton>
        <button type="button" disabled={revocacion.isPending} onClick={alCancelar} className="text-center text-[14px] text-tinta-suave hover:text-principal hover:underline disabled:cursor-not-allowed">Cancelar</button>
      </div>
    </Tarjeta>
  </div>;
}
