---
id: JZ-17
titulo: Disponibilidad del enrutamiento (RNF-06)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P2
estado: pendiente
depende_de: [JZ-05, JZ-06, JG-05]
requisitos: [RNF-06]
pantallas: []
---

# JZ-17 · Disponibilidad del enrutamiento (RNF-06)

**Responsable:** José Pablo Zúñiga · **Avance:** final · **Prioridad:** P2 · **Depende de:** JZ-05, JZ-06, JG-05

## Objetivo
Medir la disponibilidad del enrutamiento en el ambiente productivo simulado con una sonda externa, comprobar que la compuerta se recupera sola y dejar el resultado en el manual técnico.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RNF-06 (cómo se mide)
- `docs/specs/06-arquitectura.md` §7.1 y §8
- `docs/specs/08-compuerta.md` §1 (cabecera `X-Api-Key`) y §4 (contrato de errores)
- El `## Resultado` de JZ-05 (claves fijas de la siembra) y de JZ-06 (reinicio automático y *healthchecks*)

## Archivos que creas o modificas
- `scripts/sonda-disponibilidad.mjs` (crear)
- `scripts/sonda-disponibilidad.test.mjs` (crear; la CI ya ejecuta `scripts/*.test.mjs`)
- `docs/manual-tecnico.md` (modificar: sección "Disponibilidad (RNF-06)")

## Criterios de aceptación
1. `node scripts/sonda-disponibilidad.mjs --url <URL> [--intervalo 30] [--salida sonda.csv]` hace un GET cada `intervalo` segundos (30 por omisión) con la cabecera `X-Api-Key`, que toma de la variable `SHAPI_SONDA_CLAVE` y nunca de un argumento, para que no quede en el historial. Por cada muestra agrega una línea al CSV: fecha en UTC (ISO 8601), código HTTP o `error`, el `codigo` del cuerpo JSON si lo hay, la latencia en ms y si estuvo disponible.
2. Una muestra es una **caída** si hay un error de conexión o de TLS, si la respuesta no termina en 5 s, si la respuesta es `503 servicio_no_disponible`, o si es un 502 o 504 sin el `codigo` de la compuerta (lo devuelve el borde porque la compuerta no responde). Cualquier otra respuesta cuenta como disponible, incluidos 401, 403, 404, 429, `502 origen_inaccesible` y `504 origen_sin_respuesta`, porque la compuerta enrutó y el problema es de la clave o del origen.
3. `node scripts/sonda-disponibilidad.mjs --resumen sonda.csv` muestra el periodo, las muestras, las caídas, los minutos sin servicio (las caídas por el intervalo) y el porcentaje de disponibilidad. Termina con código 1 si es menor que 99.5 %.
4. Con el ambiente productivo simulado levantado, si se termina el proceso de la compuerta desde dentro de su contenedor, la compuerta vuelve a responder sola en menos de 60 s. No se usa `docker kill` ni `docker stop`: Docker los trata como una parada manual y no aplica la política de reinicio.
5. El manual técnico explica cómo correr la sonda contra `https://envios.api.shapi.localhost/cobertura`, con una de las claves fijas de la siembra que no se usa en los guiones de demostración, para no mezclar su consumo con el de la demostración. También registra el resultado de la medición: la sonda corre desde que se levanta el ambiente para la exposición, durante al menos 24 horas. Incluye el resultado de la prueba de recuperación.

## Pruebas obligatorias
- `scripts/sonda-disponibilidad.test.mjs`, nombradas con RNF-06, contra un servidor HTTP local que simula: 200, 429, `503 servicio_no_disponible`, 502 sin cuerpo, `502 origen_inaccesible`, una respuesta que tarda más de 5 s y una conexión rechazada
- El cálculo del resumen, incluido el código de salida con menos de 99.5 %

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node --test "scripts/*.test.mjs"
docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml up -d --build
SHAPI_SONDA_CLAVE=<clave de la siembra> node scripts/sonda-disponibilidad.mjs --url https://envios.api.shapi.localhost/cobertura --intervalo 5 --salida sonda.csv   # unos minutos, y luego Ctrl+C
node scripts/sonda-disponibilidad.mjs --resumen sonda.csv
```

## Fuera de alcance
- Mostrar la disponibilidad en B3.1 o guardarla en la base de datos
- Alertas
- Réplicas de la compuerta

## Notas
- Tarea creada en la auditoría del 25 sep (H-113). Las decisiones de Jordin (27 sep) son una sonda externa, y que solo cuentan las caídas del enrutamiento, no las del origen.
