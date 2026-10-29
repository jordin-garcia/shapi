import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/servidor';
import Apis from '../paginas/A3-1-Apis';
import Especificacion from '../paginas/A3-3-Especificacion';
import Rutas from '../paginas/A3-4-Rutas';
import Configuracion from '../paginas/A3-5-ConfigRutas';
import Dominio from '../paginas/A3-6-Dominio';
import Portal from '../paginas/A3-7-Portal';
import Planes from '../paginas/A4-1-PlanesApi';

const API = 'http://localhost/api/apis';
const ruta = { id: 'ruta-1', metodo: 'GET', patron: '/tarifas', resumen: 'Tarifas', descripcion: null, expuesta: true, limiteMinuto: 60, cacheSegundos: 30, pesoLlamadas: 5 };
let cliente: QueryClient;

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false, staleTime: Infinity } } });
  cliente.setQueryData(['sesion'], { rol: 'lector' });
  server.use(
    http.get(API, () => HttpResponse.json({ elementos: [{ id: 'api-1', nombre: 'API de Envíos', subdominio: 'envios', estado: 'publicada' }], total: 1, planNombre: 'Producto', maxApis: 10 })),
    http.get(`${API}/api-1/rutas`, () => HttpResponse.json({ apiNombre: 'API de Envíos', elementos: [ruta], totalExpuestas: 1, totalOcultas: 0, especificacion: { titulo: 'Envíos', versionOpenApi: '3.0.3', tamanoBytes: 100 } })),
    http.get(`${API}/api-1/dominio`, () => HttpResponse.json({ apiNombre: 'API de Envíos', urlApi: 'https://envios.api.shapi.localhost', urlPortal: 'https://envios.shapi.localhost', secretoEnmascarado: 'shps_••••1234', dominio: 'api.envios.localhost', destinoCname: 'envios.api.shapi.localhost', estado: 'pendiente', modoSimulado: true, dominioPropioPermitido: true, planNombre: 'Producto' })),
    http.get(`${API}/api-1/portal`, () => HttpResponse.json({ apiNombre: 'API de Envíos', nombreOrganizacion: 'Envíos', nombre: 'Envíos', color: '#B8322A', bienvenida: 'Bienvenido', urlLogo: null, tieneLogo: false, urlPortal: 'https://envios.shapi.localhost', rutas: [], planes: [] })),
    http.get(`${API}/api-1/planes`, () => HttpResponse.json([{ id: 'plan-1', nombre: 'Comercio', descripcion: 'Plan de envíos', precio: 150, esGratuito: false, vigenciaDias: 30, cuotaLlamadas: 50000, limiteMinuto: 120, activo: true }])),
  );
});

afterEach(() => { cleanup(); cliente.clear(); vi.restoreAllMocks(); });

const pantallas = [
  { nombre: 'APIs', componente: <Apis />, titulo: 'APIs de la organización', accion: 'Despublicar' },
  { nombre: 'especificación', componente: <Especificacion />, titulo: 'Cargar especificación OpenAPI', accion: 'elíjalo en su equipo' },
  { nombre: 'rutas', componente: <Rutas />, titulo: 'Rutas expuestas', accion: 'Guardar rutas expuestas' },
  { nombre: 'configuración', componente: <Configuracion />, titulo: 'Configuración por ruta', accion: 'Guardar configuración' },
  { nombre: 'dominio', componente: <Dominio />, titulo: 'Direcciones y dominio propio', accion: 'Regenerar' },
  { nombre: 'portal', componente: <Portal />, titulo: 'Personalización del portal', accion: 'Guardar personalización' },
  { nombre: 'planes', componente: <Planes />, titulo: 'Planes de la API', accion: 'Crear un plan' },
];

function montar(componente: React.ReactNode) {
  render(<QueryClientProvider client={cliente}><MemoryRouter initialEntries={['/panel/apis/api-1']}>
    <Routes><Route path="/panel/apis/:id" element={componente} /></Routes>
  </MemoryRouter></QueryClientProvider>);
}

describe('JG-26 · RF-07 · controles de escritura del panel', () => {
  it.each(pantallas)('oculta las escrituras de $nombre al lector y conserva la consulta', async ({ componente, titulo }) => {
    montar(componente);
    expect(await screen.findByRole('heading', { name: titulo })).toBeDefined();
    // La consulta terminó: se conserva el contenido y se ocultan todos los controles que lo modifican.
    expect(await screen.findAllByText(/Envíos|Comercio|\/tarifas/)).not.toHaveLength(0);
    expect(screen.queryByRole('link', { name: 'Registrar una API' })).toBeNull();
    expect(screen.queryByRole('button')).toBeNull();
    expect(screen.queryByRole('radio')).toBeNull();
    expect(screen.queryByRole('spinbutton')).toBeNull();
    expect(screen.queryByRole('textbox')).toBeNull();
    expect(screen.queryByRole('combobox')).toBeNull();
    expect(document.querySelector('input[type="file"]')).toBeNull();
  });

  it('conserva el estado de exposición y los valores de configuración como texto para el lector', async () => {
    montar(<Configuracion />);
    expect(await screen.findByText('60 peticiones')).toBeDefined();
    expect(screen.getByText('30 segundos')).toBeDefined();
    expect(screen.getByText('5 llamadas por petición')).toBeDefined();
  });

  it.each([['APIs', <Apis />], ['planes', <Planes />]])('oculta la creación también en la lista vacía de %s', async (nombre, componente) => {
    server.use(
      http.get(API, () => HttpResponse.json({ elementos: [], total: 0, planNombre: 'Producto', maxApis: 10 })),
      http.get(`${API}/api-1/planes`, () => HttpResponse.json([])),
    );
    montar(componente);
    await screen.findByRole('heading', { name: nombre === 'APIs' ? 'Todavía no tiene APIs registradas' : 'Todavía no tiene planes para esta API' });
    expect(screen.queryByRole('link', { name: 'Registrar una API' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Crear un plan' })).toBeNull();
  });

  it('no muestra escrituras mientras se resuelve la sesión ni si su consulta falla', async () => {
    cliente.removeQueries({ queryKey: ['sesion'] });
    let responder!: (valor: Response) => void;
    server.use(http.get('http://localhost/api/auth/sesion', () => new Promise<Response>(resolver => { responder = resolver; })));
    montar(<Rutas />);
    await screen.findByText('/tarifas');
    expect(screen.queryByRole('button', { name: 'Guardar rutas expuestas' })).toBeNull();
    await waitFor(() => expect(responder).toBeDefined());
    await act(async () => responder(new HttpResponse(null, { status: 503 })));
    await waitFor(() => expect(cliente.getQueryState(['sesion'])?.status).toBe('error'));
    expect(screen.queryByRole('button', { name: 'Guardar rutas expuestas' })).toBeNull();
    expect(screen.queryByRole('radio')).toBeNull();
  });

  describe.each(['propietario', 'editor'])('%s', rol => {
    it.each(pantallas)('conserva las acciones de $nombre', async ({ componente, accion }) => {
      cliente.setQueryData(['sesion'], { rol });
      montar(componente);
      expect(await screen.findByRole('button', { name: accion })).toBeDefined();
    });
  });

  it('RF-09 invalida las rutas tras cargar y A3.4 muestra la nueva especificación con caché vigente', async () => {
    cliente.setQueryData(['sesion'], { rol: 'editor' });
    cliente.setQueryData(['rutas-api', 'api-1'], { apiNombre: 'API de Envíos', elementos: [ruta] });
    const nueva = { ...ruta, id: 'ruta-2', patron: '/cobertura', expuesta: false };
    let cargada = false;
    server.use(
      http.get(`${API}/api-1/rutas`, () => HttpResponse.json({ apiNombre: 'API de Envíos', elementos: cargada ? [nueva] : [ruta] })),
      http.put(`${API}/api-1/especificacion`, () => { cargada = true; return HttpResponse.json({ apiNombre: 'API de Envíos', versionOpenApi: '3.0.3', rutas: [nueva] }); }),
    );
    render(<QueryClientProvider client={cliente}><MemoryRouter initialEntries={['/panel/apis/api-1/especificacion']}>
      <Routes>
        <Route path="/panel/apis/:id/especificacion" element={<Especificacion />} />
        <Route path="/panel/apis/:id/rutas" element={<Rutas />} />
      </Routes>
    </MemoryRouter></QueryClientProvider>);
    // jsdom File y FormData de Node no comparten implementación; se simula solo el transporte multipart.
    vi.spyOn(FormData.prototype, 'set').mockImplementation(() => undefined);
    await userEvent.upload(await screen.findByLabelText('Elegir archivo OpenAPI'), new File(['{}'], 'api.json', { type: 'application/json' }));
    await screen.findByText('Cargado');
    await userEvent.click(screen.getByRole('link', { name: 'Continuar a la selección de rutas' }));
    expect(await screen.findByLabelText('Exponer GET /cobertura')).toBeDefined();
    expect(screen.queryByText('/tarifas')).toBeNull();
  });

  it('RF-18 impide enviar un plan de pago con precio cero y conserva el gratuito', async () => {
    cliente.setQueryData(['sesion'], { rol: 'propietario' });
    let enviados = 0;
    server.use(http.post(`${API}/api-1/planes`, () => { enviados++; return HttpResponse.json({ id: 'nuevo' }); }));
    montar(<Planes />);
    await userEvent.click(await screen.findByRole('button', { name: 'Crear un plan' }));
    await userEvent.type(screen.getByLabelText('Nombre'), 'Nuevo');
    await userEvent.type(screen.getByLabelText('Descripción'), 'Un plan');
    await userEvent.type(screen.getByLabelText('Cuota mensual'), '100');
    await userEvent.type(screen.getByLabelText('Límite por minuto'), '10');
    await userEvent.click(screen.getByLabelText(/Plan gratuito/));
    const precio = screen.getByLabelText('Precio') as HTMLInputElement;
    await userEvent.clear(precio);
    await userEvent.type(precio, '0');
    await userEvent.click(screen.getByRole('button', { name: 'Crear plan' }));
    expect(precio.checkValidity()).toBe(false);
    expect(enviados).toBe(0);
    await userEvent.click(screen.getByLabelText(/Plan gratuito/));
    await userEvent.click(screen.getByRole('button', { name: 'Crear plan' }));
    await screen.findByRole('heading', { name: 'Planes de la API' });
    expect(enviados).toBe(1);
  });
});
