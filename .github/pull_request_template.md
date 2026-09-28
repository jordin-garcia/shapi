## [ID] Título de la tarea

**Tarea:** `docs/plan/tareas/<ID>-....md` · **Responsable:** <Nombre> · **Requisitos:** RF-xx, RNF-xx

### Qué se hizo
- ...

### Decisiones tomadas (si la especificación no lo decía)
- Ninguna / ...

### Evidencia de verificación
<!-- Pega la salida resumida de cada comando de la sección Verificación de la tarea -->
- [ ] `dotnet build` / `dotnet test`: ...
- [ ] `pnpm lint` / `pnpm typecheck` / `pnpm test`: ...
- [ ] Pantallas comparadas con el mockup (si aplica): ...

### Revisión en contexto limpio (docs/plan/prompts/revision.md)
- Hallazgos de corrección encontrados y corregidos: ...
- Hallazgos opcionales no aplicados: ...

### Lista de control
- [ ] Todos los criterios de aceptación tienen una prueba que pasa
- [ ] El contrato OpenAPI está actualizado (si hay endpoints)
- [ ] Las especificaciones están actualizadas (si se precisó algún comportamiento)
- [ ] El archivo de la tarea tiene `estado: hecha` y una sección **Resultado**. En una corrección de auditoría (protocolo §E3), la tarea ya estaba hecha y tiene la subsección **Correcciones de la auditoría (AAAA-MM-DD)**
- [ ] Hay una entrada nueva en `docs/plan/bitacora/<persona>.md`, con un aviso para cada persona dueña de un archivo modificado, si el PR es del coordinador
- [ ] No hay secretos ni archivos `.env`
