import { useEffect, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import { crearCliente } from '@shapi/api';
import type { components, paths } from '@shapi/api/planes';
import { EstadoCargando, EstadoError } from '@shapi/ui';
import { Link } from 'react-router';

const cliente = crearCliente<paths>(window.location.origin);
type Plan = components['schemas']['PlanPlataforma'];
const dinero = (monto: number) => 'Q ' + monto.toLocaleString('es-GT', { minimumFractionDigits: 2 });

const ESTRELLAS_CAPA_1 = Array.from({ length: 180 }, (_, i) => {
  const x = ((i * 173 + 47) % 1980) + ((i * 29) % 20);
  const y = ((i * 251 + 83) % 1980) + ((i * 19) % 20);
  return `rgb(255, 255, 255) ${x}px ${y}px 0px 0px`;
}).join(', ');

const ESTRELLAS_CAPA_2 = Array.from({ length: 130 }, (_, i) => {
  const x = ((i * 227 + 113) % 1980) + ((i * 37) % 20);
  const y = ((i * 313 + 59) % 1980) + ((i * 23) % 20);
  return `rgb(127, 166, 255) ${x}px ${y}px 0px 0px`;
}).join(', ');

const ESTRELLAS_CAPA_3 = Array.from({ length: 85 }, (_, i) => {
  const x = ((i * 349 + 19) % 1980) + ((i * 41) % 20);
  const y = ((i * 197 + 149) % 1980) + ((i * 31) % 20);
  return `rgb(167, 139, 250) ${x}px ${y}px 0px 0px`;
}).join(', ');

const FILAS_MATRIZ_TOKENS = [
  'sk_live_9f8a2b1c · sha256:4e91a0c3 · GET /v1/pagos · 200 OK · 12ms · cuota 99.4%',
  'sk_test_3d7e5a90 · hmac_v2:8b2f19d4 · POST /v1/cobros · 201 · 18ms · rpm 42/120',
  'org_shapi_gt_01 · tls1.3_aes256 · GET /v1/facturas · 200 OK · 9ms · verificada',
  'sk_live_7c1d4e8b · sha256:0a3c7f21 · POST /v1/clientes · 200 · 14ms · activa',
  'sub_prod_88412a · bearer_jwt_rs256 · GET /v1/saldos · 200 OK · 11ms · cuota 84.1%',
];

// Mapa mundi ASCII real de alta resolución (120 columnas x 48 filas)
const MAPA_MUNDI_ASCII: readonly string[] = [
  '............................................##############..............................................................',
  '.........................................####################...........................................................',
  '.....................#####............########################...................####.......##############..............',
  '............#################.........########################.................########...########################......',
  '........########################.......######################................#######################################....',
  '......############################......####################...............###########################################..',
  '.....##############################.......################................############################################..',
  '....################################........############................#############################################...',
  '......##############################..........########.................#############################################....',
  '........############################..................................############################################......',
  '..........##########################.................................###########################################........',
  '...........########################.................................####..####################################..........',
  '............#######################................................#####...###################################..........',
  '.............#####################.................................###....####################################..........',
  '..............###################.................................#####..#####################################..........',
  '...............#################...................................############...############################..........',
  '................###############....................................##########.......##########################..........',
  '.................#############....................................##########.........########################...........',
  '..................###########....###..............................################....######################..##........',
  '...................########.....#####............................###################...####################..###........',
  '....................######........###...........................#####################...##################...##.........',
  '.....................#####.....................................#######################....###############...............',
  '......................####....................................########################.....#############................',
  '.......................####...................................########################......###########.................',
  '........................#####.................................#######################........#########..................',
  '.........................######...............................######################..........#######...................',
  '..........................##########..........................#####################...........######....##..............',
  '...........................############........................###################.............####.....###.............',
  '............................##############......................#################...............##.......###............',
  '............................################.....................###############.........................####...........',
  '............................#################.....................#############...........................#####.........',
  '.............................################......................############............................#####........',
  '..............................###############.......................###########......................###....####........',
  '...............................#############.........................##########.......................###...............',
  '................................###########..........................##########.........................................',
  '.................................#########............................#########.........................................',
  '..................................########............................#########...##........................######......',
  '..................................#######..............................#######...###......................##########....',
  '...................................######...............................######...##......................############...',
  '...................................#####.................................#####...........................############...',
  '...................................#####..................................###............................############...',
  '....................................####.................................................................###########....',
  '....................................###...................................................................#########.....',
  '....................................###....................................................................######.......',
  '....................................##..................................................................................',
  '....................................##..........................................................................##......',
  '....................................##.........................................................................###......',
  '........................................................................................................................',
];

interface PuntoTierra {
  lat: number;
  lon: number;
}

// Generamos los puntos del planeta Tierra estrictamente organizados en anillos de latitud (como en tu segunda captura)
const PUNTOS_TIERRA: PuntoTierra[] = (() => {
  const filas = MAPA_MUNDI_ASCII.length;
  const cols = MAPA_MUNDI_ASCII[0].length;
  const resultado: PuntoTierra[] = [];

  const celdaEsTierra = (latDeg: number, lonDeg: number) => {
    const fracFila = (82 - latDeg) / 140;
    const f = Math.max(0, Math.min(filas - 1, Math.floor(fracFila * filas)));
    const fracCol = ((lonDeg + 180) % 360) / 360;
    const c = Math.max(0, Math.min(cols - 1, Math.floor(fracCol * cols)));
    return MAPA_MUNDI_ASCII[f][c] === '#';
  };

  // Anillos concéntricos de latitud cada 1.75° para lograr las hileras curvas de puntos blancos nítidos
  for (let latDeg = -56; latDeg <= 81; latDeg += 1.75) {
    const latRad = (latDeg * Math.PI) / 180;
    const circunferencia = Math.max(0.14, Math.cos(latRad));
    const numPuntosAnillo = Math.max(24, Math.round(250 * circunferencia));
    for (let k = 0; k < numPuntosAnillo; k++) {
      const lonDeg = -180 + (k / numPuntosAnillo) * 360;
      if (celdaEsTierra(latDeg, lonDeg)) {
        resultado.push({
          lat: latRad,
          lon: (lonDeg * Math.PI) / 180,
        });
      }
    }
  }
  return resultado;
})();

interface PaisApiNodo {
  id: string;
  pais: string;
  etiquetaApi: string;
  latDeg: number;
  lonDeg: number;
  color: string;
  destino: 'izq' | 'der';
}

const PAISES_NODOS: PaisApiNodo[] = [
  { id: 'gt', pais: 'Guatemala', etiquetaApi: 'POST /cobros · 200 OK', latDeg: 15, lonDeg: -90.5, color: '#3FBF88', destino: 'izq' },
  { id: 'mx', pais: 'México', etiquetaApi: 'GET /pagos · 12ms', latDeg: 23, lonDeg: -102, color: '#7FA6FF', destino: 'izq' },
  { id: 'us', pais: 'EE. UU.', etiquetaApi: 'X-API-Key · Válida', latDeg: 38, lonDeg: -96, color: '#A78BFA', destino: 'izq' },
  { id: 'co', pais: 'Colombia', etiquetaApi: 'GET /saldo · Q 599', latDeg: 5, lonDeg: -74, color: '#3FBF88', destino: 'der' },
  { id: 'br', pais: 'Brasil', etiquetaApi: 'POST /clientes · 201', latDeg: -14, lonDeg: -51, color: '#7FA6FF', destino: 'der' },
  { id: 'es', pais: 'España', etiquetaApi: 'GET /facturas · TLS 1.3', latDeg: 40, lonDeg: -4, color: '#A78BFA', destino: 'der' },
  { id: 'de', pais: 'Alemania', etiquetaApi: 'Cuota · 98.4%', latDeg: 51, lonDeg: 10, color: '#3FBF88', destino: 'der' },
  { id: 'jp', pais: 'Japón', etiquetaApi: 'GET /v1/catalogo · 9ms', latDeg: 36, lonDeg: 138, color: '#7FA6FF', destino: 'izq' },
];

export default function PaginaA01Inicio() {
  const contenedorRef = useRef<HTMLDivElement>(null);
  const canvasPlanetaRef = useRef<HTMLCanvasElement>(null);

  const consulta = useQuery({
    queryKey: ['planes-plataforma'],
    queryFn: async ({ signal }) => {
      const { data, response } = await cliente.GET('/api/planes-plataforma', { signal });
      if (!response.ok || !data) throw new Error('No se pudieron cargar los planes.');
      return data;
    },
    retry: false,
  });

  const planAnual = consulta.data?.find(plan => plan.nombre === 'Escala anual');
  const visibles = consulta.data?.filter(plan => plan.nombre !== 'Escala anual') ?? [];

  // Planeta Tierra en hileras de puntos + estela blanca exterior + burbujas de países hacia las 2 APIs
  useEffect(() => {
    const canvas = canvasPlanetaRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let animId = 0;
    let rotacion = 1.05;
    const W = 1440;
    const H = 880;
    canvas.width = W;
    canvas.height = H;

    // Geometría exacta de domo planetario con inclinación hacia el polo superior como en tu foto
    const cx = W / 2;
    const cy = 735;
    const R = 410;
    const inclinacion = 0.52; // Inclinación marcada para que los anillos de latitud formen los arcos concéntricos de puntos en el polo
    const cosInc = Math.cos(inclinacion);
    const sinInc = Math.sin(inclinacion);

    const apiIzq = { x: 205, y: 392 };
    const apiDer = { x: 1235, y: 392 };

    const proyectar = (latRad: number, lonRad: number) => {
      const x = Math.cos(latRad) * Math.sin(lonRad);
      const y = Math.sin(latRad);
      const z = Math.cos(latRad) * Math.cos(lonRad);

      const yRot = y * cosInc - z * sinInc;
      const zRot = y * sinInc + z * cosInc;

      return {
        px: cx + x * R,
        py: cy - yRot * R,
        z: zRot,
      };
    };

    const dibujarFrame = (tiempoMs: number) => {
      ctx.clearRect(0, 0, W, H);

      const scrollActual = window.scrollY || 0;
      const anguloActual = rotacion + scrollActual * 0.002;

      // 1. Halo / Estela blanca exterior arqueada sobre el planeta Tierra (exactamente como en tu segunda captura)
      ctx.save();
      const radioHalo = R + 22;
      const gradEstelaBlanca = ctx.createLinearGradient(cx - radioHalo, cy - radioHalo * 0.35, cx + radioHalo, cy - radioHalo * 0.35);
      gradEstelaBlanca.addColorStop(0, 'rgba(255, 255, 255, 0)');
      gradEstelaBlanca.addColorStop(0.14, 'rgba(225, 232, 255, 0.22)');
      gradEstelaBlanca.addColorStop(0.5, 'rgba(255, 255, 255, 0.55)');
      gradEstelaBlanca.addColorStop(0.86, 'rgba(225, 232, 255, 0.22)');
      gradEstelaBlanca.addColorStop(1, 'rgba(255, 255, 255, 0)');

      // Difuminado suave detrás de la estela blanca
      ctx.beginPath();
      ctx.arc(cx, cy, radioHalo, Math.PI * 1.07, Math.PI * 1.93);
      ctx.strokeStyle = gradEstelaBlanca;
      ctx.lineWidth = 6;
      ctx.shadowColor = 'rgba(255, 255, 255, 0.45)';
      ctx.shadowBlur = 14;
      ctx.stroke();

      // Trazo nítido de la estela blanca
      ctx.beginPath();
      ctx.arc(cx, cy, radioHalo, Math.PI * 1.06, Math.PI * 1.94);
      ctx.strokeStyle = gradEstelaBlanca;
      ctx.lineWidth = 2.4;
      ctx.shadowBlur = 0;
      ctx.stroke();
      ctx.restore();

      // 2. Sombra esférica interior muy oscura para que los puntos blancos del planeta destaquen con limpieza
      ctx.save();
      const gradEsfera = ctx.createRadialGradient(cx, cy - R * 0.75, R * 0.05, cx, cy, R);
      gradEsfera.addColorStop(0, 'rgba(14, 20, 38, 0.42)');
      gradEsfera.addColorStop(0.55, 'rgba(7, 10, 20, 0.85)');
      gradEsfera.addColorStop(1, 'rgba(6, 9, 16, 0.98)');
      ctx.fillStyle = gradEsfera;
      ctx.beginPath();
      ctx.arc(cx, cy, R, 0, Math.PI * 2);
      ctx.fill();
      ctx.restore();

      // 3. Puntos blancos del Planeta Tierra en anillos de latitud (más brillantes en la cumbre superior y desvaneciéndose hacia abajo)
      ctx.save();
      for (let i = 0; i < PUNTOS_TIERRA.length; i++) {
        const p = PUNTOS_TIERRA[i];
        const proy = proyectar(p.lat, p.lon + anguloActual);

        if (proy.z > 0.03 && proy.py < H - 30) {
          // Desvanecido vertical idéntico a la foto: máximo brillo en el polo/cumbre superior (py ~ 325) y desvanecido hacia abajo (py ~ 700)
          const factorAltura = Math.max(0, Math.min(1, (705 - proy.py) / 375));
          const factorHorizonte = Math.pow(proy.z, 0.65);
          const alfa = Math.min(0.96, Math.pow(factorAltura, 1.25) * factorHorizonte * 1.15);

          if (alfa > 0.035) {
            const radioPunto = proy.py < 430 ? 1.15 : 0.95;
            ctx.fillStyle = `rgba(238, 242, 255, ${alfa.toFixed(3)})`;
            ctx.beginPath();
            ctx.arc(proy.px, proy.py, radioPunto, 0, Math.PI * 2);
            ctx.fill();
          }
        }
      }
      ctx.restore();

      // 4. Países saliendo del planeta Tierra con burbujas de texto y conectando con las 2 APIs
      for (let i = 0; i < PAISES_NODOS.length; i++) {
        const nodo = PAISES_NODOS[i];
        const latRad = (nodo.latDeg * Math.PI) / 180;
        const lonRad = (nodo.lonDeg * Math.PI) / 180 + anguloActual;
        const proy = proyectar(latRad, lonRad);

        if (proy.z > 0.24 && proy.py < 685) {
          const visibilidad = Math.min(1, (proy.z - 0.24) / 0.35);
          const destino = nodo.destino === 'izq' ? apiIzq : apiDer;

          const ctrlX = (proy.px + destino.x) * 0.5;
          const ctrlY = Math.min(proy.py, destino.y) - 65;

          // Arco conector punteado entre el país y la API
          ctx.save();
          ctx.beginPath();
          ctx.moveTo(proy.px, proy.py);
          ctx.quadraticCurveTo(ctrlX, ctrlY, destino.x, destino.y);
          ctx.strokeStyle = `rgba(190, 212, 255, ${(visibilidad * 0.42).toFixed(3)})`;
          ctx.lineWidth = 1.25;
          ctx.setLineDash([4, 6]);
          ctx.lineDashOffset = -(tiempoMs * 0.032);
          ctx.stroke();
          ctx.restore();

          // Partícula viajando desde el país hasta la API
          const t = (tiempoMs * 0.00045 + i * 0.22) % 1;
          const invT = 1 - t;
          const partX = invT * invT * proy.px + 2 * invT * t * ctrlX + t * t * destino.x;
          const partY = invT * invT * proy.py + 2 * invT * t * ctrlY + t * t * destino.y;

          ctx.save();
          ctx.beginPath();
          ctx.arc(partX, partY, 2.6, 0, Math.PI * 2);
          ctx.fillStyle = nodo.color;
          ctx.shadowColor = nodo.color;
          ctx.shadowBlur = 8;
          ctx.globalAlpha = visibilidad;
          ctx.fill();
          ctx.restore();

          // Punto en el país sobre el planeta
          ctx.save();
          ctx.globalAlpha = visibilidad;
          ctx.beginPath();
          ctx.arc(proy.px, proy.py, 3.5, 0, Math.PI * 2);
          ctx.fillStyle = nodo.color;
          ctx.fill();
          ctx.beginPath();
          ctx.arc(proy.px, proy.py, 7.5 + Math.sin(tiempoMs * 0.004 + i) * 2, 0, Math.PI * 2);
          ctx.strokeStyle = nodo.color;
          ctx.lineWidth = 1;
          ctx.globalAlpha = visibilidad * 0.48;
          ctx.stroke();
          ctx.restore();

          // Burbuja de texto del país
          const flotarY = Math.sin(tiempoMs * 0.0026 + i * 1.5) * 4.5;
          const bx = proy.px;
          const by = proy.py - 26 + flotarY;
          const textoBurbuja = `${nodo.pais} · ${nodo.etiquetaApi}`;

          ctx.save();
          ctx.globalAlpha = visibilidad * 0.94;
          ctx.font = '500 10.5px "IBM Plex Sans", monospace';
          const anchoTexto = ctx.measureText(textoBurbuja).width;
          const padX = 10;
          const wCaja = anchoTexto + padX * 2 + 10;
          const hCaja = 22;
          const xCaja = bx - wCaja / 2;
          const yCaja = by - hCaja / 2;

          ctx.beginPath();
          ctx.moveTo(proy.px, proy.py - 4);
          ctx.lineTo(bx, yCaja + hCaja);
          ctx.strokeStyle = 'rgba(200, 220, 255, 0.45)';
          ctx.lineWidth = 1;
          ctx.stroke();

          ctx.beginPath();
          ctx.roundRect(xCaja, yCaja, wCaja, hCaja, 11);
          ctx.fillStyle = 'rgba(9, 14, 26, 0.90)';
          ctx.fill();
          ctx.strokeStyle = 'rgba(200, 220, 255, 0.38)';
          ctx.lineWidth = 1;
          ctx.stroke();

          ctx.beginPath();
          ctx.arc(xCaja + 10, by, 2.8, 0, Math.PI * 2);
          ctx.fillStyle = nodo.color;
          ctx.fill();

          ctx.fillStyle = '#E8EDF7';
          ctx.textBaseline = 'middle';
          ctx.fillText(textoBurbuja, xCaja + 18, by + 0.5);
          ctx.restore();
        }
      }

      // 5. Desvanecido negro suave en la parte inferior del planeta
      const gradInferior = ctx.createLinearGradient(0, H - 175, 0, H);
      gradInferior.addColorStop(0, 'rgba(6, 9, 16, 0)');
      gradInferior.addColorStop(1, 'rgba(6, 9, 16, 1)');
      ctx.fillStyle = gradInferior;
      ctx.fillRect(0, H - 175, W, 175);

      rotacion += 0.0022;
      animId = window.requestAnimationFrame(dibujarFrame);
    };

    animId = window.requestAnimationFrame(dibujarFrame);
    return () => {
      if (animId) window.cancelAnimationFrame(animId);
    };
  }, []);

  useEffect(() => {
    const raiz = contenedorRef.current;
    if (!raiz) return;

    let rafId = 0;
    const actualizarScroll = () => {
      rafId = 0;
      const y = window.scrollY || 0;
      const progresoHeroe = Math.min(1, Math.max(0, y / 680));
      raiz.style.setProperty('--scroll-y', `${y}px`);
      raiz.style.setProperty('--heroe-progreso', progresoHeroe.toFixed(4));
    };

    const alHacerScroll = () => {
      if (rafId === 0) {
        rafId = window.requestAnimationFrame(actualizarScroll);
      }
    };

    actualizarScroll();
    window.addEventListener('scroll', alHacerScroll, { passive: true });

    if (typeof IntersectionObserver === 'undefined') {
      raiz
        .querySelectorAll('.shapi-scroll-item, .shapi-foco-contenedor')
        .forEach(el => el.classList.add('es-visible'));
      return () => {
        window.removeEventListener('scroll', alHacerScroll);
        if (rafId) window.cancelAnimationFrame(rafId);
      };
    }

    const observador = new IntersectionObserver(
      entradas => {
        for (const entrada of entradas) {
          if (entrada.isIntersecting) {
            entrada.target.classList.add('es-visible');
          } else {
            const rect = entrada.boundingClientRect;
            if (rect.top > window.innerHeight * 0.85) {
              entrada.target.classList.remove('es-visible');
            }
          }
        }
      },
      {
        threshold: 0.12,
        rootMargin: '0px 0px -6% 0px',
      },
    );

    const elementos = raiz.querySelectorAll('.shapi-scroll-item, .shapi-foco-contenedor');
    elementos.forEach(el => observador.observe(el));

    return () => {
      observador.disconnect();
      window.removeEventListener('scroll', alHacerScroll);
      if (rafId) window.cancelAnimationFrame(rafId);
    };
  }, [visibles.length, consulta.isPending]);

  return (
    <div
      ref={contenedorRef}
      className="shapi-inicio dark relative min-h-screen bg-fondo text-tinta overflow-x-clip"
    >
      <style>{`
        html:has(.shapi-inicio),
        body:has(.shapi-inicio) {
          background-color: #060910;
          color-scheme: dark;
        }
        .shapi-inicio {
          --scroll-y: 0px;
          --heroe-progreso: 0;
          --shadow-lg: rgba(255, 255, 255, 0.04) 0px 0px 24px 0px inset;
          --shadow-lg-2: rgba(255, 255, 255, 0.06) 0px 0px 24px 0px inset;
          --shadow-md: rgba(127, 166, 255, 0.18) 0px -7px 11px 0px inset;
          --shadow-subtle-4: rgba(255, 255, 255, 0.03) 0px 0px 0px 8px inset;
        }
        .shapi-insignia {
          display: inline-flex;
          align-items: center;
          gap: 8px;
          padding: 6px 16px;
          border-radius: 32px;
          background: linear-gradient(180deg, rgba(16, 18, 38, 0.92) 0%, rgba(10, 14, 28, 0.94) 100%);
          backdrop-filter: blur(8px);
          border: 1px solid rgba(167, 139, 250, 0.35);
          box-shadow: var(--shadow-lg), 0 0 16px -2px rgba(139, 92, 246, 0.18), var(--shadow-md);
        }
        .shapi-btn-primario {
          display: inline-flex;
          align-items: center;
          justify-content: center;
          border-radius: 10px;
          background: linear-gradient(180deg, rgba(68, 102, 242, 0.44) 0%, rgba(98, 78, 222, 0.40) 50%, rgba(24, 34, 88, 0.78) 100%);
          border: 1px solid rgba(167, 139, 250, 0.45);
          color: #E8EDF7;
          box-shadow:
            rgba(255, 255, 255, 0.16) 0px 1px 0px 0px inset,
            rgba(139, 92, 246, 0.28) 0px -8px 16px 0px inset,
            0 8px 24px -6px rgba(124, 58, 237, 0.42);
          transition: transform 0.25s cubic-bezier(0.22, 1, 0.36, 1), border-color 0.25s ease, box-shadow 0.25s ease, background 0.25s ease;
        }
        .shapi-btn-primario:hover {
          transform: translateY(-2px);
          border-color: #C4B5FD;
          background: linear-gradient(180deg, rgba(88, 118, 248, 0.58) 0%, rgba(124, 98, 244, 0.52) 50%, rgba(30, 42, 108, 0.88) 100%);
          box-shadow:
            rgba(255, 255, 255, 0.24) 0px 1px 0px 0px inset,
            rgba(167, 139, 250, 0.42) 0px -10px 20px 0px inset,
            0 14px 34px -6px rgba(124, 58, 237, 0.58);
        }
        .shapi-btn-secundario {
          display: inline-flex;
          align-items: center;
          justify-content: center;
          border-radius: 10px;
          background: rgba(14, 18, 36, 0.76);
          backdrop-filter: blur(10px);
          border: 1px solid rgba(167, 139, 250, 0.22);
          color: #E8EDF7;
          box-shadow: var(--shadow-lg), rgba(255, 255, 255, 0.06) 0px 1px 0px 0px inset;
          transition: transform 0.25s cubic-bezier(0.22, 1, 0.36, 1), border-color 0.25s ease, box-shadow 0.25s ease, background-color 0.25s ease;
        }
        .shapi-btn-secundario:hover {
          transform: translateY(-2px);
          border-color: rgba(196, 181, 253, 0.5);
          background-color: rgba(22, 26, 56, 0.88);
          box-shadow:
            var(--shadow-lg-2),
            rgba(139, 92, 246, 0.26) 0px -6px 14px 0px inset,
            0 10px 26px -6px rgba(124, 58, 237, 0.32);
        }
        .shapi-aurora {
          height: 1px;
          width: 100%;
          max-width: 860px;
          background: linear-gradient(90deg, rgba(127, 166, 255, 0) 0%, rgba(59, 111, 240, 0.6) 24%, #A78BFA 50%, rgba(59, 111, 240, 0.6) 76%, rgba(127, 166, 255, 0) 100%);
          box-shadow: 0 0 16px 1px rgba(167, 139, 250, 0.35);
        }
        .shapi-tarjeta-cristal {
          position: relative;
          overflow: hidden;
          background: linear-gradient(180deg, rgba(16, 22, 42, 0.78) 0%, rgba(9, 12, 24, 0.94) 100%);
          backdrop-filter: blur(12px);
          box-shadow: var(--shadow-lg-2), rgba(167, 139, 250, 0.10) 0px 1px 0px 0px inset;
        }
        .shapi-tarjeta-destacada-cristal {
          position: relative;
          overflow: hidden;
          background: linear-gradient(180deg, rgba(30, 36, 88, 0.88) 0%, rgba(13, 18, 48, 0.96) 100%);
          backdrop-filter: blur(12px);
          box-shadow:
            var(--shadow-lg-2),
            rgba(139, 92, 246, 0.35) 0px -10px 22px 0px inset,
            var(--shadow-subtle-4),
            0 16px 42px -12px rgba(124, 58, 237, 0.42);
        }
        .shapi-titulo-gradiente {
          background: linear-gradient(180deg, #FFFFFF 20%, #DDD6FE 65%, #A78BFA 100%);
          -webkit-background-clip: text;
          background-clip: text;
          color: transparent;
        }
        .shapi-estrellas-1,
        .shapi-estrellas-2,
        .shapi-estrellas-3 {
          position: absolute;
          top: 0;
          left: 0;
          border-radius: 999px;
          background: transparent;
        }
        .shapi-estrellas-1 {
          width: 1px;
          height: 1px;
          opacity: 0.42;
          box-shadow: ${ESTRELLAS_CAPA_1};
        }
        .shapi-estrellas-2 {
          width: 1.5px;
          height: 1.5px;
          opacity: 0.62;
          box-shadow: ${ESTRELLAS_CAPA_2};
        }
        .shapi-estrellas-3 {
          width: 2px;
          height: 2px;
          opacity: 0.8;
          box-shadow: ${ESTRELLAS_CAPA_3};
        }
        @media (prefers-reduced-motion: no-preference) {
          @keyframes shapi-entrada {
            from {
              opacity: 0;
              transform: translateY(20px);
            }
            to {
              opacity: 1;
              transform: translateY(0);
            }
          }
          @keyframes shapi-flotar {
            0%, 100% {
              transform: translateY(0);
            }
            50% {
              transform: translateY(-7px);
            }
          }
          @keyframes shapi-flotar-inverso {
            0%, 100% {
              transform: translateY(0);
            }
            50% {
              transform: translateY(7px);
            }
          }
          @keyframes shapi-orbita {
            from {
              stroke-dashoffset: 72;
            }
            to {
              stroke-dashoffset: 0;
            }
          }
          @keyframes shapi-orbita-inversa {
            from {
              stroke-dashoffset: 0;
            }
            to {
              stroke-dashoffset: 72;
            }
          }
          @keyframes shapi-radar-barrido {
            from {
              transform: rotate(-75deg);
            }
            50% {
              transform: rotate(75deg);
            }
            to {
              transform: rotate(-75deg);
            }
          }
          @keyframes shapi-lupa-recorrido {
            0%, 100% {
              transform: translate3d(-24px, -10px, 0);
            }
            50% {
              transform: translate3d(28px, 12px, 0);
            }
          }
          @keyframes shapi-nodo-pulso {
            0%, 100% {
              transform: scale(0.92);
              opacity: 0.72;
            }
            50% {
              transform: scale(1.12);
              opacity: 1;
            }
          }
          @keyframes shapi-pulso {
            0%, 100% {
              opacity: 0.52;
            }
            50% {
              opacity: 1;
            }
          }
          @keyframes shapi-flujo-paquete {
            from {
              stroke-dashoffset: 120;
            }
            to {
              stroke-dashoffset: 0;
            }
          }
          @keyframes shapi-deriva-estrellas {
            0%, 100% {
              transform: translate3d(0, 0, 0);
            }
            50% {
              transform: translate3d(-24px, -36px, 0);
            }
          }
          @keyframes shapi-deriva-estrellas-2 {
            0%, 100% {
              transform: translate3d(0, 0, 0);
            }
            50% {
              transform: translate3d(20px, -42px, 0);
            }
          }
          @keyframes shapi-matriz-desplazar {
            0%, 100% {
              transform: translateX(-8px);
            }
            50% {
              transform: translateX(8px);
            }
          }

          /* Animación del FOCO ("Cómo funciona") abriéndose y prendiéndose como párpados con desvanecido */
          @keyframes shapi-foco-parpados {
            0% {
              opacity: 0;
              transform: scaleX(0.03) scaleY(0.15);
            }
            16% {
              opacity: 0.85;
              transform: scaleX(0.42) scaleY(0.88);
            }
            28% {
              opacity: 0.18;
              transform: scaleX(0.10) scaleY(0.45);
            }
            56% {
              opacity: 1;
              transform: scaleX(1.04) scaleY(1);
            }
            78% {
              opacity: 0.88;
              transform: scaleX(0.97) scaleY(1);
            }
            100% {
              opacity: 1;
              transform: scaleX(1) scaleY(1);
            }
          }
          @keyframes shapi-foco-respirar {
            0%, 100% {
              opacity: 0.82;
              transform: scaleX(0.95) scaleY(0.98);
            }
            45% {
              opacity: 1;
              transform: scaleX(1.05) scaleY(1.02);
            }
            50% {
              /* Parpadeo sutil periódico */
              opacity: 0.48;
              transform: scaleX(0.76) scaleY(0.92);
            }
            56% {
              opacity: 1;
              transform: scaleX(1.04) scaleY(1.02);
            }
          }
          @keyframes shapi-foco-bombilla {
            0% {
              opacity: 0;
              transform: scale(0.2);
            }
            20% {
              opacity: 1;
              transform: scale(1.35);
            }
            30% {
              opacity: 0.35;
              transform: scale(0.6);
            }
            55%, 100% {
              opacity: 1;
              transform: scale(1);
            }
          }

          .shapi-foco-contenedor .shapi-foco-haz {
            opacity: 0;
            transform-origin: 450px 0px;
            transform: scaleX(0.03) scaleY(0.15);
          }
          .shapi-foco-contenedor .shapi-foco-origen {
            opacity: 0;
            transform-origin: 450px 4px;
            transform: scale(0.2);
          }
          .shapi-foco-contenedor.es-visible .shapi-foco-haz {
            animation:
              shapi-foco-parpados 1.65s cubic-bezier(0.16, 1, 0.3, 1) forwards,
              shapi-foco-respirar 6.5s ease-in-out 1.65s infinite;
          }
          .shapi-foco-contenedor.es-visible .shapi-foco-origen {
            animation: shapi-foco-bombilla 1.4s cubic-bezier(0.16, 1, 0.3, 1) forwards;
          }

          /* Animaciones al hacer scroll (estilo GSAP ScrollTrigger) */
          .shapi-scroll-item {
            opacity: 0;
            transform: translate3d(0, 44px, 0) scale(0.96);
            transition:
              opacity 0.85s cubic-bezier(0.16, 1, 0.3, 1),
              transform 0.85s cubic-bezier(0.16, 1, 0.3, 1);
            transition-delay: var(--retraso, 0ms);
            will-change: opacity, transform;
          }
          .shapi-scroll-item.shapi-scroll-izq {
            transform: translate3d(-42px, 32px, 0) scale(0.96);
          }
          .shapi-scroll-item.shapi-scroll-der {
            transform: translate3d(42px, 32px, 0) scale(0.96);
          }
          .shapi-scroll-item.shapi-scroll-zoom {
            transform: translate3d(0, 52px, 0) scale(0.9);
          }
          .shapi-scroll-item.es-visible {
            opacity: 1;
            transform: translate3d(0, 0, 0) scale(1);
          }

          .shapi-estrellas-1 {
            animation: shapi-deriva-estrellas 26s ease-in-out infinite;
          }
          .shapi-estrellas-2 {
            animation: shapi-deriva-estrellas-2 32s ease-in-out infinite, shapi-pulso 5s ease-in-out infinite;
          }
          .shapi-estrellas-3 {
            animation: shapi-deriva-estrellas 38s ease-in-out infinite, shapi-pulso 4s ease-in-out 1.2s infinite;
          }
          .shapi-anim-heroe-1 {
            animation: shapi-entrada 0.65s cubic-bezier(0.22, 1, 0.36, 1) both;
          }
          .shapi-anim-heroe-2 {
            animation: shapi-entrada 0.75s cubic-bezier(0.22, 1, 0.36, 1) 0.08s both;
          }
          .shapi-anim-heroe-3 {
            animation: shapi-entrada 0.75s cubic-bezier(0.22, 1, 0.36, 1) 0.16s both;
          }
          .shapi-anim-heroe-4 {
            animation: shapi-entrada 0.75s cubic-bezier(0.22, 1, 0.36, 1) 0.24s both;
          }
          .shapi-anim-flotar {
            animation: shapi-flotar 6s ease-in-out infinite;
          }
          .shapi-anim-flotar-inverso {
            animation: shapi-flotar-inverso 6.8s ease-in-out infinite;
          }
          .shapi-anim-orbita {
            animation: shapi-orbita 3.4s linear infinite;
          }
          .shapi-anim-orbita-inversa {
            animation: shapi-orbita-inversa 4.4s linear infinite;
          }
          .shapi-anim-radar {
            transform-origin: 400px 240px;
            animation: shapi-radar-barrido 7s ease-in-out infinite;
          }
          .shapi-anim-lupa {
            animation: shapi-lupa-recorrido 6s ease-in-out infinite;
          }
          .shapi-anim-nodo-1 {
            transform-box: fill-box;
            transform-origin: center;
            animation: shapi-nodo-pulso 3.2s ease-in-out infinite;
          }
          .shapi-anim-nodo-2 {
            transform-box: fill-box;
            transform-origin: center;
            animation: shapi-nodo-pulso 3.8s ease-in-out 1.1s infinite;
          }
          .shapi-anim-nodo-3 {
            transform-box: fill-box;
            transform-origin: center;
            animation: shapi-nodo-pulso 4.2s ease-in-out 2s infinite;
          }
          .shapi-anim-flujo {
            animation: shapi-flujo-paquete 3s linear infinite;
          }
          .shapi-anim-pulso {
            animation: shapi-pulso 3.8s ease-in-out infinite;
          }
          .shapi-anim-matriz {
            animation: shapi-matriz-desplazar 8s ease-in-out infinite;
          }
          .shapi-tarjeta {
            transition: transform 0.3s cubic-bezier(0.22, 1, 0.36, 1), border-color 0.3s ease, box-shadow 0.3s ease;
          }
          .shapi-tarjeta:hover {
            transform: translateY(-5px) !important;
            border-color: rgba(167, 139, 250, 0.75) !important;
            box-shadow:
              rgba(255, 255, 255, 0.12) 0px 0px 30px 0px inset,
              rgba(139, 92, 246, 0.30) 0px -10px 22px 0px inset,
              var(--shadow-subtle-4),
              0 22px 48px -12px rgba(124, 58, 237, 0.40) !important;
          }
          .shapi-tarjeta-destacada {
            transition: transform 0.3s cubic-bezier(0.22, 1, 0.36, 1), border-color 0.3s ease, box-shadow 0.3s ease;
          }
          .shapi-tarjeta-destacada:hover {
            transform: translateY(-7px) scale(1.025) !important;
            border-color: #DDD6FE !important;
            box-shadow:
              rgba(255, 255, 255, 0.18) 0px 0px 36px 0px inset,
              rgba(167, 139, 250, 0.55) 0px -14px 28px 0px inset,
              var(--shadow-subtle-4),
              0 26px 56px -8px rgba(124, 58, 237, 0.65),
              0 0 35px rgba(196, 181, 253, 0.55) !important;
          }

          /* Animación de encendido secuencial de cada plan y fijado en Producto (Q 599 - Recomendado) */
          @keyframes shapi-plan-secuencia-flash {
            0% {
              border-color: rgba(255, 255, 255, 0.08);
              box-shadow: var(--shadow-lg-2);
            }
            35% {
              border-color: rgba(167, 139, 250, 0.95);
              box-shadow:
                var(--shadow-lg-2),
                0 0 32px 2px rgba(139, 92, 246, 0.60),
                inset 0 0 24px rgba(167, 139, 250, 0.30),
                0 10px 28px rgba(59, 111, 240, 0.40);
              transform: translateY(-3px) scale(1.01);
            }
            70% {
              border-color: rgba(127, 166, 255, 0.55);
              box-shadow:
                var(--shadow-lg-2),
                0 0 16px rgba(59, 111, 240, 0.25);
              transform: translateY(-1px);
            }
            100% {
              border-color: rgba(255, 255, 255, 0.08);
              box-shadow: var(--shadow-lg-2);
              transform: translateY(0);
            }
          }

          @keyframes shapi-plan-destacado-fijar {
            0% {
              border-color: rgba(255, 255, 255, 0.08);
              box-shadow: var(--shadow-lg-2);
              transform: translateY(0);
            }
            30% {
              border-color: rgba(167, 139, 250, 0.65);
              box-shadow:
                0 0 28px rgba(139, 92, 246, 0.45),
                inset 0 0 22px rgba(167, 139, 250, 0.25);
              transform: translateY(-3px);
            }
            65% {
              border-color: #FFFFFF;
              box-shadow:
                0 0 55px 3px rgba(167, 139, 250, 0.95),
                inset 0 0 38px rgba(196, 181, 253, 0.60),
                0 22px 52px -6px rgba(124, 58, 237, 0.70);
              transform: translateY(-6px) scale(1.025);
            }
            100% {
              border-color: #A78BFA;
              box-shadow:
                var(--shadow-lg-2),
                rgba(139, 92, 246, 0.45) 0px -12px 26px 0px inset,
                var(--shadow-subtle-4),
                0 20px 48px -10px rgba(124, 58, 237, 0.52),
                0 0 28px -4px rgba(167, 139, 250, 0.45);
              transform: translateY(-4px) scale(1.015);
            }
          }

          @keyframes shapi-plan-destacado-aura-viva {
            0% {
              border-color: #A78BFA;
              box-shadow:
                var(--shadow-lg-2),
                rgba(139, 92, 246, 0.40) 0px -12px 26px 0px inset,
                var(--shadow-subtle-4),
                0 18px 44px -10px rgba(124, 58, 237, 0.48),
                0 0 24px -4px rgba(167, 139, 250, 0.40);
              transform: translateY(-4px) scale(1.015);
            }
            100% {
              border-color: #C4B5FD;
              box-shadow:
                var(--shadow-lg-2),
                rgba(167, 139, 250, 0.55) 0px -14px 30px 0px inset,
                var(--shadow-subtle-4),
                0 24px 54px -8px rgba(124, 58, 237, 0.65),
                0 0 38px -2px rgba(196, 181, 253, 0.60);
              transform: translateY(-6px) scale(1.02);
            }
          }

          @keyframes shapi-badge-destacado-anim {
            0% {
              opacity: 0;
              transform: translateY(12px) scale(0.92);
            }
            65% {
              opacity: 0.6;
              transform: translateY(-2px) scale(1.04);
            }
            100% {
              opacity: 1;
              transform: translateY(0) scale(1);
            }
          }

          .es-visible.shapi-plan-0 {
            animation: shapi-plan-secuencia-flash 0.65s cubic-bezier(0.22, 1, 0.36, 1) 0.30s both;
          }
          .es-visible.shapi-plan-1 {
            animation: shapi-plan-secuencia-flash 0.65s cubic-bezier(0.22, 1, 0.36, 1) 0.75s both;
          }
          .es-visible.shapi-plan-3 {
            animation: shapi-plan-secuencia-flash 0.65s cubic-bezier(0.22, 1, 0.36, 1) 1.20s both;
          }
          .es-visible.shapi-plan-2 {
            animation:
              shapi-plan-destacado-fijar 1.1s cubic-bezier(0.22, 1, 0.36, 1) 1.65s both,
              shapi-plan-destacado-aura-viva 3.8s ease-in-out 2.75s infinite alternate;
          }
          .es-visible.shapi-plan-2 .shapi-badge-recomendado {
            animation: shapi-badge-destacado-anim 0.85s cubic-bezier(0.16, 1, 0.3, 1) 1.95s both;
          }

          @media (prefers-reduced-motion: reduce) {
            .shapi-plan-0,
            .shapi-plan-1,
            .shapi-plan-2,
            .shapi-plan-3,
            .shapi-badge-recomendado {
              animation: none !important;
            }
          }

          /* Hoyo negro animado (horizonte de sucesos y disco de acreción en azul con morado) */
          @keyframes shapi-hoyo-pulso-nucleo {
            0%, 100% {
              transform: scaleX(0.97) scaleY(0.95);
              opacity: 0.88;
            }
            50% {
              transform: scaleX(1.04) scaleY(1.05);
              opacity: 1;
            }
          }
          @keyframes shapi-hoyo-ondas {
            0%, 100% {
              transform: scaleX(0.95) scaleY(0.93);
              opacity: 0.72;
            }
            50% {
              transform: scaleX(1.05) scaleY(1.04);
              opacity: 1;
            }
          }
          @keyframes shapi-hoyo-acrecion {
            0% {
              stroke-dashoffset: 360;
            }
            100% {
              stroke-dashoffset: 0;
            }
          }
          @keyframes shapi-hoyo-acrecion-inv {
            0% {
              stroke-dashoffset: 0;
            }
            100% {
              stroke-dashoffset: 360;
            }
          }
          @keyframes shapi-hoyo-particula {
            0% {
              transform: translate3d(0, 0, 0) scale(1);
              opacity: 0;
            }
            25% {
              opacity: 0.95;
            }
            85% {
              opacity: 0.85;
            }
            100% {
              transform: translate3d(var(--dx, 0px), var(--dy, 48px), 0) scale(0.25);
              opacity: 0;
            }
          }
          .shapi-hoyo-nucleo {
            transform-origin: 600px 340px;
            animation: shapi-hoyo-pulso-nucleo 4.8s ease-in-out infinite;
          }
          .shapi-hoyo-onda-1 {
            transform-origin: 600px 340px;
            animation: shapi-hoyo-ondas 5.6s ease-in-out infinite;
          }
          .shapi-hoyo-onda-2 {
            transform-origin: 600px 340px;
            animation: shapi-hoyo-ondas 6.8s ease-in-out 1.4s infinite;
          }
          .shapi-hoyo-flujo-1 {
            animation: shapi-hoyo-acrecion 6.5s linear infinite;
          }
          .shapi-hoyo-flujo-2 {
            animation: shapi-hoyo-acrecion-inv 8.2s linear infinite;
          }
          .shapi-hoyo-chispa {
            animation: shapi-hoyo-particula var(--dur, 4.5s) ease-in var(--del, 0s) infinite;
          }
        }
      `}</style>

      {/* Fondo cósmico fijo con las 3 capas de estrellas del CSS */}
      <div aria-hidden="true" className="pointer-events-none fixed inset-0 overflow-hidden">
        <div className="shapi-estrellas-1" />
        <div className="shapi-estrellas-2" />
        <div className="shapi-estrellas-3" />
      </div>

      <div className="relative z-10 mx-auto max-w-[1440px]">
        {/* Encabezado y Héroe: Planeta Tierra de puntos en anillos + estela blanca + las 2 APIs en el fondo de las letras de Shapi */}
        <div className="relative overflow-hidden">
          {/* Rejilla superior suave en el fondo */}
          <div
            aria-hidden="true"
            className="pointer-events-none absolute inset-0"
            style={{
              backgroundImage:
                'linear-gradient(to right, rgba(18, 27, 46, 0.55) 1px, transparent 1px), linear-gradient(to bottom, rgba(18, 27, 46, 0.55) 1px, transparent 1px)',
              backgroundSize: '64px 64px',
              WebkitMaskImage: 'radial-gradient(ellipse 84% 72% at 50% 34%, #000 28%, transparent 95%)',
              maskImage: 'radial-gradient(ellipse 84% 72% at 50% 34%, #000 28%, transparent 95%)',
            }}
          />

          {/* Resplandor ambiental en el fondo de las letras de Shapi */}
          <div
            aria-hidden="true"
            className="shapi-anim-pulso pointer-events-none absolute inset-0"
            style={{
              background:
                'radial-gradient(ellipse 62% 46% at 50% 18%, rgba(99, 102, 241, 0.24), transparent 70%), radial-gradient(ellipse 52% 40% at 50% 62%, rgba(139, 92, 246, 0.16), transparent 72%)',
            }}
          />

          {/* CAPA DE FONDO DEL HÉROE: Rejilla en perspectiva + Tarjetas de las 2 APIs + Órbitas + Planeta Tierra de puntos con estela blanca */}
          <div aria-hidden="true" className="pointer-events-none absolute inset-0 overflow-hidden">
            <svg
              viewBox="0 0 1440 880"
              className="absolute inset-0 h-full w-full"
              preserveAspectRatio="xMidYMid slice"
            >
              <defs>
                <linearGradient id="v4RejillaFondo" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#1B2A47" stopOpacity="0.12" />
                  <stop offset="50%" stopColor="#263B63" stopOpacity="0.58" />
                  <stop offset="100%" stopColor="#121B2E" stopOpacity="0" />
                </linearGradient>
              </defs>

              {/* Rejilla en perspectiva al fondo */}
              <g stroke="url(#v4RejillaFondo)" strokeWidth="1">
                <path d="M0 800 H1440" />
                <path d="M0 748 H1440" />
                <path d="M0 702 H1440" />
                <path d="M0 662 H1440" />
                <path d="M0 628 H1440" />
                <path d="M0 600 H1440" />
                <path d="M720 520 L60 880" />
                <path d="M720 520 L250 880" />
                <path d="M720 520 L440 880" />
                <path d="M720 520 L585 880" />
                <path d="M720 520 L720 880" />
                <path d="M720 520 L855 880" />
                <path d="M720 520 L1000 880" />
                <path d="M720 520 L1190 880" />
                <path d="M720 520 L1380 880" />
              </g>

              {/* Anillos orbitales concéntricos rodeando el Planeta Tierra y conectando con las 2 APIs */}
              <g stroke="#243656" strokeWidth="1" fill="none" opacity="0.78">
                <ellipse cx="720" cy="560" rx="450" ry="126" />
                <ellipse
                  cx="720"
                  cy="560"
                  rx="450"
                  ry="126"
                  stroke="#3B6FF0"
                  strokeWidth="1.5"
                  strokeDasharray="10 28"
                  className="shapi-anim-orbita"
                />
                <ellipse cx="720" cy="560" rx="350" ry="96" />
                <ellipse
                  cx="720"
                  cy="560"
                  rx="350"
                  ry="96"
                  stroke="#7FA6FF"
                  strokeWidth="1.2"
                  strokeDasharray="7 30"
                  className="shapi-anim-orbita-inversa"
                />
              </g>

              {/* Líneas punteadas desde las 2 tarjetas de APIs hacia el anillo orbital */}
              <g stroke="#2A3F68" strokeWidth="1.2" strokeDasharray="4 6" fill="none">
                <path d="M205 426 L242 502 L270 560" className="shapi-anim-flujo" />
                <path d="M265 392 L318 450 L370 560" />
                <path d="M1235 426 L1198 502 L1170 560" className="shapi-anim-flujo" />
                <path d="M1175 392 L1122 450 L1070 560" />
              </g>

              {/* Tarjeta flotante izquierda (API Consumidores / Peticiones) */}
              <g className="shapi-anim-flotar">
                <rect x="96" y="346" width="196" height="82" rx="10" fill="#0A101E" fillOpacity="0.9" stroke="#263554" />
                <text x="114" y="370" fill="#7FA6FF" fontSize="10.5" fontWeight="600" fontFamily="monospace">
                  API Consumidores
                </text>
                <path d="M114 386 H244" stroke="#2C3B5C" strokeWidth="1.5" strokeLinecap="round" />
                <text x="114" y="406" fill="#8FA2C7" fontSize="9.5" fontFamily="monospace">
                  X-API-Key · Cuota activa
                </text>
                <circle cx="272" cy="367" r="4" fill="#3FBF88" className="shapi-anim-pulso" />
              </g>

              {/* Tarjeta flotante derecha (API Servidor de Origen / Proveedor) */}
              <g className="shapi-anim-flotar-inverso">
                <rect x="1148" y="346" width="196" height="82" rx="10" fill="#0A101E" fillOpacity="0.9" stroke="#263554" />
                <text x="1166" y="370" fill="#7FA6FF" fontSize="10.5" fontWeight="600" fontFamily="monospace">
                  API Servidor Origen
                </text>
                <path d="M1166 386 H1296" stroke="#2C3B5C" strokeWidth="1.5" strokeLinecap="round" />
                <text x="1166" y="406" fill="#8FA2C7" fontSize="9.5" fontFamily="monospace">
                  Enrutamiento · Cobro GTQ
                </text>
                <circle cx="1324" cy="367" r="4" fill="#3B6FF0" className="shapi-anim-pulso" />
              </g>

              {/* Nodos laterales en los extremos del anillo orbital */}
              <circle cx="270" cy="560" r="5" fill="#3B6FF0" className="shapi-anim-pulso" />
              <circle cx="1170" cy="560" r="5" fill="#3B6FF0" className="shapi-anim-pulso" />
            </svg>

            {/* Canvas del Planeta Tierra en puntos con la estela blanca exterior y burbujas de países */}
            <canvas
              ref={canvasPlanetaRef}
              className="absolute inset-0 h-full w-full object-cover"
            />
          </div>

          {/* Barra superior integrada de extremo a extremo */}
          <div className="relative z-20 flex h-[76px] items-center justify-between border-b border-borde-barra/70 px-8 lg:px-20">
            <div className="flex items-center gap-[11px]">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <rect x="1" y="1" width="22" height="22" rx="6" stroke="var(--principal)" strokeWidth="1.5" />
                <path d="M6 8.5 H14" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
                <path d="M6 15.5 H12" stroke="var(--principal)" strokeWidth="1.5" strokeLinecap="round" />
                <path d="M6 12 H22" stroke="#3B6FF0" strokeWidth="2.5" strokeLinecap="round" />
              </svg>
              <span className="font-display text-[20px] font-medium tracking-[-0.02em] text-tinta">Shapi</span>
            </div>
            <div className="flex items-center gap-[28px]">
              <a
                href="#planes"
                className="text-[14.5px] text-tinta-navegacion transition-colors hover:text-tinta hover:no-underline"
              >
                Precios
              </a>
              <Link
                to="/entrar"
                className="text-[14.5px] text-tinta-navegacion transition-colors hover:text-tinta hover:no-underline"
              >
                Entrar
              </Link>
              <Link
                to="/registro"
                className="shapi-btn-primario px-[18px] py-[9px] text-[14.5px] font-medium hover:no-underline"
              >
                Crear cuenta
              </Link>
            </div>
          </div>

          {/* Letras de Shapi y botones sobre el fondo del planeta Tierra giratorio */}
          <div className="relative z-20 flex flex-col items-center px-8 pt-[78px] pb-[410px] text-center lg:px-20">
            <p className="shapi-anim-heroe-1 text-etiqueta font-medium uppercase tracking-[0.16em] text-tinta-rotulo">
              Plataforma como servicio
            </p>

            <h1
              className="shapi-anim-heroe-2 mt-[22px] font-display text-[124px] font-light leading-none tracking-[-0.045em] text-transparent bg-clip-text"
              style={{
                backgroundImage: 'linear-gradient(180deg, #FFFFFF 20%, #DDD6FE 65%, #A78BFA 100%)',
              }}
            >
              Shapi
            </h1>

            <p className="shapi-anim-heroe-3 mt-[26px] max-w-[52ch] text-[20px] leading-[1.55] text-[#E2E8F0] [text-shadow:0_2px_10px_rgba(6,9,16,0.95),0_0_2px_rgba(6,9,16,0.9)]">
              Publique su API, controle quién la usa y cobre por ella. Sin modificar el servidor que ya tiene.
            </p>

            <div className="shapi-anim-heroe-4 relative z-20 mt-[34px] flex items-center gap-[14px]">
              <Link
                to="/registro"
                className="shapi-btn-primario px-[26px] py-[12px] text-[15px] font-medium hover:no-underline"
              >
                Crear cuenta
              </Link>
              <a
                href="#planes"
                className="shapi-btn-secundario px-[25px] py-[12px] text-[15px] font-medium hover:no-underline"
              >
                Ver precios
              </a>
            </div>
          </div>
        </div>

        {/* Divisor Aurora */}
        <div className="flex justify-center px-8">
          <div aria-hidden="true" className="shapi-aurora" />
        </div>

        {/* Sección: Cómo funciona — Con el foco que se enciende y abre como párpados con desvanecido suave */}
        <section className="shapi-foco-contenedor relative flex flex-col items-center px-8 pt-28 text-center lg:px-20">
          {/* Foco de luz cónico con apertura tipo párpado + desvanecido radial suave */}
          <svg
            aria-hidden="true"
            viewBox="0 0 900 360"
            className="pointer-events-none absolute top-0 left-1/2 -z-10 h-[360px] w-full max-w-[900px] -translate-x-1/2 overflow-visible"
          >
            <defs>
              {/* Desvanecido vertical del haz */}
              <linearGradient id="conoLuzGrad" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#FFFFFF" stopOpacity="0.95" />
                <stop offset="14%" stopColor="#DDD6FE" stopOpacity="0.52" />
                <stop offset="38%" stopColor="#8B5CF6" stopOpacity="0.22" />
                <stop offset="68%" stopColor="#3B6FF0" stopOpacity="0.10" />
                <stop offset="100%" stopColor="#060910" stopOpacity="0" />
              </linearGradient>
              {/* Desvanecido horizontal para que los bordes laterales del foco se fundan como párpados suaves */}
              <radialGradient id="conoMascaraSuave" cx="50%" cy="0%" r="85%">
                <stop offset="0%" stopColor="#FFFFFF" stopOpacity="1" />
                <stop offset="55%" stopColor="#FFFFFF" stopOpacity="0.7" />
                <stop offset="100%" stopColor="#FFFFFF" stopOpacity="0" />
              </radialGradient>
              <filter id="focoDesenfoque" x="-20%" y="-20%" width="140%" height="140%">
                <feGaussianBlur stdDeviation="14" />
              </filter>
            </defs>

            {/* Grupo del haz que hace la animación de encendido/párpados */}
            <g className="shapi-foco-haz">
              {/* Halo exterior difuminado para el desvanecido suave */}
              <polygon
                points="450,0 50,350 850,350"
                fill="url(#conoLuzGrad)"
                filter="url(#focoDesenfoque)"
                opacity="0.85"
              />
              {/* Cono principal del foco */}
              <polygon points="450,0 130,340 770,340" fill="url(#conoLuzGrad)" opacity="0.85" />
              {/* Núcleo central más intenso del foco */}
              <polygon points="450,0 290,310 610,310" fill="url(#conoLuzGrad)" opacity="0.55" />
              {/* Filamento central de luz */}
              <line x1="450" y1="0" x2="450" y2="145" stroke="#FFFFFF" strokeOpacity="0.65" strokeWidth="1.5" />
            </g>

            {/* Punto emisor del foco en el borde superior */}
            <g className="shapi-foco-origen">
              <ellipse cx="450" cy="2" rx="46" ry="8" fill="#C4B5FD" opacity="0.5" filter="url(#focoDesenfoque)" />
              <circle cx="450" cy="3" r="4.5" fill="#FFFFFF" />
            </g>
          </svg>

          <div className="shapi-scroll-item shapi-insignia">
            <span className="h-1.5 w-1.5 rounded-full bg-[#A78BFA] shapi-anim-pulso" aria-hidden="true" />
            <p className="text-etiqueta font-medium uppercase tracking-[0.16em] text-tinta-rotulo">Cómo funciona</p>
          </div>
          <h2
            style={{ '--retraso': '80ms' } as React.CSSProperties}
            className="shapi-scroll-item mt-[18px] max-w-[20ch] font-display text-[44px] font-light leading-[1.15] tracking-[-0.03em]"
          >
            Tres pasos para publicar y cobrar por su API.
          </h2>
          <p
            style={{ '--retraso': '150ms' } as React.CSSProperties}
            className="shapi-scroll-item mt-[18px] max-w-[60ch] text-[17px] leading-[1.6] text-tinta-suave"
          >
            Usted conecta su servidor, sube su especificación OpenAPI y define los planes. Shapi se encarga del resto.
          </p>

          <div className="mt-[52px] grid w-full grid-cols-1 gap-6 text-left md:grid-cols-3">
            {/* Paso 01: Grafo de nodos conectados estilo Reflect */}
            <div
              style={{ '--retraso': '60ms' } as React.CSSProperties}
              className="shapi-scroll-item shapi-scroll-izq shapi-tarjeta shapi-tarjeta-cristal flex flex-col gap-[18px] rounded-[16px] border border-borde bg-panel p-7"
            >
              <svg viewBox="0 0 320 160" width="100%" className="block" aria-hidden="true">
                <defs>
                  <radialGradient id="paso1NodoGlow" cx="50%" cy="50%" r="50%">
                    <stop offset="0%" stopColor="#8B5CF6" stopOpacity="0.34" />
                    <stop offset="60%" stopColor="#3B6FF0" stopOpacity="0.16" />
                    <stop offset="100%" stopColor="#3B6FF0" stopOpacity="0" />
                  </radialGradient>
                </defs>
                <circle cx="160" cy="80" r="68" fill="url(#paso1NodoGlow)" className="shapi-anim-pulso" />
                <g stroke="#293E66" strokeWidth="1.4" fill="none">
                  <path d="M68 44 C 110 44, 122 80, 160 80" />
                  <path d="M58 118 C 104 118, 122 80, 160 80" />
                  <path d="M160 80 C 198 80, 212 46, 254 46" />
                  <path d="M160 80 C 198 80, 212 118, 256 118" />
                </g>
                <g
                  stroke="#7FA6FF"
                  strokeWidth="1.8"
                  strokeDasharray="8 44"
                  strokeLinecap="round"
                  fill="none"
                  className="shapi-anim-flujo"
                >
                  <path d="M68 44 C 110 44, 122 80, 160 80" />
                  <path d="M58 118 C 104 118, 122 80, 160 80" />
                  <path d="M160 80 C 198 80, 212 46, 254 46" />
                  <path d="M160 80 C 198 80, 212 118, 256 118" />
                </g>
                <circle cx="68" cy="36" r="10" fill="#132544" stroke="#7FA6FF" strokeWidth="1.5" className="shapi-anim-nodo-1" />
                <rect x="24" y="52" width="88" height="18" rx="9" fill="#0B1220" stroke="#243554" />
                <text x="68" y="64" textAnchor="middle" fill="#B5C4E3" fontSize="8.5">
                  Cliente móvil
                </text>

                <circle cx="58" cy="110" r="10" fill="#0F292A" stroke="#3FBF88" strokeWidth="1.5" className="shapi-anim-nodo-2" />
                <rect x="16" y="126" width="84" height="18" rx="9" fill="#0B1220" stroke="#243554" />
                <text x="58" y="138" textAnchor="middle" fill="#B5C4E3" fontSize="8.5">
                  Backend web
                </text>

                <circle cx="160" cy="74" r="15" fill="#162B56" stroke="#7FA6FF" strokeWidth="2" className="shapi-anim-nodo-1" />
                <rect x="128" y="95" width="64" height="18" rx="9" fill="#0E182E" stroke="#3B6FF0" />
                <text x="160" y="107" textAnchor="middle" fill="#FFFFFF" fontSize="9" fontWeight="500">
                  Shapi
                </text>

                <circle cx="254" cy="38" r="10" fill="#132544" stroke="#3B6FF0" strokeWidth="1.5" className="shapi-anim-nodo-3" />
                <rect x="210" y="54" width="88" height="18" rx="9" fill="#0B1220" stroke="#243554" />
                <text x="254" y="66" textAnchor="middle" fill="#B5C4E3" fontSize="8.5">
                  Su servidor
                </text>

                <circle cx="256" cy="110" r="10" fill="#2A1F1D" stroke="#F08A5F" strokeWidth="1.5" className="shapi-anim-nodo-2" />
                <rect x="212" y="126" width="88" height="18" rx="9" fill="#0B1220" stroke="#243554" />
                <text x="256" y="138" textAnchor="middle" fill="#B5C4E3" fontSize="8.5">
                  Sin cambios
                </text>
              </svg>
              <div className="flex flex-col gap-[9px]">
                <span className="tabular-nums text-[12px] font-semibold tracking-[0.14em] text-principal">01</span>
                <h3 className="font-display text-[21px] font-normal leading-[1.3] tracking-[-0.03em]">
                  Conectar el servidor de origen
                </h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Indique la dirección del servidor que ya tiene en funcionamiento. Shapi no lo modifica: solamente enruta el tráfico hacia él.
                </p>
              </div>
            </div>

            {/* Paso 02: Lente / Lupa de cristal flotante inspeccionando rutas OpenAPI */}
            <div
              style={{ '--retraso': '180ms' } as React.CSSProperties}
              className="shapi-scroll-item shapi-scroll-zoom shapi-tarjeta shapi-tarjeta-cristal flex flex-col gap-[18px] rounded-[16px] border border-borde bg-panel p-7"
            >
              <svg viewBox="0 0 320 160" width="100%" className="block" aria-hidden="true">
                <defs>
                  <radialGradient id="lupaCristal" cx="50%" cy="40%" r="60%">
                    <stop offset="0%" stopColor="#301F52" stopOpacity="0.94" />
                    <stop offset="68%" stopColor="#121028" stopOpacity="0.95" />
                    <stop offset="100%" stopColor="#8B5CF6" stopOpacity="0.38" />
                  </radialGradient>
                </defs>
                <g opacity="0.55">
                  <rect x="18" y="22" width="284" height="24" rx="6" fill="#0B1220" stroke="#1E2C47" />
                  <text x="32" y="38" fill="#6F82A6" fontSize="9.5" fontFamily="monospace">
                    GET /v1/catalogo · pública
                  </text>

                  <rect x="18" y="54" width="284" height="24" rx="6" fill="#0B1220" stroke="#1E2C47" />
                  <text x="32" y="70" fill="#6F82A6" fontSize="9.5" fontFamily="monospace">
                    POST /v1/cobros · protegida
                  </text>

                  <rect x="18" y="86" width="284" height="24" rx="6" fill="#0B1220" stroke="#1E2C47" />
                  <text x="32" y="102" fill="#6F82A6" fontSize="9.5" fontFamily="monospace">
                    DELETE /v1/interna · oculta
                  </text>

                  <rect x="18" y="118" width="284" height="24" rx="6" fill="#0B1220" stroke="#1E2C47" />
                  <text x="32" y="134" fill="#6F82A6" fontSize="9.5" fontFamily="monospace">
                    GET /v1/facturas · expuesta
                  </text>
                </g>

                <g className="shapi-anim-lupa">
                  <circle
                    cx="160"
                    cy="76"
                    r="46"
                    fill="url(#lupaCristal)"
                    stroke="#C4B5FD"
                    strokeWidth="1.5"
                  />
                  <rect x="118" y="62" width="84" height="28" rx="7" fill="#0D172B" stroke="#8B5CF6" />
                  <circle cx="129" cy="76" r="3.5" fill="#C4B5FD" />
                  <text x="138" y="80" fill="#FFFFFF" fontSize="10" fontWeight="600" fontFamily="monospace">
                    POST /cobros
                  </text>
                </g>
              </svg>
              <div className="flex flex-col gap-[9px]">
                <span className="tabular-nums text-[12px] font-semibold tracking-[0.14em] text-principal">02</span>
                <h3 className="font-display text-[21px] font-normal leading-[1.3] tracking-[-0.03em]">
                  Cargar la especificación OpenAPI
                </h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Suba el archivo. Shapi extrae las rutas y usted decide cuáles quedan expuestas y cuáles permanecen ocultas.
                </p>
              </div>
            </div>

            {/* Paso 03: Planes con perspectiva y brillo */}
            <div
              style={{ '--retraso': '300ms' } as React.CSSProperties}
              className="shapi-scroll-item shapi-scroll-der shapi-tarjeta shapi-tarjeta-cristal flex flex-col gap-[18px] rounded-[16px] border border-borde bg-panel p-7"
            >
              <svg viewBox="0 0 320 160" width="100%" className="block" aria-hidden="true">
                <defs>
                  <radialGradient id="paso3Glow" cx="50%" cy="50%" r="48%">
                    <stop offset="0%" stopColor="#C4B5FD" stopOpacity="0.35" />
                    <stop offset="55%" stopColor="#8B5CF6" stopOpacity="0.18" />
                    <stop offset="100%" stopColor="#3B6FF0" stopOpacity="0" />
                  </radialGradient>
                </defs>
                <ellipse cx="160" cy="80" rx="82" ry="60" fill="url(#paso3Glow)" className="shapi-anim-pulso" />
                <rect x="16" y="34" width="86" height="94" rx="8" fill="#0B1220" stroke="#243554" />
                <rect x="116" y="22" width="88" height="116" rx="10" fill="#101E3B" stroke="#7FA6FF" strokeWidth="1.5" />
                <rect x="218" y="34" width="86" height="94" rx="8" fill="#0B1220" stroke="#243554" />
                <text x="59" y="58" textAnchor="middle" fill="#8FA2C7" fontSize="9.5">
                  Básico
                </text>
                <text x="59" y="78" textAnchor="middle" fill="#E8EDF7" fontSize="13" fontWeight="600">
                  Q 99
                </text>
                <text x="160" y="48" textAnchor="middle" fill="#7FA6FF" fontSize="10" fontWeight="600">
                  PRO · API
                </text>
                <text x="160" y="72" textAnchor="middle" fill="#FFFFFF" fontSize="16" fontWeight="600">
                  Q 349
                </text>
                <rect x="132" y="88" width="56" height="16" rx="8" fill="#3B6FF0" className="shapi-anim-pulso" />
                <text x="160" y="99" textAnchor="middle" fill="#FFFFFF" fontSize="8.5" fontWeight="500">
                  600 rpm
                </text>
                <text x="261" y="58" textAnchor="middle" fill="#8FA2C7" fontSize="9.5">
                  Escala
                </text>
                <text x="261" y="78" textAnchor="middle" fill="#E8EDF7" fontSize="13" fontWeight="600">
                  Q 899
                </text>
              </svg>
              <div className="flex flex-col gap-[9px]">
                <span className="tabular-nums text-[12px] font-semibold tracking-[0.14em] text-principal">03</span>
                <h3 className="font-display text-[21px] font-normal leading-[1.3] tracking-[-0.03em]">
                  Definir los planes
                </h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Nombre, descripción, precio, vigencia, cuota mensual y límite por minuto para cada plan de su API.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Sección: Qué recibe el proveedor — Con el radar de semicírculos concéntricos y animaciones de scroll */}
        <section className="relative mt-[120px] flex flex-col items-center px-8 pt-24 text-center lg:px-20">
          <svg
            aria-hidden="true"
            viewBox="0 0 800 260"
            className="shapi-scroll-item shapi-scroll-zoom pointer-events-none mx-auto -mb-[36px] block w-full max-w-[780px]"
          >
            <defs>
              <radialGradient id="radarFondoGlow" cx="50%" cy="92%" r="65%">
                <stop offset="0%" stopColor="#8B5CF6" stopOpacity="0.36" />
                <stop offset="45%" stopColor="#3B6FF0" stopOpacity="0.18" />
                <stop offset="80%" stopColor="#1E1B4B" stopOpacity="0.06" />
                <stop offset="100%" stopColor="#060910" stopOpacity="0" />
              </radialGradient>
              <linearGradient id="radarHazGrad" x1="0" y1="0" x2="1" y2="1">
                <stop offset="0%" stopColor="#C4B5FD" stopOpacity="0.48" />
                <stop offset="55%" stopColor="#8B5CF6" stopOpacity="0.22" />
                <stop offset="85%" stopColor="#3B6FF0" stopOpacity="0.08" />
                <stop offset="100%" stopColor="#3B6FF0" stopOpacity="0" />
              </linearGradient>
            </defs>

            <path d="M 80 240 A 320 320 0 0 1 720 240 Z" fill="url(#radarFondoGlow)" />

            <g fill="none" stroke="#24385E" strokeWidth="1">
              <path d="M 120 240 A 280 280 0 0 1 680 240" />
              <path d="M 180 240 A 220 220 0 0 1 620 240" />
              <path d="M 240 240 A 160 160 0 0 1 560 240" />
              <path d="M 300 240 A 100 100 0 0 1 500 240" />
            </g>

            <g className="shapi-anim-radar">
              <path d="M 400 240 L 240 16 A 280 280 0 0 1 400 0 Z" fill="url(#radarHazGrad)" />
              <line x1="400" y1="240" x2="240" y2="16" stroke="#C4B5FD" strokeOpacity="0.75" strokeWidth="1.5" />
            </g>

            <circle cx="268" cy="112" r="3.5" fill="#7FA6FF" className="shapi-anim-nodo-1" />
            <circle cx="534" cy="96" r="3.5" fill="#3FBF88" className="shapi-anim-nodo-2" />
            <circle cx="468" cy="158" r="3" fill="#A78BFA" className="shapi-anim-nodo-3" />

            <circle cx="400" cy="226" r="24" fill="#0D162B" stroke="#A78BFA" strokeWidth="1.5" />
            <circle cx="400" cy="226" r="16" fill="#162850" stroke="#8B5CF6" strokeWidth="1" className="shapi-anim-pulso" />
            <path d="M393 226 L398 231 L408 221" stroke="#FFFFFF" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
          </svg>

          <div className="shapi-scroll-item shapi-insignia">
            <span className="h-1.5 w-1.5 rounded-full bg-[#A78BFA] shapi-anim-pulso" aria-hidden="true" />
            <p className="text-etiqueta font-medium uppercase tracking-[0.16em] text-tinta-rotulo">
              Qué recibe el proveedor
            </p>
          </div>
          <h2
            style={{ '--retraso': '80ms' } as React.CSSProperties}
            className="shapi-scroll-item mt-[18px] max-w-[22ch] font-display text-[44px] font-light leading-[1.15] tracking-[-0.03em]"
          >
            Todo lo que hace falta para operar una API de paga.
          </h2>

          <div
            aria-hidden="true"
            style={
              {
                '--retraso': '140ms',
                WebkitMaskImage: 'radial-gradient(ellipse 75% 80% at 50% 50%, #000 35%, transparent 100%)',
                maskImage: 'radial-gradient(ellipse 75% 80% at 50% 50%, #000 35%, transparent 100%)',
              } as React.CSSProperties
            }
            className="shapi-scroll-item relative mt-8 w-full max-w-[820px] overflow-hidden rounded-2xl border border-white/[0.06] bg-[#080D18]/80 px-6 py-4"
          >
            <div className="shapi-anim-matriz flex flex-col gap-1.5 font-mono text-[11px] tracking-[0.06em] text-[#4C689E]">
              {FILAS_MATRIZ_TOKENS.map((fila, idx) => (
                <div key={idx} className="whitespace-nowrap">
                  {fila} · {fila}
                </div>
              ))}
            </div>
          </div>

          <div className="mt-[44px] grid w-full grid-cols-1 gap-6 text-left md:grid-cols-2">
            <div
              style={{ '--retraso': '60ms' } as React.CSSProperties}
              className="shapi-scroll-item shapi-scroll-izq shapi-tarjeta shapi-tarjeta-cristal flex items-start gap-[26px] rounded-[16px] border border-borde bg-panel p-[30px]"
            >
              <svg width="132" height="132" viewBox="0 0 132 132" className="shrink-0" aria-hidden="true">
                <defs>
                  <radialGradient id="prov1Glow" cx="50%" cy="55%" r="50%">
                    <stop offset="0%" stopColor="#3B6FF0" stopOpacity="0.28" />
                    <stop offset="100%" stopColor="#3B6FF0" stopOpacity="0" />
                  </radialGradient>
                </defs>
                <circle cx="66" cy="66" r="58" fill="url(#prov1Glow)" className="shapi-anim-pulso" />
                <rect x="6" y="18" width="120" height="96" rx="8" fill="#0E1729" stroke="#26334D" />
                <path d="M6 40 H126" stroke="#26334D" />
                <circle cx="22" cy="29" r="4" fill="#3B6FF0" className="shapi-anim-pulso" />
                <rect x="34" y="25" width="40" height="8" rx="4" fill="#1B2740" />
                <g stroke="#3D4C6B" strokeWidth="1">
                  <path d="M22 58 H82" />
                  <path d="M22 72 H108" />
                  <path d="M22 86 H70" />
                </g>
                <rect x="22" y="96" width="46" height="10" rx="5" fill="#3B6FF0" />
                <circle cx="106" cy="29" r="2.5" fill="#7FA6FF" className="shapi-anim-nodo-1" />
              </svg>
              <div className="flex flex-col gap-[10px]">
                <h3 className="font-display text-[22px] font-normal tracking-[-0.03em]">Un portal con su propia marca</h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Su logotipo, su color principal y sus textos. La documentación se genera desde su especificación OpenAPI e incluye consola de pruebas.
                </p>
              </div>
            </div>

            <div
              style={{ '--retraso': '160ms' } as React.CSSProperties}
              className="shapi-scroll-item shapi-scroll-der shapi-tarjeta shapi-tarjeta-cristal flex items-start gap-[26px] rounded-[16px] border border-borde bg-panel p-[30px]"
            >
              <svg width="132" height="132" viewBox="0 0 132 132" className="shrink-0" aria-hidden="true">
                <defs>
                  <radialGradient id="prov2Glow" cx="45%" cy="50%" r="50%">
                    <stop offset="0%" stopColor="#7FA6FF" stopOpacity="0.28" />
                    <stop offset="100%" stopColor="#3B6FF0" stopOpacity="0" />
                  </radialGradient>
                </defs>
                <circle cx="60" cy="64" r="54" fill="url(#prov2Glow)" className="shapi-anim-pulso" />
                <circle cx="42" cy="52" r="22" fill="none" stroke="#3B6FF0" strokeWidth="2" />
                <circle cx="42" cy="52" r="8" fill="#0E1729" stroke="#7FA6FF" strokeWidth="1.5" className="shapi-anim-pulso" />
                <path d="M60 60 L104 96" stroke="#3B6FF0" strokeWidth="2" strokeLinecap="round" />
                <path d="M90 84 L82 94" stroke="#3B6FF0" strokeWidth="2" strokeLinecap="round" />
                <path d="M100 92 L92 102" stroke="#3B6FF0" strokeWidth="2" strokeLinecap="round" />
                <circle cx="86" cy="34" r="18" fill="none" stroke="#3B6FF0" strokeWidth="1.5" strokeDasharray="4 5" className="shapi-anim-orbita" />
                <circle cx="86" cy="34" r="3" fill="#7FA6FF" className="shapi-anim-nodo-2" />
                <path d="M22 96 H70" stroke="#26334D" strokeWidth="1.5" />
              </svg>
              <div className="flex flex-col gap-[10px]">
                <h3 className="font-display text-[22px] font-normal tracking-[-0.03em]">Claves de acceso</h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Claves de producción y de pruebas, emitidas al activarse cada suscripción. Sus consumidores las rotan desde su portal y usted puede revocarlas en cualquier momento.
                </p>
              </div>
            </div>

            <div
              style={{ '--retraso': '240ms' } as React.CSSProperties}
              className="shapi-scroll-item shapi-scroll-izq shapi-tarjeta shapi-tarjeta-cristal flex items-start gap-[26px] rounded-[16px] border border-borde bg-panel p-[30px]"
            >
              <svg width="132" height="132" viewBox="0 0 132 132" className="shrink-0" aria-hidden="true">
                <defs>
                  <radialGradient id="prov3Glow" cx="50%" cy="55%" r="50%">
                    <stop offset="0%" stopColor="#3B6FF0" stopOpacity="0.28" />
                    <stop offset="100%" stopColor="#3B6FF0" stopOpacity="0" />
                  </radialGradient>
                </defs>
                <circle cx="66" cy="66" r="56" fill="url(#prov3Glow)" className="shapi-anim-pulso" />
                <g stroke="#26334D" strokeWidth="1.5">
                  <path d="M14 30 V102" />
                  <path d="M38 30 V102" />
                  <path d="M62 30 V102" />
                  <path d="M86 30 V102" />
                  <path d="M110 30 V102" />
                </g>
                <path d="M6 52 H126" stroke="#3B6FF0" strokeWidth="2" />
                <circle cx="14" cy="52" r="4" fill="#7FA6FF" className="shapi-anim-pulso" />
                <circle cx="38" cy="52" r="4" fill="#7FA6FF" className="shapi-anim-pulso" />
                <circle cx="62" cy="52" r="4" fill="#7FA6FF" className="shapi-anim-pulso" />
                <circle cx="86" cy="52" r="3.5" fill="#7FA6FF" className="shapi-anim-nodo-1" />
                <path d="M6 80 H62" stroke="#C2481F" strokeWidth="2" />
                <circle cx="62" cy="80" r="4" fill="#F08A5F" className="shapi-anim-pulso" />
                <path d="M70 80 H126" stroke="#1B2740" strokeWidth="2" strokeDasharray="4 4" />
              </svg>
              <div className="flex flex-col gap-[10px]">
                <h3 className="font-display text-[22px] font-normal tracking-[-0.03em]">
                  Cuotas aplicadas en cada petición
                </h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  La compuerta verifica la clave, la suscripción, el límite por minuto y la cuota antes de que la petición llegue a su servidor.
                </p>
              </div>
            </div>

            <div
              style={{ '--retraso': '340ms' } as React.CSSProperties}
              className="shapi-scroll-item shapi-scroll-der shapi-tarjeta shapi-tarjeta-cristal flex items-start gap-[26px] rounded-[16px] border border-borde bg-panel p-[30px]"
            >
              <svg width="132" height="132" viewBox="0 0 132 132" className="shrink-0" aria-hidden="true">
                <defs>
                  <radialGradient id="prov4Glow" cx="50%" cy="55%" r="50%">
                    <stop offset="0%" stopColor="#3B6FF0" stopOpacity="0.28" />
                    <stop offset="100%" stopColor="#3B6FF0" stopOpacity="0" />
                  </radialGradient>
                </defs>
                <circle cx="66" cy="66" r="56" fill="url(#prov4Glow)" className="shapi-anim-pulso" />
                <rect x="6" y="18" width="120" height="96" rx="8" fill="#0E1729" stroke="#26334D" />
                <g stroke="#1B2740" strokeWidth="1">
                  <path d="M6 42 H126" />
                  <path d="M6 66 H126" />
                  <path d="M6 90 H126" />
                </g>
                <rect x="20" y="28" width="54" height="10" rx="3" fill="#1B2740" />
                <rect x="20" y="52" width="82" height="10" rx="3" fill="#3B6FF0" className="shapi-anim-pulso" />
                <rect x="20" y="76" width="38" height="10" rx="3" fill="#1B2740" />
                <rect x="20" y="98" width="66" height="10" rx="3" fill="#1F8A5B" />
              </svg>
              <div className="flex flex-col gap-[10px]">
                <h3 className="font-display text-[22px] font-normal tracking-[-0.03em]">Registro de consumo y pagos</h3>
                <p className="text-[15px] leading-[1.6] text-tinta-suave">
                  Consumo consolidado por suscripción, ruta y día, e historial de pagos de la organización y de cada consumidor.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Planes — Con el horizonte curvo luminoso y aparición escalonada al hacer scroll */}
        <section id="planes" aria-label="Planes" className="relative mt-[120px] px-8 pt-20 lg:px-20">
          <svg
            aria-hidden="true"
            viewBox="0 0 960 150"
            className="shapi-scroll-item shapi-scroll-zoom pointer-events-none mx-auto -mb-[28px] block w-full max-w-[900px]"
          >
            <defs>
              <radialGradient id="planesArcoGlow" cx="50%" cy="100%" r="60%">
                <stop offset="0%" stopColor="#C4B5FD" stopOpacity="0.58" />
                <stop offset="38%" stopColor="#8B5CF6" stopOpacity="0.30" />
                <stop offset="70%" stopColor="#3B6FF0" stopOpacity="0.14" />
                <stop offset="100%" stopColor="#060910" stopOpacity="0" />
              </radialGradient>
              <linearGradient id="planesArcoBorde" x1="0" y1="0" x2="1" y2="0">
                <stop offset="0%" stopColor="#3B6FF0" stopOpacity="0" />
                <stop offset="26%" stopColor="#8B5CF6" stopOpacity="0.75" />
                <stop offset="50%" stopColor="#FFFFFF" stopOpacity="0.95" />
                <stop offset="74%" stopColor="#8B5CF6" stopOpacity="0.75" />
                <stop offset="100%" stopColor="#3B6FF0" stopOpacity="0" />
              </linearGradient>
            </defs>
            <ellipse cx="480" cy="145" rx="400" ry="120" fill="url(#planesArcoGlow)" className="shapi-anim-pulso" />
            <path
              d="M 100 145 Q 480 35 860 145"
              fill="#060910"
              stroke="url(#planesArcoBorde)"
              strokeWidth="2.5"
            />
          </svg>

          <div className="relative flex flex-col items-center text-center">
            <div className="shapi-scroll-item shapi-insignia">
              <span className="h-1.5 w-1.5 rounded-full bg-[#A78BFA] shapi-anim-pulso" aria-hidden="true" />
              <p className="text-etiqueta font-medium uppercase tracking-[0.16em] text-tinta-rotulo">Planes</p>
            </div>
            <h2
              style={{ '--retraso': '80ms' } as React.CSSProperties}
              className="shapi-scroll-item mt-[18px] font-display text-[44px] font-light leading-[1.15] tracking-[-0.03em]"
            >
              Los planes de Shapi.
            </h2>
            <p
              style={{ '--retraso': '140ms' } as React.CSSProperties}
              className="shapi-scroll-item mt-[18px] max-w-[56ch] text-[17px] leading-[1.6] text-tinta-suave"
            >
              Precios en quetzales. La vigencia se cuenta desde la contratación del plan.
            </p>

            <div className="mt-[52px] w-full text-left">
              {consulta.isPending ? (
                <EstadoCargando />
              ) : consulta.isError ? (
                <EstadoError
                  mensaje="No se pudieron cargar los planes."
                  reintentar={() => void consulta.refetch()}
                />
              ) : (
                <div className="grid grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-4">
                  {visibles.map((plan: Plan, indice: number) => {
                    const destacado = plan.nombre === 'Producto';
                    const esEscala = plan.nombre.startsWith('Escala');
                    return (
                      <article
                        key={plan.id}
                        style={{ '--retraso': `${indice * 100}ms` } as React.CSSProperties}
                        className={`shapi-scroll-item shapi-scroll-zoom shapi-tarjeta-plan shapi-plan-${indice} flex flex-col gap-5 rounded-[16px] border p-[26px] ${
                          destacado
                            ? 'shapi-tarjeta-destacada shapi-tarjeta-destacada-cristal border-principal bg-fondo-activo'
                            : 'shapi-tarjeta shapi-tarjeta-cristal border-borde bg-panel'
                        }`}
                      >
                        <div className="flex flex-col gap-2">
                          <h3
                            className={`font-display text-[22px] font-normal tracking-[-0.03em] ${
                              destacado ? 'text-white' : 'text-tinta'
                            }`}
                          >
                            {esEscala ? 'Escala' : plan.nombre}
                          </h3>
                          <p className="min-h-[42px] text-[14px] leading-[1.5] text-tinta-suave">
                            {plan.descripcion}
                          </p>
                        </div>

                        <div className="flex flex-col gap-1 border-t border-borde pt-5">
                          <span
                            className={`tabular-nums font-display text-[32px] font-normal tracking-[-0.03em] ${
                              destacado ? 'text-white' : 'text-tinta'
                            }`}
                          >
                            {dinero(plan.precio)}
                          </span>
                          <span className="text-[13px] text-tinta-rotulo">
                            {plan.nombre === 'Escala mensual'
                              ? `Mensual · vigencia de ${plan.vigenciaDias} días`
                              : `Vigencia de ${plan.vigenciaDias} días`}
                          </span>
                          {plan.nombre === 'Escala mensual' && planAnual && (
                            <span className="mt-[6px] tabular-nums text-[13px] text-tinta-navegacion">
                              o {dinero(planAnual.precio)} anual · {planAnual.vigenciaDias} días y{' '}
                              {planAnual.cuotaPeticiones.toLocaleString('es-GT')} peticiones
                            </span>
                          )}
                        </div>

                        <ul className="flex flex-1 flex-col gap-[11px] text-[14.5px] text-tinta-navegacion">
                          <li>
                            {plan.maxApis == null
                              ? 'APIs ilimitadas'
                              : `${plan.maxApis} ${plan.maxApis === 1 ? 'API' : 'APIs'}`}
                          </li>
                          <li className="tabular-nums">
                            {plan.cuotaPeticiones.toLocaleString('es-GT')} peticiones
                          </li>
                          <li>
                            {plan.maxMiembros == null
                              ? 'Miembros ilimitados'
                              : `${plan.maxMiembros} ${plan.maxMiembros === 1 ? 'miembro' : 'miembros'}`}
                          </li>
                          {plan.dominioPropio && <li>Dominio propio</li>}
                          {plan.esPrueba && <li>Prueba gratis por {plan.vigenciaDias} días</li>}
                        </ul>

                        {destacado && (
                          <div className="shapi-contenedor-recomendado mt-auto pt-4 border-t border-white/[0.08]">
                            <div className="shapi-badge-recomendado flex items-center justify-center gap-2 rounded-xl border border-[#C4B5FD]/50 bg-gradient-to-r from-[#8B5CF6]/20 via-[#9382FF]/30 to-[#8B5CF6]/20 py-2.5 px-4 text-center shadow-[0_0_18px_rgba(147,130,255,0.3)]">
                              <svg width="13" height="13" viewBox="0 0 16 16" fill="none" aria-hidden="true" className="shrink-0">
                                <path
                                  d="M8 1.5 L9.4 6.6 L14.5 8 L9.4 9.4 L8 14.5 L6.6 9.4 L1.5 8 L6.6 6.6 Z"
                                  fill="#C4B5FD"
                                />
                              </svg>
                              <span className="font-display text-[12.5px] font-semibold tracking-[0.06em] uppercase text-white">
                                Recomendado
                              </span>
                            </div>
                          </div>
                        )}
                      </article>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        </section>

        {/* Cierre final (CTA debajo de los precios): Hoyo negro animado en azul con morado */}
        <section className="relative mt-[136px] flex flex-col items-center overflow-hidden px-8 pt-12 text-center lg:px-20">
          {/* Resplandor ambiental azul-morado de fondo */}
          <div
            aria-hidden="true"
            className="pointer-events-none absolute inset-x-0 bottom-0 h-[440px]"
            style={{
              background:
                'radial-gradient(ellipse 56% 72% at 50% 100%, rgba(124, 58, 237, 0.32) 0%, rgba(59, 111, 240, 0.22) 38%, rgba(6, 9, 16, 0) 76%)',
            }}
          />

          <div className="relative z-10 flex flex-col items-center">
            <div className="shapi-scroll-item shapi-insignia">
              <span className="h-1.5 w-1.5 rounded-full bg-[#A78BFA] shapi-anim-pulso" aria-hidden="true" />
              <p className="text-etiqueta font-medium uppercase tracking-[0.16em] text-tinta-rotulo">
                Empieza hoy · Prueba gratis 30 días
              </p>
            </div>

            <h2
              style={{ '--retraso': '80ms' } as React.CSSProperties}
              className="shapi-scroll-item shapi-titulo-gradiente mt-[18px] max-w-[20ch] font-display text-[46px] font-light leading-[1.12] tracking-[-0.03em] md:text-[54px]"
            >
              Monetiza tu API en minutos, sin tocar tu código.
            </h2>

            <p
              style={{ '--retraso': '140ms' } as React.CSSProperties}
              className="shapi-scroll-item mt-[16px] max-w-[54ch] text-[17px] leading-[1.6] text-tinta-suave"
            >
              Publica tu primera API gratis, activa cobros automáticos en quetzales y protege cada petición desde hoy.
            </p>

            <div
              style={{ '--retraso': '210ms' } as React.CSSProperties}
              className="shapi-scroll-item mt-[30px] flex flex-wrap items-center justify-center gap-4"
            >
              <Link
                to="/registro"
                className="group relative inline-flex items-center gap-2.5 rounded-[10px] border border-[#9382FF]/55 bg-[#100E2E]/90 px-6 py-3 text-[15px] font-medium text-white shadow-[inset_0_0_24px_rgba(147,130,255,0.28),0_0_28px_rgba(91,76,245,0.35)] transition-all duration-300 hover:-translate-y-0.5 hover:border-[#C4B5FD] hover:bg-[#161342] hover:shadow-[inset_0_0_28px_rgba(196,181,253,0.42),0_0_40px_rgba(124,58,237,0.55)]"
              >
                <span>Empieza tu prueba gratis ahora</span>
                <span aria-hidden="true" className="transition-transform duration-300 group-hover:translate-x-1">
                  →
                </span>
              </Link>
            </div>
          </div>

          {/* Video oficial Diseño-sin-título.webm mostrando únicamente la mitad superior sobre el horizonte */}
          <div
            aria-hidden="true"
            style={
              {
                '--retraso': '180ms',
                WebkitMaskImage:
                  'radial-gradient(ellipse 68% 96% at 50% 100%, #000 58%, rgba(0, 0, 0, 0.72) 78%, transparent 100%)',
                maskImage:
                  'radial-gradient(ellipse 68% 96% at 50% 100%, #000 58%, rgba(0, 0, 0, 0.72) 78%, transparent 100%)',
              } as React.CSSProperties
            }
            className="shapi-scroll-item shapi-scroll-zoom pointer-events-none relative -mt-8 h-[240px] w-full max-w-[1280px] overflow-hidden sm:h-[300px] md:h-[360px]"
          >
            <video
              src="/diseno-sin-titulo.webm"
              autoPlay
              loop
              muted
              playsInline
              className="pointer-events-none mx-auto block h-[480px] w-full object-cover object-center mix-blend-screen sm:h-[600px] md:h-[720px]"
            />
          </div>
        </section>

        {/* Pie */}
        <footer className="flex items-start justify-between gap-16 border-t border-borde-barra px-8 pt-11 pb-[60px] lg:px-20">
          <div className="flex items-center gap-[11px]">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <rect x="1" y="1" width="22" height="22" rx="6" stroke="var(--tinta-suave)" strokeWidth="1.5" />
              <path d="M6 8.5 H14" stroke="var(--tinta-suave)" strokeWidth="1.5" strokeLinecap="round" />
              <path d="M6 15.5 H12" stroke="var(--tinta-suave)" strokeWidth="1.5" strokeLinecap="round" />
              <path d="M6 12 H22" stroke="var(--tinta-suave)" strokeWidth="2.5" strokeLinecap="round" />
            </svg>
            <span className="font-display text-[17px] font-medium tracking-[-0.02em] text-tinta-rotulo">Shapi</span>
          </div>
          <p className="text-right text-[14px] text-tinta-rotulo">
            Shapi es un proyecto académico de la Universidad Rafael Landívar, Facultad de Ingeniería.
          </p>
        </footer>
      </div>
    </div>
  );
}
