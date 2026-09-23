---
id: JG-10
titulo: Pantalla de claves del proveedor (A4.3 y A4.3b)
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P2
estado: pendiente
depende_de: [JG-07, DC-02]
requisitos: [RF-28]
pantallas: [A4.3, A4.3b]
---

# JG-10 · Pantalla de claves del proveedor (A4.3 y A4.3b)

**Responsable:** Jordin García · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** JG-07, DC-02

## Objetivo
Implementar en el panel la pantalla de claves de los consumidores de una API, con la opción de revocar, igual que los mockups.

## Contexto que debes leer
- Mockups: `mockups/A4/Claves.dc.html`, `mockups/A4/ClavesVacia.dc.html`, `mockups/A4/RevocarClave.dc.html`
- `docs/specs/11-interfaz.md` §3 (A4.3) y §4
- `docs/specs/04-roles-y-permisos.md` §3.1
- `contratos/openapi/claves.yaml`

## Archivos que creas o modificas
- `frontend/apps/panel/src/paginas/A4-3-Claves.tsx` y `A4-3b-RevocarClave.tsx` (reemplazar los de relleno)
- `frontend/apps/panel/src/modulos/claves/**` (crear)
- `frontend/packages/api/src/generado/claves.ts` (regenerar)

## Criterios de aceptación
1. La tabla muestra consumidor, plan, tipo, clave enmascarada, estado y acción, con los textos del mockup. Las claves rotadas muestran "Vigente hasta {fecha}" y las revocadas "El {fecha}".
2. Solo existe la acción **Revocar**; no hay "Rotar". El rol lector no ve la acción.
3. Revocar lleva a la confirmación A4.3b. Al confirmar, llama a la API, muestra un aviso y actualiza la lista.
4. Si la API no tiene claves, se muestra la variante vacía del mockup.

## Pruebas obligatorias
- Vitest + Testing Library + MSW: lista, variante vacía, revocación y rol lector

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

Verificación manual con el entorno levantado:
- Captura de A4.3 con la siembra de demostración, comparada con el mockup (protocolo B8)

## Fuera de alcance
- Backend de claves (JG-07)
