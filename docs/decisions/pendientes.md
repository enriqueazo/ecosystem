# Decisiones pendientes

Fuente única de las decisiones técnicas abiertas. El resto de documentos enlaza aquí en lugar de
repetir su contenido. Al resolver una decisión se crea su ADR y, en el mismo commit, su entrada
se sustituye por un enlace en la sección *Resueltas*.

## Abiertas

### D-01 · Runtime de los módulos de Nakama

- **Opciones**:
  - **Go**: máximo rendimiento y tipado estático. El plugin debe compilarse con la versión
    exacta de Nakama y de su toolchain, así que cada actualización de Nakama obliga a recompilar.
  - **TypeScript/JavaScript**: tipado (TS) y tooling conocido. Se ejecuta en la VM de JavaScript
    embebida en Nakama y requiere empaquetar en un único archivo; las pruebas en Node no
    equivalen al runtime real.
  - **Lua**: el más simple, con menos tooling y sin tipado.
- **Bloquea**: el código y la estructura interna de `server-nakama/`; la compilación, pruebas,
  formato y análisis del servidor; las reglas de `.gitignore` del runtime; la carga de módulos en
  `deploy/local/compose.yaml`; y el trabajo de CI del servidor.
- **Información necesaria**: experiencia del equipo, requisitos de rendimiento de las partidas
  autoritativas, coste de mantener las actualizaciones y herramientas de pruebas disponibles.
- **Límite**: antes de implementar lógica de servidor sustancial (constitución).

### D-04 · Proveedor de CI

- **Opciones**: GitHub Actions (el remoto actual está en GitHub) u otro proveedor.
- **Bloquea**: el flujo de CI. Hasta resolverla, la CI **no está configurada**.
- **Información necesaria**: confirmación del responsable, coste de minutos de ejecución y cómo
  se gestionará la licencia de Unity en CI.
- **Límite**: antes de depender de la CI para integrar cambios. Flujo mínimo previsto: ejecutar
  `tools/tests/run.sh` y `tools/check-repo.sh` en cada PR, con versiones de acciones o imágenes
  fijadas.

### D-05 · Formato de definición de contratos

- **Opciones**: esquemas JSON, Protocol Buffers u otro formato que permita generar o validar
  código para C# y para el runtime elegido.
- **Bloquea**: el formato descrito en `shared/README.md` y la primera definición de contrato.
- **Información necesaria**: el runtime de Nakama (D-01) y las necesidades de tamaño y frecuencia
  de los mensajes en directo.
- **Límite**: antes del primer RPC o mensaje de partida.

### D-06 · Nombre de producto e identificador de aplicación definitivos

- **Opciones**: por decidir. Mientras tanto se usan valores provisionales documentados en el
  README del cliente.
- **Bloquea**: nada en la fase actual.
- **Información necesaria**: nombre comercial y organización que publicará la aplicación.
- **Límite**: antes de configurar la firma o publicar en tiendas.

### D-08 · Uso de Git LFS para binarios

- **Opciones**: usar Git LFS para arte y audio, o no usarlo.
- **Bloquea**: nada en la fase actual (no hay recursos binarios).
- **Información necesaria**: volumen previsto de recursos binarios y límites del alojamiento Git.
- **Límite**: antes de añadir el primer recurso de arte o audio.

### D-09 · Comprobación automática de formato y análisis de C#

- **Estado**: aplazada.
- **Opciones**: herramientas que funcionen con los proyectos que genera la versión de Unity
  fijada (p. ej. `dotnet format` sobre los `.csproj` generados).
- **Motivo del aplazamiento**: esas herramientas necesitan los `.csproj` que genera el editor de
  Unity, que no están versionados. En CI requerirían ejecutar Unity en modo batch, que depende de
  la licencia (D-04). Además, de momento solo hay código de pruebas. Entretanto, el formato se
  aplica con `.editorconfig` en el IDE.
- **Bloquea**: solo la comprobación automática de formato de C#.
- **Límite**: al resolver D-04 o cuando exista código de ejecución en el cliente.

## Resueltas

| ID | Decisión | Registro |
|----|----------|----------|
| D-02 | Unity 6.3 LTS (rama `6000.3`); la revisión exacta queda fijada en `ProjectSettings/ProjectVersion.txt` | [ADR-0005](0005-unity-63-lts.md) |
| D-03 | Versiones de Nakama y PostgreSQL: 3.42.0 y 16.16 | [ADR-0002](0002-entorno-local-nakama-postgresql.md) |
| D-07 | Pipeline de render del cliente: URP | [ADR-0003](0003-pipeline-render-urp.md) |
