import { Outlet } from 'react-router';

export function LayoutPublico() {
  return (
    <div className="min-h-screen bg-[var(--fondo)] flex flex-col">
      <header className="h-[76px] bg-white border-b border-[var(--borde)] flex items-center px-8">
        <div className="flex items-center gap-3">
          <span className="font-display text-xl font-medium text-[var(--tinta)]">Shapi</span>
        </div>
      </header>
      <main className="flex-1 p-8">
        <Outlet />
      </main>
    </div>
  );
}
