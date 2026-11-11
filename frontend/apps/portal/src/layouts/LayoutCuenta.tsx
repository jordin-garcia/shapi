import { Outlet } from 'react-router';
import { EstructuraPortal } from './LayoutPortal';

export function LayoutCuenta() {
  return <EstructuraPortal claseContenido="p-5 sm:p-8 lg:p-11"><Outlet /></EstructuraPortal>;
}
