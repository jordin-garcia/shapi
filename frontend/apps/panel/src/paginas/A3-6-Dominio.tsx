import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { paths } from '@shapi/api/apis';
import { Aviso, Boton, EstadoCargando, EstadoError, Etiqueta, Tarjeta } from '@shapi/ui';
import { useParams } from 'react-router';
import { usePuedeConfigurarApi } from '../modulos/sesion/usePuedeConfigurarApi';

const cliente = crearCliente<paths>(window.location.origin);
const claseCampo = 'h-10 w-full rounded-base border border-borde-campo bg-panel px-3 text-[14px] outline-none focus:border-principal focus:ring-[3px] focus:ring-anillo-foco';

export default function PaginaA36Dominio() {
  const puedeConfigurar = usePuedeConfigurarApi();
  const { id = '' } = useParams();
  const cache = useQueryClient();
  const [dominio, setDominio] = useState<string>();
  const [secretoNuevo, setSecretoNuevo] = useState<string>();
  const [mensaje, setMensaje] = useState<string>();
  const [error, setError] = useState<string>();
  const consulta = useQuery({
    queryKey: ['dominio-api', id],
    queryFn: async () => {
      const { data, response } = await cliente.GET('/api/apis/{id}/dominio', { params: { path: { id } } });
      if (!response.ok || !data) throw new Error('No se pudo cargar la configuración de dominios.');
      return data;
    },
    enabled: Boolean(id),
  });

  const conectar = useMutation({
    mutationFn: async () => {
      const { data, response } = await cliente.POST('/api/apis/{id}/dominio', { params: { path: { id }, header: { 'X-Requested-With': 'shapi' } }, body: { dominio: dominio ?? consulta.data?.dominio ?? '' } });
      if (!response.ok || !data) throw new Error('No se pudo conectar el dominio.');
      return data;
    }, onSuccess: async () => { setMensaje('El dominio quedó pendiente de verificación.'); setError(undefined); await cache.invalidateQueries({ queryKey: ['dominio-api', id] }); },
    onError: fallo => { setMensaje(undefined); setError(fallo instanceof ErrorApi ? fallo.errores?.dominio?.join(' ') || fallo.titulo : 'No se pudo conectar el dominio.'); },
  });
  const useAccion = (tipo: 'simular' | 'verificar' | 'eliminar') => useMutation({
    mutationFn: async () => {
      const params = { path: { id }, header: { 'X-Requested-With': 'shapi' as const } };
      const resultado = tipo === 'simular'
        ? await cliente.POST('/api/apis/{id}/dominio/simular-registro', { params })
        : tipo === 'verificar'
          ? await cliente.POST('/api/apis/{id}/dominio/verificar', { params })
          : await cliente.DELETE('/api/apis/{id}/dominio', { params });
      if (!resultado.response.ok) throw new Error('No se pudo completar la acción.');
    }, onSuccess: async () => { if (tipo === 'eliminar') setDominio(undefined); setMensaje(tipo === 'simular' ? 'El registro DNS simulado se creó.' : tipo === 'verificar' ? 'La verificación DNS terminó.' : 'El dominio se desconectó.'); setError(undefined); await cache.invalidateQueries({ queryKey: ['dominio-api', id] }); },
    onError: fallo => { setMensaje(undefined); setError(fallo instanceof ErrorApi ? fallo.titulo : 'No se pudo completar la acción.'); },
  });
  const simular = useAccion('simular');
  const verificar = useAccion('verificar');
  const eliminar = useAccion('eliminar');
  const secreto = useMutation({
    mutationFn: async () => {
      const { data, response } = await cliente.POST('/api/apis/{id}/secreto/regenerar', { params: { path: { id }, header: { 'X-Requested-With': 'shapi' } } });
      if (!response.ok || !data) throw new Error('No se pudo regenerar el secreto.');
      return data;
    }, onSuccess: data => { setSecretoNuevo(data.secretoOrigen); setMensaje('Guarde el secreto ahora: no volverá a mostrarse.'); setError(undefined); },
    onError: fallo => setError(fallo instanceof ErrorApi ? fallo.titulo : 'No se pudo regenerar el secreto.'),
  });
  if (consulta.isPending) return <EstadoCargando />;
  if (consulta.isError) return <EstadoError mensaje="No se pudo cargar la configuración de dominios." reintentar={() => void consulta.refetch()} />;
  const datos = consulta.data;
  const dominioMostrado = dominio ?? datos.dominio ?? '';
  const estadoEtiqueta = datos.estado === 'verificado' ? 'correcto' : datos.estado === 'fallido' ? 'alerta' : 'neutro';

  return <div>
    <header className="flex flex-col gap-[10px]"><p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">{datos.apiNombre}</p><h1 className="font-display text-[32px] leading-[1.2]">Direcciones y dominio propio</h1><p className="text-[15px] leading-[1.55] text-tinta-suave">Dónde responden su portal y su API, el secreto que Shapi envía a su servidor y cómo conectar su propio dominio.</p></header>
    <div className="mt-8 grid gap-6 xl:grid-cols-2"><div className="grid content-start gap-6">
      <Tarjeta><div className="flex items-center justify-between"><h2 className="font-display text-[22px]">Direcciones asignadas</h2><Etiqueta estado="correcto">Activas</Etiqueta></div><dl className="mt-5 grid gap-4 text-[14px]"><div className="flex justify-between gap-5"><dt className="text-tinta-suave">Portal</dt><dd>{datos.urlPortal}</dd></div><div className="flex justify-between gap-5"><dt className="text-tinta-suave">API</dt><dd>{datos.urlApi}</dd></div><div className="flex justify-between gap-5"><dt className="text-tinta-suave">Certificado TLS</dt><dd className="flex items-center gap-2">Automático</dd></div></dl><p className="mt-5 text-[13px] text-tinta-suave">Shapi emite y renueva los certificados de estas direcciones. No tiene que configurar nada.</p></Tarjeta>
      {puedeConfigurar && <Tarjeta><h2 className="font-display text-[22px]">Secreto de origen</h2><p className="mt-2 text-[14px] leading-[1.5] text-tinta-suave">Shapi lo envía a su servidor en la cabecera <span className="font-medium text-tinta">X-Shapi-Secreto</span>. Si lo valida, su servidor rechaza lo que no pasó por la compuerta. Es opcional.</p><div className="mt-5 flex items-center justify-between gap-4 rounded-base bg-fondo px-4 py-3 font-mono text-[13px]"><span>{secretoNuevo ?? datos.secretoEnmascarado}</span><Boton principal={false} type="button" onClick={() => secreto.mutate()}>Regenerar</Boton></div><p className="mt-3 text-[13px] text-tinta-suave">Por seguridad solo se muestra completo al generarlo.</p></Tarjeta>}
    </div><Tarjeta><div className="flex items-center justify-between gap-3"><h2 className="font-display text-[22px]">Dominio propio</h2>{datos.dominio && <Etiqueta estado={estadoEtiqueta}>{datos.estado === 'verificado' ? 'Verificado' : datos.estado === 'fallido' ? 'Verificación fallida' : 'Pendiente de verificación'}</Etiqueta>}</div><p className="mt-2 text-[14px] leading-[1.5] text-tinta-suave">Conecte un dominio de su organización a su API. Shapi lo verifica con un registro DNS y le emite su certificado TLS. {datos.dominioPropioPermitido ? `Incluido en su plan ${datos.planNombre}.` : datos.planNombre ? `Su plan ${datos.planNombre} no incluye dominio propio.` : 'No tiene un plan que incluya dominio propio.'}</p>
      {puedeConfigurar ? <>
      <label className="mt-5 block text-[13px] font-medium">Dominio<input className={`${claseCampo} mt-2`} value={dominioMostrado} placeholder="api.enviosxelaju.localhost" onChange={e => setDominio(e.target.value)} /></label><div className="mt-4 flex gap-3"><Boton type="button" deshabilitado={!dominioMostrado.trim()} onClick={() => conectar.mutate()}>Conectar dominio</Boton>{datos.dominio && <Boton principal={false} type="button" onClick={() => eliminar.mutate()}>Desconectar</Boton>}</div>
      </> : <p className="mt-5 text-[15px]">{datos.dominio ?? "Sin dominio propio"}</p>}
      {datos.dominio && <div className="mt-6"><p className="mb-4 text-[14px] text-tinta-suave">Certificado TLS: <span aria-label="Certificado TLS del dominio propio">{datos.estado === 'verificado' ? 'Se emite con la primera petición' : datos.estado === 'fallido' ? 'No disponible' : 'Pendiente de verificación'}</span></p><p className="text-[13px] font-medium text-tinta-suave">Registro DNS que debe crear</p><div className="mt-3 overflow-auto"><table className="w-full border-collapse text-left text-[14px]"><thead><tr>{['Tipo', 'Nombre', 'Valor'].map(x => <th key={x} className="border-b border-borde pb-2 uppercase text-tinta-suave">{x}</th>)}</tr></thead><tbody><tr><td className="py-3 font-semibold">CNAME</td><td>{datos.dominio}</td><td>{datos.destinoCname}</td></tr></tbody></table></div>{datos.motivo && <div className="mt-4"><Aviso estado="neutro">{datos.motivo}</Aviso></div>}{datos.estado === 'verificado' && <div className="mt-4"><Aviso estado="exito">Dominio verificado. El certificado se emitirá con la primera petición.</Aviso></div>}{puedeConfigurar && datos.modoSimulado && <p className="mt-4 text-[14px] text-tinta-suave">En este entorno los dominios son simulados: pulse «Simular la creación del registro» para que el DNS simulado lo tenga y luego verifíquelo.</p>}{puedeConfigurar && <div className="mt-5 flex flex-wrap gap-3">{datos.modoSimulado && <Boton principal={false} type="button" onClick={() => simular.mutate()}>Simular la creación del registro</Boton>}<Boton type="button" onClick={() => verificar.mutate()}>Verificar registro DNS</Boton></div>}</div>}
      {mensaje && <div className="mt-5"><Aviso estado="exito">{mensaje}</Aviso></div>}{error && <div className="mt-5"><Aviso estado="error">{error}</Aviso></div>}
    </Tarjeta></div>
  </div>;
}
