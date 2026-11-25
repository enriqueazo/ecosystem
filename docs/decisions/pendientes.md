# Decisiones pendientes

Fuente única de las decisiones técnicas abiertas. El resto de documentos enlaza aquí en lugar de
repetir su contenido. Al resolver una decisión se crea su ADR y, en el mismo commit, su entrada
se sustituye por un enlace en la sección *Resueltas*.

## Abiertas

### D-06 · Nombre de producto e identificador de aplicación definitivos

- **Opciones**: por decidir. Mientras tanto se usan valores provisionales documentados en el
  README del cliente.
- **Bloquea**: nada en la fase actual.
- **Información necesaria**: nombre comercial y organización que publicará la aplicación.
- **Límite**: antes de configurar la firma o publicar en tiendas.

### D-09 · Comprobación automática de formato y análisis de C#

- **Estado**: aplazada.
- **Opciones**: herramientas que funcionen con los proyectos que genera la versión de Unity
  fijada (p. ej. `dotnet format` sobre los `.csproj` generados).
- **Motivo del aplazamiento**: esas herramientas necesitan los `.csproj` que genera el editor de
  Unity, que no están versionados. En CI requerirían ejecutar Unity en modo batch, que depende de
  la licencia; la CI de GitHub Actions (ADR-0007) no ejecuta Unity. Además, de momento solo hay
  código de pruebas. Entretanto, el formato se
  aplica con `.editorconfig` en el IDE.
- **Bloquea**: solo la comprobación automática de formato de C#.
- **Límite**: cuando se decida ejecutar Unity en la CI o cuando exista código de ejecución en el
  cliente.

## Resueltas

| ID | Decisión | Registro |
|----|----------|----------|
| D-01 | Runtime de los módulos de Nakama: TypeScript transpilado a ES5 para la VM JavaScript | [ADR-0006](0006-runtime-nakama-typescript.md) |
| D-02 | Unity 6.3 LTS (rama `6000.3`); la revisión exacta queda fijada en `ProjectSettings/ProjectVersion.txt` | [ADR-0005](0005-unity-63-lts.md) |
| D-03 | Versiones de Nakama y PostgreSQL: 3.42.0 y 16.16 | [ADR-0002](0002-entorno-local-nakama-postgresql.md) |
| D-04 | Proveedor de CI: GitHub Actions (repositorio, servidor y backend; sin Unity) | [ADR-0007](0007-ci-github-actions.md) |
| D-05 | Formato de contratos: JSON versionado con JSON Schema 2020-12 | [ADR-0008](0008-contratos-json-versionados.md) |
| D-07 | Pipeline de render del cliente: URP | [ADR-0003](0003-pipeline-render-urp.md) |
| D-08 | Binarios de arte sin Git LFS, con umbral de revisión (5 MB por archivo, primer audio o vídeo, 50 MB en total) | [ADR-0011](0011-sin-git-lfs.md) |
