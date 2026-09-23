---
id: JZ-05
titulo: Siembra de demostración
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 2
prioridad: P1
estado: pendiente
depende_de: [EM-01, JZ-02, DC-05]
requisitos: [RNF-14]
pantallas: []
---

# JZ-05 · Siembra de demostración

**Responsable:** José Pablo Zúñiga · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** EM-01, JZ-02, DC-05

## Objetivo
Crear un comando que cargue todos los datos de demostración de los mockups, con fechas relativas al día en que se ejecuta, para que el sistema y los diseños coincidan en cualquier demostración.

## Contexto que debes leer
- `docs/specs/07-modelo-de-datos.md` §6 **completo**
- Todos los mockups de A3, A4, A5, A6, B1, B2 y B3 (datos que se muestran)
- `docs/specs/08-compuerta.md` §2 (hash de las claves)
- Sección Resultado de DC-05 (extracción de rutas desde la especificación)

## Archivos que creas o modificas
- `src/Shapi.Infraestructura/Siembra/Demo/**` (crear)
- `src/Shapi.Trabajador/Program.cs` (modificar: comando `sembrar-demo [--reiniciar]`)
- `docs/manual-tecnico.md` (modificar: cuentas y claves de demostración)
- `tests/**/Siembra/**`

## Criterios de aceptación
1. `dotnet run --project src/Shapi.Trabajador -- sembrar-demo` solo funciona con `SHAPI_MODO_DEMO=true`. Es idempotente: si ya existe Envíos Xelajú, no hace nada. Con `--reiniciar`, borra y recrea solo los datos de demostración.
2. Crea todo lo que dice 07 §6: el personal de la plataforma, las organizaciones con sus planes y estados (Envíos Xelajú en Producto; Agro Precios en Lanzamiento; Cafetalera en Prueba; Petén en gracia; Datos Chapines suspendida), los miembros, las APIs `envios` (publicada) y `recolecciones` (despublicada), `agro` (publicada), las rutas sacadas de la especificación de los orígenes de demostración con el peso, el límite y la caché de A3.5, los planes de A4.1 y A5.5, los consumidores con sus suscripciones, las claves, los pagos, los casos CAS-100 a CAS-104 y las entradas de la bitácora de B3.2.
3. Las fechas son **relativas a hoy**, con las mismas proporciones que los mockups (por ejemplo, el ciclo de Mercadito empezó hace 21 días). El consumo de los últimos 30 días se genera con la forma de B1.1 y las proporciones por ruta de B2.1.
4. Las claves de Mercadito Antigua son exactamente las de los mockups: `shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e` y `shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d` (SHA-256 en hex minúsculas en UTF-8). Las demás son fijas, y el manual técnico las lista en una tabla.
5. `url_origen` sale de `SHAPI_URL_ORIGEN_ENVIOS` y `SHAPI_URL_ORIGEN_AGRO` (por defecto `http://localhost:5101` y `http://localhost:5102`; en producción, los nombres de servicio).
6. Todas las cuentas usan la contraseña `Shapi2026!demo`. Al terminar, se ejecuta la resincronización de Redis (si JG-04 ya existe).

## Pruebas obligatorias
- Integración: sembrar dos veces no duplica nada; `--reiniciar` recrea
- Muestras: Envíos tiene 2 APIs, Mercadito tiene 2 claves activas y existe CAS-104

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Datos base (EM-01)
