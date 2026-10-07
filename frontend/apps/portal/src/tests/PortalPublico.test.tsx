import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, type RouterProviderProps } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import App from '../App';
import { crearRutas } from '../rutas';
import { server } from '../../../../test/servidor';

const configuracionEnvios = {
  nombrePortal: 'Envíos Xelajú',
  colorPrincipal: '#B8322A',
  urlLogo: null,
  bienvenida: 'Cotice y genere guías de envío a todo Guatemala desde su tienda en línea.',
  nombreApi: 'API de Cotización de Envíos',
  descripcionApi: 'Cotice envíos entre municipios, genere guías, consulte la cobertura y rastree sus paquetes.',
  hostPortal: 'envios.shapi.localhost',
  hostApi: 'envios.api.shapi.localhost',
  nombreOrganizacion: 'Envíos Xelajú, S.A.',
};

const rutasEnvios = [
  {
    metodo: 'POST', patron: '/cotizaciones', resumen: 'Cotizar un envío',
    descripcion: 'Calcula el costo de un envío con **datos seguros**. <script>alert("xss")</script>',
    parametros: [
      { nombre: 'origen', tipo: 'string', obligatorio: true, descripcion: 'Código del municipio de origen' },
      { nombre: 'peso_kg', tipo: 'number', obligatorio: false, descripcion: 'Peso del paquete' },
    ],
    ejemploPeticion: { origen: '0901', peso_kg: 2.5 },
    ejemploRespuesta: { tarifa: 'Q 38.50' }, codigoRespuesta: 200, pesoLlamadas: 1,
    urlCompleta: 'https://envios.api.shapi.localhost/cotizaciones',
  },
  {
    metodo: 'POST', patron: '/guias', resumen: 'Crear una guía', descripcion: 'Genera una guía.',
    parametros: [], ejemploPeticion: { cotizacion: 'COT-1' }, ejemploRespuesta: { guia: 'GX-1' },
    codigoRespuesta: 200, pesoLlamadas: 5, urlCompleta: 'https://envios.api.shapi.localhost/guias',
  },
];

let cliente: QueryClient;

function montar(ruta: string) {
  const enrutador = createMemoryRouter(crearRutas(), { initialEntries: [ruta] });
  const vista = render(
    <QueryClientProvider client={cliente}>
      <App enrutador={enrutador as RouterProviderProps['router']} />
    </QueryClientProvider>,
  );
  return { ...vista, enrutador };
}

function responder(configuracion = configuracionEnvios, rutas = rutasEnvios) {
  server.use(
    http.get('http://localhost/api/portal/configuracion', () => HttpResponse.json(configuracion)),
    http.get('http://localhost/api/portal/documentacion', () => HttpResponse.json({ rutas })),
  );
}

beforeEach(() => {
  cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  responder();
});

afterEach(() => {
  cleanup();
  cliente.clear();
});

describe('RF-16 · inicio y documentación generados', () => {
  it('A5.0 muestra bienvenida, primer ejemplo, rutas, pesos y el espacio para planes', async () => {
    montar('/');

    expect(await screen.findByRole('heading', { name: configuracionEnvios.bienvenida })).toBeDefined();
    expect(screen.getByText(configuracionEnvios.descripcionApi)).toBeDefined();
    expect(screen.getByText((_, elemento) => elemento?.tagName === 'PRE' && elemento.textContent?.includes('"origen": "0901"') === true)).toBeDefined();
    expect(screen.getByText((_, elemento) => elemento?.tagName === 'PRE' && elemento.textContent?.includes('"tarifa": "Q 38.50"') === true)).toBeDefined();
    expect(screen.getByText('Descuenta 1 llamada de su cuota')).toBeDefined();
    expect(screen.getByText('Descuenta 5 llamadas de su cuota')).toBeDefined();
    expect(screen.getByRole('heading', { name: 'Planes de la API' })).toBeDefined();
  });

  it('/documentacion redirige a la primera ruta y permite elegir otra desde la lista lateral', async () => {
    const { enrutador } = montar('/documentacion');

    await waitFor(() => expect(enrutador.state.location.pathname).toBe('/documentacion/%2Fcotizaciones'));
    expect(await screen.findByRole('heading', { name: '/cotizaciones' })).toBeDefined();
    expect(screen.getByText('https://envios.api.shapi.localhost/cotizaciones')).toBeDefined();
    expect(screen.getByRole('link', { name: 'Probar en la consola' }).getAttribute('href')).toBe('/consola');
    expect(screen.getByText('datos seguros').tagName).toBe('STRONG');
    expect(document.querySelector('script')).toBeNull();
    expect(screen.getByText('texto')).toBeDefined();
    expect(screen.getByText('número')).toBeDefined();

    await userEvent.click(screen.getByRole('link', { name: /POST\/guias/ }));
    await waitFor(() => expect(enrutador.state.location.pathname).toBe('/documentacion/%2Fguias'));
    expect(await screen.findByRole('heading', { name: '/guias' })).toBeDefined();
  });

  it.each([
    ['Envíos Xelajú', configuracionEnvios, rutasEnvios, 'Cotice y genere guías de envío a todo Guatemala desde su tienda en línea.', '/cotizaciones'],
    ['Agro Precios', {
      ...configuracionEnvios,
      nombrePortal: 'Agro Precios', colorPrincipal: '#2F7D4A',
      bienvenida: 'Consulte los precios del día en los mercados mayoristas de Guatemala.',
      nombreApi: 'API de Precios de Mercado', nombreOrganizacion: 'Agro Datos, S.A.',
    }, [{ ...rutasEnvios[0], metodo: 'GET', patron: '/precios', resumen: 'Consultar precios', pesoLlamadas: 3 }],
    'Consulte los precios del día en los mercados mayoristas de Guatemala.', '/precios'],
  ])('usa los datos y la marca de %s', async (_nombre, configuracion, rutas, bienvenida, patron) => {
    responder(configuracion, rutas);
    const { container } = montar('/');

    expect(await screen.findByRole('heading', { name: bienvenida })).toBeDefined();
    expect(screen.getAllByText(patron).length).toBeGreaterThan(0);
    expect(container.firstElementChild?.getAttribute('style')).toContain(`--marca-principal: ${configuracion.colorPrincipal}`);
  });
});
