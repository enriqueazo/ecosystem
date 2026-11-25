# 0007 · Proveedor de CI: GitHub Actions

- **Estado**: aceptada
- **Fecha**: 2026-11-25

## Contexto

La constitución exige que la CI ejecute las comprobaciones pertinentes y bloquee la integración si
fallan: configuración mínima, documentada, sin secretos y con versiones fijadas. También prohíbe
declararla configurada hasta que exista un flujo funcional. El repositorio está alojado en
GitHub. Esta es la decisión D-04.

## Opciones consideradas

- **GitHub Actions**: integrado con el alojamiento actual. Ejecutores con Docker y resultados
  visibles en el PR, sin servicio externo.
- **Otro proveedor** (GitLab CI, CircleCI…): exigiría conectar un servicio externo sin una
  necesidad que lo justifique.

## Decisión

Se usa **GitHub Actions**, con un único flujo, `.github/workflows/ci.yml`
([contrato](../../specs/002-servidor-typescript-ci/contracts/ci-workflow.md)):

- **Disparadores**: PR hacia `main` y `push` a `main`. Se cancelan las ejecuciones anteriores de la
  misma rama.
- **Tres trabajos en paralelo** con nombres estables:
  - `repo`: pruebas de `tools/` y `check-repo.sh`;
  - `server`: `npm ci` y `npm run check`;
  - `backend`: entorno local con el módulo, `check-backend.sh --module` y pruebas de integración.
- **Seguridad**:
  - `permissions: contents: read` y `persist-credentials: false`;
  - **ningún secreto del repositorio**: el `.env` del entorno local se genera en cada ejecución
    con `openssl rand` y sus valores se enmascaran (`::add-mask::`);
  - los PR desde forks funcionan igual.
- **Reproducibilidad**:
  - acciones fijadas por commit, con la versión en un comentario:
    - `actions/checkout` v7.0.1 (`3d3c42e…`);
    - `actions/setup-node` v7.1.0 (`949feb2…`);
  - ejecutor `ubuntu-24.04`;
  - Node desde `server-nakama/.node-version`;
  - imágenes de Nakama y PostgreSQL por digest (ADR-0002).
- **Límites de tiempo**: 10, 15 y 20 minutos por trabajo. Si el backend falla, el flujo muestra
  los registros de los servicios, y siempre limpia el entorno.
- **Sin Unity**: la licencia Personal (ADR-0005) y el coste no lo justifican todavía. Las pruebas
  del cliente se ejecutan en local.

## Consecuencias

- **Paso manual del responsable**: para que la CI *bloquee* la integración hay que exigir los
  trabajos `repo`, `server` y `backend` en una regla de protección de `main` en GitHub. Sin ella,
  la CI solo informa.
- **Actualizar una acción** es un cambio propio: nuevo commit fijado, comentario actualizado y
  revisión.
- **Lo que no fija la CI**: la imagen del ejecutor `ubuntu-24.04` la actualiza GitHub (versión de
  Docker, Compose…). Las versiones se muestran al inicio de cada trabajo para poder diagnosticar
  cambios.
- **Validación**: la CI no se da por configurada hasta verla en un PR real con los tres trabajos en
  verde y con un fallo deliberado detectado en cada uno (spec 002, FR-028). El resultado se
  registra en el *Registro de verificación* de la spec 002.
- **Pendiente**: incorporar Unity a la CI requiere una decisión propia, con licencia y coste.
