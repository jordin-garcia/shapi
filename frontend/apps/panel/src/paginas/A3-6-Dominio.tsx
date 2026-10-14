import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { paths } from '@shapi/api/apis';
import { Aviso, Boton, EstadoCargando, EstadoError, Tarjeta } from '@shapi/ui';
import { useParams } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
const claseCampo = 'h-10 w-full rounded-base border border-borde-campo bg-panel px-3 text-[14px] outline-none focus:border-principal focus:ring-[3px] focus:ring-anillo-foco';

export default function PaginaA36Dominio() {
  const { id = '' } = useParams();
  const cache = useQueryClient();
  const [dominio, setDominio] = useState('');
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
      const { data, response } = await cliente.POST('/api/apis/{id}/dominio', { params: { path: { id }, header: { 'X-Requested-With': 'shapi' } }, body: { dominio } });
      if (!response.ok || !data) throw new Error('No se pudo conectar el dominio.');
      return data;
    }, onSuccess: async () => { setMensaje('El dominio quedó pendiente de verificación.'); setError(undefined); await cache.invalidateQueries({ queryKey: ['dominio-api', id] }); },
    onError: fallo => { setMensaje(undefined); setError(fallo instanceof ErrorApi ? fallo.titulo : 'No se pudo conectar el dominio.'); },
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
    }, onSuccess: async () => { setMensaje(tipo === 'simular' ? 'El registro DNS simulado se creó.' : tipo === 'verificar' ? 'La verificación DNS terminó.' : 'El dominio se desconectó.'); setError(undefined); await cache.invalidateQueries({ queryKey: ['dominio-api', id] }); },
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

  return <div>
    <header className="flex flex-col gap-[10px]"><p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">{datos.apiNombre}</p><h1 className="font-display text-[32px] leading-[1.2]">Direcciones y dominio propio</h1><p className="text-[15px] leading-[1.55] text-tinta-suave">Dónde responden su portal y su API, el secreto que Shapi envía a su servidor y cómo conectar su propio dominio.</p></header>
    <div className="mt-8 grid gap-6 xl:grid-cols-3">
      <Tarjeta><div className="flex items-center justify-between"><h2 className="font-display text-[22px]">Direcciones asignadas</h2><span className="text-[12px] font-semibold uppercase text-exito">Activas</span></div><dl className="mt-5 grid gap-4 text-[14px]"><div className="flex justify-between gap-5"><dt className="text-tinta-suave">Portal</dt><dd>{datos.urlPortal}</dd></div><div className="flex justify-between gap-5"><dt className="text-tinta-suave">API</dt><dd>{datos.urlApi}</dd></div><div className="flex justify-between gap-5"><dt className="text-tinta-suave">Certificado TLS</dt><dd>Automático · Vigente</dd></div></dl><p className="mt-5 text-[13px] text-tinta-suave">Shapi emite y renueva los certificados de estas direcciones. No tiene que configurar nada.</p></Tarjeta>
      <Tarjeta><h2 className="font-display text-[22px]">Secreto de origen</h2><p className="mt-2 text-[14px] leading-[1.5] text-tinta-suave">Shapi lo envía a su servidor en la cabecera <span className="font-medium text-tinta">X-Shapi-Secreto</span>. Si lo valida, su servidor rechaza lo que no pasó por la compuerta. Es opcional.</p><div className="mt-5 flex items-center justify-between gap-4 rounded-base bg-fondo px-4 py-3 font-mono text-[13px]"><span>{secretoNuevo ?? datos.secretoEnmascarado}</span><Boton principal={false} type="button" onClick={() => secreto.mutate()}>Regenerar</Boton></div><p className="mt-3 text-[13px] text-tinta-suave">Por seguridad solo se muestra completo al generarlo.</p></Tarjeta>
    <Tarjeta><h2 className="font-display text-[22px]">Dominio propio</h2><p className="mt-2 text-[14px] leading-[1.5] text-tinta-suave">Conecte un dominio de su organización a su API. Shapi lo verifica con un registro DNS y le emite su certificado TLS. Incluido en su plan Producto.</p>
      <label className="mt-5 block text-[13px] font-medium">Dominio<input className={`${claseCampo} mt-2`} value={dominio} placeholder="api.enviosxelaju.localhost" onChange={e => setDominio(e.target.value)} /></label><div className="mt-4 flex gap-3"><Boton type="button" deshabilitado={!dominio.trim()} onClick={() => conectar.mutate()}>Conectar dominio</Boton>{datos.dominio && <Boton principal={false} type="button" onClick={() => eliminar.mutate()}>Desconectar</Boton>}</div>
      {datos.dominio && <div className="mt-6"><p className="text-[13px] font-medium text-tinta-suave">{datos.estado === 'verificado' ? 'Verificado' : datos.estado === 'fallido' ? 'Verificación fallida' : 'Pendiente de verificación'}</p><p className="mt-4 text-[13px] font-medium text-tinta-suave">Registro DNS que debe crear</p><div className="mt-3 overflow-auto"><table className="w-full border-collapse text-left text-[14px]"><thead><tr>{['Tipo', 'Nombre', 'Valor'].map(x => <th key={x} className="border-b border-borde pb-2 uppercase text-tinta-suave">{x}</th>)}</tr></thead><tbody><tr><td className="py-3 font-semibold">CNAME</td><td>{datos.dominio}</td><td>{datos.destinoCname}</td></tr></tbody></table></div>{datos.motivo && <div className="mt-4"><Aviso estado="neutro">{datos.motivo}</Aviso></div>}{datos.estado === 'verificado' && <div className="mt-4"><Aviso estado="exito">Dominio verificado. El certificado se emitirá con la primera petición.</Aviso></div>}{datos.modoSimulado && <p className="mt-4 text-[14px] text-tinta-suave">En este entorno los dominios son simulados: pulse «Simular la creación del registro» para que el DNS simulado lo tenga y luego verifíquelo.</p>}<div className="mt-5 flex flex-wrap gap-3">{datos.modoSimulado && <Boton principal={false} type="button" onClick={() => simular.mutate()}>Simular la creación del registro</Boton>}<Boton type="button" onClick={() => verificar.mutate()}>Verificar registro DNS</Boton></div></div>}
      {mensaje && <div className="mt-5"><Aviso estado="exito">{mensaje}</Aviso></div>}{error && <div className="mt-5"><Aviso estado="error">{error}</Aviso></div>}
    </Tarjeta></div>
  </div>;
}
