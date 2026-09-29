export function PaginaRelleno({ id, titulo }: { id: string; titulo: string }) {
  return (
    <section className="p-11">
      <p className="text-etiqueta uppercase text-tinta-suave">{id}</p>
      <h1 className="mt-2 font-display text-titulo text-tinta">{id} · {titulo}</h1>
      <p className="mt-3 text-cuerpo text-tinta-suave">Esta pantalla se implementará en una tarea posterior.</p>
    </section>
  );
}
