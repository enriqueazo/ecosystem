---

description: "Task list for feature 001-base-proyecto-entorno"
---

# Tasks: Base del proyecto y entorno de desarrollo

**Input**: Design documents from `specs/001-base-proyecto-entorno/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: SÍ. La constitución (Principio VI) exige TDD para la lógica comprobable: los scripts
de `tools/` y las pruebas de configuración de Unity. Las tareas de prueba van antes de la
implementación y deben **fallar** primero. Lo que no se puede automatizar se valida con los
escenarios de [quickstart.md](quickstart.md).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5 de spec.md)
- Rutas relativas a la raíz del repositorio (estructura en plan.md → *Project Structure*)

### Convención de citas

`data-model.md` → `Entidad.campo` remite a la regla canónica del campo en
[data-model.md](data-model.md). Las variables y comandos remiten a
[contracts/local-environment.md](contracts/local-environment.md); los códigos de salida, a
[contracts/tooling-cli.md](contracts/tooling-cli.md).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Configuración general del repositorio (FR-024–FR-030), previa a crear cualquier archivo de área.

- [X] T001 Comprobar precondiciones y anotar el resultado en `tasks.md` → *Registro de verificación*:
  - la rama actual es `feat/001-base-proyecto-entorno`;
  - `docker info` responde (si no, arrancar Docker Desktop);
  - `docker pull` de `postgres:16.16-alpine` y `registry.heroiclabs.com/heroiclabs/nakama:3.42.0`
    funciona (imágenes de contracts/local-environment.md → *Servicios*).
- [X] T002 Reescribir `.gitignore` según research.md R-10:
  - eliminar la sección "Go / Nakama" (C-1) y las rutas `/deploy/data/` y `/deploy/volumes/`;
  - limitar `*.csproj`, `*.sln`, `*.suo`, `*.user`, `*.userprefs`, `*.pidb`, `*.booproj`,
    `*.svclog`, `*.pdb`, `*.mdb`, `*.opendb`, `*.VC.db`, `*.VC.VC.opendb` a `/client-unity/`
    (C-2);
  - añadir `.vscode/`, `/client-unity/.utmp/`, `/client-unity/[Rr]ecordings/`, `*.apk`, `*.aab`,
    `*.ipa`, `*.unitypackage` y `crashlytics-build.properties`;
  - conservar los bloques de Unity, IDE, logs, secretos (`.env`, `.env.*`, `!.env.example`) y
    sistema operativo;
  - ninguna regla debe ignorar `.meta`, `Packages/manifest.json`, `Packages/packages-lock.json`
    ni `ProjectSettings/` (FR-027).
  - Cumple `data-model.md` → `Configuración local.no versionado`.
- [X] T003 [P] Crear `.gitattributes` según research.md R-10:
  - `* text=auto eol=lf`;
  - como texto: `*.cs`, `*.unity`, `*.prefab`, `*.asset`, `*.meta`, `*.mat`, `*.asmdef`, `*.json`,
    `*.yaml`, `*.yml`, `*.md`;
  - `*.sh text eol=lf`;
  - binarios: `png`, `jpg`, `jpeg`, `psd`, `tga`, `fbx`, `wav`, `mp3`, `ogg`, `ttf`, `otf`.
- [X] T004 [P] Crear `.editorconfig` (FR-029):
  - `root = true`, UTF-8, LF, eliminar espacios finales y salto de línea final;
  - indentación: 4 espacios en `*.cs` y 2 en `*.{json,yaml,yml,md,asmdef}`; en `*.sh`, 2 espacios;
  - en `*.md`, `trim_trailing_whitespace = false`;
  - convenciones .NET para C#: llaves en línea nueva, `using` del sistema primero, `PascalCase`
    para tipos y miembros públicos, `_camelCase` para campos privados.

**Checkpoint**: commit `chore(repo): …` (plan.md → *Orden de entrega*, bloque 1).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Registro de decisiones (FR-031–FR-033). Lo enlazan todos los README y ADR posteriores.

**⚠️ CRITICAL**: Las historias referencian `docs/decisions/pendientes.md`; debe existir antes.

- [X] T005 Crear `docs/decisions/0000-plantilla.md` con:
  - los campos de `data-model.md` → `Registro de decisión (ADR).Número`, `.Título`, `.Estado`,
    `.Fecha` y `.Secciones`;
  - instrucciones breves de uso, en español.
- [X] T006 Crear `docs/decisions/pendientes.md` como fuente única (FR-033):
  - contenido: una entrada por cada decisión abierta (D-01, D-02, D-04, D-05, D-06, D-08, D-09)
    con los campos `data-model.md` → `Decisión pendiente.ID`, `.Opciones`, `.Bloquea`,
    `.Información necesaria` y `.Límite`, tomados de spec.md → *Decisiones pendientes*;
  - D-02: incluir la propuesta Unity 6.3 LTS de research.md R-2, marcada como **propuesta**;
  - D-09: estado "aplazada" con el motivo de research.md R-7;
  - D-03 y D-07 no figuran como pendientes, solo como enlaces a ADR-0002 y ADR-0003.
- [X] T007 [P] Crear `docs/decisions/0001-estructura-monorepo.md` (estado `aceptada`): áreas y
  responsabilidades de spec.md FR-001, regla FR-002 y alternativa `contracts/` descartada
  (spec.md → *Assumptions*).
- [X] T008 [P] Crear `docs/decisions/0003-pipeline-render-urp.md` (estado `aceptada`): Clarifications Q2 y research.md R-9.
- [X] T009 [P] Crear `docs/decisions/0004-conectividad-sin-sdk-cliente.md` (estado `aceptada`): Clarifications Q1 y research.md R-5.
- [X] T010 Crear `docs/decisions/README.md`:
  - índice de ADR (0000, 0001, 0003 y 0004; 0002 se añade en T017);
  - enlace a `pendientes.md`;
  - procedimiento para crear un ADR y resolver una pendiente (transición de `data-model.md` →
    *Decisión pendiente*, sustitución en el mismo commit).

**Checkpoint**: commit `docs(decisions): …` (bloque 2). Ya se puede trabajar en las historias.

---

## Phase 3: User Story 1 - Levantar el backend local de forma reproducible (Priority: P1) 🎯 MVP

**Goal**: Nakama + PostgreSQL arrancan, se verifican, se detienen conservando datos y se limpian, siguiendo solo la documentación.

**Independent Test**: quickstart.md escenarios B1–B11.

### Tests for User Story 1 ⚠️

> Escribir primero y comprobar que FALLAN antes de implementar.

- [X] T011 [US1] Crear `tools/tests/run.sh`, un ejecutor en bash sin dependencias
  (contracts/tooling-cli.md → `tools/tests/run.sh`):
  - cada caso crea `<tmp>/` con `git init`, copia `tools/check-backend.sh` a `<tmp>/tools/` y,
    si el caso lo necesita, escribe `<tmp>/deploy/local/.env` de prueba; después ejecuta la
    copia;
  - funciones `assert_exit` y `assert_stderr_contains`, y recuento final con código 0/1;
  - los casos de `tools/check-backend.sh` sustituyen `curl` y `docker` por stubs puestos al
    frente de `PATH` en un directorio temporal. Casos:
    - HTTP 200 → 0;
    - fallo de conexión con docker disponible → 1, "backend no disponible";
    - HTTP 500 → 2;
    - fallo de conexión con docker no disponible → 3;
    - argumento desconocido → 64;
    - se usa `NAKAMA_API_PORT` del entorno y, si no está, el de un `.env` de prueba.
  - Ejecutar `tools/tests/run.sh` y confirmar que falla porque el script no existe (Red).

### Implementation for User Story 1

- [X] T012 [US1] Implementar `tools/check-backend.sh` según contracts/tooling-cli.md:
  - `set -euo pipefail` y resolución de la raíz del repo;
  - prioridad del puerto: entorno, luego `deploy/local/.env`, luego `7350`;
  - petición a `http://127.0.0.1:<puerto>/healthcheck` con tiempo límite corto;
  - códigos 0/1/2/3/64 con sus mensajes;
  - marcarlo ejecutable (`git update-index --chmod=+x` si hace falta);
  - `tools/tests/run.sh` debe quedar en verde (Green).
- [X] T013 [P] [US1] Crear `deploy/local/.env.example`:
  - todas las variables de contracts/local-environment.md → *Variables*, con un comentario por
    variable y valores ficticios con prefijo `cambia-esto-`;
  - `POSTGRES_PASSWORD` cumple `data-model.md` → `Configuración local.POSTGRES_PASSWORD`
    (alfanumérica: usar `cambiaestolocal`);
  - las claves de sesión cumplen `data-model.md` → `Configuración local.claves de cifrado de
    sesión` (verificar la longitud exigida en la documentación de Nakama 3.42.0 y ajustar los
    valores de ejemplo);
  - cabecera que indica: copiar a `.env`, `.env` no se versiona y los valores solo valen en local.
- [X] T014 [US1] Crear `deploy/local/compose.yaml` según contracts/local-environment.md y
  research.md R-3/R-4:
  - **proyecto**: `name: ecosystem-local`;
  - **servicio `postgres`**: imagen fijada por etiqueta y digest (`<imagen>:<etiqueta>@sha256:<digest>`; ver más abajo), `POSTGRES_DB=nakama`, usuario y contraseña desde
    `${POSTGRES_PASSWORD:?…}`, volumen con nombre `postgres-data`, `healthcheck` con
    `pg_isready` y sin `ports`;
  - **servicio `nakama`**:
    - imagen fijada por etiqueta y digest; entrypoint `/bin/sh -ec` que ejecuta `nakama migrate up --database.address …`
      y luego `exec nakama` con los flags de secretos de research.md R-4, más `--name`,
      `--logger.level` y `--session.token_expiry_sec`;
    - `depends_on: postgres: condition: service_healthy`, `healthcheck` con `nakama healthcheck`
      y `restart: unless-stopped`;
    - `ports` solo `127.0.0.1:${NAKAMA_API_PORT:-7350}:7350` y
      `127.0.0.1:${NAKAMA_CONSOLE_PORT:-7351}:7351`;
    - sin `volumes` (FR-020) ni `container_name` ni `links`.
  - Cada secreto usa `${VAR:?Falta VAR: copia deploy/local/.env.example a deploy/local/.env}`.
  - Debe implementar los estados de `data-model.md` → `Entorno local.Estado`.
  - Debe cumplir `data-model.md` → `Configuración local.secretos obligatorios` (cada secreto con
    `${VAR:?…}`) y `Configuración local.puertos opcionales` (valor por defecto documentado).
  - Digest: el del índice multiarquitectura, obtenido con
    `docker buildx imagetools inspect <imagen>:<etiqueta>`, para no atarlo a una sola
    arquitectura.
- [X] T015 [US1] Validar `deploy/local/compose.yaml` en la práctica, desde `deploy/local/`:
  - antes de B6, comprobar en la documentación de la API de Nakama 3.42.0 la ruta, el formato
    del cuerpo y la longitud mínima del id de dispositivo, y la ruta `/healthcheck` que usa
    T012 (principio V); si difieren, ajustar B6/B8 de quickstart.md o T012;
  - `docker compose --env-file .env.example config --quiet`;
  - ejecutar los escenarios B1–B10 de quickstart.md;
  - en B3, revisar `docker compose logs nakama` y confirmar que no aparecen
    `POSTGRES_PASSWORD` ni las claves (riesgo R-4). Si aparecen, bajar `--logger.level` y volver
    a comprobar;
  - anotar los resultados en `tasks.md` → *Registro de verificación*; un escenario que no pueda ejecutarse se
    marca "no verificado" con su motivo (FR-043).
- [X] T016 [US1] Crear `deploy/local/README.md`, procedimiento único del backend (FR-019):
  - requisitos (Docker y Compose verificados en 29.2.1 / v5.0.2);
  - configuración (`.env`, contraseña alfanumérica);
  - operaciones arrancar, estado, registros, detener, reanudar, limpiar y verificar, tomadas de
    contracts/local-environment.md → *Operaciones*;
  - acceso a la consola;
  - estados según `data-model.md` → `Entorno local.Estado`;
  - resolución de problemas: falta `.env`, secreto vacío, Docker detenido, puerto ocupado,
    imagen no disponible, migración fallida (se resuelve con `down -v`, nunca con SQL sobre
    tablas internas);
  - límites: solo desarrollo y sin módulos hasta D-01, con enlace a
    `docs/decisions/pendientes.md`.
- [X] T017 [US1] Crear `docs/decisions/0002-entorno-local-nakama-postgresql.md` (estado `aceptada`, versiones y fundamento de research.md R-1/R-3) y añadirlo al índice `docs/decisions/README.md`. Registra ambos digests, la fecha en que se obtuvieron y el procedimiento para actualizarlos (cambio explícito, principio VIII).

**Checkpoint**: commits `test(tools)` (T011), `feat(tools)` (T012) y `feat(deploy)` (T013–T017). La US1 funciona de forma independiente.

---

## Phase 4: User Story 3 - Orientarse en el repositorio y conocer las decisiones pendientes (Priority: P2)

**Goal**: Una persona nueva entiende áreas, requisitos, primeros pasos y decisiones abiertas desde el README raíz.

**Independent Test**: quickstart.md escenarios D1 y D2 (A4 se completa en Polish, cuando existan los README de US4 y US2).

> La US2 (P2) está **bloqueada por D-02**, así que se ejecuta después (Phase 7). Esta historia va
> primero porque no tiene bloqueos.

- [X] T018 [US3] Crear `README.md` raíz con los apartados de spec.md FR-034:
  - descripción breve;
  - requisitos previos: Docker y Compose (versiones verificadas), git, bash y curl; Unity
    enlaza a D-02 en `docs/decisions/pendientes.md`;
  - mapa de áreas según `data-model.md` → `Área del repositorio.Ruta` y
    `Área del repositorio.Dependencias`;
  - primeros pasos: backend con enlace a `deploy/local/README.md` y cliente con enlace a
    `client-unity/README.md` (pendiente de D-02);
  - sección *Comprobaciones* (la completa T024);
  - "Estado de la CI: no configurada (D-04)";
  - flujo de trabajo: spec-kit en `specs/`, una rama por spec con `<tipo>/<NNN>-<nombre-corto>`
    y Conventional Commits, con enlace a `.specify/memory/constitution.md`;
  - enlace al registro de decisiones.
  - **No** duplicar los procedimientos de `deploy/local/README.md` (FR-035): como mucho, tres
    líneas de inicio rápido con enlace.

**Checkpoint**: commit `docs(repo): …`.

---

## Phase 5: User Story 4 - Contratos y servidor preparados sin decisiones prematuras (Priority: P3)

**Goal**: Ubicaciones y reglas de contratos y servidor documentadas, sin mensajes de juego ni runtime implícito.

**Independent Test**: quickstart.md escenarios D3 y A5.

- [X] T019 [P] [US4] Crear `shared/README.md` según spec.md FR-021:
  - (a) organización prevista de las definiciones;
  - (b) formato **pendiente de D-05**, con los criterios de elección de spec.md;
  - (c) versionado: versión de protocolo y versión por contrato, con reglas para cambios
    compatibles e incompatibles (las partes que dependan del formato se marcan como pendientes
    de D-05, FR-023);
  - (d) estructura de error: código estable y mensaje sin datos sensibles ni detalles internos;
  - (e) reglas de compatibilidad del Principio II;
  - (f) checklist de cambio de contrato;
  - aviso: `shared/` solo contiene contratos (`data-model.md` → `Área del repositorio.Dependencias`);
  - sin mensajes, RPC ni opcodes (FR-022).
- [X] T020 [P] [US4] Crear `server-nakama/README.md`:
  - propósito: módulos de servidor autoritativo;
  - límites: sin dependencias del cliente; solo `shared/`;
  - estado: runtime pendiente (enlace a D-01 en `docs/decisions/pendientes.md`);
  - qué se añadirá al resolver D-01: código, pruebas, comprobaciones de formato y análisis,
    reglas de `.gitignore` del runtime y montaje de módulos en `deploy/local/compose.yaml`, todo
    en el mismo cambio que el ADR.
- [X] T021 [US4] Ejecutar quickstart.md A5:
  - `git ls-files` + `git grep -nIiE '\bgo\.mod\b|\.so\b|package\.json|tsconfig|\.lua\b'` fuera
    de `specs/` y `docs/decisions/`, sin coincidencias en configuración;
  - confirmar que no hay referencias cruzadas entre `client-unity/` y `server-nakama/` (FR-004).

**Checkpoint**: commit `docs(shared): …` y `docs(server): …`.

---

## Phase 6: User Story 5 - Comprobaciones de calidad y CI (Priority: P3)

**Goal**: Comprobaciones locales reproducibles que detectan secretos versionados, configuración inválida y archivos imprescindibles ignorados. CI documentada como pendiente.

**Independent Test**: quickstart.md escenarios A1–A3.

### Tests for User Story 5 ⚠️

- [X] T022 [US5] Añadir a `tools/tests/run.sh` los casos de `tools/check-repo.sh`
  (contracts/tooling-cli.md → `tools/check-repo.sh`). Cada caso trabaja en un repo Git temporal
  creado con `git init`, que copia `tools/check-repo.sh` a `<tmp>/tools/`, además de
  `.gitignore`, `deploy/local/compose.yaml` y `deploy/local/.env.example`, y ejecuta la copia.
  Casos:
  - repo válido → 0;
  - `--only secrets` con `deploy/local/.env` añadido con `git add -f` → 1, nombrando el archivo;
  - `--only ignored` con una regla `*.meta` añadida a `.gitignore` y un `client-unity/ProjectSettings/`
    simulado → 1;
  - `--only ignored` sin `client-unity/ProjectSettings/` → 0, omitiendo las rutas del cliente;
  - `--only compose` con un `compose.yaml` inválido → 1;
  - `docker` ausente (stub que devuelve 127) → `compose` falla con mensaje explícito;
  - `--only compose` con docker real disponible: si no lo está, se marca **OMITIDO** de forma
    explícita;
  - argumento desconocido → 64.
  - Confirmar Red.

### Implementation for User Story 5

- [X] T023 [US5] Implementar `tools/check-repo.sh` según contracts/tooling-cli.md:
  - comprobaciones `compose`, `secrets` e `ignored`, con `--only`;
  - informe por comprobación; códigos 0/1/64;
  - `git check-ignore --no-index` para las rutas imprescindibles;
  - ejecutable; `tools/tests/run.sh` debe quedar en verde.
- [X] T024 [US5] Completar la sección *Comprobaciones* de `README.md`:
  - cómo ejecutar `tools/tests/run.sh`, `tools/check-repo.sh` y `tools/check-backend.sh`, con
    enlace a sus códigos de salida;
  - estado de la CI "no configurada" y lo que el flujo mínimo ejecutará cuando se resuelva D-04
    (research.md R-11);
  - el formato de C# se aplica con `.editorconfig` en el IDE, y la comprobación automática queda
    pendiente (D-09).
  - Ejecutar quickstart.md A1–A3.

**Checkpoint**: commits `test(tools)` (T022), `feat(tools)` (T023) y `docs(repo)` (T024).

---

## Phase 7: User Story 2 - Abrir, compilar y probar el cliente Unity (Priority: P2) ⛔ BLOQUEADA por D-02

**Goal**: Proyecto Unity URP mínimo para Android e iOS que compila, pasa pruebas y genera builds de desarrollo.

**Independent Test**: quickstart.md escenarios C1–C6.

> No empezar hasta que el responsable confirme D-02 y estén instalados Unity Hub, el editor
> `6000.3.x` y los módulos *Android Build Support* (con OpenJDK y SDK/NDK) e *iOS Build Support*.
> Si no se puede, cerrar la funcionalidad con la US2 marcada como **no verificada** (FR-043) y
> continuar con Polish.

### Setup for User Story 2

- [X] T025 [US2] Resolver D-02:
  - crear `docs/decisions/0005-version-unity.md` (estado `aceptada`) con la revisión exacta
    instalada, los módulos y el tipo de licencia;
  - sustituir la entrada D-02 de `docs/decisions/pendientes.md` por un enlace al ADR y añadirlo
    a `docs/decisions/README.md`, todo en el mismo commit (`data-model.md` →
    *Decisión pendiente*, transición).
- [ ] T026 [US2] Crear el proyecto con la plantilla **Universal 3D** de Unity Hub de modo que
  `client-unity/` sea la raíz del proyecto (`client-unity/Assets`, `client-unity/Packages` y
  `client-unity/ProjectSettings`). Cerrar el editor y confirmar con `git status` que no aparecen
  `Library/`, `Temp/`, `Logs/`, `UserSettings/` ni `.csproj`.
- [ ] T027 [US2] Limpiar la plantilla según research.md R-9:
  - eliminar escena de ejemplo, materiales, texturas, `TutorialInfo` y `Readme`;
  - mover los assets de pipeline y renderer URP a `client-unity/Assets/_Project/Settings/`
    (desde el editor, para conservar los GUID);
  - quitar de `client-unity/Packages/manifest.json` los paquetes sin uso, incluido Input System
    si no se usa; dejar URP, Test Framework y sus dependencias obligatorias;
  - reabrir el editor y confirmar que se regenera `client-unity/Packages/packages-lock.json`.
- [ ] T028 [US2] Comprobar en la documentación de la versión fijada los nombres exactos de las
  API que usarán las pruebas (research.md R-8: `PlayerSettings`, `NamedBuildTarget`,
  `EditorBuildSettings`, `EditorSettings` y `GraphicsSettings`/URP) y anotarlos en
  `client-unity/README.md` → sección *Pruebas*.

### Tests for User Story 2 ⚠️

- [ ] T029 [P] [US2] Crear `client-unity/Assets/_Project/Tests/EditMode/`:
  - definición de ensamblado `Ecosystem.Tests.EditMode.asmdef` (solo Editor; referencias a
    Unity Test Framework y URP);
  - `ProjectConfigurationTests.cs` con las seis aserciones de research.md R-8 (escena única en
    índice 0 con ruta `Assets/_Project/Scenes/Bootstrap.unity`, IL2CPP en Android e iOS, solo
    ARM64, Force Text, pipeline URP activo, identificador de aplicación provisional definido);
  - ejecutarlas y confirmar que **fallan** con la configuración de la plantilla.
- [ ] T030 [P] [US2] Crear `client-unity/Assets/_Project/Tests/PlayMode/`:
  - `Ecosystem.Tests.PlayMode.asmdef`;
  - `BootstrapSceneTests.cs`, que carga la escena `Bootstrap`, espera un fotograma y comprueba
    con `LogAssert.NoUnexpectedReceived()` que no hay errores ni excepciones;
  - confirmar que **falla** porque la escena aún no existe.

### Implementation for User Story 2

- [ ] T031 [US2] Crear `client-unity/Assets/_Project/Scenes/Bootstrap.unity`:
  - solo una cámara (más la luz que exija URP, si la exige), sin scripts ni arte;
  - configurarla como la única escena de la build, en el índice 0 (FR-008).
- [ ] T032 [US2] Configurar `client-unity/ProjectSettings/` (FR-006):
  - serialización Force Text y `.meta` visibles;
  - IL2CPP en Android e iOS y Android solo ARM64;
  - identificador de aplicación provisional `com.ecosystem.dev`, documentado como provisional
    (D-06);
  - nombre de producto provisional;
  - asset URP asignado como pipeline por defecto y en los niveles de calidad.
  - Ejecutar EditMode y PlayMode: 100 % en verde (Green).
- [ ] T033 [US2] Validar quickstart.md C1–C6:
  - abrir desde un clon limpio;
  - generar una build de desarrollo para Android en `client-unity/Builds/` (ignorada) y exportar
    el proyecto Xcode;
  - `git status` sin cambios tras cerrar el editor;
  - ejecutar `tools/check-repo.sh`, que ahora valida las rutas del cliente.
  - Anotar en `tasks.md` → *Registro de verificación* lo no verificado con su motivo.
- [ ] T034 [US2] Crear `client-unity/README.md`, procedimiento único del cliente:
  - versión exacta (enlace a ADR-0005) y módulos requeridos;
  - cómo abrir el proyecto (versión exacta; no confirmar cambios de versión, Principio VIII);
  - estructura de `Assets/_Project/`;
  - pruebas en el Test Runner y en modo batch (research.md R-8, sin `-quit`);
  - builds Android e iOS (sin firma);
  - tabla de paquetes del manifiesto con la necesidad de cada uno (FR-010);
  - identificadores provisionales (D-06);
  - sin SDK de Nakama (ADR-0004).
- [ ] T035 [US2] Actualizar la sección de requisitos y primeros pasos del cliente en `README.md`: sustituir el enlace a D-02 por la versión fijada y enlazar a `client-unity/README.md`.

**Checkpoint**: commits `docs(decisions)` (T025), `test(client)` (T029–T030), `feat(client)`
(T026–T027 y T031–T032) y `docs(client)` (T034–T035). La US2 funciona de forma independiente.
Si se prefiere que el commit de la plantilla compile sin las pruebas, T026–T027 pueden ir en su
propio commit antes de T029.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Verificaciones transversales de la spec (SC-004, SC-008, SC-009) y cierre.

- [X] T036 Ejecutar quickstart.md A4:
  - cada área de `data-model.md` → `Área del repositorio.Ruta` tiene contenido o el README de
    `Área del repositorio.README`;
  - `git ls-files | grep -c .gitkeep` = 0;
  - eliminar las carpetas locales vacías que no se usen (p. ej. `docs/` tiene contenido; no
    crear nada más).
- [X] T037 [P] Revisar fuente única (FR-035, SC-009):
  - `git grep -n "D-0[0-9]"` fuera de `specs/` y `docs/decisions/pendientes.md` solo debe
    devolver enlaces o menciones, no definiciones;
  - los comandos `docker compose` solo aparecen con detalle en `deploy/local/README.md`.
- [X] T038 [P] Auditar secretos en la rama (SC-004):
  - `tools/check-repo.sh`;
  - `git log -p main..HEAD` filtrado con los valores reales de tu `deploy/local/.env` local:
    sin coincidencias;
  - confirmar que `.env.example` y los README solo contienen valores ficticios (FR-036).
  - Verifica `data-model.md` → `Configuración local.no versionado`.
- [X] T039 Ejecutar la validación completa de quickstart.md:
  - escenarios A, B y D; C solo si la US2 está desbloqueada;
  - B11: cronometrar desde un clon limpio en un directorio temporal;
  - anotar en `tasks.md` → *Registro de verificación* el resultado de cada escenario, marcando como
    **no verificado** lo que no se pudo ejecutar y su motivo (FR-043).
- [ ] T040 Actualizar `specs/001-base-proyecto-entorno/checklists/requirements.md`, añadiendo
  en *Notes* el estado final de los bloqueos:
  - D-02: resuelta, o US2 sin verificar;
  - D-04: CI pendiente.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup y bloquea todas las historias (los README enlazan
  `pendientes.md`).
- **US1 (Phase 3)**: depende de Foundational. Es el MVP.
- **US3 (Phase 4)**: depende de Foundational. Enlaza `deploy/local/README.md`, así que conviene
  hacerla tras US1 (el enlace debe resolver).
- **US4 (Phase 5)**: depende de Foundational; es independiente de US1 y US3.
- **US5 (Phase 6)**: depende de US1, porque reutiliza `tools/tests/run.sh` (T011) y valida
  `deploy/local/compose.yaml` (T014). T024 depende de T018.
- **US2 (Phase 7)**: depende de Foundational y de **D-02 (externa)**. T035 depende de T018.
  T033 usa `tools/check-repo.sh` (T023).
- **Polish (Phase 8)**: depende de todas las historias ejecutables.

### Within Each User Story

- Las pruebas (T011, T022, T029 y T030) se escriben y se ven fallar antes de implementar.
- US1: T011 → T012; T013 ∥ T012; T013 → T014 → T015 → T016 → T017.
- US2: T025 → T026 → T027 → T028 → (T029 ∥ T030) → T031 → T032 → T033 → T034 → T035.

### Parallel Opportunities

- Phase 1: T003 ∥ T004 (después de T002 o a la vez; son archivos distintos).
- Phase 2: T007 ∥ T008 ∥ T009, tras T005 y T006.
- Phase 3: T013 ∥ T011/T012.
- Phase 5 completa (T019 ∥ T020) en paralelo con Phase 3 o 4.
- Phase 7: T029 ∥ T030.
- Phase 8: T037 ∥ T038.

---

## Parallel Example: User Story 4

```bash
Task: "T019 [P] [US4] Crear shared/README.md según spec.md FR-021"
Task: "T020 [P] [US4] Crear server-nakama/README.md"
```

## Parallel Example: User Story 1

```bash
Task: "T011 [US1] Crear tools/tests/run.sh con los casos de check-backend.sh"
Task: "T013 [P] [US1] Crear deploy/local/.env.example"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 (Setup) → Phase 2 (Foundational).
2. Phase 3 (US1).
3. **STOP and VALIDATE**: quickstart.md B1–B11.

### Incremental Delivery

1. Setup + Foundational → repositorio con configuración y registro de decisiones.
2. US1 → backend local verificable (MVP).
3. US3 → orientación en el README raíz.
4. US4 → reglas de contratos y servidor.
5. US5 → comprobaciones locales.
6. US2 → cliente Unity, en cuanto se resuelva D-02.
7. Polish → verificación completa y PR.

Los bloques 1–5 coinciden con los bloques 1–7 de plan.md → *Orden de entrega*, y la US2 con el
bloque 8.

---

## Notes

- [P] = archivos distintos y sin dependencias pendientes.
- Commits atómicos con Conventional Commits en los checkpoints indicados. No se hace push, merge
  ni PR sin petición explícita (constitución, *Git*).
- No se elige runtime de Nakama, proveedor de CI ni formato de contratos en ninguna tarea.
- Ninguna tarea se marca como completada sin ejecutar su verificación (FR-043).

## Registro de verificación

Se rellena durante la implementación. Cada fila es un escenario de quickstart.md o una precondición.
Se copia a la descripción del PR solo cuando se pida crearlo.

| Escenario | Fecha | Resultado (✅ / ❌ / no verificado) | Evidencia o motivo |
|-----------|-------|------------------------------------|--------------------|
| T001 rama | 2026-11-25 | ✅ | `feat/001-base-proyecto-entorno` |
| T001 Docker | 2026-11-25 | ✅ | Docker Engine 29.2.1 (aarch64), Compose v5.0.2 |
| T001 imágenes | 2026-11-25 | ✅ | `postgres:16.16-alpine` (índice `sha256:f2f98726820d0342055351c6e9d36e705f5bfffb3dd53ff556325114b08afb6d`) y `nakama:3.42.0` (índice `sha256:009bfadc8264e4c4e82f1c1bd8b800a74e4dfeb03be59e0faab79117d394cfb8`) descargadas |
| T002 `.gitignore` | 2026-11-25 | ✅ | `git check-ignore --no-index`: `.env.example`, `manifest.json`, `packages-lock.json`, `ProjectVersion.txt`, `.meta` no ignorados; `.env`, `Library/`, `client-unity/*.csproj` ignorados; `tools/*.csproj` no ignorado (C-2) |
| T011 Red | 2026-11-25 | ✅ | `tools/tests/run.sh`: 9 pruebas de `check-backend.sh` fallan (script inexistente, código 127) |
| T012 Green | 2026-11-25 | ✅ | `tools/tests/run.sh`: 9/9 correctas |
| B0 config | 2026-11-25 | ✅ | `docker compose --env-file .env.example config --quiet` → 0 |
| B1 | 2026-11-25 | ✅ | Sin `.env`: aborta con «required variable … Falta X: copia deploy/local/.env.example…» para cada variable; 0 contenedores creados |
| B2 | 2026-11-25 | ✅ | `NAKAMA_SERVER_KEY` vacía: aborta nombrándola; 0 contenedores |
| B3 | 2026-11-25 | ✅ | `up -d --wait` → 0 en 9,4 s; ambos `healthy`; log «Successfully applied migration count=20»; ningún valor secreto de `.env` en los registros de `nakama` ni `postgres` (nivel INFO). Aviso benigno: Google auth sin `client_ids` |
| B4 | 2026-11-25 | ✅ | `check-backend.sh` → 0; `GET /healthcheck` → HTTP 200 (ruta verificada) |
| B5 | 2026-11-25 | ✅ | `lsof`: solo `127.0.0.1:7350` y `127.0.0.1:7351`; 5432 y 7349 cerrados en el anfitrión |
| B6 | 2026-11-25 | ✅ | `POST /v2/account/authenticate/device` (ruta y cuerpo verificados empíricamente) crea uid `ac9c7807…`; 3 ciclos stop/start: mismo uid con `create=false` y `check-backend.sh` → 0 |
| B7 | 2026-11-25 | ✅ | Tras `stop`: `check-backend.sh` → 1 «backend no disponible» |
| B8 | 2026-11-25 | ✅ | `down -v` elimina el volumen; tras arrancar, `create=false` → HTTP 404 «User account not found» |
| B9 | 2026-11-25 | ✅ | Puerto 7350 ocupado: «ports are not available … address already in use»; con `NAKAMA_API_PORT=17350` arranca y `check-backend.sh` → 0 |
| B10 | 2026-11-25 | ✅ | Docker Desktop detenido: `check-backend.sh` → 3 «Docker no está en ejecución» |
| B extra | 2026-11-25 | ✅ | Arranque con los valores de `.env.example` sin modificar (proyecto desechable): sano y `check-backend.sh` → 0 |
| A5 / SC-010 | 2026-11-25 | ✅ | Sin `go.mod`, `package.json`, `tsconfig`, `.lua` ni `.so` fuera de `specs/` y `docs/decisions/`; sin reglas de runtime en `.gitignore`, `deploy/` ni `tools/` (las coincidencias son `/bin/sh` y shebangs) |
| FR-004 | 2026-11-25 | ✅ | Sin referencias entre `client-unity/` y `server-nakama/` |
| T022 Red | 2026-11-25 | ✅ | 12 pruebas nuevas de `check-repo.sh` fallan (script inexistente) |
| A1 / T023 Green | 2026-11-25 | ✅ | `tools/tests/run.sh`: 21 correctas, 0 fallidas, 0 omitidas |
| A2 | 2026-11-25 | ✅ | `tools/check-repo.sh` → 0 (compose, secrets, ignored) |
| A3 | 2026-11-25 | ✅ | `git add -f deploy/local/.env` → `--only secrets` devuelve 1 nombrando el archivo; retirado con `git restore --staged` |
| A4 / SC-008 | 2026-11-25 | ✅ | Todas las áreas versionadas tienen contenido o README; 0 `.gitkeep`; eliminada la carpeta local vacía `client-unity/` (no versionada; la creará Unity Hub) |
| T037 / SC-009 | 2026-11-25 | ✅ | Definiciones D-NN solo en `docs/decisions/pendientes.md` (el resto son enlaces o menciones); procedimiento del backend solo en `deploy/local/README.md` (el README raíz tiene 3 líneas de inicio rápido y enlaza) |
| T038 / SC-004 | 2026-11-25 | ✅ | `check-repo.sh --only secrets` → 0; ningún valor real de `deploy/local/.env` en `git log -p main..HEAD` ni en el árbol; `.env.example` solo contiene `cambia-esto-…`, `cambiaestolocal` y puertos |
| B11 / SC-001 | 2026-11-25 | ⚠️ parcial | Clon limpio → `.env` → `up -d --wait` → `check-backend.sh` 0 en 10 s (imágenes ya descargadas). Primer intento: `up` falló en 6 s sin salida capturada; no se reprodujo en 4 intentos posteriores (incluidos 3 de `down -v` + `up` inmediato); causa sin identificar. **No verificado**: que una persona ajena al trabajo lo consiga leyendo solo el README (SC-001 requiere esa persona) |
| D1 | 2026-11-25 | ✅ | README raíz con todos los apartados de FR-034 (resolución de problemas por enlace a `deploy/local/README.md`); CI «no configurada (D-04)» |
| D2 | 2026-11-25 | ✅ | Ver T037 |
| D3 | 2026-11-25 | ✅ | `shared/README.md` cubre (a)–(f); (b) pendiente de D-05; sin mensajes, RPC ni opcodes |
| C1–C6 / US2 | 2026-11-25 | no verificado | Bloqueada por D-02: versión de Unity sin confirmar y Unity Hub/editor no instalados en la máquina. T025–T035 sin ejecutar |
| T040 | 2026-11-25 | no ejecutada | `/speckit-implement` trata los archivos de `checklists/` como solo lectura; el estado final de los bloqueos queda en este registro (D-02 pendiente → US2 sin verificar; D-04 pendiente → CI no configurada) |
| T025 / D-02 | 2026-11-25 | ✅ | Confirmada por el responsable: Unity 6.3 LTS. Instalado `6000.3.29f1` con AndroidPlayer (OpenJDK, SDK, NDK) e iOSSupport. Registrada como ADR-0005: la tabla aportada proponía ADR-0004, pero ese número ya está asignado (conectividad sin SDK) y los números no se reutilizan |
