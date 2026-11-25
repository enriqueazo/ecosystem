# Contrato: comandos de `server-nakama/`

Todos se ejecutan desde `server-nakama/` con `npm run <comando>`, salvo `npm ci` y `npm test`.
Cada uno termina con código 0 si todo va bien y con un código distinto de 0 si falla.

| Comando | Hace | Necesita |
|---------|------|----------|
| `npm ci` | Instala exactamente el lockfile. Falla fuera de Node 26 o de npm 11 (`engine-strict`). | Node 26.13.1 o superior dentro de la 26, con npm 11.20.0 o superior dentro de la 11 |
| `generate` | `shared/contracts/` → `src/generated/` (validadores y constantes) | — |
| `format` | Aplica el formato de Biome. | — |
| `format:check` | Comprueba el formato sin modificar nada. | — |
| `lint` | Análisis de Biome. | — |
| `check:imports` | Rechaza en `src/` (excepto `generated/`) cualquier import que no sea relativo o `import type` de `nakama-runtime` (FR-004). | — |
| `typecheck` | `generate` y después `tsc --noEmit` (código y pruebas). | — |
| `test` | `generate` y después las pruebas unitarias con `node --test`. | — |
| `contracts:check` | Comprobaciones de [contract-format.md](contract-format.md). | — |
| `contracts:lock` | Actualiza `shared/contracts/contracts.lock.json`. Solo al subir una versión. | — |
| `build` | `generate`, Rollup y Babel → `build/index.js`, y después `check:es5`. | — |
| `check:es5` | Analiza `build/index.js` con `ecmaVersion: 5`. | Haber ejecutado `build` |
| `check` | `format:check`, `lint`, `check:imports`, `typecheck`, `test`, `contracts:check` y `build`, en ese orden. Es lo mismo que el trabajo `server` de la CI (R-01). | — |
| `test:integration` | Pruebas contra Nakama local. | El entorno local arrancado con el módulo construido; las claves de `deploy/local/.env` o de las variables `NAKAMA_SERVER_KEY` y `NAKAMA_RUNTIME_HTTP_KEY` |

`test:integration` falla de inmediato, con el mensaje «entorno local no disponible: consulta
deploy/local/README.md», si `tools/check-backend.sh --module` no devuelve 0.
