import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, type RouterProviderProps } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import App from '../App';
import { crearRutas } from '../rutas';
import { server } from '../../../../test/servidor';

const API_PORTAL = 'http://localhost/api/portal';
const configuracion = {
  nombrePortal: 'Envíos Xelajú', colorPrincipal: '#B8322A', urlLogo: null,
  bienvenida: 'Cotice y genere guías.', nombreApi: 'API de Cotización de Envíos',
  descripcionApi: 'Cotice envíos.', hostPortal: 'envios.shapi.localhost',
  hostApi: 'envios.api.shapi.localhost', nombreOrganizacion: 'Envíos Xelajú, S.A.',
};
const rutas = [
  {
    metodo: 'POST', patron: '/cotizaciones', resumen: 'Cotizar un envío', descripcion: null,
    parametros: [
      { nombre: 'origen', tipo: 'string', obligatorio: true, descripcion: null, ubicacion: 'cuerpo' },
      { nombre: 'peso_kg', tipo: 'number', obligatorio: true, descripcion: null, ubicacion: 'cuerpo' },
      { nombre: 'tipo_servicio', tipo: 'string', obligatorio: false, descripcion: null, ubicacion: 'cuerpo' },
    ],
    ejemploPeticion: { origen: '0901', peso_kg: 2.5, tipo_servicio: 'normal' },
    ejemploRespuesta: { tarifa: 'Q 38.50' }, codigoRespuesta: 200, pesoLlamadas: 1,
    urlCompleta: 'https://envios.api.shapi.localhost/cotizaciones',
  },
  {
    metodo: 'GET', patron: '/rastreo/{guia}', resumen: 'Rastrear una guía', descripcion: null,
    parametros: [
      { nombre: 'guia', tipo: 'string', obligatorio: true, descripcion: null, ubicacion: 'ruta' },
      { nombre: 'detalle', tipo: 'boolean', obligatorio: false, descripcion: null, ubicacion: 'consulta' },
    ],
    ejemploPeticion: { guia: 'GX-100', detalle: true }, ejemploRespuesta: null,
    codigoRespuesta: 200, pesoLlamadas: 1,
    urlCompleta: 'https://envios.api.shapi.localhost/rastreo/{guia}',
  },
  {
    metodo: 'POST', patron: '/procesar', resumen: 'Procesar valores', descripcion: null,
    parametros: [
      { nombre: 'modo', tipo: 'string', obligatorio: true, descripcion: null, ubicacion: 'consulta' },
      { nombre: 'cuerpo', tipo: 'array', obligatorio: true, descripcion: null, ubicacion: 'cuerpo', representaCuerpoCompleto: true },
    ],
    ejemploPeticion: [1, 2], ejemploRespuesta: null, codigoRespuesta: 200, pesoLlamadas: 1,
    urlCompleta: 'https://envios.api.shapi.localhost/procesar',
  },
  {
    metodo: 'POST', patron: '/eco', resumen: 'Repetir JSON libre', descripcion: null,
    parametros: [
      { nombre: 'cuerpo', tipo: 'object', obligatorio: true, descripcion: null, ubicacion: 'cuerpo', representaCuerpoCompleto: true },
    ],
    ejemploPeticion: { libre: 'valor' }, ejemploRespuesta: null, codigoRespuesta: 200, pesoLlamadas: 1,
    urlCompleta: 'https://envios.api.shapi.localhost/eco',
  },
];

let cliente: QueryClient;

function montar() {
  const enrutador = createMemoryRouter(crearRutas(), { initialEntries: ['/consola'] });
  return render(
    <QueryClientProvider client={cliente}>
      <App enrutador={enrutador as RouterProviderProps['router']} />
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  sessionStorage.clear();
  localStorage.clear();
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  server.use(
    http.get(`${API_PORTAL}/configuracion`, () => HttpResponse.json(configuracion)),
    http.get(`${API_PORTAL}/documentacion`, () => HttpResponse.json({ rutas })),
    http.get(`${API_PORTAL}/auth/sesion`, () => new HttpResponse(null, { status: 401 })),
  );
});

afterEach(() => {
  cleanup();
  cliente.clear();
  sessionStorage.clear();
  localStorage.clear();
});

describe('RF-16 y RF-45 · consola de pruebas del portal', () => {
  it('mantiene separada la forma de caché al navegar desde Documentación', async () => {
    const enrutador = createMemoryRouter(crearRutas(), { initialEntries: ['/documentacion'] });
    render(
      <QueryClientProvider client={cliente}>
        <App enrutador={enrutador as RouterProviderProps['router']} />
      </QueryClientProvider>,
    );

    await waitFor(() => expect(enrutador.state.location.pathname)
      .toBe('/documentacion/POST%20%2Fcotizaciones'));
    await userEvent.click(await screen.findByRole('link', { name: 'Consola de pruebas' }));

    expect(await screen.findByRole('heading', { name: 'Consola de pruebas' })).toBeDefined();
    expect(screen.getByLabelText('Ruta')).toBeDefined();
  });

  it('permite elegir una ruta y muestra sus parámetros, tipos y obligatoriedad', async () => {
    montar();

    expect(await screen.findByRole('heading', { name: 'Consola de pruebas' })).toBeDefined();
    expect(screen.getByLabelText('origen')).toBeDefined();
    expect(screen.getByText('texto · obligatorio')).toBeDefined();
    expect(screen.getByText('número · obligatorio')).toBeDefined();
    expect(screen.getByText('texto · opcional')).toBeDefined();

    await userEvent.selectOptions(screen.getByLabelText('Ruta'), 'GET /rastreo/{guia}');

    expect(screen.getByLabelText('guia')).toBeDefined();
    expect(screen.getByText('booleano · opcional')).toBeDefined();
    expect(screen.queryByLabelText('origen')).toBeNull();
  });

  it('guarda la clave solo en sessionStorage y la presenta en un campo enmascarado', async () => {
    montar();
    const campo = await screen.findByLabelText('Su clave de pruebas') as HTMLInputElement;

    await userEvent.type(campo, 'shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d');

    expect(campo.type).toBe('password');
    expect(sessionStorage.getItem('shapi:portal:clave-pruebas')).toBe('shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d');
    expect(localStorage.length).toBe(0);

    cleanup();
    montar();
    expect((await screen.findByLabelText('Su clave de pruebas') as HTMLInputElement).value)
      .toBe('shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d');
  });

  it('envía la petición real y muestra el código, el cuerpo y las cabeceras de límites', async () => {
    let peticionRecibida = false;
    server.use(http.post('https://envios.api.shapi.localhost/cotizaciones', async ({ request }) => {
      peticionRecibida = true;
      expect(request.headers.get('X-Api-Key')).toBe('shp_prueba_valida');
      expect(request.headers.get('Accept')).toBe('application/json');
      expect(request.headers.get('Content-Type')).toContain('application/json');
      expect(await request.json()).toEqual({ origen: '0901', peso_kg: 2.5, tipo_servicio: 'normal' });
      return HttpResponse.json(
        { origen: 'Quetzaltenango', tarifa: 'Q 38.50' },
        { headers: {
          'X-Shapi-Plan': 'B%C3%A1sico',
          'X-RateLimit-Limit': '10', 'X-RateLimit-Remaining': '9', 'X-RateLimit-Reset': '42',
          'X-Cuota-Limite': '1000', 'X-Cuota-Restante': '999',
          'X-Cuota-Reinicio': '2026-10-14T06:00:00Z',
        } },
      );
    }));
    montar();

    await userEvent.type(await screen.findByLabelText('Su clave de pruebas'), 'shp_prueba_valida');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar petición' }));

    await waitFor(() => expect(peticionRecibida).toBe(true));
    expect(await screen.findByText('200')).toBeDefined();
    expect(screen.getByText((_, elemento) => elemento?.tagName === 'PRE'
      && elemento.textContent?.includes('"tarifa": "Q 38.50"') === true)).toBeDefined();
    expect(screen.getByText('Básico')).toBeDefined();
    expect(screen.getByText('X-RateLimit-Limit')).toBeDefined();
    expect(screen.getByText('X-RateLimit-Remaining')).toBeDefined();
    expect(screen.getByText('X-RateLimit-Reset')).toBeDefined();
    expect(screen.getByText('X-Cuota-Limite')).toBeDefined();
    expect(screen.getByText('X-Cuota-Restante')).toBeDefined();
    expect(screen.getByText('X-Cuota-Reinicio')).toBeDefined();
  });

  it('sustituye los parámetros de ruta y agrega los demás a la query en una petición GET', async () => {
    let urlRecibida = '';
    server.use(http.get('https://envios.api.shapi.localhost/rastreo/GX-100', ({ request }) => {
      urlRecibida = request.url;
      expect(request.headers.get('X-Api-Key')).toBe('shp_prueba_valida');
      return HttpResponse.json({ estado: 'en_transito' });
    }));
    montar();

    await userEvent.selectOptions(await screen.findByLabelText('Ruta'), 'GET /rastreo/{guia}');
    await userEvent.type(screen.getByLabelText('Su clave de pruebas'), 'shp_prueba_valida');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar petición' }));

    await waitFor(() => expect(urlRecibida).toContain('/rastreo/GX-100?detalle=true'));
    expect(await screen.findByText('200')).toBeDefined();
  });

  it('separa la query del cuerpo JSON no objeto en una petición POST', async () => {
    let peticionRecibida = false;
    server.use(http.post('https://envios.api.shapi.localhost/procesar', async ({ request }) => {
      peticionRecibida = true;
      expect(new URL(request.url).searchParams.get('modo')).toBe('rapido');
      expect(await request.json()).toEqual([1, 2]);
      return HttpResponse.json({ procesados: 2 });
    }));
    montar();

    await userEvent.selectOptions(await screen.findByLabelText('Ruta'), 'POST /procesar');
    await userEvent.type(screen.getByLabelText('modo'), 'rapido');
    await userEvent.type(screen.getByLabelText('Su clave de pruebas'), 'shp_prueba_valida');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar petición' }));

    await waitFor(() => expect(peticionRecibida).toBe(true));
    expect(await screen.findByText('200')).toBeDefined();
  });

  it('envía un objeto libre como cuerpo completo sin envolverlo', async () => {
    let peticionRecibida = false;
    server.use(http.post('https://envios.api.shapi.localhost/eco', async ({ request }) => {
      peticionRecibida = true;
      expect(await request.json()).toEqual({ libre: 'valor' });
      return HttpResponse.json({ libre: 'valor' });
    }));
    montar();

    await userEvent.selectOptions(await screen.findByLabelText('Ruta'), 'POST /eco');
    await userEvent.type(screen.getByLabelText('Su clave de pruebas'), 'shp_prueba_valida');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar petición' }));

    await waitFor(() => expect(peticionRecibida).toBe(true));
    expect(await screen.findByText('200')).toBeDefined();
  });

  it.each([
    [401, 'clave_invalida', 'La clave no es válida.'],
    [403, 'suscripcion_inactiva', 'La suscripción no está activa.'],
    [429, 'cuota_agotada', 'Agotó las 1,000 peticiones de pruebas del día.'],
  ])('muestra el error %i de la compuerta con su código y mensaje', async (estado, codigo, mensaje) => {
    server.use(http.post('https://envios.api.shapi.localhost/cotizaciones', () => HttpResponse.json(
      { error: { codigo, mensaje, estado } },
      { status: estado },
    )));
    montar();

    await userEvent.type(await screen.findByLabelText('Su clave de pruebas'), 'shp_prueba_invalida');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar petición' }));

    expect(await screen.findByText(String(estado))).toBeDefined();
    expect(screen.getByText(mensaje)).toBeDefined();
    expect(screen.getByText(codigo)).toBeDefined();
  });
});
