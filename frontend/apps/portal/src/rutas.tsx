import { createBrowserRouter, Navigate, type RouteObject } from 'react-router';
import { LayoutPublico } from './layouts/LayoutPublico';
import { LayoutCuenta } from './layouts/LayoutCuenta';
import { RequiereSesionConsumidor } from './modulos/sesion/RequiereSesionConsumidor';
import A50Inicio from './paginas/A5-0-Inicio';
import A51Documentacion from './paginas/A5-1-Documentacion';
import A52Consola from './paginas/A5-2-Consola';
import A53Registro from './paginas/A5-3-Registro';
import A53bInvitacion from './paginas/A5-3b-Invitacion';
import A54Planes from './paginas/A5-4-Planes';
import A56Pago from './paginas/A5-6-Pago';
import A57Acceso from './paginas/A5-7-Acceso';
import A58Verificacion from './paginas/A5-8-Verificacion';
import A59Recuperacion from './paginas/A5-9-Recuperacion';
import A510NuevaContrasena from './paginas/A5-10-NuevaContrasena';
import B21Consumo from './paginas/B2-1-Consumo';
import B22Pagos from './paginas/B2-2-Pagos';
import B23Suscripcion from './paginas/B2-3-Suscripcion';
import B27CambioPlan from './paginas/B2-7-CambioPlan';
import Error404 from './paginas/Error-404';
import { ErrorRuta } from './paginas/Error-Ruta';

export function crearRutas(): RouteObject[] {
  return [
    {
      element: <LayoutPublico />,
      errorElement: <ErrorRuta />,
      children: [
        { path: '/', element: <A50Inicio /> },
        // Los enlaces de «Documentación» llevan a /documentacion; DC-07 lo redirigirá a la primera ruta expuesta.
        { path: '/documentacion', element: <A51Documentacion /> },
        { path: '/documentacion/:ruta', element: <A51Documentacion /> },
        { path: '/consola', element: <A52Consola /> },
        { path: '/registro', element: <A53Registro /> },
        { path: '/invitacion', element: <A53bInvitacion /> },
        { path: '/planes', element: <A54Planes /> },
        { path: '/contratar/:plan', element: <A56Pago /> },
        { path: '/entrar', element: <A57Acceso /> },
        { path: '/verificar-correo', element: <A58Verificacion /> },
        { path: '/recuperar', element: <A59Recuperacion /> },
        { path: '/restablecer', element: <A510NuevaContrasena /> },
        { path: '*', element: <Error404 /> },
      ],
    },
    {
      path: '/cuenta',
      element: <RequiereSesionConsumidor><LayoutCuenta /></RequiereSesionConsumidor>,
      errorElement: <ErrorRuta />,
      children: [
        { index: true, element: <Navigate to="suscripcion" replace /> },
        { path: 'consumo', element: <B21Consumo /> },
        { path: 'pagos', element: <B22Pagos /> },
        { path: 'suscripcion', element: <B23Suscripcion /> },
        { path: 'cambiar-plan/:plan', element: <B27CambioPlan /> },
        { path: '*', element: <Error404 /> },
      ],
    },
  ];
}

export const router = createBrowserRouter(crearRutas());
