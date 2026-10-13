import { crearCliente, ErrorApi } from '@shapi/api';
import type { components, paths } from '@shapi/api/claves';

export const clienteClaves = crearCliente<paths>(window.location.origin);
export type Clave = components['schemas']['Clave'];
export type ConsumidorConClaves = components['schemas']['ClavesDeConsumidorDeApi'];
export type SeleccionClave = { consumidor: ConsumidorConClaves; clave: Clave };
export const tipoClave = (clave: Clave) => clave.tipo === 'produccion' ? 'Producción' : 'Pruebas';

/** Fechas del mockup en la zona horaria de Guatemala, sin depender de la del navegador. */
export function fechaClave(fecha: string) {
  const partes = new Intl.DateTimeFormat('es-GT', { timeZone: 'America/Guatemala', day: 'numeric', month: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).formatToParts(new Date(fecha));
  const parte = (tipo: Intl.DateTimeFormatPartTypes) => partes.find(p => p.type === tipo)?.value ?? '';
  const meses = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
  return `${parte('day')} ${meses[Number(parte('month')) - 1]} ${parte('year')}, ${parte('hour')}:${parte('minute')}`;
}

export function mensajeClaves(error: unknown, alternativo: string) {
  return error instanceof ErrorApi && error.estado < 500 ? error.titulo : alternativo;
}
