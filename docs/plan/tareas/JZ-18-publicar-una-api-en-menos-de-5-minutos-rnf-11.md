---
id: JZ-18
titulo: Publicar una API en menos de 5 minutos (RNF-11)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P2
estado: pendiente
depende_de: [JZ-13]
requisitos: [RNF-11]
pantallas: []
---

# JZ-18 · Publicar una API en menos de 5 minutos (RNF-11)

**Responsable:** José Pablo Zúñiga · **Avance:** final · **Prioridad:** P2 · **Depende de:** JZ-13

## Objetivo
Una prueba E2E cronometrada que recorre la publicación de una API desde el registro hasta la primera respuesta válida y falla si tarda 5 minutos o más.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RNF-11 (cómo se verifica)
- `docs/specs/05-casos-de-uso.md` CU-01, CU-05, CU-09, CU-11, CU-12 y CU-14
- El `## Resultado` de JZ-07 (Playwright y ayudas de Mailpit) y de JZ-13 (flujo "publicar una API de punta a punta")

## Archivos que creas o modificas
- `tests/e2e/specs/publicar-en-5-minutos.spec.ts` (crear)
- `docs/plan/tareas/<nuevas>.md` (una por cada falla en un módulo ajeno)

## Criterios de aceptación
1. La prueba, nombrada con RNF-11, empieza a cronometrar al abrir `/registro` con un correo nuevo y se detiene con el primer 200 de la API a través de la compuerta. Recorre:
   - el registro del proveedor y la verificación del correo con el enlace de Mailpit (CU-01);
   - el registro de una API con un subdominio nuevo y un origen de demostración, la carga de su OpenAPI, la exposición de al menos una ruta GET y la publicación (CU-05);
   - un plan gratuito (CU-09);
   - en el portal de esa API: el registro del consumidor, la verificación de su correo y la contratación del plan gratuito, que le entrega su clave (CU-11 y CU-12);
   - un GET a la ruta expuesta con `X-Api-Key`, que responde 200 (CU-14).
2. La prueba falla si el tiempo total es de 5 minutos o más. Deja el tiempo de cada etapa en las anotaciones del reporte de Playwright.
3. Pasa en `e2e.yml` junto con las demás. Si falla por un error de otro módulo, se crea una tarea P1 para su dueño.

## Pruebas obligatorias
- La propia

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd tests/e2e && pnpm test
```

## Fuera de alcance
- Pruebas de usabilidad con personas (decisión de Jordin del 27 sep: solo la prueba E2E cronometrada)
- Planes de pago y dominio propio

## Notas
- Tarea creada en la auditoría del 25 sep (H-113).
