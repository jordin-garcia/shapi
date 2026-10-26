// JG-16: pruebas del contrato del generador, sin navegador ni dependencias instaladas.
import { test } from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { integrantesDesdeVision, tituloDesdeSalida, separarTablas, opcionesPdf, generarPdf } from "./generar-pdf.mjs";

test("JG-16 CA2: la carátula toma los cuatro integrantes y carnés de la fuente de verdad", async () => {
  const vision = await readFile(new URL("../docs/specs/01-vision-y-alcance.md", import.meta.url), "utf8");
  const integrantes = integrantesDesdeVision(vision);
  assert.equal(integrantes.length, 4);
  assert.deepEqual(integrantes[0], { nombre: "Héctor Jordin Adolfo García Coyoy", carne: "2427124" });
  assert.equal(integrantes.at(-1).carne, "1507524");
  assert.throws(() => integrantesDesdeVision("Sin integrantes"), /integrantes/);
});

test("JG-16 CA1 y CA3: el mismo comando admite documentos y manuales, incluso rutas con espacios", () => {
  assert.equal(tituloDesdeSalida("ruta con espacios/Documento_de_Requisitos.pdf"), "Documento de Requisitos");
  assert.equal(tituloDesdeSalida("Documento_de_Diseno.pdf"), "Documento de Diseño");
  assert.equal(tituloDesdeSalida("Manual_Tecnico.pdf"), "Manual Técnico");
  assert.equal(tituloDesdeSalida("Manual_de_Usuario.pdf"), "Manual de Usuario");
});

test("JG-16 CA4: las tablas preservan formato en línea, celdas vacías y barras escapadas", () => {
  const bloques = separarTablas("Antes\n\n| Nombre | Detalle |\n|---|---|\n| **API** | `a\\|b` |\n| Vacío | |\n\nDespués");
  const tabla = bloques.find((bloque) => bloque.tipo === "tabla");
  assert.deepEqual(tabla.filas, [["Nombre", "Detalle"], ["**API**", "`a|b`"], ["Vacío", ""]]);
  assert.ok(bloques[0].texto.includes("Antes"));
  assert.ok(bloques.at(-1).texto.includes("Después"));
});

test("JG-16 CA1: los bloques de código con tablas siguen siendo código", () => {
  const texto = "```text\n| a | b |\n|---|---|\n| 1 | 2 |\n```";
  assert.deepEqual(separarTablas(texto), [{ tipo: "markdown", texto }]);
});

test("JG-16 CA2 y CA4: el PDF usa la paginación de Chromium, encabezado y pie con números", () => {
  const opciones = opcionesPdf("Documento de Diseño");
  assert.equal(opciones.preferCSSPageSize, true);
  assert.equal(opciones.printBackground, true);
  assert.equal(opciones.displayHeaderFooter, true);
  assert.match(opciones.headerTemplate, /Documento de Diseño/);
  assert.match(opciones.footerTemplate, /class="pageNumber"/);
  assert.match(opciones.footerTemplate, /class="totalPages"/);
  assert.equal(opciones.tagged, true);
  assert.equal(opciones.outline, true);
});

test("JG-16 CA1: los argumentos incorrectos o una fuente ausente fallan antes de generar", async () => {
  await assert.rejects(generarPdf("salida.html", ["fuente.md"]), /Uso:/);
  await assert.rejects(generarPdf("salida.pdf", []), /Uso:/);
  await assert.rejects(generarPdf("salida.pdf", ["salida.pdf"]), /sobrescribir/);
  await assert.rejects(generarPdf("salida.pdf", ["fuente-que-no-existe.md"]), /ENOENT/);
});
