#!/usr/bin/env node
// JG-16. Markdown de las especificaciones y los manuales -> HTML -> PDF, sin LaTeX.
// Reutiliza React/React Markdown del frontend y Playwright de tests/e2e/.
import { existsSync, realpathSync, readFileSync, writeFileSync } from "node:fs";
import { readFile, writeFile, mkdir, mkdtemp, rm } from "node:fs/promises";
import { dirname, basename, resolve, join, delimiter, relative } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { createRequire } from "node:module";
import { spawn } from "node:child_process";

const RAIZ = fileURLToPath(new URL("../", import.meta.url));
const MERMAID = "@mermaid-js/mermaid-cli@11.12.0";
const escapar = (texto) => String(texto).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
const slug = (texto) => texto.normalize("NFD").replace(/\p{M}/gu, "").toLowerCase().replace(/[^\p{L}\p{N}\s_-]/gu, "").replace(/\s/g, "-");

export function integrantesDesdeVision(vision) {
  const integrantes = [...vision.matchAll(/^\|\s*([^|]+?)\s*\|\s*(\d{7})\s*\|/gm)]
    .map(([, nombre, carne]) => ({ nombre: nombre.trim(), carne }));
  if (integrantes.length !== 4) throw new Error("La visión debe identificar a los cuatro integrantes con su carné.");
  return integrantes;
}

export function tituloDesdeSalida(salida) {
  const nombre = basename(salida, ".pdf").replaceAll("_", " ");
  return ({ "documento de requisitos": "Documento de Requisitos", "documento de diseno": "Documento de Diseño",
    "manual tecnico": "Manual Técnico", "manual de usuario": "Manual de Usuario" })[slug(nombre).replaceAll("-", " ")] ?? nombre;
}

// Las tablas de las fuentes usan la sintaxis con barras. El resto del Markdown lo procesa
// React Markdown. Así no se añade otro motor ni una dependencia fuera del stack autorizado.
function celdas(linea) {
  return linea.trim().replace(/^\|/, "").replace(/(?<!\\)\|$/, "").split(/(?<!\\)\|/).map((celda) => celda.trim().replaceAll("\\|", "|"));
}

export function separarTablas(texto) {
  const lineas = texto.split("\n");
  const bloques = [];
  let acumulado = [];
  let cerca = null;
  const guardar = () => { if (acumulado.length) bloques.push({ tipo: "markdown", texto: acumulado.join("\n") }); acumulado = []; };
  for (let i = 0; i < lineas.length; i++) {
    const marca = lineas[i].match(/^ {0,3}(`{3,}|~{3,})/);
    if (marca && (!cerca || (marca[1][0] === cerca[0] && marca[1].length >= cerca.length))) cerca = cerca ? null : marca[1];
    if (!cerca && lineas[i].trim().startsWith("|") && /^\|?\s*:?-{3,}:?\s*(?:\|\s*:?-{3,}:?\s*)+\|?$/.test(lineas[i + 1]?.trim() ?? "")) {
      guardar();
      const filas = [celdas(lineas[i])];
      i += 2;
      while (i < lineas.length && lineas[i].trim().startsWith("|")) filas.push(celdas(lineas[i++]));
      i--;
      bloques.push({ tipo: "tabla", filas });
    } else acumulado.push(lineas[i]);
  }
  guardar();
  return bloques;
}

export function opcionesPdf(titulo) {
  const estilo = "font-family:Arial,sans-serif;font-size:8px;color:#52616b;width:100%;margin:0 20mm;";
  return {
    preferCSSPageSize: true, printBackground: true, displayHeaderFooter: true, tagged: true, outline: true,
    headerTemplate: `<div style="${estilo}border-bottom:1px solid #d9e1e6;padding-bottom:5px;">Shapi · ${escapar(titulo)}</div>`,
    footerTemplate: `<div style="${estilo}text-align:right;">Ingeniería de Software I · Página <span class="pageNumber"></span> de <span class="totalPages"></span></div>`,
  };
}

function npxCli() {
  for (const carpeta of [dirname(process.execPath), ...(process.env.PATH ?? "").split(delimiter)]) {
    const instalado = join(carpeta, "node_modules/npm/bin/npx-cli.js");
    if (existsSync(instalado)) return instalado;
    const enlace = join(carpeta, "npx");
    if (existsSync(enlace) && realpathSync(enlace).endsWith(".js")) return realpathSync(enlace);
  }
  throw new Error("No se encontró npx. Instale Node.js 24 con npm.");
}

function ejecutar(programa, args, opciones) {
  return new Promise((aceptar, rechazar) => {
    // Sin shell: los nombres de archivo con espacios o metacaracteres son argumentos literales.
    const hijo = spawn(programa, args, { ...opciones, stdio: ["ignore", "pipe", "pipe"], windowsHide: true });
    let registro = "";
    hijo.stdout.on("data", (datos) => { registro = (registro + datos).slice(-12000); });
    hijo.stderr.on("data", (datos) => { registro = (registro + datos).slice(-12000); });
    hijo.on("error", rechazar);
    hijo.on("close", (codigo) => codigo === 0 ? aceptar() : rechazar(new Error(`Mermaid CLI terminó con código ${codigo}:\n${registro}`)));
  });
}

async function dependencias() {
  try {
    const frontend = createRequire(join(RAIZ, "frontend/package.json"));
    const e2e = createRequire(join(RAIZ, "tests/e2e/package.json"));
    const [react, servidor, markdown] = await Promise.all([
      import(pathToFileURL(frontend.resolve("react")).href),
      import(pathToFileURL(frontend.resolve("react-dom/server")).href),
      import(pathToFileURL(frontend.resolve("react-markdown")).href),
    ]);
    return { h: react.createElement, Fragment: react.Fragment, render: servidor.renderToStaticMarkup,
      Markdown: markdown.default, urlSegura: markdown.defaultUrlTransform, chromium: e2e("@playwright/test").chromium };
  } catch (error) {
    throw new Error("Instale las dependencias existentes: pnpm install --frozen-lockfile en frontend/ y tests/e2e/. Después, en tests/e2e/: pnpm exec playwright install chromium.", { cause: error });
  }
}

async function convertirMermaid(documentos, temporal, chromium) {
  const patron = /^```mermaid[^\S\n]*\n([\s\S]*?)^```[^\S\n]*$/gm;
  const bloques = documentos.flatMap((d) => [...d.texto.matchAll(patron)]);
  const total = bloques.length;
  if (!total) return documentos;
  await writeFile(join(temporal, "puppeteer.json"), JSON.stringify({ executablePath: chromium.executablePath() }));
  await writeFile(join(temporal, "mermaid.json"), JSON.stringify({ theme: "neutral", securityLevel: "strict", htmlLabels: false,
    flowchart: { htmlLabels: false }, themeVariables: { fontFamily: "Arial, sans-serif", fontSize: "18px" }, maxTextSize: 200000 }));
  console.log(`Convirtiendo diagramas con ${MERMAID}…`);
  for (let inicio = 0; inicio < total; inicio += 2) {
    const lote = bloques.slice(inicio, inicio + 2).map((bloque) => bloque[0]).join("\n\n");
    await writeFile(join(temporal, "lote.md"), lote);
    await ejecutar(process.execPath, [npxCli(), "--yes", MERMAID, "-i", "lote.md", "-o", `diagramas-${inicio}.md`, "-p", "puppeteer.json", "-c", "mermaid.json", "-b", "transparent"],
      { cwd: temporal, env: { ...process.env, PUPPETEER_SKIP_DOWNLOAD: "true", npm_config_fetch_retries: "0", npm_config_fetch_timeout: "15000" } });
    console.log(`Diagramas ${inicio + 1}–${Math.min(inicio + 2, total)}/${total} convertidos.`);
  }
  const convertidos = [];
  let numero = 0;
  // mmdc procesa los bloques de un Markdown con Promise.all. Lotes de dos evitan
  // abrir decenas de páginas a la vez y amortizan el arranque de Chromium.
  for (const documento of documentos) {
    let texto = "";
    let posicion = 0;
    for (const bloque of documento.texto.matchAll(patron)) {
      const lote = Math.floor(numero / 2) * 2;
      const posicionEnLote = numero % 2 + 1;
      numero++;
      texto += documento.texto.slice(posicion, bloque.index) + `![Diagrama ${numero}](./diagramas-${lote}-${posicionEnLote}.svg)`;
      posicion = bloque.index + bloque[0].length;
    }
    convertidos.push({ ...documento, texto: texto + documento.texto.slice(posicion) });
  }
  return convertidos;
}

async function crearHtml(documentos, temporal, salida, fecha, deps) {
  const { h, Fragment, render, Markdown, urlSegura } = deps;
  const titulo = tituloDesdeSalida(salida);
  const integrantes = integrantesDesdeVision(await readFile(join(RAIZ, "docs/specs/01-vision-y-alcance.md"), "utf8"));
  const css = await readFile(join(RAIZ, "docs/pdf/plantilla.css"), "utf8");
  const indice = [];
  let diagramas = 0;
  const textoNodo = (nodo) => Array.isArray(nodo) ? nodo.map(textoNodo).join("") : typeof nodo === "object" && nodo !== null ? textoNodo(nodo.props?.children) : nodo ?? "";
  const capitulos = [];
  for (const [i, documento] of documentos.entries()) {
    const namespace = `capitulo-${i}`;
    // Las referencias de Markdown pertenecen al documento, también cuando una
    // tabla se renderiza por celdas. Las fuentes usan definiciones de una línea.
    const referencias = (documento.texto.match(/^ {0,3}\[[^\]\n]+\]:[^\n]+$/gm) ?? []).join("\n");
    const usados = new Map();
    const componentes = { p: ({ children }) => h("div", { className: "parrafo" }, children) };
    for (let nivel = 1; nivel <= 6; nivel++) componentes[`h${nivel}`] = ({ children }) => {
      const texto = String(textoNodo(children));
      const base = slug(texto);
      const repetido = usados.get(base) ?? 0;
      usados.set(base, repetido + 1);
      const id = `${namespace}-${base}${repetido ? `-${repetido}` : ""}`;
      if (nivel <= 2) indice.push({ id, texto, nivel });
      return h(`h${nivel}`, { id }, children);
    };
    componentes.a = ({ href, children }) => {
      if (href?.startsWith("#shapi-ancla-")) return h("span", { id: `${namespace}-${slug(href.slice("#shapi-ancla-".length))}` });
      if (href && !/^[a-z][\w+.-]*:/i.test(href)) {
        const [ruta, ancla] = href.split("#");
        const destino = ruta ? resolve(dirname(documento.archivo), decodeURIComponent(ruta)) : documento.archivo;
        const capitulo = documentos.findIndex((d) => d.archivo === destino);
        href = capitulo >= 0 ? `#capitulo-${capitulo}${ancla ? `-${slug(decodeURIComponent(ancla))}` : ""}` :
          `https://github.com/jordin-garcia/shapi/blob/main/${relative(RAIZ, destino).split("\\").join("/")}${ancla ? `#${ancla}` : ""}`;
      }
      return h("a", { href }, children);
    };
    componentes.img = ({ src, alt }) => {
      if (!src || /^[a-z][\w+.-]*:/i.test(src)) throw new Error(`La imagen debe ser local: ${src}`);
      const esDiagrama = /^\.\/diagramas-\d+-\d+\.svg$/.test(src);
      if (esDiagrama) diagramas++;
      const archivo = resolve(esDiagrama ? temporal : dirname(documento.archivo), decodeURIComponent(src));
      if (!existsSync(archivo)) throw new Error(`No existe la imagen ${archivo}`);
      const svg = esDiagrama ? readFileSync(archivo, "utf8") : null;
      const caja = svg?.match(/viewBox="([^\"]+)"/)?.[1].split(/\s+/).map(Number);
      const imagen = (ruta, ancho, alto) => h("img", { src: pathToFileURL(ruta).href, alt: alt ?? "", width: ancho, height: alto,
        "data-ancho": ancho, "data-alto": alto });
      if (!esDiagrama) return h("span", { className: "imagen" }, imagen(archivo));
      const figuras = [h("figure", { className: "diagrama" }, h("figcaption", null, `Diagrama ${diagramas} · Vista general`), imagen(archivo, caja?.[2], caja?.[3]))];
      if (caja && Math.min(1450 / caja[2], 830 / caja[3]) < 0.60) {
        // Además de la vista general vectorial, detalles con solapamiento para
        // que los UML y MER extensos se puedan leer también al imprimirlos.
        const ancho = Math.min(1800, caja[2]);
        const alto = Math.min(1000, caja[3]);
        const columnas = Math.ceil((caja[2] - 100) / (ancho - 100));
        const filas = Math.ceil((caja[3] - 100) / (alto - 100));
        for (let fila = 0; fila < filas; fila++) for (let col = 0; col < columnas; col++) {
          const x = caja[0] + Math.min(col * (ancho - 100), caja[2] - ancho);
          const y = caja[1] + Math.min(fila * (alto - 100), caja[3] - alto);
          const detalle = archivo.replace(/\.svg$/, `-detalle-${fila}-${col}.svg`);
          writeFileSync(detalle, svg.replace(/viewBox="[^\"]+"/, `viewBox="${x} ${y} ${ancho} ${alto}"`));
          figuras.push(h("figure", { className: "diagrama grande detalle" },
            h("figcaption", null, `Diagrama ${diagramas} · Detalle ${fila * columnas + col + 1} de ${filas * columnas} · Fila ${fila + 1}, columna ${col + 1}`),
            imagen(detalle, ancho, alto)));
        }
      }
      return h(Fragment, null, ...figuras);
    };
    const md = (texto, enCelda = false) => h(Markdown, {
      components: enCelda ? { ...componentes, p: ({ children }) => h(Fragment, null, children) } : componentes,
      urlTransform: (url) => urlSegura(url),
    }, `${texto.replace(/<a\s+id=(["'])([\w-]+)\1\s*><\/a>/g, (_, _comilla, id) => `[ ](#shapi-ancla-${id})`)}\n\n${referencias}`);
    const bloques = separarTablas(documento.texto).map((bloque) => {
      if (bloque.tipo === "markdown") return md(bloque.texto);
      const pesos = bloque.filas[0].map((cabecera, col) => Math.max(2, Math.sqrt(cabecera.length) * 1.25, Math.min(10,
        Math.sqrt(bloque.filas.reduce((suma, fila) => suma + (fila[col] ?? "").replace(/<[^>]*>/g, "").length, 0) / bloque.filas.length))));
      const total = pesos.reduce((suma, peso) => suma + peso, 0);
      return h("table", null, h("colgroup", null, ...pesos.map((peso) => h("col", { style: { width: `${peso / total * 100}%` } }))),
        h("thead", null, h("tr", null, ...bloque.filas[0].map((celda) => h("th", null, md(celda, true))))),
        h("tbody", null, ...bloque.filas.slice(1).map((fila) => h("tr", null, ...bloque.filas[0].map((_, col) => h("td", null, md(fila[col] ?? "", true)))))));
    });
    capitulos.push(`<section class="capitulo" id="${namespace}">${render(h(Fragment, null, ...bloques))}</section>`);
  }
  const fechaTexto = new Intl.DateTimeFormat("es-GT", { dateStyle: "long", timeZone: "America/Guatemala" }).format(fecha);
  return { titulo, diagramas, html: `<!doctype html><html lang="es"><head><meta charset="utf-8"><title>${escapar(titulo)} · Shapi</title><style>${css}</style></head><body>
    <section class="caratula"><div class="universidad">Universidad Rafael Landívar</div><div>Facultad de Ingeniería</div><div>Ingeniería en Informática y Sistemas</div>
    <div class="curso">Ingeniería de Software I · Proyecto final 2026</div><div class="marca">Shapi</div><h1>${escapar(titulo)}</h1><p>Plataforma como servicio para publicar APIs, controlar su uso y cobrar por ellas</p>
    <div class="integrantes">${integrantes.map(({ nombre, carne }) => `<div class="integrante"><span>${escapar(nombre)}</span><span>${carne}</span></div>`).join("")}</div>
    <div class="lugar">Quetzaltenango, Guatemala<br>${escapar(fechaTexto)}</div></section>
    <nav class="indice" aria-label="Índice"><h1>Índice</h1><p>Seleccione un capítulo o sección para ir a su contenido. El PDF también incluye marcadores de navegación.</p><ol>${indice.map((entrada) => `<li class="nivel-${entrada.nivel}"><a href="#${entrada.id}">${escapar(entrada.texto)}</a></li>`).join("")}</ol></nav>
    <main>${capitulos.join("\n")}</main></body></html>` };
}

/** Genera el archivo final solo después de cargar todas las fuentes e imágenes. */
export async function generarPdf(salida, archivos, { conservarHtml = false, fecha = new Date() } = {}) {
  if (!salida || !/\.pdf$/i.test(salida) || !archivos.length) throw new Error("Uso: node scripts/generar-pdf.mjs <salida.pdf> <archivo1.md> [archivo2.md ...]");
  const destino = resolve(salida);
  if (archivos.some((archivo) => resolve(archivo) === destino)) throw new Error("La salida no puede sobrescribir una fuente.");
  const documentos = await Promise.all(archivos.map(async (archivo) => ({ archivo: resolve(archivo), texto: (await readFile(archivo, "utf8")).replaceAll("\r\n", "\n") })));
  const deps = await dependencias();
  await mkdir(join(RAIZ, ".shapi/pdf"), { recursive: true });
  const temporal = await mkdtemp(join(RAIZ, ".shapi/pdf/generacion-"));
  let navegador;
  let completado = false;
  try {
    const convertidos = await convertirMermaid(documentos, temporal, deps.chromium);
    const documento = await crearHtml(convertidos, temporal, salida, fecha, deps);
    const urlHtml = pathToFileURL(join(temporal, "documento.html")).href;
    await writeFile(join(temporal, "documento.html"), documento.html);
    navegador = await deps.chromium.launch();
    const pagina = await navegador.newPage({ javaScriptEnabled: false });
    await pagina.route(/^https?:/, (ruta) => ruta.abort());
    await pagina.goto(urlHtml, { waitUntil: "load" });
    await pagina.emulateMedia({ media: "print" });
    const imagenes = await pagina.locator("img").evaluateAll((imgs) => imgs.map((img) => ({ ok: img.complete && img.naturalWidth > 0, src: img.src })));
    if (imagenes.some((img) => !img.ok)) throw new Error(`No se pudo cargar una imagen: ${imagenes.find((img) => !img.ok).src}`);
    await pagina.locator(".diagrama").evaluateAll((figuras) => figuras.forEach((figura) => {
      const img = figura.querySelector("img");
      if (Number(img.dataset.ancho) > 1050 || Number(img.dataset.alto) > 800) {
        figura.classList.add("grande");
        const anterior = figura.parentElement.previousElementSibling;
        if (anterior && /^H[1-6]$/.test(anterior.tagName)) figura.prepend(anterior);
      }
    }));
    // Guardar también la clasificación de los diagramas en el HTML de revisión.
    await writeFile(join(temporal, "documento.html"), `<!doctype html>\n${await pagina.content()}`);
    await mkdir(dirname(destino), { recursive: true });
    const pdf = await pagina.pdf(opcionesPdf(documento.titulo));
    await writeFile(destino, pdf);
    completado = true;
    console.log(`PDF generado: ${salida} · ${archivos.length} fuentes · ${documento.diagramas} diagramas SVG · ${(pdf.length / 1024 / 1024).toFixed(2)} MB`);
    return { ...documento, temporal, urlHtml };
  } finally {
    await navegador?.close();
    if (!conservarHtml || !completado) await rm(temporal, { recursive: true, force: true });
  }
}

if (import.meta.main) {
  const [salida, ...archivos] = process.argv.slice(2);
  generarPdf(salida, archivos).catch((error) => { console.error(error.message); process.exitCode = 1; });
}
