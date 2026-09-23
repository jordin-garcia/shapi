"""Regenera los paquetes autocontenidos de mockups/ (*.html de la raíz)
a partir de las fuentes editables de cada tanda (<Tanda>/*.dc.html y canvas.json).

Uso:  python mockups/herramientas/empaquetar.py

Cada paquete es una aplicación de visualización que incrusta, como JSON en el
elemento <script id="appifact-doc">, el contenido de su carpeta. Este script
reemplaza solo ese JSON; el visor (React y el lienzo) se conserva intacto.
Los paquetes nuevos se crean copiando el visor de PLANTILLA.
"""
import json
import os
import re
import sys

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PLANTILLA = "a1-acceso-proveedor.html"

# paquete -> (carpeta, título)
PAQUETES = {
    "a0-inicio.html": ("A0", "Shapi A0 · Sitio público (variante 4 oficial)"),
    "a1-acceso-proveedor.html": ("A1", "Shapi A1 · Acceso del proveedor"),
    "a2-contratacion-plan.html": ("A2", "Shapi A2 · Contratación del plan de plataforma"),
    "a3-publicacion-api.html": ("A3", "Shapi A3 · Publicación de una API"),
    "a4-planes-claves-equipo.html": ("A4", "Shapi A4 · Planes, claves y equipo"),
    "a5-portal-marca-blanca.html": ("A5", "Shapi A5 · Portal de marca blanca"),
    "a6-administracion-soporte.html": ("A6", "Shapi A6 · Administración y soporte"),
    "a7-soporte-proveedor.html": ("A7", "Shapi A7 · Soporte del proveedor"),
    "a8-cuenta.html": ("A8", "Shapi A8 · Cuenta del personal"),
    "b1-salidas-proveedor.html": ("B1", "Shapi B1 · Salidas del proveedor"),
    "b2-salidas-consumidor.html": ("B2", "Shapi B2 · Salidas del consumidor"),
    "b3-salidas-administracion-soporte.html": ("B3", "Shapi B3 · Salidas del administrador y del soporte"),
    "navegacion-proveedor.html": ("Navegacion", "Shapi · Barra lateral del proveedor"),
}

PATRON = re.compile(r'(<script type="application/json" id="appifact-doc">)(.*?)(</script>)', re.S)


def leer(ruta):
    with open(ruta, encoding="utf-8", newline="") as f:
        return f.read()


def contenido_de_carpeta(carpeta):
    base = os.path.join(RAIZ, carpeta)
    canvas_txt = leer(os.path.join(base, "canvas.json"))
    canvas = json.loads(canvas_txt)
    archivos = {}
    for tablero in canvas["artboards"]:
        nombre = tablero["file"]
        ruta = os.path.join(base, nombre)
        if not os.path.exists(ruta):
            sys.exit(f"ERROR: {carpeta}/canvas.json menciona {nombre}, que no existe")
        archivos[nombre] = leer(ruta)
    sobrantes = sorted(set(n for n in os.listdir(base) if n.endswith(".dc.html")) - set(archivos))
    if sobrantes:
        print(f"  aviso: {carpeta} tiene archivos que no están en canvas.json: {sobrantes}")
    archivos["canvas.json"] = canvas_txt
    return archivos


def serializar(doc):
    texto = json.dumps(doc, ensure_ascii=False, separators=(",", ":"))
    texto = texto.replace("<", "\\u003c")
    assert "<" not in texto
    return "\n" + texto + "\n"


def main():
    plantilla = leer(os.path.join(RAIZ, PLANTILLA))
    for paquete, (carpeta, titulo) in PAQUETES.items():
        if not os.path.isdir(os.path.join(RAIZ, carpeta)):
            print(f"  omitido: {paquete} (no existe la carpeta {carpeta})")
            continue
        ruta = os.path.join(RAIZ, paquete)
        html = leer(ruta) if os.path.exists(ruta) else plantilla
        m = PATRON.search(html)
        if not m:
            sys.exit(f"ERROR: {paquete} no tiene el bloque appifact-doc")
        doc = json.loads(m.group(2))
        doc["title"] = titulo
        doc["content"] = {"files": contenido_de_carpeta(carpeta)}
        doc.setdefault("comments", [])
        html = html[: m.start(2)] + serializar(doc) + html[m.end(2):]
        html = re.sub(r"<title>[^<\"]*</title>", f"<title>{titulo}</title>", html, count=1)
        with open(ruta, "w", encoding="utf-8", newline="") as f:
            f.write(html)
        print(f"ok  {paquete:42s} <- {carpeta} ({len(doc['content']['files']) - 1} pantallas)")


if __name__ == "__main__":
    main()
