import { useSesion } from '../modulos/sesion/useSesion';
import { EnlaceNavegacion, EstructuraConBarras, GrupoNavegacion, ListaEnlaces } from './Navegacion';

// A6 (administrador, mockups/A6/Main.dc.html) y su versión reducida B3 (soporte, mockups/B3/Soporte.dc.html).
export function LayoutAdmin() {
  const { data } = useSesion();
  const esAdministrador = data?.rol === 'administrador';

  return (
    <EstructuraConBarras textoSuperior="Plataforma Shapi" perfil="/admin/perfil" nombre={data?.nombre} rol={data?.rol}>
      {esAdministrador && (
        <GrupoNavegacion titulo="Plataforma">
          <ListaEnlaces>
            <EnlaceNavegacion a="/admin/planes">Planes de plataforma</EnlaceNavegacion>
            <EnlaceNavegacion a="/admin/organizaciones">Organizaciones</EnlaceNavegacion>
            <EnlaceNavegacion a="/admin/pagos">Pagos</EnlaceNavegacion>
          </ListaEnlaces>
        </GrupoNavegacion>
      )}

      <GrupoNavegacion titulo="Soporte">
        <ListaEnlaces>
          <EnlaceNavegacion a="/admin/casos">Casos</EnlaceNavegacion>
        </ListaEnlaces>
      </GrupoNavegacion>

      <GrupoNavegacion titulo="Sistema">
        <ListaEnlaces>
          <EnlaceNavegacion a="/admin/estado">Estado de los componentes</EnlaceNavegacion>
          <EnlaceNavegacion a="/admin/bitacora">Bitácora de acciones</EnlaceNavegacion>
          {esAdministrador && <EnlaceNavegacion a="/admin/cuentas">Cuentas de plataforma</EnlaceNavegacion>}
        </ListaEnlaces>
      </GrupoNavegacion>
    </EstructuraConBarras>
  );
}
