import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/apis';
import { Aviso, Boton, Campo } from '@shapi/ui';
import { Link, useNavigate } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
const csrf = { header: { 'X-Requested-With': 'shapi' } } as const;
const dominioBase = window.location.hostname === 'localhost' ? 'shapi.localhost' : window.location.hostname;

type ApiRegistrada = components['schemas']['ApiRegistrada'];
type Errores = Partial<Record<'nombre' | 'urlOrigen' | 'subdominio', string>>;

export default function PaginaA32Registro() {
  const navegar = useNavigate();
  const cache = useQueryClient();
  const [nombre, setNombre] = useState('');
  const [urlOrigen, setUrlOrigen] = useState('');
  const [subdominio, setSubdominio] = useState('');
  const [errores, setErrores] = useState<Errores>({});
  const [errorGeneral, setErrorGeneral] = useState<string>();
  const [registrada, setRegistrada] = useState<ApiRegistrada>();

  const registro = useMutation({
    mutationFn: async () => {
      const { data, response } = await cliente.POST('/api/apis', {
        params: csrf,
        body: { nombre, urlOrigen, subdominio },
      });
      if (!response.ok || !data) throw new Error('No se pudo registrar la API.');
      return data;
    },
    onSuccess: async data => {
      setRegistrada(data);
      await cache.invalidateQueries({ queryKey: ['apis'] });
    },
    onError: error => {
      if (error instanceof ErrorApi) {
        if (error.codigo === 'datos_invalidos') {
          setErrores({
            nombre: error.errores?.nombre?.[0],
            urlOrigen: error.errores?.urlOrigen?.[0],
            subdominio: error.errores?.subdominio?.[0],
          });
          return;
        }
        if (error.codigo === 'subdominio_ocupado') {
          setErrores({ subdominio: 'El subdominio no está disponible.' });
          return;
        }
        setErrorGeneral(error.titulo);
        return;
      }
      setErrorGeneral('No se pudo registrar la API. Inténtelo de nuevo.');
    },
  });

  function enviar(evento: React.FormEvent) {
    evento.preventDefault();
    setErrores({});
    setErrorGeneral(undefined);
    registro.mutate();
  }

  const subdominioVisible = subdominio || 'subdominio';

  return <div className="flex min-h-full items-center justify-center">
    <form onSubmit={enviar} className="w-[600px] rounded-base border border-borde bg-panel p-10">
      <div className="flex flex-col gap-2">
        <p className="text-etiqueta uppercase font-medium tracking-[0.16em] text-tinta-suave">Nueva API</p>
        <h1 className="font-display text-[32px] leading-[1.2]">Registrar una API</h1>
        <p className="text-[15px] leading-[1.55] text-tinta-suave">Dele un nombre a su API e indique la URL del servidor donde hoy responde. Shapi le envía las peticiones que atraviesan la compuerta, sin modificar ese servidor.</p>
      </div>

      <Campo className="mt-8" etiqueta="Nombre de la API" value={nombre} onChange={evento => setNombre(evento.target.value)} error={errores.nombre} />

      <div className="mt-5">
        <Campo etiqueta="URL del servidor de origen" value={urlOrigen} onChange={evento => setUrlOrigen(evento.target.value)} error={errores.urlOrigen} />
        <p className="mt-[7px] text-[13px] leading-[1.5] text-tinta-suave">Es la dirección de su servidor, no la que usarán sus consumidores.</p>
      </div>

      <div className="mt-5">
        <div className="flex items-end gap-[10px]">
          <Campo className="w-[220px]" etiqueta="Subdominio" value={subdominio} onChange={evento => setSubdominio(evento.target.value)} error={errores.subdominio} />
          <span className="mb-[13px] text-[15px] text-tinta-suave">.{dominioBase}</span>
        </div>
        <p className="mt-[7px] text-[13px] leading-[1.5] text-tinta-suave">Su portal quedará en <span className="font-medium text-tinta">{subdominioVisible}.{dominioBase}</span> y su API en <span className="font-medium text-tinta">{subdominioVisible}.api.{dominioBase}</span>. De 3 a 30 letras minúsculas, números o guiones.</p>
      </div>

      {errorGeneral && <div className="mt-6"><Aviso estado="error">{errorGeneral}</Aviso></div>}

      {registrada && <div className="mt-6 flex flex-col gap-4">
        <Aviso estado="exito">
          <p>Conexión probada: su servidor respondió en <span className="tabular-nums">{registrada.conexionMilisegundos} ms</span>. Shapi probó la conexión antes de guardar.</p>
        </Aviso>
        <div className="rounded-base border border-borde bg-fondo p-4">
          <p className="text-[13px] font-semibold text-tinta">Secreto de origen</p>
          <code className="mt-2 block break-all rounded-base bg-panel px-3 py-2 text-[14px] text-tinta">{registrada.secretoOrigen}</code>
          <p className="mt-2 text-[13px] text-tinta-suave">Guárdelo ahora. Por seguridad, Shapi no volverá a mostrarlo.</p>
        </div>
      </div>}

      <div className="mt-8 flex flex-col gap-[14px] border-t border-borde-fila pt-6">
        {registrada
          ? <Boton type="button" onClick={() => void navegar(`/panel/apis/${encodeURIComponent(registrada.id)}/especificacion`)}>Continuar a la especificación</Boton>
          : <Boton type="submit" deshabilitado={registro.isPending}>{registro.isPending ? 'Probando conexión…' : 'Registrar y continuar'}</Boton>}
        <p className="text-center text-[14px] text-tinta-suave"><Link to="/panel/apis" className="text-principal hover:underline">Volver a las APIs</Link></p>
      </div>
    </form>
  </div>;
}
