# Pruebas de carga de la compuerta

## Equipo y ambiente

- Fecha: 18 de octubre de 2026, zona `America/Guatemala`.
- Código de la aplicación: `e052180` de `main`; escenarios de JZ-14 en esta rama.
- Procesador: Intel Core i5-1135G7 a 2.40 GHz, 4 núcleos y 8 procesadores lógicos.
- Memoria física informada por Windows: 8 215 105 536 bytes (7.65 GiB).
- Docker Desktop 4.92.0, Engine 29.8.0, Linux/amd64; 8 CPU y
  3 919 187 968 bytes (3.65 GiB) disponibles para el motor.
- k6: imagen oficial `grafana/k6:1.6.1`, digest
  `sha256:a5ad6bc089a08d77c3ec49f3db8c6fa7a148e4073efcac44c675dbaf3568d8e1`.
- Ambiente productivo simulado construido desde el repositorio, proyecto
  `shapi-jz14`, con PostgreSQL, Redis, Caddy, compuerta, API de control,
  trabajador y ambos orígenes de demostración. El ambiente habitual `shapi`
  permanece levantado, sin recibir esta carga.

## Método

Los comandos completos están en [`tests/carga/README.md`](../tests/carga/README.md).
La preparación crea datos aislados y una clave de producción aleatoria; eleva
el límite por minuto del plan y de `/cotizaciones` a 24 000, la cuota del
consumidor a 1 000 000 y la de plataforma a 10 000 000. La caché está desactivada
para la ruta POST y cada petición pesa una llamada. Los límites se aplican con
el Lua real; no se reemplaza la validación ni se escriben entradas de Redis a
mano.

Se calientan ambos caminos con el modo de humo, de 5 peticiones/s por 10 s.
Después se ejecuta primero B y después A, secuencialmente, con tasa abierta de
200 peticiones/s por 300 s, 100 VU preasignados y máximo 500. Cada iteración hace
un solo POST con el mismo cuerpo y comprueba la cotización de Q 38.50.

- **A:** `https://envios.api.shapi.localhost/cotizaciones`, a través de Caddy
  y la compuerta, con clave de producción.
- **B:** `http://localhost:5101/cotizaciones`, al origen directo, con su secreto
  de origen cuando está configurado.

k6 se ejecuta con `docker run --network host`. Para conservar el ambiente
habitual, su opción `hosts` traduce el destino A a `127.0.0.1:18443` y B a
`127.0.0.1:15101`; mantiene los hosts HTTP y el SNI de los destinos originales.
Se admite la CA local de Caddy mediante `insecureSkipTLSVerify`.

Los percentiles son de `http_req_duration` en milisegundos: envío, espera y
recepción; excluye DNS, conexión inicial y negociación TLS, que k6 registra
por separado ([definición oficial](https://grafana.com/docs/k6/latest/using-k6/metrics/reference/)). La diferencia
`p95(A) − p95(B)` es la medición de RNF-01 e incluye el camino adicional por
Caddy. Es una comparación de dos distribuciones secuenciales, no una resta
individual por petición ni la métrica interna de B3.1. Los errores de A se
cuentan de forma conservadora como cualquier respuesta distinta de 200 o
fallo de transporte. Una respuesta 200 con contenido incorrecto invalida la
prueba. La referencia B debe estar libre de errores.

## Resultados

| Medición | B · origen directo | A · compuerta |
|---|---:|---:|
| Peticiones completadas | 60 000 | 60 001 |
| Iteraciones completadas | 60 000 | 60 001 |
| Iteraciones omitidas | 0 | 0 |
| Errores | 0 (0 %) | 0 (0 %) |
| Respuestas 200 con contenido inválido | 0 | 0 |
| Peticiones/s reportadas por k6 | 200.109 | 200.030 |
| p50 (ms) | 1.246 | 4.708 |
| p95 (ms) | 2.003 | 10.585 |
| p99 (ms) | 7.263 | 35.759 |

La medición B terminó aproximadamente a las 00:58 y A a las 01:03, hora de
Guatemala. Ambas ofrecieron 200 peticiones/s durante 300 segundos. A contiene
una iteración adicional iniciada en el límite temporal del ejecutor, contemplada
por los umbrales; no se admitió ninguna petición menos que las 60 000 previstas.

La diferencia calculada con los valores sin redondear es
`10.585167 − 2.0025267 = 8.5826403 ms`.

**Conclusión:** RNF-01 se cumple en este equipo: el p95 adicional es **8.583 ms**,
inferior al máximo de 15 ms. RNF-03 también se cumple: la compuerta completó
la carga sostenida de 200 peticiones/s durante cinco minutos, sin omisiones,
con **0 % de errores**, inferior al 1 %. No se requiere una tarea de optimización
para Jordin a partir de esta ejecución.

`node tests/carga/comparar.js .shapi/carga/A.json .shapi/carga/B.json` terminó
con código 0:

```json
{
  "cumple": true,
  "adicionalP95": 8.582640300000005,
  "porcentajeErrores": 0,
  "problemas": []
}
```

## Evidencia y límites

Los resúmenes completos de k6, registros de ambos procesos y la comparación
se guardan en `.shapi/carga/` (excluido de Git); las claves quedan únicamente
en el archivo local `.env.carga`. Las cifras de la tabla proceden de esos
resúmenes, redondeadas a tres decimales; la comparación usa todos los decimales.

Las nueve pruebas de Node verifican la comparación, los límites estrictos de
15 ms y 1 %, el rechazo de evidencia incompleta o inválida, el modelo abierto
y la extracción de métricas. También cubren la iteración adicional observada
en el límite temporal. Las pruebas de humo y ambos escenarios completos
terminaron con código 0 tras incorporar esa validación.

La tasa reportada por k6 incluye el breve tiempo de cierre; la tasa programada
y las 60 000 iteraciones sin omisiones (se admite una adicional en el límite
temporal del ejecutor) permiten comprobar que se ofreció la
carga durante los cinco minutos completos. Una ejecución de humo nunca se
considera evidencia de los requisitos. Esta medición describe este equipo y
sus recursos de Docker; no estima capacidad ni disponibilidad de un despliegue
en otro equipo.
