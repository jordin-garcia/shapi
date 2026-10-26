import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { paths } from '@shapi/api/suscripciones';
import { Aviso } from '@shapi/ui';
import { Link } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);

export function AvisoPrueba({ venceEn }: { venceEn: string }) {
  const fecha = new Intl.DateTimeFormat('es-GT', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'America/Guatemala' }).format(new Date(venceEn));
  return <Aviso estado="error">
    <span>Su Prueba vence el {fecha}.</span>
    <Link className="shrink-0 font-medium underline" to="/panel/suscripcion/planes">Ver planes de pago</Link>
  </Aviso>;
}

export function AvisoPruebaPanel() {
  const consulta = useQuery({
    queryKey: ['suscripcion-plataforma'],
    queryFn: async ({ signal }) => {
      const { data } = await cliente.GET('/api/suscripcion', { signal });
      if (!data) throw new Error('No se pudo cargar la suscripción.');
      return data;
    },
    retry: false,
  });
  return consulta.data?.avisoPrueba ? <div className="mb-6"><AvisoPrueba venceEn={consulta.data.avisoPrueba.venceEn} /></div> : null;
}
