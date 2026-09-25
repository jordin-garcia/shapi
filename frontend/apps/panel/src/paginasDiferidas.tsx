import { lazy, Suspense } from 'react';
import { EstadoCargando } from '@shapi/ui';
// A0
export const A0Lamina = lazy(() => import('./paginas/_UI'));
export const A0Inicio = lazy(() => import('./paginas/A0-1-Inicio'));
// A1
export const A1Registro = lazy(() => import('./paginas/A1-1-Registro'));
export const A1Verificacion = lazy(() => import('./paginas/A1-2-Verificacion'));
export const A1Sesion = lazy(() => import('./paginas/A1-3-Sesion'));
export const A1Recuperacion = lazy(() => import('./paginas/A1-4a-Recuperacion'));
export const A1NuevaContrasena = lazy(() => import('./paginas/A1-4b-NuevaContrasena'));
// A2
export const A2PlanesPlataforma = lazy(() => import('./paginas/A2-1-PlanesPlataforma'));
export const A2Contratacion = lazy(() => import('./paginas/A2-2-Contratacion'));
export const A2CambioPlan = lazy(() => import('./paginas/A2-5-CambioPlan'));
// A3
export const A3Apis = lazy(() => import('./paginas/A3-1-Apis'));
export const A3Registro = lazy(() => import('./paginas/A3-2-Registro'));
export const A3Especificacion = lazy(() => import('./paginas/A3-3-Especificacion'));
export const A3Rutas = lazy(() => import('./paginas/A3-4-Rutas'));
export const A3ConfigRutas = lazy(() => import('./paginas/A3-5-ConfigRutas'));
export const A3Dominio = lazy(() => import('./paginas/A3-6-Dominio'));
export const A3Portal = lazy(() => import('./paginas/A3-7-Portal'));
// A4
export const A4PlanesApi = lazy(() => import('./paginas/A4-1-PlanesApi'));
export const A4Miembros = lazy(() => import('./paginas/A4-2-Miembros'));
export const A4Claves = lazy(() => import('./paginas/A4-3-Claves'));
// A6
export const A6PlanesPlataforma = lazy(() => import('./paginas/A6-1-PlanesPlataforma'));
export const A6Organizaciones = lazy(() => import('./paginas/A6-2-Organizaciones'));
export const A6Pagos = lazy(() => import('./paginas/A6-3-Pagos'));
export const A6Casos = lazy(() => import('./paginas/A6-4-Casos'));
export const A6Caso = lazy(() => import('./paginas/A6-4b-Caso'));
export const A6Cuentas = lazy(() => import('./paginas/A6-5-Cuentas'));
// A7
export const A7Casos = lazy(() => import('./paginas/A7-1-Casos'));
export const A7Caso = lazy(() => import('./paginas/A7-2-Caso'));
// A8
export const A8Perfil = lazy(() => import('./paginas/A8-1-Perfil'));
export const A8Invitacion = lazy(() => import('./paginas/A8-2-Invitacion'));
// B1
export const B1Consumo = lazy(() => import('./paginas/B1-1-Consumo'));
export const B1Consumidores = lazy(() => import('./paginas/B1-2-Consumidores'));
export const B1Pagos = lazy(() => import('./paginas/B1-3-Pagos'));
export const B1Suscripcion = lazy(() => import('./paginas/B1-4-Suscripcion'));
export const B1InvitarConsumidores = lazy(() => import('./paginas/B1-5-InvitarConsumidores'));
// B3
export const B3Estado = lazy(() => import('./paginas/B3-1-Estado'));
export const B3Bitacora = lazy(() => import('./paginas/B3-2-Bitacora'));
export const Suspensify = ({ children }: { children: React.ReactNode }) => (
  <Suspense fallback={<EstadoCargando />}>
    {children}
  </Suspense>
);
