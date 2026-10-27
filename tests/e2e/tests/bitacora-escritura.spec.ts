import { test, expect } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { entrar, consultar, contratarPlataforma, especificacion, panel, registrarProveedor, registrarConsumidor, sufijo, contrasena } from '../soporte/flujos';
import { esperarEnlaceCorreo } from '../soporte/mailpit';

// RF-41: se recorren por HTTP todas las acciones del catálogo que puede originar una persona.
// Cada intento tiene organización, cuentas, API y consumidor propios; no adelanta el reloj global.
test('RF-41 · todas las acciones HTTP guardan el nombre real y descripciones con punto final', async ({ browser, page }, info) => {
  test.setTimeout(420_000);
  const proveedor = await registrarProveedor(page);
  const contextos = await Promise.all(Array.from({ length: 3 }, () => browser.newContext({ ignoreHTTPSErrors: true })));
  const [admin, consumidor, invitado] = await Promise.all(contextos.map(c => c.newPage()));
  try {
    await entrar(admin, panel, 'rodrigo.alvarado@shapi.localhost');
    // Se excluyen las filas históricas de la siembra, que son append-only.
    const anteriores = await consultar<{ elementos: { id: number }[] }>(admin, '/api/admin/bitacora?tamano=100');
    const ultimoId = Math.max(0, ...anteriores.elementos.map(e => e.id));
    await contratarPlataforma(page);
    const planes = await consultar<{ id: string; nombre: string }[]>(page, '/api/planes-plataforma');
    await consultar(page, '/api/suscripcion/cambiar', 'POST', { planId: planes.find(p => p.nombre === 'Producto')!.id, usarRegistrada: true });

    const listaOrganizaciones = await consultar<{ id: string; nombre: string }[]>(admin, '/api/admin/organizaciones');
    const organizacionId = listaOrganizaciones.find(o => o.nombre === proveedor.organizacion)!.id;
    const subdominio = `bit-${proveedor.id}`.slice(0, 30);
    const api = await consultar<{ id: string }>(page, '/api/apis', 'POST', {
      nombre: 'API de Bitácora', urlOrigen: 'http://origen-envios:8080', subdominio,
    });
    await page.goto(`${panel}/panel/apis/${api.id}/especificacion`);
    await page.getByLabel('Elegir archivo OpenAPI').setInputFiles({ name: 'bitacora.json', mimeType: 'application/json', buffer: Buffer.from(especificacion) });
    await expect(page.getByRole('link', { name: 'Continuar a la selección de rutas' })).toBeVisible();
    const rutas = await consultar<{ elementos: { id: string }[] }>(page, `/api/apis/${api.id}/rutas`);
    const exposicion = `/api/apis/${api.id}/rutas/exposicion`;
    await consultar(page, exposicion, 'PUT', [{ rutaId: rutas.elementos[0].id, expuesta: true }]);
    await consultar(page, exposicion, 'PUT', [{ rutaId: rutas.elementos[0].id, expuesta: false }]);
    await consultar(page, exposicion, 'PUT', [{ rutaId: rutas.elementos[0].id, expuesta: true }]);

    const datosPlan = { nombre: 'Plan de bitácora', descripcion: 'Prueba de RF-41', precio: 0, esGratuito: true, vigenciaDias: 30, cuotaLlamadas: 1000, limiteMinuto: 60 };
    const plan = await consultar<{ id: string }>(page, `/api/apis/${api.id}/planes`, 'POST', datosPlan);
    await consultar(page, `/api/apis/${api.id}/planes/${plan.id}`, 'PUT', { ...datosPlan, descripcion: 'Plan editado' });
    await consultar(page, `/api/apis/${api.id}/planes/${plan.id}/desactivar`, 'POST');
    const planVigente = await consultar<{ id: string }>(page, `/api/apis/${api.id}/planes`, 'POST', { ...datosPlan, nombre: 'Plan vigente' });
    await consultar(page, `/api/apis/${api.id}/publicar`, 'POST');
    await consultar(page, `/api/apis/${api.id}/despublicar`, 'POST');
    await consultar(page, `/api/apis/${api.id}/publicar`, 'POST');
    await consultar(page, `/api/apis/${api.id}/dominio`, 'POST', { dominio: `${subdominio}.localhost` });
    await consultar(page, `/api/apis/${api.id}/dominio/simular-registro`, 'POST');
    await consultar(page, `/api/apis/${api.id}/dominio/verificar`, 'POST');
    await consultar(page, `/api/apis/${api.id}/secreto/regenerar`, 'POST');

    await registrarConsumidor(consumidor, `https://${subdominio}.shapi.localhost`);
    await consultar(consumidor, '/api/portal/suscripciones', 'POST', { planId: planVigente.id });
    const claves = await consultar<{ elementos: { id: string; tipo: string }[] }>(consumidor, '/api/portal/claves/');
    const produccion = claves.elementos.find(c => c.tipo === 'produccion')!;
    const pruebas = claves.elementos.find(c => c.tipo === 'pruebas')!;
    const rotada = await consultar<{ id: string }>(consumidor, `/api/portal/claves/${produccion.id}/rotar`, 'POST');
    await consultar(consumidor, `/api/portal/claves/${rotada.id}/revocar`, 'POST');
    await consultar(page, `/api/apis/${api.id}/claves/${pruebas.id}/revocar`, 'POST');

    const correoInvitado = `miembro-${sufijo()}@ejemplo.com`;
    await page.goto(`${panel}/panel/miembros`);
    await page.getByLabel('Correo', { exact: true }).fill(correoInvitado);
    await page.getByRole('button', { name: 'Enviar invitación' }).click();
    await expect(page.getByText('Acción confirmada', { exact: true })).toBeVisible();
    await page.screenshot({ path: info.outputPath('A4-2-invitacion-confirmada.png'), fullPage: true });
    const enlace = await esperarEnlaceCorreo(correoInvitado, { contiene: '/invitacion?token=' });
    await invitado.goto(enlace);
    await consultar(invitado, `/api/invitaciones/${encodeURIComponent(new URL(enlace).searchParams.get('token')!)}/aceptar`, 'POST', { nombre: 'Miembro de bitácora', contrasena });
    const miembros = await consultar<{ elementos: { id: string; correo: string }[] }>(page, '/api/miembros');
    const miembro = miembros.elementos.find(m => m.correo === correoInvitado)!;
    await consultar(page, `/api/miembros/${miembro.id}/rol`, 'PUT', { rol: 'lector' });
    await consultar(page, `/api/miembros/${miembro.id}`, 'DELETE');

    // RF-40: abrir por A7.1, responder y cerrar por A6.4b, con persistencia y correo reales.
    await page.goto(`${panel}/panel/soporte`);
    const asunto = `Caso de bitácora ${proveedor.id}`;
    await page.getByLabel('Asunto', { exact: true }).fill(asunto);
    await page.getByLabel('Descripción', { exact: true }).fill('Caso para verificar nombres y notificaciones.');
    await page.getByRole('button', { name: 'Abrir caso', exact: true }).click();
    const fila = page.getByRole('row').filter({ hasText: asunto });
    await expect(fila).toBeVisible();
    await page.screenshot({ path: info.outputPath('A7-1-caso-sin-recargar.png'), fullPage: true });
    const casos = await consultar<{ elementos: { numero: number; asunto: string }[] }>(page, '/api/casos');
    const numero = casos.elementos.find(c => c.asunto === asunto)!.numero;
    await admin.goto(`${panel}/admin/casos/${numero}`);
    await admin.getByRole('button', { name: 'Asignarme el caso' }).click();
    await admin.getByLabel('Respuesta', { exact: true }).fill('Respuesta del administrador.');
    await admin.getByRole('button', { name: 'Enviar respuesta', exact: true }).click();
    await expect(admin.getByText('Respuesta del administrador.', { exact: true })).toBeVisible();
    await admin.screenshot({ path: info.outputPath('A6-4b-respuesta.png'), fullPage: true });
    await consultar(page, `/api/casos/${numero}/mensajes`, 'POST', { cuerpo: 'Respuesta del proveedor.' });
    await admin.getByRole('button', { name: 'Cerrar el caso' }).click();
    await expect(admin.getByText('Cerrado', { exact: true })).toBeVisible();
    await page.goto(`${panel}/panel/soporte/${numero}`);
    await expect(page.getByText('Respuesta del administrador.', { exact: true })).toBeVisible();
    await page.screenshot({ path: info.outputPath('A7-2-conversacion-cerrada.png'), fullPage: true });
    const correo = await esperarEnlaceCorreo(proveedor.correo, { contiene: `/panel/soporte/${numero}` });
    expect(correo).toBe(`${panel}/panel/soporte/${numero}`);

    const datosPlataforma = { nombre: `Plan ${proveedor.id}`, descripcion: 'RF-41', precio: 19, vigenciaDias: 30, maxApis: 2, maxMiembros: 2, cuotaPeticiones: 1000, dominioPropio: false };
    const planPlataforma = await consultar<{ id: string }>(admin, '/api/admin/planes-plataforma', 'POST', datosPlataforma);
    await consultar(admin, `/api/admin/planes-plataforma/${planPlataforma.id}`, 'PUT', { ...datosPlataforma, precio: 20 });
    await consultar(admin, `/api/admin/planes-plataforma/${planPlataforma.id}/desactivar`, 'POST');
    const cuenta = await consultar<{ id: string }>(admin, '/api/admin/cuentas', 'POST', { nombre: 'Personal de bitácora', correo: `soporte-${sufijo()}@ejemplo.com`, rol: 'soporte' });
    await consultar(admin, `/api/admin/cuentas/${cuenta.id}/desactivar`, 'POST');
    await consultar(admin, `/api/admin/cuentas/${cuenta.id}/activar`, 'POST');
    await consultar(admin, `/api/admin/organizaciones/${organizacionId}/suspender`, 'POST', { motivo: 'Prueba RF-41' });
    await consultar(admin, `/api/admin/organizaciones/${organizacionId}/reactivar`, 'POST');
    const pagos = await consultar<{ elementos: { id: string; estado: string }[] }>(page, '/api/pagos');
    await consultar(admin, `/api/admin/pagos/${pagos.elementos.find(p => p.estado === 'autorizado')!.id}/revertir`, 'POST');

    interface Entrada { id: number; accion: string; descripcion: string; actor: { nombre: string; rol: string } }
    const entradas: Entrada[] = [];
    for (let pagina = 1; ; pagina++) {
      const resultado = await consultar<{ elementos: Entrada[]; total: number }>(admin, `/api/admin/bitacora?tamano=100&pagina=${pagina}`);
      entradas.push(...resultado.elementos.filter(e => e.id > ultimoId));
      if (pagina * 100 >= resultado.total) break;
    }
    // El catálogo se lee de la especificación para detectar una acción nueva sin cobertura.
    const tabla = readFileSync(new URL('../../../docs/specs/10-identidad-y-seguridad.md', import.meta.url), 'utf8').split('## 7. Bitácora')[1].split('## 8. Amenazas')[0];
    const catalogo = tabla.split('\n').filter(l => l.startsWith('| `')).flatMap(l => {
      let entidad = '';
      return [...l.split('|')[1].matchAll(/`([a-z_]*\.[a-z_]+)`/g)].map(m => {
        if (m[1].startsWith('.')) return entidad + m[1];
        entidad = m[1].split('.')[0];
        return m[1];
      });
    }).filter(a => a !== 'suscripcion.suspendida');
    expect([...new Set(entradas.map(e => e.accion))].sort()).toEqual(catalogo.sort());
    for (const entrada of entradas) {
      // El trabajador puede verificar el DNS entre simular-registro y la petición manual.
      if (entrada.actor.rol === 'sistema') {
        expect(entrada.accion).toBe('dominio.verificado');
        expect(entrada.actor.nombre).toBe('Sistema');
        expect(entrada.descripcion).toMatch(/[^.]\.$/);
        continue;
      }
      const esperado = /^(organizacion|pago|plan_plataforma|cuenta_plataforma)\./.test(entrada.accion) || entrada.accion === 'caso.cerrado'
        ? 'Rodrigo Alvarado' : ['clave.rotada', 'clave.revocada_por_consumidor'].includes(entrada.accion) ? 'Consumidora E2E' : 'Proveedor E2E';
      expect(entrada.actor.nombre, entrada.accion).toBe(esperado);
      expect(entrada.descripcion, entrada.accion).toMatch(/[^.]\.$/);
      expect(entrada.descripcion).not.toContain('API API');
    }
    await admin.goto(`${panel}/admin/bitacora`);
    await expect(admin.getByRole('heading', { name: 'Bitácora de acciones sensibles', exact: true })).toBeVisible();
    await admin.screenshot({ path: info.outputPath('B3-2-nombres-y-puntos.png'), fullPage: true });
  } finally {
    await Promise.all(contextos.map(c => c.close()));
  }
});
