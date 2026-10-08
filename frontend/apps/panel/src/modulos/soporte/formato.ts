const meses = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

export function fechaCorta(valor: string) {
  const partes = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'America/Guatemala', year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(new Date(valor));
  const dato = (tipo: Intl.DateTimeFormatPartTypes) => Number(partes.find(parte => parte.type === tipo)!.value);
  return `${dato('day')} ${meses[dato('month') - 1]} ${dato('year')}`;
}

export function fechaLarga(valor: string) {
  return new Intl.DateTimeFormat('es-GT', {
    timeZone: 'America/Guatemala', year: 'numeric', month: 'long', day: 'numeric',
  }).format(new Date(valor));
}

export function diaAnterior(valor: string) {
  const fecha = new Date(valor);
  fecha.setUTCDate(fecha.getUTCDate() - 1);
  return fecha.toISOString();
}

export function fechaHora(valor: string) {
  const fecha = new Date(valor);
  const hora = new Intl.DateTimeFormat('es-GT', {
    timeZone: 'America/Guatemala', hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
  }).format(fecha);
  return `${fechaCorta(valor)}, ${hora}`;
}
