import { useParams } from 'react-router';
import { useSesion } from '../modulos/sesion/useSesion';
import { SelectorApi } from '../modulos/apis/SelectorApi';
import { EnlaceNavegacion, EstructuraConBarras, GrupoNavegacion, ListaEnlaces } from './Navegacion';

// N.1 · Barra lateral del proveedor (mockups/Navegacion/Main.dc.html).
export function LayoutPanel() {
  const { data } = useSesion();
  const { id } = useParams();
  const api = id ? `/panel/apis/${id}` : null;

  return (
    <EstructuraConBarras textoSuperior={data?.nombreOrganizacion || 'Panel del proveedor'} perfil="/panel/perfil" nombre={data?.nombre} rol={data?.rol}>
      <GrupoNavegacion titulo="Publicación">
        <EnlaceNavegacion a="/panel/apis" exacto>APIs</EnlaceNavegacion>
      </GrupoNavegacion>

      <GrupoNavegacion titulo="API">
        <SelectorApi />
        {api ? (
          <ListaEnlaces>
            <EnlaceNavegacion a={`${api}/especificacion`}>Especificación</EnlaceNavegacion>
            <EnlaceNavegacion a={`${api}/rutas`}>Rutas expuestas</EnlaceNavegacion>
            <EnlaceNavegacion a={`${api}/configuracion-rutas`}>Configuración por ruta</EnlaceNavegacion>
            <EnlaceNavegacion a={`${api}/dominios`}>Dominios</EnlaceNavegacion>
            <EnlaceNavegacion a={`${api}/portal`}>Portal</EnlaceNavegacion>
            <EnlaceNavegacion a={`${api}/planes`}>Planes</EnlaceNavegacion>
            <EnlaceNavegacion a={`${api}/claves`}>Claves</EnlaceNavegacion>
            <EnlaceNavegacion a={`${api}/consumo`}>Consumo</EnlaceNavegacion>
            <EnlaceNavegacion a={`${api}/consumidores`}>Consumidores</EnlaceNavegacion>
          </ListaEnlaces>
        ) : <span className="px-3 text-sm text-tinta-suave">Seleccione una API para ver sus opciones</span>}
      </GrupoNavegacion>

      <GrupoNavegacion titulo="Organización">
        <ListaEnlaces>
          {data?.rol === 'propietario' && <EnlaceNavegacion a="/panel/miembros">Miembros y roles</EnlaceNavegacion>}
          {data?.rol !== 'editor' && <EnlaceNavegacion a="/panel/suscripcion">Suscripción de plataforma</EnlaceNavegacion>}
          {data?.rol !== 'editor' && <EnlaceNavegacion a="/panel/pagos">Historial de pagos</EnlaceNavegacion>}
          <EnlaceNavegacion a="/panel/soporte">Casos de soporte</EnlaceNavegacion>
        </ListaEnlaces>
      </GrupoNavegacion>
    </EstructuraConBarras>
  );
}
