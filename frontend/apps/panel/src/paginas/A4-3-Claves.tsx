import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from 'react-router';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { paths } from '@shapi/api/apis';
import { Boton, EstadoCargando, EstadoError, EstadoSinPermiso, Etiqueta, Tarjeta, Toast } from '@shapi/ui';
import { useSesion } from '../modulos/sesion/useSesion';
import { clienteClaves, fechaClave, mensajeClaves, tipoClave, type SeleccionClave } from '../modulos/claves/claves';
import RevocarClave from './A4-3b-RevocarClave';

const clienteApis = crearCliente<paths>(window.location.origin);
const TAMANO = 20;

export default function PaginaA43Claves() {
  const { id } = useParams<{ id: string }>();
  return id ? <ClavesApi key={id} apiId={id} /> : <EstadoSinPermiso />;
}

function ClavesApi({ apiId }: { apiId: string }) {
  const cache = useQueryClient();
  const sesion = useSesion();
  const [pagina, setPagina] = useState(1);
  const [seleccion, setSeleccion] = useState<SeleccionClave>();
  const [aviso, setAviso] = useState(0);
  const prefijoConsulta = ['claves-proveedor', sesion.data?.organizacionId, apiId];
  const consulta = useQuery({
    queryKey: [...prefijoConsulta, pagina],
    queryFn: async ({ signal }) => {
      const { data, response } = await clienteClaves.GET('/api/apis/{apiId}/claves', { params: { path: { apiId }, query: { pagina, tamano: TAMANO } }, signal });
      if (response.status === 403) throw new ErrorApi('error', 'Sin permiso', 403);
      if (!response.ok || !data) throw new Error('No se pudieron cargar las claves.');
      return data;
    },
    enabled: !!sesion.data,
    retry: false,
  });
  const apis = useQuery({
    queryKey: ['apis'],
    queryFn: async ({ signal }) => {
      const { data, response } = await clienteApis.GET('/api/apis', { signal });
      if (!response.ok || !data) throw new Error('No se pudo cargar la API.');
      return data;
    },
    enabled: !!sesion.data,
    retry: false,
  });
  const puedeRevocar = sesion.data?.rol === 'propietario' || sesion.data?.rol === 'editor';
  if (sesion.isError) return <EstadoError mensaje="No se pudo consultar la sesión." reintentar={() => void sesion.refetch()} />;
  if (!sesion.isPending && !sesion.data) return <EstadoSinPermiso />;
  if (sesion.isPending || consulta.isPending || apis.isPending) return <EstadoCargando />;
  if (!sesion.data || (consulta.error instanceof ErrorApi && consulta.error.estado === 403)) return <EstadoSinPermiso />;
  if (consulta.isError) return <EstadoError mensaje={mensajeClaves(consulta.error, 'No se pudieron cargar las claves.')} reintentar={() => void consulta.refetch()} />;
  if (apis.isError) return <EstadoError mensaje="No se pudo cargar la API." reintentar={() => void apis.refetch()} />;
  if (seleccion && puedeRevocar) return <RevocarClave apiId={apiId} seleccion={seleccion} alCancelar={() => setSeleccion(undefined)} alRevocar={async () => {
    await cache.invalidateQueries({ queryKey: prefijoConsulta });
    setSeleccion(undefined);
    setAviso(n => n + 1);
  }} />;
  const { elementos, total } = consulta.data;
  const hayClaves = elementos.some(c => c.claves.length > 0);
  return <div>
    {aviso > 0 && <Toast key={aviso}>Clave revocada.</Toast>}
    <div className="flex flex-col gap-[10px]">
      <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">{apis.data?.elementos.find(a => a.id === apiId)?.nombre ?? 'API'}</p>
      <h1 className="font-display text-[32px] leading-[1.2]">Claves de sus consumidores</h1>
      <p className="text-[15px] leading-[1.55] text-tinta-suave">Cada suscripción tiene una clave de producción y una de pruebas. Aquí solo se muestran sus últimos cuatro caracteres. Sus consumidores rotan sus claves desde su portal; usted puede revocar cualquiera.</p>
    </div>
    <Tarjeta className="mt-8">
      <div className="overflow-x-auto">
        <table className="w-full border-collapse text-left">
          <thead><tr>{['Consumidor', 'Plan', 'Tipo', 'Clave', 'Estado', ...(puedeRevocar ? ['Acciones'] : [])].map((titulo, i) => <th key={titulo} scope="col" className={`border-b border-borde pb-[10px] pr-4 text-encabezado font-semibold uppercase text-tinta-suave ${i === 5 ? '!pr-0 text-right' : ''}`}>{titulo}</th>)}</tr></thead>
          <tbody>{elementos.flatMap((consumidor, grupo) => consumidor.claves.map((clave, i) => {
            const ultima = i === consumidor.claves.length - 1;
            return <tr key={clave.id} className={`[&>td]:py-[9px] ${i === 0 ? '[&>td]:pt-[14px]' : ''} ${ultima ? grupo < elementos.length - 1 ? 'border-b border-borde-fila [&>td]:pb-[14px]' : '[&>td]:pb-1' : ''}`}>
              <td className="pr-4 text-[15px] font-medium">{i === 0 ? consumidor.consumidor : ''}</td>
              <td className="pr-4 text-[15px]">{i === 0 ? consumidor.plan : ''}</td>
              <td className="whitespace-nowrap pr-4 text-[15px]">{tipoClave(clave)}</td>
              <td className="whitespace-nowrap pr-4 text-[15px] tracking-[0.02em]">{clave.claveEnmascarada}</td>
              <td className="pr-4"><div className="flex items-center gap-3 whitespace-nowrap">
                <Etiqueta estado={clave.estado === 'activa' ? 'correcto' : clave.estado === 'rotada' ? 'neutro' : 'alerta'}>{clave.estado === 'activa' ? 'Activa' : clave.estado === 'rotada' ? 'Rotada' : 'Revocada'}</Etiqueta>
                {clave.estado === 'rotada' && clave.expiraEn && <span className="text-[14px] text-tinta-suave">Vigente hasta {fechaClave(clave.expiraEn)}</span>}
                {clave.estado === 'revocada' && clave.revocadaEn && <span className="text-[14px] text-tinta-suave">El {fechaClave(clave.revocadaEn)}</span>}
              </div></td>
              {puedeRevocar && <td className="text-right">{clave.estado !== 'revocada' && <button type="button" className="text-[15px] font-medium text-principal hover:underline" onClick={() => setSeleccion({ consumidor, clave })}>Revocar</button>}</td>}
            </tr>;
          }))}</tbody>
        </table>
      </div>
      {!hayClaves ? <div className="flex flex-col items-center gap-6 pb-[52px] pt-16 text-center">
        <svg aria-hidden="true" width="40" height="40" viewBox="0 0 40 40" fill="none"><rect x="1" y="1" width="38" height="38" rx="8" stroke="var(--borde-campo)" strokeWidth="1.5" strokeDasharray="4 4" /><circle cx="15.5" cy="20" r="4.5" stroke="var(--principal)" strokeWidth="1.5" /><path d="M20 20 H29 M26 20 V23.5" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" /></svg>
        <div className="flex max-w-[520px] flex-col gap-[10px]"><h2 className="font-display text-[22px] leading-[1.3]">Todavía no hay claves emitidas</h2><p className="text-[15px] leading-[1.55] text-tinta-suave">Cuando un consumidor contrate uno de sus planes en su portal, Shapi emitirá su clave de producción y su clave de pruebas.</p></div>
      </div> : <div className="mt-[14px] flex gap-11 border-t border-borde-fila pt-[18px] text-[14px] leading-[1.5] text-tinta-suave"><p><strong className="font-semibold text-tinta">Rotada</strong>: el consumidor emitió una clave nueva desde su portal; la anterior sigue vigente 24 horas.</p><p><strong className="font-semibold text-tinta">Revocar</strong> deja la clave sin efecto en menos de 10 segundos.</p></div>}
      {total > TAMANO && <nav aria-label="Páginas de consumidores" className="mt-5 flex items-center justify-end gap-4"><Boton principal={false} deshabilitado={pagina === 1} onClick={() => setPagina(p => p - 1)}>Anterior</Boton><span className="text-sm text-tinta-suave">Página {pagina} de {Math.ceil(total / TAMANO)}</span><Boton principal={false} deshabilitado={pagina * TAMANO >= total} onClick={() => setPagina(p => p + 1)}>Siguiente</Boton></nav>}
    </Tarjeta>
  </div>;
}
