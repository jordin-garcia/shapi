// JG-16: integración real. Requiere las instalaciones de frontend/ y tests/e2e/ y Chromium.
import { test } from "node:test";
import assert from "node:assert/strict";
import { mkdtemp, readFile, writeFile, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import { generarPdf } from "../generar-pdf.mjs";

test("JG-16 CA1-4: Markdown, Mermaid SVG, índice, tablas y paginación producen un PDF legible", { timeout: 240_000 }, async () => {
  const temporal = await mkdtemp(join(tmpdir(), "shapi pdf prueba "));
  try {
    const entrada = join(temporal, "entrada con espacios.md");
    const salida = join(temporal, "Manual_Tecnico.pdf");
    await writeFile(entrada, "# Manual técnico\n\n## Instalación\n\n**Texto en negrita** y <script>throw 1</script>.\n\n| Requisito | Resultado |\n|---|---|\n" +
      Array.from({ length: 90 }, (_, i) => `| RF-${i} | Fila ${i} completa, sin recortes |`).join("\n") +
      "\n\n## Diagrama\n\n```mermaid\nflowchart LR\n A[Proveedor] --> B[Compuerta]\n```\n\n## Fin\n\nÚltima sección.\n");
    const resultado = await generarPdf(salida, [entrada], { conservarHtml: true, fecha: new Date("2026-10-25T18:00:00Z") });
    const pdf = await readFile(salida);
    assert.equal(pdf.subarray(0, 5).toString(), "%PDF-");
    assert.ok(pdf.length > 15_000);
    assert.ok((pdf.toString("latin1").match(/\/Type \/Page\b/g) ?? []).length >= 4, "La tabla debe paginarse en el PDF real");
    assert.match(pdf.toString("latin1"), /\/Outlines/);
    const requireE2e = createRequire(new URL("../../tests/e2e/package.json", import.meta.url));
    const navegador = await requireE2e("@playwright/test").chromium.launch();
    try {
      const pagina = await navegador.newPage();
      await pagina.goto(resultado.urlHtml);
      await pagina.emulateMedia({ media: "print" });
      assert.match(await pagina.title(), /Manual Técnico/);
      assert.equal(await pagina.locator(".caratula .integrante").count(), 4);
      assert.match(await pagina.locator(".caratula").innerText(), /Universidad Rafael Landívar/);
      assert.match(await pagina.locator(".caratula").innerText(), /Quetzaltenango/);
      assert.match(await pagina.locator(".caratula").innerText(), /25 de octubre de 2026/);
      assert.equal(await pagina.locator("tbody tr").count(), 90);
      assert.equal(await pagina.locator("thead").count(), 1);
      assert.equal(await pagina.locator(".diagrama img").count(), 1);
      assert.equal(await pagina.locator("script").count(), 0);
      assert.equal(await pagina.locator(".indice a").count(), 4);
      assert.equal(await pagina.locator(".indice a").evaluateAll((links) => links.every((a) => document.getElementById(a.hash.slice(1)) !== null)), true);
      assert.equal(await pagina.locator("img").evaluateAll((imgs) => imgs.every((img) => img.complete && img.naturalWidth > 0)), true);
      assert.equal(await pagina.locator("table").evaluateAll((tablas) => tablas.every((t) => t.scrollWidth <= t.clientWidth + 1)), true);
      assert.equal(await pagina.locator("tr").first().evaluate((fila) => getComputedStyle(fila).breakInside), "avoid");
      assert.equal(await pagina.locator("thead").evaluate((cabecera) => getComputedStyle(cabecera).display), "table-header-group");
    } finally {
      await navegador.close();
      await rm(resultado.temporal, { recursive: true, force: true });
    }
  } finally {
    await rm(temporal, { recursive: true, force: true });
  }
});

test("JG-16 CA3: los entregables incluyen las doce especificaciones completas y sus diagramas", { timeout: 240_000 }, async () => {
  const raiz = fileURLToPath(new URL("../../", import.meta.url));
  const temporal = await mkdtemp(join(tmpdir(), "shapi-entregables-"));
  const grupos = [
    { salida: "Documento_de_Requisitos.pdf", fuentes: ["01-vision-y-alcance", "02-glosario", "03-requisitos", "04-roles-y-permisos", "05-casos-de-uso"], contenidos: ["RF-47", "RNF-15", "CU-26", "los reintenta hasta 5 veces"] },
    { salida: "Documento_de_Diseno.pdf", fuentes: ["06-arquitectura", "07-modelo-de-datos", "08-compuerta", "09-cobros-y-suscripciones", "10-identidad-y-seguridad", "11-interfaz", "12-decisiones"], contenidos: ["ADR-35", "Modelo de datos", "cada integrante instala el SDK de .NET 10"] },
  ];
  try {
    for (const grupo of grupos) {
      const archivos = grupo.fuentes.map((nombre) => join(raiz, "docs/specs", `${nombre}.md`));
      const fuentes = await Promise.all(archivos.map((archivo) => readFile(archivo, "utf8")));
      const resultado = await generarPdf(join(temporal, grupo.salida), archivos);
      assert.equal((resultado.html.match(/class="capitulo"/g) ?? []).length, archivos.length);
      assert.equal(resultado.diagramas, fuentes.reduce((total, texto) => total + (texto.match(/^```mermaid/gm) ?? []).length, 0));
      for (const fuente of fuentes) {
        assert.ok(resultado.html.includes(fuente.match(/^# (.+)$/m)[1]), "Falta el título de una especificación");
      }
      for (const contenido of grupo.contenidos) assert.ok(resultado.html.includes(contenido), `Falta contenido obligatorio de ${grupo.salida}: ${contenido}`);
      const pdf = await readFile(join(temporal, grupo.salida));
      assert.equal(pdf.subarray(0, 5).toString(), "%PDF-");
      assert.ok(pdf.length > 100_000);
    }
  } finally {
    await rm(temporal, { recursive: true, force: true });
  }
});
