# Pruebas de carga · JZ-14

Miden RNF-01 y RNF-03 contra el ambiente productivo simulado. Requieren Docker
Desktop con **Enable host networking** habilitado (o Docker Engine en Linux),
Node.js 22 o posterior y el `.env` local de la raíz. k6 corre únicamente en Docker
con la imagen oficial `grafana/k6:1.6.1`.

Desde la raíz del repositorio:

```powershell
node --test tests/carga/comparar.test.js tests/carga/comun.test.js
node tests/carga/preparar.js
node tests/carga/ejecutar.js --humo
node tests/carga/ejecutar.js
node tests/carga/comparar.js .shapi/carga/A.json .shapi/carga/B.json
node tests/carga/preparar.js --apagar
```

Si Docker no está en `PATH`, defina `CARGA_DOCKER` con la ruta de `docker.exe`.
`preparar.js --sin-compilar` reutiliza las imágenes locales para repetir la
preparación. La primera ejecución debe compilar el código que se va a medir.

## Ambiente y datos

`preparar.js` construye las imágenes oficiales del repositorio y levanta el
proyecto **shapi-jz14**, con red `172.31.14.0/24` y volúmenes independientes.
No modifica los contenedores ni los datos del proyecto `shapi` existente.
No debe ejecutarse simultáneamente con otra preparación o prueba de JZ-14.
Detenga compilaciones y otras cargas del equipo antes de medir.

La sobreescritura de Compose se genera en `.shapi/carga/compose.yml`. Solo publica
el borde en `127.0.0.1:18443` (HTTP 18080) y el origen Envíos en
`127.0.0.1:15101`; PostgreSQL y Redis permanecen privados. Se conserva Caddy,
HTTPS, el secreto de origen y todo el pipeline real de la compuerta.

Se ejecuta `sembrar-demo`, se adapta exclusivamente la base aislada y se
resincroniza Redis mediante el comando del trabajador:

- Planes de Envíos: 24 000 peticiones/min y 1 000 000 de llamadas por ciclo.
- Ruta `/cotizaciones`: 24 000/min, sin caché y peso 1 de la siembra.
- Planes de plataforma: 10 000 000 de peticiones por ciclo.
- La clave de producción activa de Ferretería Zona 11 se sustituye por una
  aleatoria de 26 caracteres base62. PostgreSQL recibe solo su SHA-256; el valor
  en claro queda en `.shapi/carga/.env.carga`, excluido de Git.

La siembra normal no sirve sin esta adaptación: la ruta permite 120/min y el
plan Volumen 600/min, ambos inferiores a las 12 000/min de la prueba.
Para repetir muchas veces, revise el consumo restante o reconstruya únicamente
los datos del proyecto aislado. `--apagar` conserva sus volúmenes y resultados.

## Escenarios y evidencia

Ambos envían el mismo `POST /cotizaciones`: origen `0901`, destino `0101`,
2.5 kg, servicio normal; comprueban la cotización de Q 38.50.

- **A:** `https://envios.api.shapi.localhost/cotizaciones`, clave de producción.
- **B:** `http://localhost:5101/cotizaciones`, directo al origen, con su secreto.

El comando Docker usa `--network host`. La opción `hosts` de k6 traduce los
destinos anteriores a `127.0.0.1:18443` y `127.0.0.1:15101`, respectivamente,
para evitar colisiones con el ambiente habitual. El hostname HTTP y SNI siguen
siendo los originales. `insecureSkipTLSVerify` admite la CA interna de Caddy;
este escenario se usa solo en el equipo local.

`ejecutar.js` corre B y luego A de forma secuencial. El modo `--humo` envía 5/s
durante 10 segundos por escenario para comprobar conectividad y calentar ambos
caminos; esos archivos no sirven para certificar los requisitos. La ejecución
completa usa un modelo abierto de **200 iteraciones/s durante 300 s**, una
petición por iteración, sin pausas ni redirecciones, con 100 VU preasignados y
máximo 500. Cada escenario debe completar 60 000 peticiones e iteraciones y
tener cero `dropped_iterations`. Se admite una petición adicional si k6 inicia
una iteración en el límite temporal exacto; nunca se admiten menos de 60 000.

Los JSON `A.json` y `B.json` guardan p50, p95, p99 de `http_req_duration`,
conteos, errores y las métricas originales de k6. `peticionesPorSegundo` es la
tasa realmente reportada por k6 (incluye el breve cierre de iteraciones), no
la tasa configurada. Los archivos `.log` guardan las salidas de cada proceso;
no se habilita depuración de cabeceras ni se registran las claves.

`comparar.js` termina con código 1 ante datos inválidos, carga incompleta,
errores de A de 1 % o más, una respuesta 200 que no sea la cotización, cualquier
error del origen de referencia o una diferencia `p95(A) - p95(B)` mayor de
15 ms. Una diferencia negativa se reporta sin ajustarla: refleja variación del
equipo y no demuestra latencia negativa de la compuerta.

Los resultados, equipo, versiones y conclusión van en
[`docs/pruebas-de-carga.md`](../../docs/pruebas-de-carga.md). Si la medición
incumple un requisito se registra una tarea para Jordin; optimizar la compuerta
queda fuera de JZ-14.

Referencia del modelo abierto y las iteraciones omitidas:
[documentación oficial de k6](https://grafana.com/docs/k6/latest/using-k6/scenarios/concepts/).
