import { lazy, Suspense } from 'react';
import { createBrowserRouter, Outlet } from 'react-router';
import { LayoutPublico } from './layouts/LayoutPublico';
import { LayoutPanel } from './layouts/LayoutPanel';
import { LayoutAdmin } from './layouts/LayoutAdmin';
import { RequiereSesion } from './modulos/sesion/RequiereSesion';
import { RequiereRol } from './modulos/sesion/RequiereRol';
import Error404 from './paginas/Error-404';

// A0
const A0Inicio = lazy(() => import('./paginas/A0-1-Inicio'));
// A1
const A1Registro = lazy(() => import('./paginas/A1-1-Registro'));
const A1Verificacion = lazy(() => import('./paginas/A1-2-Verificacion'));
const A1Sesion = lazy(() => import('./paginas/A1-3-Sesion'));
const A1Recuperacion = lazy(() => import('./paginas/A1-4a-Recuperacion'));
const A1NuevaContrasena = lazy(() => import('./paginas/A1-4b-NuevaContrasena'));
// A2
const A2PlanesPlataforma = lazy(() => import('./paginas/A2-1-PlanesPlataforma'));
const A2Contratacion = lazy(() => import('./paginas/A2-2-Contratacion'));
const A2CambioPlan = lazy(() => import('./paginas/A2-5-CambioPlan'));
// A3
const A3Apis = lazy(() => import('./paginas/A3-1-Apis'));
const A3Registro = lazy(() => import('./paginas/A3-2-Registro'));
const A3Especificacion = lazy(() => import('./paginas/A3-3-Especificacion'));
const A3Rutas = lazy(() => import('./paginas/A3-4-Rutas'));
const A3ConfigRutas = lazy(() => import('./paginas/A3-5-ConfigRutas'));
const A3Dominio = lazy(() => import('./paginas/A3-6-Dominio'));
const A3Portal = lazy(() => import('./paginas/A3-7-Portal'));
// A4
const A4PlanesApi = lazy(() => import('./paginas/A4-1-PlanesApi'));
const A4Miembros = lazy(() => import('./paginas/A4-2-Miembros'));
const A4Claves = lazy(() => import('./paginas/A4-3-Claves'));
// A6
const A6PlanesPlataforma = lazy(() => import('./paginas/A6-1-PlanesPlataforma'));
const A6Organizaciones = lazy(() => import('./paginas/A6-2-Organizaciones'));
const A6Pagos = lazy(() => import('./paginas/A6-3-Pagos'));
const A6Casos = lazy(() => import('./paginas/A6-4-Casos'));
const A6Caso = lazy(() => import('./paginas/A6-4b-Caso'));
const A6Cuentas = lazy(() => import('./paginas/A6-5-Cuentas'));
// A7
const A7Casos = lazy(() => import('./paginas/A7-1-Casos'));
const A7Caso = lazy(() => import('./paginas/A7-2-Caso'));
// A8
const A8Perfil = lazy(() => import('./paginas/A8-1-Perfil'));
const A8Invitacion = lazy(() => import('./paginas/A8-2-Invitacion'));
// B1
const B1Consumo = lazy(() => import('./paginas/B1-1-Consumo'));
const B1Consumidores = lazy(() => import('./paginas/B1-2-Consumidores'));
const B1Pagos = lazy(() => import('./paginas/B1-3-Pagos'));
const B1Suscripcion = lazy(() => import('./paginas/B1-4-Suscripcion'));
const B1InvitarConsumidores = lazy(() => import('./paginas/B1-5-InvitarConsumidores'));
// B3
const B3Estado = lazy(() => import('./paginas/B3-1-Estado'));
const B3Bitacora = lazy(() => import('./paginas/B3-2-Bitacora'));

const Suspensify = ({ children }: { children: React.ReactNode }) => (
  <Suspense fallback={<div className="p-8">Cargando...</div>}>
    {children}
  </Suspense>
);

export const router = createBrowserRouter([
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
          { index: true, element: <Suspensify><B1Suscripcion /></Suspensify> },
          { path: 'planes', element: <Suspensify><A2PlanesPlataforma /></Suspensify> },
          { path: 'contratar/:plan', element: <Suspensify><A2Contratacion /></Suspensify> },
          { path: 'cambiar/:plan', element: <Suspensify><A2CambioPlan /></Suspensify> }
        ]
      },
      {
        path: 'apis',
        element: <Outlet />,
        children: [
          { index: true, element: <Suspensify><A3Apis /></Suspensify> },
          { path: 'nueva', element: <Suspensify><A3Registro /></Suspensify> },
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
              { path: 'consumidores/invitar', element: <Suspensify><B1InvitarConsumidores /></Suspensify> }
            ]
          }
        ]
      },
      { path: 'miembros', element: <Suspensify><A4Miembros /></Suspensify> },
      { path: 'pagos', element: <Suspensify><B1Pagos /></Suspensify> },
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
      { path: 'planes', element: <Suspensify><A6PlanesPlataforma /></Suspensify> },
      { path: 'organizaciones', element: <Suspensify><A6Organizaciones /></Suspensify> },
      { path: 'pagos', element: <Suspensify><A6Pagos /></Suspensify> },
      { path: 'casos', element: <Suspensify><A6Casos /></Suspensify> },
      { path: 'casos/:numero', element: <Suspensify><A6Caso /></Suspensify> },
      { path: 'cuentas', element: <Suspensify><A6Cuentas /></Suspensify> },
      { path: 'estado', element: <Suspensify><B3Estado /></Suspensify> },
      { path: 'bitacora', element: <Suspensify><B3Bitacora /></Suspensify> },
      { path: 'perfil', element: <Suspensify><A8Perfil /></Suspensify> }
    ]
  }
]);
