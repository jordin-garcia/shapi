import { useState, type ReactNode } from 'react';
import { Boton, Campo, Etiqueta, Tarjeta, Tabla, Aviso, Esqueleto, Toast, DialogoConfirmacion, Selector, EstadoCargando, EstadoError, EstadoSinPermiso } from '@shapi/ui';

// A0.2 · Lámina de estilo (mockups/A0/Lamina.dc.html). Cada muestra lleva el token que la define: la prueba comprueba
// que el valor que se ve es el de style.css.
interface Color { nombre: string; color: string; token: string; descripcion?: string }

const oscuraPaneles: Color[] = [
  { nombre: 'Fondo', color: '#060910', token: '--fondo' },
  { nombre: 'Panel', color: '#0C1220', token: '--panel' },
  { nombre: 'Borde', color: '#1B2436', token: '--borde' },
  { nombre: 'Principal aclarado', color: '#7FA6FF', token: '--principal' },
];
const oscuraTextos: Color[] = [
  { nombre: 'Texto y acción', color: '#E8EDF7', token: '--tinta' },
  { nombre: 'Texto suave', color: '#8B98B0', token: '--tinta-suave' },
  { nombre: 'Correcto', color: '#3FBF88', token: '--correcto-base' },
  { nombre: 'Alerta', color: '#F08A5F', token: '--alerta-base' },
];
const claraPaneles: Color[] = [
  { nombre: 'Principal', color: '#3B6FF0', token: '--principal', descripcion: 'Acciones, énfasis, plan destacado' },
  { nombre: 'Tinta', color: '#0B1220', token: '--tinta', descripcion: 'Texto principal y cabeceras' },
  { nombre: 'Correcto', color: '#1F8A5B', token: '--correcto-base', descripcion: 'Activa, pago confirmado' },
  { nombre: 'Alerta', color: '#C2481F', token: '--alerta-base', descripcion: 'Cuota agotada, suspensión' },
];
const claraBandas: Color[] = [
  { nombre: 'Base', color: '#FFFFFF', token: '--panel' },
  { nombre: 'Banda', color: '#F4F6FA', token: '--fondo' },
  { nombre: 'Borde', color: '#DCE3EE', token: '--borde' },
  { nombre: 'Tinta suave', color: '#5A6884', token: '--tinta-suave' },
];

function datos(muestra: Color) {
  return { 'data-nombre': muestra.nombre, 'data-color': muestra.color, 'data-token': muestra.token };
}
// En la superficie oscura, los códigos de color y los rótulos van en --tinta-rotulo y los nombres de las bandas
// en --tinta-navegacion, como en la lámina; esos tokens solo existen dentro de .dark.
const codigo = (oscura: boolean) => (oscura ? 'text-tinta-rotulo' : 'text-tinta-suave');
function MuestraPanel({ muestra, oscura = false }: { muestra: Color; oscura?: boolean }) {
  return <div {...datos(muestra)} className="border border-borde rounded-base overflow-hidden">
    <div style={{ background: muestra.color }} className="h-[88px]" />
    <div className="p-[14px] border-t border-borde flex flex-col gap-[2px]">
      <span className="text-[14px] font-semibold">{muestra.nombre}</span>
      <span className={`text-[13px] ${codigo(oscura)} tabular-nums`}>{muestra.color}</span>
      {muestra.descripcion && <span className="text-[13px] text-tinta-suave">{muestra.descripcion}</span>}
    </div>
  </div>;
}
function MuestraBanda({ muestra, conBorde = false, oscura = false }: { muestra: Color; conBorde?: boolean; oscura?: boolean }) {
  return <div {...datos(muestra)} className="flex flex-col gap-2">
    <div style={{ background: muestra.color }} className={`h-12 rounded-base ${conBorde ? 'border border-borde' : ''}`} />
    <span className={`text-[13px] ${oscura ? 'text-tinta-navegacion' : ''}`}>{muestra.nombre} <span className={`${codigo(oscura)} tabular-nums`}>{muestra.color}</span></span>
  </div>;
}
function Rotulo({ children, oscura = false }: { children: ReactNode; oscura?: boolean }) {
  return <p className={`text-etiqueta uppercase font-medium ${codigo(oscura)}`}>{children}</p>;
}
function Seccion({ titulo, children }: { titulo: string; children: ReactNode }) {
  const id = `seccion-${titulo.normalize('NFD').replace(/[^\w]+/g, '-').toLowerCase()}`;
  return <section aria-labelledby={id} className="px-20 pt-14">
    <h2 id={id} className="font-sans text-etiqueta uppercase font-medium border-b border-borde pb-3">{titulo}</h2>
    <div className="mt-7">{children}</div>
  </section>;
}
/** Logotipo: el cuadro es el plano de control; las barras cortas, las peticiones que la compuerta detiene. */
function Logotipo() {
  return <div className="flex items-center gap-4">
    <svg role="img" aria-label="Shapi" width="48" height="48" viewBox="0 0 24 24" fill="none">
      <rect x="1" y="1" width="22" height="22" rx="6" stroke="var(--principal)" strokeWidth="1.5" />
      <path d="M6 8.5 H14" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M6 15.5 H12" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
      <path d="M6 12 H22" stroke="var(--tinta)" strokeWidth="2.5" strokeLinecap="round" />
    </svg>
    <span aria-hidden="true" className="font-display text-[44px] font-medium tracking-[-0.03em]">Shapi</span>
  </div>;
}

export default function UI() {
  const [dialogo, setDialogo] = useState(false);
  const [confirmacion, setConfirmacion] = useState(0);
  const [api, setApi] = useState('envios');
  return <main className="bg-panel min-h-screen pb-20">
    <div className="dark bg-fondo text-tinta px-20 pt-14 pb-16">
      <header className="flex justify-between items-end gap-16 pb-12">
        <div className="flex flex-col gap-4"><Rotulo oscura>Lámina de estilo · Variante 4</Rotulo><h1 className="font-display text-[52px] leading-[1.05] font-light tracking-[-0.03em]">Plano azul</h1></div>
        <p className="max-w-[58ch] text-right text-[15px] text-tinta-suave">Azul de plano como color principal: sobre el fondo casi negro funciona como luz y como acento sin recurrir al morado, y sobre la superficie clara de la aplicación conserva contraste suficiente para marcar la acción disponible en una tabla larga.</p>
      </header>
      <h2 className="font-sans text-etiqueta uppercase font-medium border-b border-borde pb-3">Superficie oscura · página de inicio</h2>
      <div className="grid grid-cols-4 gap-5 mt-7">{oscuraPaneles.map(muestra => <MuestraPanel key={muestra.nombre} muestra={muestra} oscura />)}</div>
      <div className="grid grid-cols-4 gap-5 mt-5">{oscuraTextos.map(muestra => <MuestraBanda key={muestra.nombre} muestra={muestra} oscura />)}</div>
      <p className="text-[14px] text-tinta-suave mt-5 max-w-[96ch]">En la superficie oscura la acción principal se resuelve en blanco sobre fondo negro. El azul queda como acento, resplandor y retícula de plano, nunca como relleno de botón.</p>
      <div className="grid grid-cols-3 gap-8 mt-9">
        <div className="flex flex-col gap-[14px]"><Rotulo oscura>Botones sobre oscuro</Rotulo><div className="flex gap-3"><Boton className="bg-tinta! text-[#08101F]! hover:bg-tinta!">Crear cuenta</Boton><Boton principal={false} className="bg-transparent text-tinta hover:bg-panel">Ver precios</Boton></div></div>
        <div className="flex flex-col gap-[14px]"><Rotulo oscura>Panel sobre oscuro</Rotulo><div className="bg-panel border border-borde rounded-base p-[18px] flex flex-col gap-2"><h3 className="font-display text-[19px]">Plan Producto</h3><p className="text-[14px] text-tinta-suave">Proveedores con clientes establecidos</p></div></div>
        <div className="flex flex-col gap-[14px]"><Rotulo oscura>Retícula de plano</Rotulo><div className="h-[92px] border border-borde rounded-base" style={{ backgroundImage: 'linear-gradient(to right,#121B2E 1px,transparent 1px),linear-gradient(to bottom,#121B2E 1px,transparent 1px)', backgroundSize: '32px 32px' }} /></div>
      </div>
    </div>
    <Seccion titulo="Superficie clara · aplicación y portal">
      <div className="grid grid-cols-4 gap-5">{claraPaneles.map(muestra => <MuestraPanel key={muestra.nombre} muestra={muestra} />)}</div>
      <div className="grid grid-cols-4 gap-5 mt-5">{claraBandas.map((muestra, i) => <MuestraBanda key={muestra.nombre} muestra={muestra} conBorde={i < 2} />)}</div>
    </Seccion>
    <Seccion titulo="Tipografía">
      <div className="grid grid-cols-2 gap-14">
        <div className="flex flex-col gap-[26px]">
          <div className="flex flex-col gap-[6px]"><Rotulo>Títulos</Rotulo><p className="font-display text-[46px] leading-[1.05] font-light tracking-[-0.035em]">Sora</p><p className="text-[14px] text-tinta-suave">Ligera 300 · Regular 400 · Media 500. Los titulares van en 300 para que el tamaño no se vuelva peso. Respaldo: Segoe UI.</p></div>
          <div className="flex flex-col gap-[6px]"><Rotulo>Interfaz y datos</Rotulo><p className="text-[42px] leading-[1.05] font-medium">IBM Plex Sans</p><p className="text-[14px] text-tinta-suave">Regular 400 · Media 500 · Seminegra 600. Cifras tabulares activas en toda tabla. Respaldo: Segoe UI.</p></div>
        </div>
        <Tabla encabezados={['Estilo','Tamaño / interlínea','Familia y peso']} filas={[
          ['Marca del inicio','124 / 1.00','Sora 300'],['Título de sección','44 / 1.15','Sora 300'],['Título de panel','22 / 1.30','Sora 400'],['Cifra destacada','32 / 1.00','Sora 400, tabular'],['Entrada','20 / 1.55','Plex Sans 400'],['Cuerpo','16 / 1.60','Plex Sans 400'],['Dato de tabla','15 / 1.50','Plex Sans 400'],['Etiqueta','12 / 1.20','Plex Sans 500, mayúsculas, +0.16em'],
        ]} />
      </div>
    </Seccion>
    <Seccion titulo="Logotipo">
      <div className="grid grid-cols-2 gap-5">
        <div className="border border-borde rounded-base bg-panel p-10 flex flex-col gap-6"><Logotipo /><p className="text-[14px] text-tinta-suave">Positivo sobre claro. El cuadro es el plano de control; las dos barras cortas son las peticiones que la compuerta detiene y la barra larga es la que la atraviesa y sale.</p></div>
        <div className="dark border border-borde rounded-base bg-fondo text-tinta p-10 flex flex-col gap-6"><Logotipo /><p className="text-[14px] text-tinta-suave">Negativo sobre oscuro. La barra que atraviesa pasa a blanco para no perder contraste contra el azul aclarado.</p></div>
      </div>
    </Seccion>
    <Seccion titulo="Espaciado y radio"><div className="flex items-end gap-[18px]">{[4,8,12,16,20,24,32,44,64,80].map(valor => <div key={valor} className="flex flex-col items-center gap-2"><div className="bg-principal" style={{ width: valor, height: valor, borderRadius: Math.min(valor / 4, 8) }} /><span className="text-[13px] text-tinta-suave tabular-nums">{valor}</span></div>)}<div className="flex flex-col gap-2 ml-10"><div className="w-[150px] h-16 border border-tinta rounded-base" /><span className="text-[13px] text-tinta-suave">Radio único: 8 px en paneles, botones, campos y etiquetas. Márgenes de página: 80 px.</span></div></div></Seccion>
    <Seccion titulo="Componentes base · superficie clara">
      <div className="grid grid-cols-3 gap-9">
        <div className="flex flex-col gap-[14px]"><Rotulo>Botón principal</Rotulo><div className="flex gap-3"><Boton onClick={() => setDialogo(true)}>Publicar API</Boton><Boton className="bg-principal-hover" onClick={() => setDialogo(true)}>Publicar API</Boton></div><p className="text-[13px] text-tinta-suave">Reposo y cursor encima. Altura 46 px.</p></div>
        <div className="flex flex-col gap-[14px]"><Rotulo>Botón secundario</Rotulo><div className="flex gap-3"><Boton principal={false}>Cancelar</Boton><Boton principal={false} deshabilitado>Cancelar</Boton></div><p className="text-[13px] text-tinta-suave">Reposo y deshabilitado.</p></div>
        <div className="flex flex-col gap-[14px]"><Rotulo>Etiqueta de estado</Rotulo><div className="flex gap-[10px] flex-wrap"><Etiqueta estado="correcto">Activa</Etiqueta><Etiqueta estado="alerta">Suspendida</Etiqueta><Etiqueta estado="neutro">Despublicada</Etiqueta></div><p className="text-[13px] text-tinta-suave">Correcto, alerta y neutro. No hay más colores de estado.</p></div>
        <div className="flex flex-col gap-[14px]"><Rotulo>Campo de formulario</Rotulo><Campo etiqueta="Servidor de origen" placeholder="https://" /><Campo etiqueta="Subdominio" defaultValue="envios" className="[&_input]:border-principal [&_input]:ring-[3px] [&_input]:ring-anillo-foco" /><p className="text-[13px] text-tinta-suave">Vacío con texto guía, y enfocado.</p></div>
        <div className="flex flex-col gap-[14px]"><Rotulo>Tarjeta</Rotulo><Tarjeta className="flex flex-col gap-3"><div className="flex items-center justify-between gap-4"><h3 className="font-display text-[20px]">Plan Lanzamiento</h3><Etiqueta estado="correcto">Activa</Etiqueta></div><p className="text-[14px] text-tinta-suave">Empezar a cobrar por una API existente</p><p className="border-t border-borde-fila pt-3 flex items-baseline gap-2"><span className="font-display text-[26px] tracking-[-0.03em] tabular-nums">Q 199.00</span><span className="text-[13px] text-tinta-suave">cada 30 días</span></p></Tarjeta><p className="text-[13px] text-tinta-suave">Borde de 1 px, radio 8 px, sin sombra.</p></div>
        <div className="flex flex-col gap-[14px]"><Rotulo>Tabla</Rotulo><Tabla encabezados={['Ruta','Llamadas','Estado']} filas={[
          ['/cotizaciones','128,400',<span className="text-correcto-base font-semibold" key="c">Expuesta</span>],['/guias','64,120',<span className="text-correcto-base font-semibold" key="g">Expuesta</span>],['/tarifas','9,860',<span className="text-tinta-suave" key="t">Oculta</span>],
        ]} /><p className="text-[13px] text-tinta-suave">Filas de 42 px, cifras tabulares alineadas a la izquierda.</p></div>
      </div>
    </Seccion>
    <Seccion titulo="Estados e interacción">
      <div className="grid grid-cols-3 gap-8 items-start">
        <EstadoCargando />
        <EstadoError reintentar={() => undefined} />
        <EstadoSinPermiso />
        <Aviso estado="exito">Cambios guardados.</Aviso>
        <Aviso estado="neutro">No hay datos todavía.</Aviso>
        <Esqueleto className="h-24" />
        <Selector aria-label="API" opciones={[{ etiqueta: 'Envíos Xelajú', valor: 'envios' }, { etiqueta: 'Agro Precios', valor: 'agro' }]} value={api} onChange={evento => setApi(evento.target.value)} />
        <Campo etiqueta="Nombre" error="Este campo es obligatorio" />
      </div>
    </Seccion>
    <DialogoConfirmacion abierto={dialogo} titulo="Publicar API" textoConfirmar="Publicar" cerrar={() => setDialogo(false)} confirmar={() => { setDialogo(false); setConfirmacion(valor => valor + 1); }}>
      La API quedará disponible para los consumidores.
    </DialogoConfirmacion>
    {confirmacion > 0 && <Toast key={confirmacion}>API publicada.</Toast>}
  </main>;
}
