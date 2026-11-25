# Contrato: CI (`.github/workflows/ci.yml`)

## Disparadores y ámbito

| Aspecto | Valor |
|---------|-------|
| Disparadores | `pull_request` con `branches: [main]` y `push` con `branches: [main]` |
| Concurrencia | grupo `ci-${{ github.ref }}`, `cancel-in-progress: true` |
| Permisos | `contents: read` (nivel de flujo) |
| Secretos | Ninguno |
| Ejecutor | `ubuntu-24.04` |
| Unity | No se ejecuta nunca |

## Acciones fijadas

| Acción | Referencia | Versión |
|--------|-----------|---------|
| `actions/checkout` | `3d3c42e5aac5ba805825da76410c181273ba90b1` | v7.0.1 |
| `actions/setup-node` | `949feb2413d6458794dcd2491c4babbbce0c15c1` | v7.1.0 |

Para actualizarlas se cambian el commit y el comentario en un cambio propio (principio VIII).

## Trabajos

Los nombres son estables, porque los usa la protección de rama.

| Trabajo | Límite | Pasos | Falla si |
|---------|--------|-------|----------|
| `repo` | 10 min | checkout → `tools/tests/run.sh` → `tools/check-repo.sh` | Alguno devuelve un código distinto de 0 |
| `server` | 15 min | checkout → setup-node (`server-nakama/.node-version`, caché por lockfile) → muestra versiones → `npm ci` → `npm run check` | Alguno devuelve un código distinto de 0 |
| `backend` | 20 min | checkout → setup-node → `npm ci` → `npm run build` → generar `deploy/local/.env` aleatorio sin imprimirlo → `docker compose up -d --wait` → `tools/check-backend.sh --module` → `npm run test:integration` → **si falla**: `docker compose logs` → **siempre**: `docker compose down -v` | Alguno devuelve un código distinto de 0 o se supera el límite |

## Reproducción local

| Trabajo | Equivalente local |
|---------|-------------------|
| `repo` | `tools/tests/run.sh && tools/check-repo.sh` |
| `server` | `cd server-nakama && npm ci && npm run check` |
| `backend` | Procedimiento de `deploy/local/README.md` + `tools/check-backend.sh --module` + `npm run test:integration` |

## Paso manual (responsable)

En GitHub: *Settings → Branches → Add rule* (o *Rulesets*) para `main`. Hay que exigir que pasen
los trabajos `repo`, `server` y `backend` antes de integrar. Sin esta regla, la CI informa del
resultado pero no bloquea la integración.
