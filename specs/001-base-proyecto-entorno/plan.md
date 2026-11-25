# Implementation Plan: Base del proyecto y entorno de desarrollo

**Branch**: `feat/001-base-proyecto-entorno` | **Date**: 2026-11-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-base-proyecto-entorno/spec.md`

## Summary

Crear el esqueleto del monorepo:

- **Backend local reproducible**: Docker Compose con Nakama 3.42.0 y PostgreSQL 16.16. Los
  secretos salen del `.env` local, solo se publican dos puertos en loopback y no se cargan
  módulos de servidor.
- **Proyecto Unity mínimo con URP**: para Android e iOS, con una escena inicial y pruebas
  EditMode/PlayMode que validan la configuración. **Bloqueado** hasta que se confirme la versión
  de Unity (D-02).
- **Documentación y gobierno**: README raíz y uno por área, registro de decisiones (ADR),
  documentación de contratos sin mensajes de juego, y `.gitignore`, `.gitattributes` y
  `.editorconfig` corregidos.
- **Comprobaciones locales**: scripts en bash probados con TDD. La CI queda documentada como
  pendiente (D-04).

## Technical Context

**Language/Version**:
- C# para Unity: Unity 6.3 LTS (`6000.3.x`). Es una **PROPUESTA** pendiente de confirmar (D-02,
  research R-2).
- Bash para los scripts de `tools/`.
- Runtime de servidor: **pendiente (D-01)**. En esta funcionalidad no hay código de servidor.

**Primary Dependencies**:
- Nakama 3.42.0 y PostgreSQL 16.16-alpine (research R-1).
- Paquetes de Unity: URP y Unity Test Framework, en las versiones que resuelva el lockfile de la
  versión de Unity fijada.
- Herramientas: Docker Engine y Compose (verificado con 29.2.1 / v5.0.2), `git`, `curl`.

**Storage**: PostgreSQL, solo para Nakama, en un volumen Docker con nombre. No hay esquema ni
migraciones propias.

**Testing**:
- Unity Test Framework (NUnit) en EditMode y PlayMode.
- `tools/tests/run.sh`: ejecutor en bash sin dependencias para los scripts de `tools/`.
- Validación manual documentada para lo que no se puede automatizar.

**Target Platform**:
- Cliente: Android (ARM64, IL2CPP) e iOS (IL2CPP, export de Xcode).
- Entorno de desarrollo: macOS o Linux con Docker. Windows es viable para el backend con
  Docker Desktop y un shell bash (Git Bash o WSL), pero no se ha comprobado.

**Project Type**: monorepo con una app móvil (Unity), un backend de juego (Nakama), contratos e
infraestructura local.

**Performance Goals**: no aplica (no hay lógica de producto). Objetivo operativo de la spec: el
backend arranca y verifica en ≤ 15 minutos desde un clon limpio (SC-001).

**Constraints**:
- Ningún secreto en Git (Principio VII).
- Solo dos puertos publicados, ligados a `127.0.0.1`.
- Ninguna configuración específica de un runtime (D-01).
- Versiones exactas en imágenes, editor y lockfile (Principio VIII).
- Unity no está instalado en la máquina actual.
- El demonio de Docker estaba detenido durante la investigación (se arranca al implementar).

**Scale/Scope**: unos 30 archivos versionados nuevos o modificados fuera de Unity, más el
proyecto Unity mínimo.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio / norma | Cumplimiento en este plan | Estado |
|-------------------|---------------------------|--------|
| I. Servidor autoritativo | No hay lógica de juego. El cliente no recibe claves ni lógica de servidor. | ✅ N/A justificado |
| II. Contratos versionados | `shared/README.md` define versionado, errores y compatibilidad, sin mensajes. El formato queda pendiente (D-05). | ✅ |
| III. Resiliencia en tiempo real | No hay partidas ni sesiones en esta funcionalidad. | ✅ N/A |
| IV. Arquitectura desacoplada y simple | No hay dependencia cliente↔servidor. Solo los paquetes necesarios (URP, Test Framework), documentados. Sin envoltorios ni carpetas anticipadas. | ✅ |
| V. Calidad de código | Las API de Unity se verifican contra la versión fijada antes de usarse (R-8). Los scripts gestionan errores con códigos de salida explícitos. | ✅ |
| VI. TDD | Scripts: las pruebas se escriben primero (R-6). Unity: las pruebas de configuración deben fallar antes de configurar (R-8). Lo que no se automatiza tiene validación manual documentada (quickstart). | ✅ |
| VII. Seguridad y privacidad | `.env.example` con valores ficticios. `${VAR:?}` aborta si falta un secreto. Sin secretos en YAML ni docs. BD no expuesta. Hay que verificar que los logs de Nakama no impriman secretos (riesgo R-4). | ✅ con verificación pendiente |
| VIII. Reproducibilidad | Nakama y PostgreSQL fijados por etiqueta exacta. Unity se fija con D-02. El lockfile se versiona. Pasos documentados. | ⚠️ Gate parcial: la parte Unity no puede empezar hasta resolver D-02 |
| Runtime de Nakama | No se elige. No se monta `/nakama/data` ni hay reglas de `.gitignore` de un runtime. | ✅ |
| `.gitignore` / `.meta` / lockfiles | Se corrigen C-1 y C-2. Los archivos imprescindibles se protegen con `check-repo.sh`. | ✅ |
| Decisiones registradas | Se crean ADR 0001–0004 al implementar y ADR-0005 al confirmar Unity. Las pendientes van en un único documento. | ✅ |
| CI | No se declara configurada; queda pendiente (D-04). | ✅ |
| Git: una rama por spec, commits atómicos | Rama `feat/001-base-proyecto-entorno`. Commits por bloque (ver *Orden de entrega*). | ✅ |

**Resultado de la puerta**: PASA. El incumplimiento parcial del Principio VIII queda acotado:
las fases que no dependen de Unity pueden empezar ya, y las de Unity quedan bloqueadas hasta
D-02, de forma explícita. No hay violaciones que justificar en *Complexity Tracking*.

**Re-check tras la fase 1**: PASA. El diseño (data-model, contracts y quickstart) no introduce
dependencias, servicios ni carpetas nuevas aparte de `Assets/_Project/Settings/`, que se
justifica en R-9 y es coherente con FR-002.

## Project Structure

### Documentation (this feature)

```text
specs/001-base-proyecto-entorno/
├── spec.md
├── plan.md              # Este archivo
├── research.md          # Fase 0
├── data-model.md        # Fase 1
├── quickstart.md        # Fase 1: guía de validación
├── contracts/
│   ├── local-environment.md   # Variables, puertos, servicios y estados del entorno local
│   └── tooling-cli.md         # Interfaz y códigos de salida de los scripts de tools/
├── checklists/requirements.md
└── tasks.md             # Fase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
.
├── README.md                     # Punto de entrada: requisitos, mapa, primeros pasos, comandos
├── .gitignore                    # Corregido (C-1, C-2, R-10)
├── .gitattributes                # Nuevo (R-10)
├── .editorconfig                 # Nuevo (FR-029)
├── client-unity/                 # BLOQUEADO por D-02
│   ├── README.md                 # Versión, módulos, apertura, pruebas, builds, paquetes justificados
│   ├── Assets/
│   │   └── _Project/
│   │       ├── Scenes/Bootstrap.unity
│   │       ├── Settings/         # Asset de pipeline URP + renderer
│   │       └── Tests/
│   │           ├── EditMode/     # .asmdef + pruebas de configuración
│   │           └── PlayMode/     # .asmdef + prueba de carga de escena
│   ├── Packages/manifest.json, packages-lock.json
│   └── ProjectSettings/          # Incluye ProjectVersion.txt
├── server-nakama/
│   └── README.md                 # Propósito, límites, D-01 y lo que se añadirá
├── shared/
│   └── README.md                 # Contratos: ubicación, formato (D-05), versionado, errores, compatibilidad, checklist
├── deploy/
│   └── local/
│       ├── compose.yaml          # postgres + nakama
│       ├── .env.example          # Variables con valores ficticios
│       └── README.md             # Operaciones: arrancar, estado, logs, detener, limpiar, problemas
├── docs/
│   └── decisions/
│       ├── README.md             # Índice + cómo crear un ADR
│       ├── 0000-plantilla.md
│       ├── 0001-estructura-monorepo.md
│       ├── 0002-entorno-local-nakama-postgresql.md   # Incluye versiones R-1
│       ├── 0003-pipeline-render-urp.md
│       ├── 0004-conectividad-sin-sdk-cliente.md
│       ├── 0005-version-unity.md                     # Al confirmar D-02
│       └── pendientes.md         # Fuente única de decisiones pendientes
└── tools/
    ├── check-backend.sh          # Conectividad (FR-038)
    ├── check-repo.sh             # Comprobaciones del repositorio (FR-039)
    └── tests/run.sh              # Pruebas de los scripts (TDD)
```

**Structure Decision**:
- Se usan las áreas de FR-001 con los nombres ya presentes.
- `deploy/` solo contiene `local/`.
- `tools/` existe porque alberga scripts de verificación.
- `docs/` contiene solo `decisions/`; la documentación operativa vive junto a cada área, y el
  README raíz enlaza sin duplicar (FR-035):
  - el procedimiento del backend está en `deploy/local/README.md`;
  - el del cliente, en `client-unity/README.md`;
  - las decisiones pendientes, en `docs/decisions/pendientes.md`.

## Orden de entrega (commits atómicos)

Cada bloque es un commit independiente que deja el repositorio coherente. El orden también
refleja las dependencias.

1. `chore(repo)`: corregir `.gitignore` y añadir `.gitattributes` y `.editorconfig`.
2. `docs(decisions)`: plantilla, índice, `pendientes.md` y ADR 0001, 0003 y 0004.
3. `test(tools)`: `tools/tests/run.sh` con los casos de `check-repo.sh` y `check-backend.sh`
   (fallan: **Red**).
4. `feat(tools)`: `check-repo.sh` y `check-backend.sh` (**Green**).
5. `feat(deploy)`: `compose.yaml`, `.env.example`, `deploy/local/README.md` y ADR-0002. Se
   valida con el quickstart, escenarios B1–B7.
6. `docs(shared)` y `docs(server)`: READMEs de `shared/` y `server-nakama/`.
7. `docs(repo)`: README raíz.
8. **Tras D-02**:
   - `feat(client)`: proyecto Unity limpio con las pruebas de configuración en rojo;
   - `feat(client)`: configuración que las pone en verde, más la escena inicial y el README del
     cliente;
   - `docs(decisions)`: ADR-0005; el README raíz actualiza los requisitos de Unity.

Los bloques 1–7 se pueden completar y revisar sin Unity. El bloque 8 no empieza hasta que se
confirme la versión de Unity y se instale el editor.

## Riesgos

| Riesgo | Mitigación |
|--------|------------|
| Nakama imprime secretos en los logs de arranque | Revisar los logs en el escenario B1 del quickstart. Si aparecen, bajar el nivel de log y documentarlo (R-4). |
| Contraseña con caracteres especiales rompe `--database.address` | `.env.example` y el README exigen contraseña alfanumérica (R-4). |
| La plantilla Universal 3D añade paquetes o recursos no deseados | Se limpian según R-9 y se verifica con SC-007 y FR-010. |
| Nombres de API de Unity distintos en 6000.3 | Verificar contra la documentación de la versión antes de las pruebas (R-8). |
| Puerto local ocupado | Puertos configurables en `.env` y documentados en la resolución de problemas. |

## Complexity Tracking

No hay violaciones de la constitución que justificar.
