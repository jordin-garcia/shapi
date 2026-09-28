import { Outlet } from 'react-router';
import { MarcaShapi } from './Navegacion';

// Encabezado de las pantallas de acceso (mockups/A1/Main.dc.html). A0.1 (/) tiene su propio encabezado y no lo usa.
export function LayoutPublico() {
  return (
    <div className="min-h-screen bg-fondo flex flex-col">
      <header className="dark h-[76px] shrink-0 bg-fondo border-b border-borde-barra flex items-center px-20">
        <MarcaShapi />
      </header>
      <main className="flex-1 flex flex-col">
        <Outlet />
      </main>
    </div>
  );
}
