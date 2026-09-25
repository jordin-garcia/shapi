export default function PaginaError403() {
  return (
    <div className="p-8">
      <h1 className="text-[32px] font-display text-[var(--tinta)] mb-2">No tiene permiso para ver esta página</h1>
      <p className="text-[var(--tinta-suave)]">Su rol no le permite acceder a este recurso.</p>
    </div>
  );
}
