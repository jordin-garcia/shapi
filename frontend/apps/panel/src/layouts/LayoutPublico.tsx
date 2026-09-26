import { Outlet } from 'react-router';

export function LayoutPublico() {
  return (
    <div className="min-h-screen bg-[var(--fondo)] flex flex-col">
      <header className="h-[76px] bg-[#060910] border-b border-[#131B2B] flex items-center px-20">
        <div className="flex items-center gap-[11px]">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true"><rect x="1" y="1" width="22" height="22" rx="6" stroke="#7FA6FF" strokeWidth="1.5" /><path d="M6 8.5 H14 M6 15.5 H12" stroke="#7FA6FF" strokeWidth="1.5" strokeLinecap="round" /><path d="M6 12 H22" stroke="#E8EDF7" strokeWidth="2.5" strokeLinecap="round" /></svg>
          <span className="font-display text-xl font-medium tracking-[-0.03em] text-[#E8EDF7]">Shapi</span>
        </div>
      </header>
      <main className="flex-1 flex flex-col">
        <Outlet />
      </main>
    </div>
  );
}
