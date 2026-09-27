
import { createBrowserRouter, Outlet } from 'react-router';
import { LayoutPublico } from './layouts/LayoutPublico';
import { LayoutPanel } from './layouts/LayoutPanel';
import { LayoutAdmin } from './layouts/LayoutAdmin';
import { RequiereSesion } from './modulos/sesion/RequiereSesion';
import { RequiereRol } from './modulos/sesion/RequiereRol';
import Error404 from './paginas/Error-404';

import { CargaDiferida, A0Lamina, A0Inicio, A1Registro, A1Verificacion, A1Sesion, A1Recuperacion, A1NuevaContrasena, A2PlanesPlataforma, A2Contratacion, A2CambioPlan, A3Apis, A3Registro, A3Especificacion, A3Rutas, A3ConfigRutas, A3Dominio, A3Portal, A4PlanesApi, A4Miembros, A4Claves, A6PlanesPlataforma, A6Organizaciones, A6Pagos, A6Casos, A6Caso, A6Cuentas, A7Casos, A7Caso, A8Perfil, A8Invitacion, B1Consumo, B1Consumidores, B1Pagos, B1Suscripcion, B1InvitarConsumidores, B3Estado, B3Bitacora } from './paginasDiferidas';

export const router = createBrowserRouter([
  { path: '/_ui', element: <CargaDiferida><A0Lamina /></CargaDiferida>, errorElement: <Error404 /> },
  {
    path: '/',
    element: <LayoutPublico />,
    errorElement: <Error404 />,
    children: [
      { index: true, element: <CargaDiferida><A0Inicio /></CargaDiferida> },
      { path: 'registro', element: <CargaDiferida><A1Registro /></CargaDiferida> },
      { path: 'verificar-correo', element: <CargaDiferida><A1Verificacion /></CargaDiferida> },
      { path: 'entrar', element: <CargaDiferida><A1Sesion /></CargaDiferida> },
      { path: 'recuperar', element: <CargaDiferida><A1Recuperacion /></CargaDiferida> },
      { path: 'restablecer', element: <CargaDiferida><A1NuevaContrasena /></CargaDiferida> },
      { path: 'invitacion', element: <CargaDiferida><A8Invitacion /></CargaDiferida> }
    ]
  },
  {
    path: '/panel',
    element: (
      <RequiereSesion>
        <RequiereRol roles={['propietario', 'editor', 'lector']}>
          <LayoutPanel />
        </RequiereRol>
      </RequiereSesion>
    ),
    errorElement: <Error404 />,
    children: [
      {
        path: 'suscripcion',
        element: <Outlet />,
        children: [
          { index: true, element: <CargaDiferida><RequiereRol roles={['propietario', 'lector']}><B1Suscripcion /></RequiereRol></CargaDiferida> },
          { path: 'planes', element: <CargaDiferida><RequiereRol roles={['propietario']}><A2PlanesPlataforma /></RequiereRol></CargaDiferida> },
          { path: 'contratar/:plan', element: <CargaDiferida><RequiereRol roles={['propietario']}><A2Contratacion /></RequiereRol></CargaDiferida> },
          { path: 'cambiar/:plan', element: <CargaDiferida><RequiereRol roles={['propietario']}><A2CambioPlan /></RequiereRol></CargaDiferida> }
        ]
      },
      {
        path: 'apis',
        element: <Outlet />,
        children: [
          { index: true, element: <CargaDiferida><A3Apis /></CargaDiferida> },
          { path: 'nueva', element: <CargaDiferida><RequiereRol roles={['propietario', 'editor']}><A3Registro /></RequiereRol></CargaDiferida> },
          {
            path: ':id',
            element: <Outlet />,
            children: [
              { path: 'especificacion', element: <CargaDiferida><A3Especificacion /></CargaDiferida> },
              { path: 'rutas', element: <CargaDiferida><A3Rutas /></CargaDiferida> },
              { path: 'configuracion-rutas', element: <CargaDiferida><A3ConfigRutas /></CargaDiferida> },
              { path: 'dominios', element: <CargaDiferida><A3Dominio /></CargaDiferida> },
              { path: 'portal', element: <CargaDiferida><A3Portal /></CargaDiferida> },
              { path: 'planes', element: <CargaDiferida><A4PlanesApi /></CargaDiferida> },
              { path: 'claves', element: <CargaDiferida><A4Claves /></CargaDiferida> },
              { path: 'consumo', element: <CargaDiferida><B1Consumo /></CargaDiferida> },
              { path: 'consumidores', element: <CargaDiferida><B1Consumidores /></CargaDiferida> },
              { path: 'consumidores/invitar', element: <CargaDiferida><RequiereRol roles={['propietario', 'editor']}><B1InvitarConsumidores /></RequiereRol></CargaDiferida> }
            ]
          }
        ]
      },
      { path: 'miembros', element: <CargaDiferida><RequiereRol roles={['propietario']}><A4Miembros /></RequiereRol></CargaDiferida> },
      { path: 'pagos', element: <CargaDiferida><RequiereRol roles={['propietario', 'lector']}><B1Pagos /></RequiereRol></CargaDiferida> },
      { path: 'soporte', element: <CargaDiferida><A7Casos /></CargaDiferida> },
      { path: 'soporte/:numero', element: <CargaDiferida><A7Caso /></CargaDiferida> },
      { path: 'perfil', element: <CargaDiferida><A8Perfil /></CargaDiferida> }
    ]
  },
  {
    path: '/admin',
    element: (
      <RequiereSesion>
        <RequiereRol roles={['administrador', 'soporte']}>
          <LayoutAdmin />
        </RequiereRol>
      </RequiereSesion>
    ),
    errorElement: <Error404 />,
    children: [
      { path: 'planes', element: <CargaDiferida><RequiereRol roles={['administrador']}><A6PlanesPlataforma /></RequiereRol></CargaDiferida> },
      { path: 'organizaciones', element: <CargaDiferida><A6Organizaciones /></CargaDiferida> },
      { path: 'pagos', element: <CargaDiferida><RequiereRol roles={['administrador']}><A6Pagos /></RequiereRol></CargaDiferida> },
      { path: 'casos', element: <CargaDiferida><A6Casos /></CargaDiferida> },
      { path: 'casos/:numero', element: <CargaDiferida><A6Caso /></CargaDiferida> },
      { path: 'cuentas', element: <CargaDiferida><RequiereRol roles={['administrador']}><A6Cuentas /></RequiereRol></CargaDiferida> },
      { path: 'estado', element: <CargaDiferida><B3Estado /></CargaDiferida> },
      { path: 'bitacora', element: <CargaDiferida><B3Bitacora /></CargaDiferida> },
      { path: 'perfil', element: <CargaDiferida><A8Perfil /></CargaDiferida> }
    ]
  }
]);
