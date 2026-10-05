/** Las iniciales de la insignia de la marca: la primera letra de las dos primeras palabras («Envíos Xelajú» → «EX»). */
export function iniciales(nombre: string) {
  return nombre.split(/\s+/).filter(Boolean).slice(0, 2).map(parte => parte[0]?.toUpperCase()).join('');
}
