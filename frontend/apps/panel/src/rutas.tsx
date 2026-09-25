
import { createBrowserRouter, Outlet } from 'react-router';
import { LayoutPublico } from './layouts/LayoutPublico';
import { LayoutPanel } from './layouts/LayoutPanel';
import { LayoutAdmin } from './layouts/LayoutAdmin';
import { RequiereSesion } from './modulos/sesion/RequiereSesion';
import { RequiereRol } from './modulos/sesion/RequiereRol';
import Error404 from './paginas/Error-404';

import { Suspensify, A0Lamina, A0Inicio, A1Registro, A1Verificacion, A1Sesion, A1Recuperacion, A1NuevaContrasena, A2PlanesPlataforma, A2Contratacion, A2CambioPlan, A3Apis, A3Registro, A3Especificacion, A3Rutas, A3ConfigRutas, A3Dominio, A3Portal, A4PlanesApi, A4Miembros, A4Claves, A6PlanesPlataforma, A6Organizaciones, A6Pagos, A6Casos, A6Caso, A6Cuentas, A7Casos, A7Caso, A8Perfil, A8Invitacion, B1Consumo, B1Consumidores, B1Pagos, B1Suscripcion, B1InvitarConsumidores, B3Estado, B3Bitacora } from './paginasDiferidas';

export const router = createBrowserRouter([
  { path: '/_ui', element: <Suspensify><A0Lamina /></Suspensify>, errorElement: <Error404 /> },
  {
    path: '/',
    element: <LayoutPublico />,
    errorElement: <Error404 />,
    children: [
      { index: true, element: <Suspensify><A0Inicio /></Suspensify> },
      { path: 'registro', element: <Suspensify><A1Registro /></Suspensify> },
      { path: 'verificar-correo', element: <Suspensify><A1Verificacion /></Suspensify> },
      { path: 'entrar', element: <Suspensify><A1Sesion /></Suspensify> },
      { path: 'recuperar', element: <Suspensify><A1Recuperacion /></Suspensify> },
      { path: 'restablecer', element: <Suspensify><A1NuevaContrasena /></Suspensify> },
      { path: 'invitacion', element: <Suspensify><A8Invitacion /></Suspensify> }
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
          { index: true, element: <Suspensify><RequiereRol roles={['propietario', 'lector']}><B1Suscripcion /></RequiereRol></Suspensify> },
          { path: 'planes', element: <Suspensify><RequiereRol roles={['propietario']}><A2PlanesPlataforma /></RequiereRol></Suspensify> },
          { path: 'contratar/:plan', element: <Suspensify><RequiereRol roles={['propietario']}><A2Contratacion /></RequiereRol></Suspensify> },
          { path: 'cambiar/:plan', element: <Suspensify><RequiereRol roles={['propietario']}><A2CambioPlan /></RequiereRol></Suspensify> }
        ]
      },
      {
        path: 'apis',
        element: <Outlet />,
        children: [
          { index: true, element: <Suspensify><A3Apis /></Suspensify> },
          { path: 'nueva', element: <Suspensify><RequiereRol roles={['propietario', 'editor']}><A3Registro /></RequiereRol></Suspensify> },
          {
            path: ':id',
            element: <Outlet />,
            children: [
              { path: 'especificacion', element: <Suspensify><A3Especificacion /></Suspensify> },
              { path: 'rutas', element: <Suspensify><A3Rutas /></Suspensify> },
              { path: 'configuracion-rutas', element: <Suspensify><A3ConfigRutas /></Suspensify> },
              { path: 'dominios', element: <Suspensify><A3Dominio /></Suspensify> },
              { path: 'portal', element: <Suspensify><A3Portal /></Suspensify> },
              { path: 'planes', element: <Suspensify><A4PlanesApi /></Suspensify> },
              { path: 'claves', element: <Suspensify><A4Claves /></Suspensify> },
              { path: 'consumo', element: <Suspensify><B1Consumo /></Suspensify> },
              { path: 'consumidores', element: <Suspensify><B1Consumidores /></Suspensify> },
              { path: 'consumidores/invitar', element: <Suspensify><RequiereRol roles={['propietario', 'editor']}><B1InvitarConsumidores /></RequiereRol></Suspensify> }
            ]
          }
        ]
      },
      { path: 'miembros', element: <Suspensify><RequiereRol roles={['propietario']}><A4Miembros /></RequiereRol></Suspensify> },
      { path: 'pagos', element: <Suspensify><RequiereRol roles={['propietario', 'lector']}><B1Pagos /></RequiereRol></Suspensify> },
      { path: 'soporte', element: <Suspensify><A7Casos /></Suspensify> },
      { path: 'soporte/:numero', element: <Suspensify><A7Caso /></Suspensify> },
      { path: 'perfil', element: <Suspensify><A8Perfil /></Suspensify> }
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
      { path: 'planes', element: <Suspensify><RequiereRol roles={['administrador']}><A6PlanesPlataforma /></RequiereRol></Suspensify> },
      { path: 'organizaciones', element: <Suspensify><A6Organizaciones /></Suspensify> },
      { path: 'pagos', element: <Suspensify><RequiereRol roles={['administrador']}><A6Pagos /></RequiereRol></Suspensify> },
      { path: 'casos', element: <Suspensify><A6Casos /></Suspensify> },
      { path: 'casos/:numero', element: <Suspensify><A6Caso /></Suspensify> },
      { path: 'cuentas', element: <Suspensify><RequiereRol roles={['administrador']}><A6Cuentas /></RequiereRol></Suspensify> },
      { path: 'estado', element: <Suspensify><B3Estado /></Suspensify> },
      { path: 'bitacora', element: <Suspensify><B3Bitacora /></Suspensify> },
      { path: 'perfil', element: <Suspensify><A8Perfil /></Suspensify> }
    ]
  }
]);
