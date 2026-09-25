import { useState, type ReactNode } from 'react';
import { Boton, Campo, Etiqueta, Tarjeta, Tabla, Aviso, Esqueleto, Toast, DialogoConfirmacion, Selector } from '@shapi/ui';

function Muestra({ nombre, color }: { nombre: string; color: string }) {
  return <div className="border border-borde rounded-base overflow-hidden">
    <div style={{ background: color }} className="h-[88px]" />
    <div className="p-4 border-t border-borde"><p className="font-semibold">{nombre}</p><p className="text-sm text-tinta-suave tabular-nums">{color}</p></div>
  </div>;
}
function Seccion({ titulo, children }: { titulo: string; children: ReactNode }) {
  return <section className="px-20 pt-14"><h2 className="text-xs uppercase tracking-[.16em] border-b border-borde pb-3">{titulo}</h2><div className="mt-7">{children}</div></section>;
}
function Marca() {
  return <div className="flex items-center gap-3"><svg width="32" height="32" viewBox="0 0 24 24" fill="none" aria-hidden="true"><rect x="1" y="1" width="22" height="22" rx="6" stroke="currentColor" /><path d="M6 8.5 H14 M6 15.5 H12 M6 12 H22" stroke="currentColor" strokeWidth="1.5" /></svg><span className="font-display text-[32px]">Shapi</span></div>;
}

export default function UI() {
  const [dialogo, setDialogo] = useState(false);
  const [confirmacion, setConfirmacion] = useState(0);
  const [api, setApi] = useState('envios');
  return <main className="bg-white min-h-screen pb-20">
    <div className="dark bg-fondo text-tinta px-20 pt-14 pb-16">
      <header className="flex justify-between items-end gap-16 pb-12">
        <div><p className="text-xs uppercase tracking-[.16em] text-tinta-suave mb-4">Lámina de estilo · Variante 4</p><h1 className="font-display text-[52px] leading-[1.05] font-light">Plano azul</h1></div>
        <p className="max-w-[58ch] text-right text-[15px] text-tinta-suave">Azul de plano como color principal: sobre el fondo casi negro funciona como luz y como acento sin recurrir al morado, y sobre la superficie clara de la aplicación conserva contraste suficiente para marcar la acción disponible en una tabla larga.</p>
      </header>
      <h2 className="text-xs uppercase tracking-[.16em] border-b border-borde pb-3">Superficie oscura · página de inicio</h2>
      <div className="grid grid-cols-4 gap-5 mt-7">{[['Fondo','#060910'],['Panel','#0C1220'],['Borde','#1B2436'],['Principal aclarado','#7FA6FF']].map(([nombre,color]) => <Muestra key={nombre} nombre={nombre} color={color} />)}</div>
      <div className="grid grid-cols-4 gap-5 mt-5">{[['Texto y acción','#E8EDF7'],['Texto suave','#8B98B0'],['Correcto','#3FBF88'],['Alerta','#F08A5F']].map(([nombre,color]) => <div key={nombre}><div className="h-12 rounded-base mb-2" style={{ background: color }} /><p className="text-sm">{nombre} <span className="text-tinta-suave">{color}</span></p></div>)}</div>
      <p className="text-sm text-tinta-suave mt-5">En la superficie oscura la acción principal se resuelve en blanco sobre fondo negro. El azul queda como acento, resplandor y retícula de plano, nunca como relleno de botón.</p>
      <div className="grid grid-cols-3 gap-8 mt-9">
        <div><h3 className="text-xs uppercase tracking-[.16em] mb-4">Botones sobre oscuro</h3><div className="flex gap-3"><Boton className="bg-[#E8EDF7] text-[#08101F] hover:bg-white">Crear cuenta</Boton><Boton principal={false} className="bg-transparent text-tinta border-borde-campo hover:bg-panel">Ver precios</Boton></div></div>
        <div><h3 className="text-xs uppercase tracking-[.16em] mb-4">Panel sobre oscuro</h3><div className="bg-panel border border-borde rounded-base p-5"><h4 className="font-display text-[19px]">Plan Producto</h4><p className="text-sm text-tinta-suave">Proveedores con clientes establecidos</p></div></div>
        <div><h3 className="text-xs uppercase tracking-[.16em] mb-4">Retícula de plano</h3><div className="h-[92px] border border-borde rounded-base" style={{ backgroundImage: 'linear-gradient(to right,#121B2E 1px,transparent 1px),linear-gradient(to bottom,#121B2E 1px,transparent 1px)', backgroundSize: '32px 32px' }} /></div>
      </div>
    </div>
    <Seccion titulo="Superficie clara · aplicación y portal">
      <div className="grid grid-cols-4 gap-5">{[['Principal','#3B6FF0'],['Tinta','#0B1220'],['Correcto','#146542'],['Alerta','#8E3315'],['Base','#FFFFFF'],['Banda','#F4F6FA'],['Borde','#DCE3EE'],['Tinta suave','#5A6884']].map(([nombre,color]) => <Muestra key={nombre} nombre={nombre} color={color} />)}</div>
    </Seccion>
    <Seccion titulo="Tipografía">
      <div className="grid grid-cols-2 gap-14">
        <div className="space-y-7"><div><h3 className="text-xs uppercase tracking-[.16em]">Títulos</h3><p className="font-display text-[46px] font-light">Sora</p><p className="text-sm text-tinta-suave">Ligera 300 · Regular 400 · Media 500. Los titulares van en 300 para que el tamaño no se vuelva peso. Respaldo: Segoe UI.</p></div><div><h3 className="text-xs uppercase tracking-[.16em]">Interfaz y datos</h3><p className="text-[42px] font-medium">IBM Plex Sans</p><p className="text-sm text-tinta-suave">Regular 400 · Media 500 · Seminegra 600. Cifras tabulares activas en toda tabla. Respaldo: Segoe UI.</p></div></div>
        <Tabla headers={['Estilo','Tamaño / interlínea','Familia y peso']} rows={[
          ['Marca del inicio','124 / 1.00','Sora 300'],['Título de sección','44 / 1.15','Sora 300'],['Título de panel','22 / 1.30','Sora 400'],['Cifra destacada','32 / 1.00','Sora 400, tabular'],['Entrada','20 / 1.55','Plex Sans 400'],['Cuerpo','16 / 1.60','Plex Sans 400'],['Dato de tabla','15 / 1.50','Plex Sans 400'],['Etiqueta','12 / 1.20','Plex Sans 500, mayúsculas, +0.16em'],
        ]} />
      </div>
    </Seccion>
    <Seccion titulo="Logotipo"><div className="grid grid-cols-2 gap-5"><div className="border border-borde rounded-base p-8"><Marca /></div><div className="dark bg-fondo text-tinta rounded-base p-8"><Marca /></div></div></Seccion>
    <Seccion titulo="Espaciado y radio"><div className="flex items-end gap-5">{[4,8,12,16,20,24,32,44,64,80].map(valor => <div key={valor} className="text-center text-sm"><div className="bg-principal rounded-base mb-2" style={{ width: valor, height: valor }} />{valor}</div>)}<p className="text-sm text-tinta-suave max-w-sm ml-10">Radio único: 8 px en paneles, botones, campos y etiquetas. Márgenes de página: 80 px.</p></div></Seccion>
    <Seccion titulo="Componentes base · superficie clara">
      <div className="grid grid-cols-3 gap-9">
        <div><h3 className="mb-4 text-xs uppercase tracking-[.16em]">Botón principal</h3><div className="flex gap-3"><Boton onClick={() => setDialogo(true)}>Publicar API</Boton><Boton className="bg-principal-hover" onClick={() => setDialogo(true)}>Publicar API</Boton></div><p className="text-sm text-tinta-suave mt-4">Reposo y cursor encima. Altura 46 px.</p></div>
        <div><h3 className="mb-4 text-xs uppercase tracking-[.16em]">Botón secundario</h3><div className="flex gap-3"><Boton principal={false}>Cancelar</Boton><Boton principal={false} deshabilitado>Cancelar</Boton></div><p className="text-sm text-tinta-suave mt-4">Reposo y deshabilitado.</p></div>
        <div><h3 className="mb-4 text-xs uppercase tracking-[.16em]">Etiqueta de estado</h3><div className="flex gap-2 flex-wrap"><Etiqueta estado="correcto">Activa</Etiqueta><Etiqueta estado="alerta">Suspendida</Etiqueta><Etiqueta estado="neutro">Despublicada</Etiqueta></div><p className="text-sm text-tinta-suave mt-4">Correcto, alerta y neutro. No hay más colores de estado.</p></div>
        <div className="space-y-4"><h3 className="text-xs uppercase tracking-[.16em]">Campo de formulario</h3><label className="block text-sm font-semibold">Servidor de origen<Campo placeholder="https://" /></label><label className="block text-sm font-semibold">Subdominio<Campo defaultValue="envios" /></label><p className="text-sm text-tinta-suave">Vacío con texto guía, y enfocado.</p></div>
        <div><h3 className="mb-4 text-xs uppercase tracking-[.16em]">Tarjeta</h3><Tarjeta><div className="flex items-center justify-between gap-4"><h4 className="font-display text-[22px]">Plan Lanzamiento</h4><Etiqueta estado="correcto">Activa</Etiqueta></div><p className="text-sm text-tinta-suave my-3">Empezar a cobrar por una API existente</p><p className="border-t border-borde-fila pt-3"><span className="font-display text-[26px] tabular-nums">Q 199.00</span> <span className="text-sm text-tinta-suave">cada 30 días</span></p></Tarjeta><p className="text-sm text-tinta-suave mt-4">Borde de 1 px, radio 8 px, sin sombra.</p></div>
        <div><h3 className="mb-4 text-xs uppercase tracking-[.16em]">Tabla</h3><Tabla headers={['Ruta','Llamadas','Estado']} rows={[
          ['/cotizaciones','128,400',<span className="text-correcto" key="c">Expuesta</span>],['/guias','64,120',<span className="text-correcto" key="g">Expuesta</span>],['/tarifas','9,860','Oculta'],
        ]} /><p className="text-sm text-tinta-suave mt-4">Filas de 42 px, cifras tabulares alineadas a la izquierda.</p></div>
      </div>
    </Seccion>
    <Seccion titulo="Estados e interacción">
      <div className="grid grid-cols-3 gap-8"><Aviso estado="error">No se pudo cargar la información.</Aviso><Aviso estado="exito">Cambios guardados.</Aviso><Aviso estado="neutro">No hay datos todavía.</Aviso><Esqueleto className="h-24" /><label>API<Selector options={[{ label: 'Envíos Xelajú', value: 'envios' }, { label: 'Agro Precios', value: 'agro' }]} value={api} onChange={evento => setApi(evento.target.value)} /></label><Campo aria-label="Nombre" error="Este campo es obligatorio" /></div>
    </Seccion>
    <DialogoConfirmacion open={dialogo} titulo="Publicar API" onClose={() => setDialogo(false)} onConfirm={() => { setDialogo(false); setConfirmacion(valor => valor + 1); }} />
    {confirmacion > 0 && <Toast key={confirmacion}>API publicada.</Toast>}
  </main>;
}
