// Acciones del navegador, en un objeto para poder sustituirlas en las pruebas (jsdom no recarga la página).
export const pagina = {
  recargar: () => window.location.reload(),
};
