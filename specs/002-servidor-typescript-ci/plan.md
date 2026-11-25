# Implementation Plan: Base del servidor TypeScript, CI y contratos JSON

**Branch**: `feat/002-servidor-typescript-ci` | **Date**: 2026-11-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-servidor-typescript-ci/spec.md`

## Summary

Implementar las decisiones D-01, D-04 y D-05 con sus ADR (0006, 0007 y 0008):

- **Proyecto TypeScript en `server-nakama/`**:
  - Node 26.13.1 y npm con lockfile;
  - TypeScript 7 solo para comprobar tipos, con `lib: ["es5"]`;
  - Rollup + Babel 7 para producir un único `build/index.js` ES5, verificado con acorn;
  - Biome para formato y análisis, y `node:test` para las pruebas.
- **Módulo técnico mínimo**: registra un RPC `protocol_version` cuya respuesta es solo
  `{protocolVersion, moduleVersion}`. Exige sesión y valida la petición con validadores
  generados desde un contrato JSON Schema en `shared/contracts/`.
- **Entorno local**: monta el artefacto en solo lectura, y `check-backend.sh --module` verifica
  que está cargado.
- **CI en GitHub Actions**: tres trabajos (repositorio, servidor y backend con pruebas de
  integración), acciones fijadas por commit, sin secretos y sin Unity.

## Technical Context

**Language/Version**: TypeScript (comprobado con TypeScript 7.0.2) transpilado a ES5 para la VM
goja de Nakama 3.42.0. Herramientas sobre Node 26.13.1 (LTS) y npm 11.20.0. Bash para `tools/`.

**Primary Dependencies**: todas de desarrollo, con versión exacta y lockfile.
- Rollup 4.68.8, con `@rollup/plugin-babel` 7.1.0, `@rollup/plugin-node-resolve` 16.0.3 y
  `@rollup/plugin-commonjs` 29.0.3.
- `@babel/core` 7.29.13, `@babel/preset-env` 7.29.14 y `@babel/preset-typescript` 7.29.7.
- TypeScript 7.0.2, `@biomejs/biome` 2.6.9, Ajv 8.20.0 y acorn 8.x.
- `nakama-runtime` (`github:heroiclabs/nakama-common#v1.49.0`).

En ejecución solo hay código propio, más los ayudantes de `ajv/dist/runtime` que empaqueten los
validadores generados (ADR-0008). Detalle en [research.md](research.md), R-1 a R-6.

**Storage**: N/A. El RPC no persiste nada; PostgreSQL solo lo usa Nakama.

**Testing**:
- `node:test` con la eliminación nativa de tipos de Node 26: unitarias en
  `server-nakama/test/unit/` e integración contra Nakama local en `test/integration/`.
- Una tabla de casos compartida entre ambas (US2, escenario 6).
- `tools/tests/run.sh` para los scripts de bash.
- Comprobación de contratos con `npm run contracts:check`.

**Target Platform**: VM JavaScript (goja, ES5) de Nakama 3.42.0 en Docker. CI en `ubuntu-24.04`.

**Project Type**: módulo de servidor de juego, contratos compartidos y CI dentro del monorepo
existente.

**Performance Goals**:
- CI completa en ≤ 15 min (SC-004).
- Instalar, comprobar y construir el servidor en ≤ 10 min sin contar descargas (SC-001).
- No hay objetivos de rendimiento del RPC: es técnico y sin carga.

**Constraints**:
- Solo ES5 y sin APIs de Node/web en `src/`.
- Funciones RPC e `InitModule` globales.
- `const enum` del runtime no utilizables (se usan constantes propias).
- 0 secretos en el repositorio y en los registros de CI.
- Acciones fijadas por commit.
- El RPC exige sesión.

**Scale/Scope**: unos 40 archivos nuevos o modificados, 1 RPC, 1 contrato, 3 ADR y 1 flujo de
CI con 3 trabajos.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio / norma | Cumplimiento en este plan | Estado |
|-------------------|---------------------------|--------|
| I. Servidor autoritativo | El RPC valida tamaño, formato y autenticación antes de responder. No devuelve datos internos. No hay lógica de juego. | ✅ |
| II. Contratos versionados | El contrato JSON en `shared/` incluye versión, límites, errores y ejemplos; los validadores se generan desde él; la huella en el lockfile obliga a subir la versión si cambia; se toleran campos desconocidos. Contrato, servidor, pruebas y documentación cambian en el mismo PR. | ✅ |
| III. Resiliencia en tiempo real | No hay partidas ni sesiones en directo. El RPC es idempotente y sin estado. | ✅ N/A |
| IV. Desacoplado y simple | La lógica pura (`src/rpc/`) está separada de la capa de Nakama (`src/main.ts`). Cada dependencia está justificada (R-2 a R-6). No hay proyecto Node aparte para `shared/`. El cliente Unity no cambia. | ✅ |
| V. Calidad | Se usan API verificadas en la documentación oficial; los riesgos de compatibilidad (TS 7 con ES5, `const enum`) se tratan de forma explícita y con plan B. | ✅ |
| VI. TDD | Se escriben primero las pruebas unitarias de la lógica del RPC, el tamaño y los errores, las de `check-backend.sh --module` y `check-repo.sh`, y las de contratos. Las de integración se escriben antes de montar el módulo. La configuración de CI se valida con un PR real. | ✅ |
| VII. Seguridad | CI sin secretos, con `.env` aleatorio en cada ejecución y valores no impresos, y token de solo lectura. El RPC exige sesión. Hay que verificar que `http_key` no aparece en los registros (R-8). | ✅ con verificación pendiente |
| VIII. Reproducibilidad | Node, npm, dependencias, imágenes, ejecutor y acciones fijados. Lockfile versionado. Procedimientos documentados. | ✅ |
| Runtime de Nakama | D-01 se resuelve con ADR-0006 **en el mismo cambio** que el código, pruebas, `.gitignore`, montaje y README (`server-nakama/README.md`). | ✅ |
| CI | No se declara configurada hasta verla funcionar en un PR real (FR-028). | ✅ |
| Git | Rama `feat/002-servidor-typescript-ci`, commits atómicos. | ✅ |

**Resultado**: PASA. No hay violaciones que justificar.

**Re-check tras la fase 1**: PASA. El diseño añade `shared/contracts/` (con contenido real),
`server-nakama/` completo y `.github/workflows/`, todos justificados por la spec. El único
riesgo abierto (TS 7 con las definiciones de Nakama) tiene un plan B que no cambia la
arquitectura.

## Project Structure

### Documentation (this feature)

```text
specs/002-servidor-typescript-ci/
├── spec.md
├── plan.md                 # Este archivo
├── research.md             # Fase 0
├── data-model.md           # Fase 1
├── quickstart.md           # Fase 1: guía de validación
├── contracts/
│   ├── contract-format.md        # Formato de los contratos JSON (metaesquema, lockfile, comprobaciones)
│   ├── rpc-protocol-version.md   # El RPC técnico: petición, respuesta, errores y forma HTTP
│   ├── server-scripts.md         # Interfaz de los comandos npm de server-nakama
│   ├── tooling-cli.md            # Cambios en check-backend.sh (--module, código 4) y check-repo.sh
│   └── ci-workflow.md            # Trabajos, disparadores, permisos y límites de la CI
├── checklists/requirements.md
└── tasks.md                # Fase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
.
├── .github/workflows/ci.yml          # Nuevo: CI (D-04)
├── .gitignore                        # + reglas del runtime TS (R-10)
├── .editorconfig                     # + *.{ts,mjs}
├── README.md                         # Estado real de la CI y protección de rama
├── server-nakama/
│   ├── README.md                     # Procedimiento real (sustituye "Qué se añadirá…")
│   ├── .node-version                 # 26.13.1
│   ├── .npmrc                        # engine-strict=true
│   ├── package.json / package-lock.json
│   ├── tsconfig.json                 # src: noEmit, lib es5, tipos de nakama-runtime
│   ├── tsconfig.test.json            # pruebas y scripts: tipos de Node
│   ├── biome.json
│   ├── babel.config.json             # preset-env (ie 11) + preset-typescript
│   ├── rollup.config.mjs
│   ├── scripts/
│   │   ├── generate.ts               # shared/contracts → src/generated/ (validadores + constantes)
│   │   ├── contracts.ts              # contracts:check y contracts:lock
│   │   └── check-es5.ts              # analiza build/index.js con ecmaVersion 5
│   ├── src/
│   │   ├── main.ts                   # InitModule + rpcProtocolVersion (capa fina, global)
│   │   ├── errors.ts                 # Códigos gRPC propios + error estándar
│   │   ├── payload.ts                # Tamaño UTF-8 y análisis JSON seguro
│   │   ├── rpc/protocolVersion.ts    # Lógica pura del RPC
│   │   └── generated/                # (ignorado) validadores y constantes
│   └── test/
│       ├── cases/protocol-version.cases.ts   # Casos compartidos unitarias ↔ integración
│       ├── unit/*.test.ts
│       └── integration/*.test.ts
├── shared/
│   ├── README.md                     # Secciones D-05 completas
│   └── contracts/
│       ├── contract.schema.json      # Metaesquema de contratos
│       ├── protocol.json             # {"protocolVersion": 1}
│       ├── contracts.lock.json       # Huellas id@version
│       └── rpc/protocol_version.json # Contrato v1
├── deploy/local/
│   ├── compose.yaml                  # + montaje solo lectura y js_entrypoint
│   └── README.md                     # + construir el módulo antes de arrancar
├── tools/
│   ├── check-backend.sh              # + --module, código 4
│   ├── check-repo.sh                 # + lockfile y .node-version del servidor
│   └── tests/run.sh                  # + casos nuevos
└── docs/decisions/
    ├── 0006-runtime-nakama-typescript.md
    ├── 0007-ci-github-actions.md
    ├── 0008-contratos-json-versionados.md
    ├── README.md                     # Índice
    └── pendientes.md                 # D-01, D-04, D-05 → Resueltas
```

**Structure Decision**:
- Las herramientas de contratos viven en `server-nakama/scripts/`, porque es el único proyecto
  Node, y se documentan en `shared/README.md` (R-6).
- La lógica del RPC está separada de la capa de registro en Nakama para poder probarla en Node
  (FR-005).

## Orden de entrega (commits atómicos)

1. `chore(server)`: esqueleto del proyecto TypeScript.
   - Contenido: `package.json`, lockfile, `.node-version`, `.npmrc`, `tsconfig*`, Biome, Babel,
     Rollup, `.gitignore` y `.editorconfig`.
   - Verifica el riesgo de TS 7 (R-2).
2. `test(tools)` + `feat(tools)`: `check-repo.sh` con los imprescindibles del servidor.
3. `feat(shared)`: metaesquema, `protocol.json` y contrato `protocol_version` v1.
4. `test(server)` + `feat(server)`: comprobación de contratos (`contracts:check`,
   `contracts:lock`) y generación de validadores.
5. `test(server)` + `feat(server)`: lógica pura del RPC (tamaño, análisis, autenticación,
   respuesta, errores) y capa de Nakama (`main.ts`), con la construcción y la comprobación ES5.
6. `test(tools)` + `feat(tools)`: `check-backend.sh --module`.
7. `test(server)` + `feat(deploy)`: pruebas de integración y montaje del módulo en Compose,
   validados en local.
8. `docs(server|shared|deploy)`: README de las tres áreas y ADR-0006/0008, con `pendientes.md`.
9. `ci`: `.github/workflows/ci.yml`, validado en un PR real (FR-028), ADR-0007, README raíz y
   `pendientes.md` (D-04).

D-01 y D-05 se mueven a *Resueltas* en el commit 8, junto con la documentación del código que ya
existe. D-04 lo hace en el commit 9, cuando la CI se ha visto funcionar.

## Riesgos

| Riesgo | Mitigación |
|--------|------------|
| TypeScript 7 no acepta las definiciones de `nakama-common` o `lib: ["es5"]` | Se verifica en el commit 1. Plan B: TypeScript 6.0.3 solo para tipos (R-2). |
| Babel produce sintaxis no ES5 (p. ej. en ayudantes) | `check:es5` con acorn falla la construcción (R-2). |
| Nakama arranca sin módulo si falta `index.js` | `check-backend.sh --module` → código 4 (R-8). Se verifica el comportamiento real. |
| `http_key` aparece en los registros de Nakama | Se revisa en la validación local (quickstart B4). Si aparece, se documenta y se limita su uso a local y CI efímera. |
| La forma HTTP del error difiere de lo previsto | Las pruebas de integración fijan la forma real y se documenta en `rpc-protocol-version.md`. |
| La CI tarda más de 15 min | Caché de npm, imágenes fijadas y trabajos en paralelo. Se mide en el PR real. |

## Complexity Tracking

No hay violaciones de la constitución que justificar.
