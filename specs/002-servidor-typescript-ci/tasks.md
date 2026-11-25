---

description: "Task list for feature 002-servidor-typescript-ci"
---

# Tasks: Base del servidor TypeScript, CI y contratos JSON

**Input**: Design documents from `specs/002-servidor-typescript-ci/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: SÍ. La constitución (principio VI) y la spec (FR-006, FR-014, FR-017, FR-020, FR-031)
exigen TDD para la lógica comprobable: módulo del servidor, generación y comprobación de
contratos, `check-es5`, y los scripts de `tools/`. Las tareas de prueba van antes de su
implementación y deben **fallar** primero. La configuración (CI, Compose) se valida con los
escenarios de [quickstart.md](quickstart.md).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US4 de spec.md)
- Rutas relativas a la raíz del repositorio (estructura en plan.md → *Project Structure*)

### Convención de citas

- `data-model.md` → `Entidad.campo` remite a la regla canónica de [data-model.md](data-model.md).
- Las interfaces remiten a [contracts/](contracts/).
- Las decisiones técnicas remiten a [research.md](research.md) (R-n).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Esqueleto del proyecto TypeScript y reglas del repositorio. Incluye la verificación
temprana del riesgo de TypeScript 7 (research.md R-2).

- [X] T001 Comprobar precondiciones y anotarlas en `specs/002-servidor-typescript-ci/tasks.md` → *Registro de verificación*:
  - rama `feat/002-servidor-typescript-ci`;
  - `node --version` = v26.13.1 y `npm --version` = 11.20.0;
  - `docker info` responde;
  - las imágenes de `deploy/local/compose.yaml` están presentes.
- [X] T002 [P] Añadir a `.gitignore` las reglas de research.md R-10:
  - `/server-nakama/node_modules/`, `/server-nakama/build/`, `/server-nakama/src/generated/` y
    `*.tsbuildinfo`;
  - comprobar con `git check-ignore --no-index` que `server-nakama/package-lock.json`,
    `server-nakama/.node-version` y los archivos de configuración **no** quedan ignorados.
- [X] T003 [P] Añadir a `.editorconfig` la sección `[*.{ts,mjs}]` con `indent_size = 2`.
- [X] T004 Crear el esqueleto de `server-nakama/` según research.md R-1:
  - `server-nakama/.node-version` con `26.13.1`;
  - `server-nakama/.npmrc` con `engine-strict=true`;
  - `server-nakama/package.json`:
    - `name: ecosystem-server`, `version: 0.1.0`, `private: true`, `type: module`;
    - `engines.node: ">=26.13.1 <27"` y `engines.npm: ">=11.20.0 <12"` (con `engine-strict`, `npm ci` falla fuera de esos rangos);
    - `devDependencies` con versión exacta (sin `^`) de research.md R-2 a R-6, más
      `nakama-runtime: github:heroiclabs/nakama-common#v1.49.0` y `acorn` (la última 8.x);
    - `scripts` según contracts/server-scripts.md, con marcadores donde la tarea que los
      implementa aún no existe;
  - `npm install` para generar `server-nakama/package-lock.json`.
- [X] T005 Crear la configuración de herramientas:
  - `server-nakama/tsconfig.json` para `src/`: `noEmit`, `strict`, `lib: ["es5"]`, tipos solo de
    `nakama-runtime`, `allowImportingTsExtensions`, `erasableSyntaxOnly`, `moduleResolution`
    adecuada para TypeScript 7;
  - `server-nakama/tsconfig.test.json` para `test/` y `scripts/`, con los tipos de Node;
  - `server-nakama/biome.json` con `formatter.useEditorconfig: true`;
  - `server-nakama/babel.config.json`: `@babel/preset-env` con `targets: {"ie": "11"}` y
    `@babel/preset-typescript`;
  - `server-nakama/rollup.config.mjs`: entrada `src/main.ts`, `external: ["nakama-runtime"]`,
    node-resolve + commonjs + babel (`babelHelpers: "bundled"`, extensiones `.ts`/`.js`),
    salida `build/index.js` en formato `es`.
- [X] T006 Verificar el riesgo R-2 con un `server-nakama/src/main.ts` provisional:
  - el `src/main.ts` contiene una `InitModule` vacía con el truco `!InitModule &&
    InitModule.bind(null);`;
  - `npx tsc --noEmit -p server-nakama` debe aceptar las definiciones de `nakama-runtime` con
    `lib: ["es5"]`, y Rollup debe producir `build/index.js`;
  - si TypeScript 7 falla, aplicar el plan B (TypeScript 6.0.3 solo para tipos) y anotarlo.

**Checkpoint**: commit `chore(server): …` (plan.md → *Orden de entrega*, bloque 1).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Definición del contrato y su generación. US1 (`build`, `typecheck` y `test`
dependen de `generate`) y US2 los necesitan.

**⚠️ CRITICAL**: Sin contratos ni generación no hay construcción ni RPC.

- [X] T007 [P] Crear `shared/contracts/contract.schema.json` (JSON Schema 2020-12). Debe imponer las reglas de `data-model.md` → `Contrato.id`, `Contrato.kind`, `Contrato.version`, `Contrato.auth`, `Contrato.maxRequestBytes`, `Contrato.request` / `Contrato.response`, `Contrato.errors`, `Contrato.examples.request.valid` / `.invalid`, `Contrato.examples.response.valid` / `.invalid` y `Contrato.description`, según la estructura de contracts/contract-format.md.
- [X] T008 [P] Crear `shared/contracts/protocol.json` con `{"protocolVersion": 1}`, según `data-model.md` → `Versión de protocolo.protocolVersion`.
- [X] T009 Crear `shared/contracts/rpc/protocol_version.json` según contracts/rpc-protocol-version.md:
  - `version: 1`, `auth: "session"` y `maxRequestBytes: 1024`;
  - `request`: objeto con propiedades adicionales permitidas;
  - `response`: `protocolVersion` (entero ≥ 1) y `moduleVersion` (semver, `maxLength: 64`), con
    `additionalProperties: false`;
  - `errors`: `SESSION_REQUIRED`/7, `PAYLOAD_TOO_LARGE`/3 e `INVALID_ARGUMENT`/3;
  - ejemplos válidos e inválidos de petición y respuesta.

  Debe cumplir `data-model.md` → `Contrato.*` (mismas citas que T007).
- [X] T010 Escribir `server-nakama/test/unit/generate.test.ts` (Red), contra un directorio de contratos de prueba en una carpeta temporal:
  - `generate` produce validadores que aceptan y rechazan los ejemplos;
  - genera `PROTOCOL_VERSION`, `MODULE_VERSION` (`data-model.md` → `Módulo de servidor.moduleVersion`, tomado de `package.json`) y `MAX_REQUEST_BYTES`;
  - falla con un mensaje claro si falta `protocol.json`.
- [X] T011 Implementar `server-nakama/scripts/generate.ts` (Green) según contracts/contract-format.md → *Generación*:
  - Ajv en modo *standalone* con salida ESM;
  - escribe en `server-nakama/src/generated/`;
  - `npm run generate` funciona y `npm test` pasa T010.

**Checkpoint**: commits `feat(shared): …` (T007–T009) y `feat(server): …` (T010–T011).

---

## Phase 3: User Story 1 - Desarrollar y probar módulos de servidor de forma reproducible (Priority: P1) 🎯 MVP

**Goal**: Instalar, comprobar, probar y construir el servidor con comandos reproducibles; artefacto único ES5.

**Independent Test**: quickstart.md, escenarios A1–A6.

### Tests for User Story 1 ⚠️

- [X] T012 [P] [US1] Escribir `server-nakama/test/unit/check-es5.test.ts` (Red). Casos:
  - un archivo ES5 pasa;
  - un archivo con `const`, una función flecha o una clase falla con código distinto de 0,
    indicando línea y columna.
- [X] T013 [P] [US1] Escribir `server-nakama/test/unit/check-imports.test.ts` (Red). Casos:
  - un `src/` con solo imports relativos, de `./generated/` o de tipo `nakama-runtime` pasa;
  - un import de un paquete npm en `src/` falla, nombrando el archivo y el import.
- [X] T014 [P] [US1] Añadir a `tools/tests/run.sh` los casos de `check-repo.sh` de contracts/tooling-cli.md → `check-repo.sh` (Red):
  - con `server-nakama/package.json` presente, `--only ignored` falla si `server-nakama/package-lock.json` o `server-nakama/.node-version` están ignorados;
  - sin `server-nakama/package.json`, no se comprueban.

### Implementation for User Story 1

- [X] T015 [US1] Implementar `server-nakama/scripts/check-es5.ts` (Green), que analiza `build/index.js` con acorn (`ecmaVersion: 5`). Encadenarlo en el script `build` de `server-nakama/package.json` (`data-model.md` → `Módulo de servidor.Artefacto`).
- [X] T016 [US1] Implementar `server-nakama/scripts/check-imports.ts` (Green) para FR-004:
  - recorre `src/` (excepto `generated/`) y rechaza imports que no sean relativos o `import type` de `nakama-runtime`;
  - añadirlo como `check:imports` a `check` en `server-nakama/package.json`;
  - los ayudantes de `ajv/dist/runtime` solo entran a través de `src/generated/`, como justifica ADR-0008.
- [X] T017 [US1] Ampliar `tools/check-repo.sh` (Green) con los imprescindibles del servidor; `tools/tests/run.sh` en verde.
- [X] T018 [US1] Completar los `scripts` de `server-nakama/package.json` según contracts/server-scripts.md:
  - `typecheck`, `test` y `build` ejecutan antes `generate`;
  - `check` encadena `format:check`, `lint`, `check:imports`, `typecheck`, `test`, `contracts:check` y `build`;
  - `contracts:check` queda como marcador hasta T033;
  - `npm run check` debe terminar con código 0.
- [X] T019 [US1] Validar los escenarios A1–A6 de quickstart.md:
  - A2 con Node 24, si se puede instalar con un gestor de versiones; si no, se anota como no verificado;
  - A4 con tres fallos temporales que se revierten;
  - A5 con `Array.prototype.includes`.

  Anotar los resultados en `specs/002-servidor-typescript-ci/tasks.md` → *Registro de verificación*.

**Checkpoint**: commits `feat(server)` (T012, T013, T015, T016, T018) y `feat(tools)` (T014, T017).

---

## Phase 4: User Story 2 - Contrato técnico de versión de protocolo de extremo a extremo (Priority: P1)

**Goal**: RPC `protocol_version` cargado en Nakama local, con el mismo comportamiento en Node que en el runtime real.

**Independent Test**: quickstart.md, escenarios A7 y C1–C5.

### Tests for User Story 2 ⚠️

- [ ] T020 [US2] Crear `server-nakama/test/cases/protocol-version.cases.ts` con la tabla de contracts/rpc-protocol-version.md → *Casos compartidos*. Cada caso tiene una entrada, si hay sesión, y el resultado esperado: respuesta o `code` de `data-model.md` → `Error estándar.code`.
- [ ] T021 [P] [US2] Escribir `server-nakama/test/unit/payload.test.ts` (Red) para el tamaño UTF-8 con caracteres de 1 a 4 bytes, el límite exacto y el análisis JSON (cadena vacía → `{}`, JSON inválido, no objeto).
- [ ] T022 [P] [US2] Escribir `server-nakama/test/unit/errors.test.ts` (Red):
  - las constantes gRPC propias valen lo mismo que `nkruntime.Codes` (3, 7, 16);
  - el error lanzado tiene `{code: <gRPC>, message: JSON}`, con `message` conforme a `data-model.md` → `Error estándar.message` (sin contenido de la petición).
- [ ] T023 [P] [US2] Escribir `server-nakama/test/unit/protocol-version.test.ts` (Red):
  - recorre la tabla de T020 contra la función pura;
  - valida cada respuesta con el validador generado de `response`;
  - comprueba que contiene **exactamente** `protocolVersion` y `moduleVersion` (SC-008).
- [ ] T024 [P] [US2] Añadir a `tools/tests/run.sh` los casos de `check-backend.sh --module` (Red), con un stub de `curl` según contracts/tooling-cli.md:
  - 403 con `SESSION_REQUIRED` → 0;
  - 404 → 4 con el mensaje de construir;
  - salud fallida → 1 (como hasta ahora);
  - `--module` sin `NAKAMA_RUNTIME_HTTP_KEY` → 64;
  - la clave no aparece en la salida ni en stderr.

### Implementation for User Story 2

- [ ] T025 [US2] Implementar `server-nakama/src/errors.ts` y `server-nakama/src/payload.ts` (Green: T021 y T022):
  - solo ES5 y sin APIs de Node;
  - `errors.ts` construye el error según `data-model.md` → `Error estándar.code` y `Error estándar.message`.
- [ ] T026 [US2] Implementar `server-nakama/src/rpc/protocolVersion.ts` (Green: T023):
  - función pura que recibe la carga y el `userId`, y aplica el orden de validación de contracts/rpc-protocol-version.md;
  - usa los validadores y constantes generados (`data-model.md` → `Contrato.maxRequestBytes`, `Versión de protocolo.protocolVersion` y `Módulo de servidor.moduleVersion`).
- [ ] T027 [US2] Sustituir el `server-nakama/src/main.ts` provisional por la capa fina:
  - `InitModule` global que registra **solo** `protocol_version` (`data-model.md` → `Módulo de servidor.RPC registrados`) con la función global `rpcProtocolVersion`, que delega en T026 y traduce el error;
  - una línea `info` con la versión al cargar y ningún dato de la petición;
  - truco anti-*tree-shaking*;
  - `npm run check` en verde.
- [ ] T028 [US2] Implementar `tools/check-backend.sh --module` (Green: T024) y actualizar su contrato de referencia en `specs/001-base-proyecto-entorno/contracts/tooling-cli.md` con la opción y el código 4.
- [ ] T029 [US2] Escribir `server-nakama/test/integration/protocol-version.int.test.ts` y el script `test:integration`, según contracts/server-scripts.md:
  - comprueba primero `tools/check-backend.sh --module` y falla de inmediato si no da 0;
  - autentica un dispositivo de prueba y recorre la tabla de T020 contra el RPC real con `?unwrap`;
  - valida cada respuesta correcta del RPC real con el validador generado de `response` y comprueba que contiene **exactamente** `protocolVersion` y `moduleVersion` (SC-008), y que `moduleVersion` coincide con `server-nakama/package.json`;
  - cubre además: sin credenciales → 401, con `http_key` → `SESSION_REQUIRED`;
  - fija en aserciones el código HTTP y la forma del cuerpo observados.

  Ejecutarla **antes** de T030 y confirmar que falla (Red: módulo no montado).
- [ ] T030 [US2] Modificar `deploy/local/compose.yaml` (research.md R-8):
  - montar `../../server-nakama/build` en `/nakama/data/modules` como `:ro`;
  - añadir `--runtime.js_entrypoint index.js` al comando de `nakama`;
  - no montar ninguna otra ruta.
- [ ] T031 [US2] Validar C1–C5 de quickstart.md:
  - C1 sin `build/`: anotar el comportamiento real de Nakama;
  - C2 con el módulo construido: `--module` → 0 y el registro muestra el RPC;
  - C3: ninguna clave en los registros, incluida la `http_key` de la URL;
  - C4: `npm run test:integration` en verde (Green de T029);
  - C5: comparación con A7.

  Si la forma HTTP real difiere, corregir `specs/002-servidor-typescript-ci/contracts/rpc-protocol-version.md`. Anotar los resultados en el *Registro de verificación*.
- [ ] T032 [US2] Actualizar `deploy/local/README.md`:
  - construir el módulo antes de arrancar (`cd server-nakama && npm ci && npm run build`);
  - verificar con `--module`;
  - reiniciar `nakama` tras reconstruir;
  - resolución de problemas para el código 4 y para los errores de carga del módulo;
  - en *Límites*, quitar «sin módulos hasta D-01».

**Checkpoint**: commits `feat(server)` (T020–T023, T025–T027), `feat(tools)` (T024, T028) y `feat(deploy)` (T029–T032).

---

## Phase 5: User Story 4 - Contratos JSON versionados y documentación de decisiones (Priority: P2)

**Goal**: Comprobación automática de contratos, `shared/README.md` completo y D-01/D-05 registradas.

**Independent Test**: quickstart.md, escenarios B1–B4 y E1 (parte D-01/D-05).

> La US4 se ejecuta antes que la US3, con la que comparte prioridad P2: la CI ejecuta
> `contracts:check` y debe verlo implementado.

### Tests for User Story 4 ⚠️

- [ ] T033 [US4] Escribir `server-nakama/test/unit/contracts.test.ts` (Red) con contratos de prueba en una carpeta temporal. Debe cubrir cada regla de contracts/contract-format.md → *Comprobaciones* (1–6):
  - contrato que no cumple el metaesquema;
  - ejemplo válido que no valida y ejemplo inválido que valida;
  - `id` duplicado o distinto del nombre de archivo;
  - huella distinta o ausente;
  - falta `protocol.json`;
  - además, `contracts:lock` escribe la clave correcta (`data-model.md` → `Huella.Clave` y `Huella.Valor`).

### Implementation for User Story 4

- [ ] T034 [US4] Implementar `server-nakama/scripts/contracts.ts` (Green):
  - subcomandos `check` y `lock`; las entradas usan la clave de `data-model.md` → `Huella.Clave` y la huella, el JSON canónico de `data-model.md` → `Huella.Valor`;
  - conectar `contracts:check` y `contracts:lock` en `server-nakama/package.json`;
  - generar `shared/contracts/contracts.lock.json` con `npm run contracts:lock`;
  - `npm run check` en verde.
- [ ] T035 [US4] Validar B1–B3 de quickstart.md con cambios temporales que se revierten, y anotar los resultados.
- [ ] T036 [P] [US4] Completar `shared/README.md` según research.md R-6 y contracts/contract-format.md:
  - en *a. Organización*, la carpeta `contracts/`;
  - en *b. Formato*, JSON Schema 2020-12 y el metaesquema (ya no pendiente);
  - en *c. Versionado*, cómo se codifican las versiones (ya no pendiente);
  - una sección nueva de validación y comprobación (`contracts:check`, `contracts:lock`, `generate`);
  - en *d. Errores*, el transporte en Nakama;
  - en *f*, el paso «actualizar la huella».
- [ ] T037 [P] [US4] Reescribir `server-nakama/README.md` (FR-008):
  - requisitos (Node 26.13.1 y npm 11);
  - instalación, comandos (enlace a su contrato), estructura de `src/`, `test/` y `scripts/`;
  - restricciones de la VM (ES5, globales, sin APIs de Node, `const enum`);
  - pruebas unitarias y de integración;
  - carga en el entorno local (enlace a `deploy/local/README.md`);
  - sustituir la sección «Qué se añadirá al resolver D-01».
- [ ] T038 [P] [US4] Crear `docs/decisions/0006-runtime-nakama-typescript.md` (aceptada):
  - contexto y opciones de D-01;
  - decisión de research.md R-1 a R-5, incluida la separación TypeScript 7 / Babel 7 y su plan B;
  - P-03 (perfil de las partidas) como pregunta abierta.
- [ ] T039 [P] [US4] Crear `docs/decisions/0008-contratos-json-versionados.md` (aceptada):
  - research.md R-6 y R-7;
  - justificación de los ayudantes de `ajv/dist/runtime` como única dependencia de ejecución (FR-004).
- [ ] T040 [US4] Actualizar `docs/decisions/pendientes.md` (D-01 y D-05 → *Resueltas*, con enlaces a 0006 y 0008) y el índice `docs/decisions/README.md`. Validar E1 para D-01 y D-05.

**Checkpoint**: commits `feat(server)` (T033–T034) y `docs(shared|server|decisions)` (T036–T040).

---

## Phase 6: User Story 3 - Integración continua que bloquea cambios defectuosos (Priority: P2)

**Goal**: Flujo de GitHub Actions funcional, validado en un PR real, y estado de la CI documentado.

**Independent Test**: quickstart.md, escenarios D1–D4.

- [ ] T041 [US3] Crear `.github/workflows/ci.yml` según contracts/ci-workflow.md:
  - nombres de trabajos `repo`, `server` y `backend` (`data-model.md` → `Trabajo de CI.Nombre`);
  - disparadores (`Trabajo de CI.Disparadores`), límites (`Trabajo de CI.Límite`), `permissions: contents: read` (`Trabajo de CI.Permisos`) y sin `secrets.` (`Trabajo de CI.Secretos`);
  - acciones por commit con comentario de versión;
  - `ubuntu-24.04`;
  - paso de generación del `.env` con `openssl rand` sin `echo` de valores;
  - `docker compose logs` si hay fallo y `down -v` siempre.
- [ ] T042 [US3] Validar la sintaxis del flujo en local, con `actionlint` vía `docker run` si está disponible (sin añadirlo al repositorio); si no, con una revisión manual anotada. Comprobar con `git grep` que el flujo no referencia `secrets.` ni acciones sin commit (SC-006).
- [ ] T043 [US3] Actualizar el README raíz (FR-027):
  - sección *CI*: qué ejecuta cada trabajo, cuándo y cómo reproducirlo (enlace a contracts/ci-workflow.md → *Reproducción local*);
  - que no ejecuta Unity;
  - el paso manual de protección de rama.

  Actualizar también la tabla de comprobaciones (`--module`, `npm run check`).
- [ ] T044 [US3] Crear `docs/decisions/0007-ci-github-actions.md` (aceptada, research.md R-9). Mover D-04 a *Resueltas* en `docs/decisions/pendientes.md` y actualizar el índice. **No** se considera hecho hasta completar T046 (FR-028).
- [ ] T045 [US3] Con autorización explícita del responsable para hacer push y abrir el PR:
  - subir la rama y abrir el PR hacia `main`;
  - validar D1: los tres trabajos en verde y la duración total ≤ 15 min, anotada.
- [ ] T046 [US3] Validar D2 en el PR:
  - tres commits temporales, uno con un fallo por trabajo, cada uno revertido después;
  - comprobar que cada trabajo falla y que su registro identifica la causa;
  - validar D3 con los registros (ningún valor de `.env` impreso).

  Anotar los enlaces a las ejecuciones en el *Registro de verificación*.

**Checkpoint**: commits `ci: …` (T041), `docs(repo|decisions): …` (T043–T044); los commits temporales de T046 se revierten antes de integrar.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T047 [P] Revisar la fuente única (FR-030):
  - los procedimientos del servidor, entorno local, contratos y CI aparecen en un solo documento cada uno;
  - las D-NN solo se definen en `docs/decisions/pendientes.md`.
- [ ] T048 [P] Auditar secretos: `tools/check-repo.sh` y `git log -p main..HEAD`, sin valores reales de `deploy/local/.env`; registros de CI sin claves (SC-005).
- [ ] T049 Ejecutar la validación completa de quickstart.md (A–E). Medir SC-001 con un clon limpio en el *scratchpad* (instalar, comprobar y construir ≤ 10 min sin descargas). Anotar lo no verificado y su motivo.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (1)**: sin dependencias.
- **Foundational (2)**: depende de Setup. Bloquea US1 y US2.
- **US1 (3)**: depende de la fase 2.
- **US2 (4)**: depende de la fase 2 y de T018 (scripts). T028 depende de T024. T031 depende de T027, T028, T029 y T030.
- **US4 (5)**: depende de la fase 2. T037 documenta lo hecho en US1 y US2, así que conviene hacerla después de ellas.
- **US3 (6)**: depende de US1, US2 y US4, porque la CI ejecuta `npm run check`, que incluye `contracts:check`, y las pruebas de integración. T045 y T046 necesitan autorización para hacer push y abrir el PR.
- **Polish (7)**: depende de todas.

### Within Each User Story

- Las pruebas (T010, T012, T013, T014, T021–T024, T029, T033) se escriben y se ven fallar antes de implementar.
- US2: T020 → (T021 ∥ T022 ∥ T023 ∥ T024) → T025 → T026 → T027 → T028 → T029 → T030 → T031 → T032.

### Parallel Opportunities

- Fase 1: T002 ∥ T003.
- Fase 2: T007 ∥ T008.
- US1: T012 ∥ T013 ∥ T014.
- US2: T021 ∥ T022 ∥ T023 ∥ T024.
- US4: T036 ∥ T037 ∥ T038 ∥ T039.
- Polish: T047 ∥ T048.

---

## Parallel Example: User Story 2

```bash
Task: "T021 [P] [US2] payload.test.ts"
Task: "T022 [P] [US2] errors.test.ts"
Task: "T023 [P] [US2] protocol-version.test.ts"
Task: "T024 [P] [US2] casos de check-backend.sh --module en tools/tests/run.sh"
```

## Parallel Example: User Story 4

```bash
Task: "T036 [P] [US4] shared/README.md"
Task: "T037 [P] [US4] server-nakama/README.md"
Task: "T038 [P] [US4] ADR-0006"
Task: "T039 [P] [US4] ADR-0008"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Fases 1 y 2.
2. US1: servidor que se instala, comprueba, prueba y construye.
3. **STOP and VALIDATE**: escenarios A1–A6.

### Incremental Delivery

1. US1 → toolchain reproducible.
2. US2 → RPC de extremo a extremo en local (núcleo funcional de la spec).
3. US4 → comprobación de contratos y decisiones D-01/D-05 registradas.
4. US3 → CI validada en un PR real y D-04 registrada.
5. Polish → verificación completa.

---

## Notes

- [P] = archivos distintos y sin dependencias pendientes.
- Cada commit agrupa la prueba y su implementación, así que deja la suite en verde. El paso Red (la prueba fallando antes de implementar) se ejecuta igual, se anota en el *Registro de verificación* y se menciona en el mensaje del commit.
- Commits atómicos con Conventional Commits en los checkpoints. No se hace push ni se abre PR sin autorización explícita (T045).
- Ninguna tarea se marca como completada sin haber ejecutado su verificación.

## Registro de verificación

Se rellena durante la implementación. Cada fila es un escenario de quickstart.md o una precondición.

| Escenario | Fecha | Resultado (✅ / ❌ / no verificado) | Evidencia o motivo |
|-----------|-------|------------------------------------|--------------------|
| T001 | 2026-11-25 | ✅ | Rama `feat/002-servidor-typescript-ci`; Node v26.13.1 y npm 11.20.0; Docker Engine 29.9.3 (actualizado desde 29.2.1 por Docker Desktop); imágenes `postgres:16.16-alpine` y `nakama:3.42.0` presentes |
| T002 / T003 | 2026-11-25 | ✅ | `git check-ignore --no-index`: `node_modules/`, `build/` y `src/generated/` ignorados; `package-lock.json`, `.node-version`, `.npmrc`, `biome.json`, `tsconfig.json` y `rollup.config.mjs` versionados. `.editorconfig`: `[*.{ts,mjs}]` con 2 espacios |
| T004 | 2026-11-25 | ✅ | `package.json` con versiones exactas y `engines` node `>=26.13.1 <27` y npm `>=11.20.0 <12`; 0 vulnerabilidades. **Ajuste**: `github:heroiclabs/nakama-common#v1.49.0` se resolvía como `git+ssh` en el lockfile (no funcionaría en la CI sin claves) → dependencia cambiada a la tarball HTTPS del commit `e6bf1ab…` (al que apunta la etiqueta v1.49.0), con `integrity` sha512. `@types/node` 26.6.10 añadido para pruebas y scripts. `fsevents` (opcional de macOS, sin uso) con `allowScripts: {fsevents: false}` explícito |
| T005 | 2026-11-25 | ✅ | `tsconfig.json` (`lib: ["es5"]`, `module: preserve`, `moduleResolution: bundler`, `erasableSyntaxOnly`, `verbatimModuleSyntax`), `tsconfig.test.json` (tipos de Node), Babel (`ie 11` + TypeScript) y Rollup (`format: es`, sin envoltorio). **Ajustes de Biome 2.6**: `rules.recommended` obsoleto → `preset` (`biome migrate`); `vcs.root: ".."` porque `.gitignore` está en la raíz; sangría de 2 espacios explícita (Biome no lee el `.editorconfig` de la raíz); `package-lock.json` excluido de Biome para que lo formatee solo npm |
| T006 / R-2 | 2026-11-25 | ✅ | TypeScript 7.0.2 acepta `nakama-runtime` con `lib: ["es5"]` (plan B innecesario). `[1].includes(1)` en `src/` → TS2550. Rollup genera `build/index.js` con `InitModule` global y acorn lo analiza como ES5 |
| T007–T009 | 2026-11-25 | ✅ | Metaesquema, `protocol.json` y `rpc/protocol_version.json` v1. Con Ajv 2020 en modo estricto, el contrato valida contra el metaesquema y sus ejemplos se comportan como se espera (2 + 1 válidos aceptados; 4 + 4 inválidos rechazados) |
| T010 Red | 2026-11-25 | ✅ | `generate.test.ts` falla con `ERR_MODULE_NOT_FOUND` (`scripts/generate.ts` no existe) |
| T011 Green | 2026-11-25 | ✅ | 6/6 pruebas. **Desviación de R-6**: Ajv standalone emite `require("ajv/dist/runtime/ucs2length")` incluso con `esm: true` (para `maxLength`), lo que rompe ESM y añadiría una dependencia de ejecución. La opción `unicode: false` lo evitaría, pero está obsoleta y cambia la semántica. El generador sustituye ese ayudante por una implementación ES5 propia equivalente (un par sustituto cuenta como 1, verificado con 😀) y **falla** si aparece cualquier otro `require`. Resultado: **0 dependencias de ejecución** en el artefacto, y se retiran `@rollup/plugin-node-resolve` y `@rollup/plugin-commonjs`. `unicodeRegExp: false` evita la bandera `u` (ES2015) en los `RegExp` generados |
| T012–T014 Red | 2026-11-25 | ✅ | `check-es5.test.ts` y `check-imports.test.ts` fallan con `ERR_MODULE_NOT_FOUND`; `tools/tests/run.sh`: 22 correctas y 2 fallidas (lockfile y `.node-version` del servidor ignorados no se detectaban) |
| T015–T017 Green | 2026-11-25 | ✅ | Unitarias 15/15; `tools/tests/run.sh` 24/24; `check-repo.sh` comprueba 7 rutas imprescindibles |
| T018 | 2026-11-25 | ✅ | `npm run check` (formato, lint, `check:imports`, tipos de src y de pruebas, 15 pruebas, construcción y ES5) en verde en unos 2 s. `contracts:check` y `contracts:lock` son marcadores explícitos hasta T033–T034 |
| A1 | 2026-11-25 | ✅ | `npm ci` → 0, sin cambios en `package-lock.json` |
| A2 | 2026-11-25 | ✅ | Node v24.22.3 oficial (tar verificado con SHA256SUMS de nodejs.org, en el *scratchpad* y borrado después): `npm ci` → `EBADENGINE`, «Required: node >=26.13.1 <27, npm >=11.20.0 <12 / Actual: v24.22.3, 11.19.1». npm 11 ya no admite `--node-version` para simularlo |
| A3 | 2026-11-25 | ✅ | Ver T018: `build/index.js` generado y verificado como ES5 |
| A4 | 2026-11-25 | ✅ | Error de tipos → `tsc` TS2322 en `src/main.ts(14,7)`; formato → `biome format` código 1 con el archivo y el diff; análisis → `lint/suspicious/noDoubleEquals` en `src/main.ts:14:43`, código 1. Todo revertido |
| A5 | 2026-11-25 | ✅ | `[1].includes(1)` en `src/` → TS2550 (`lib: es5`) |
| A6 | 2026-11-25 | ✅ | Tras construir, `git status` no muestra `node_modules/`, `build/` ni `src/generated/` |
