# Ecosystem

Juego móvil multiplataforma (Android e iOS) de estrategia: cliente en Unity y backend
autoritativo en Nakama con PostgreSQL. Este repositorio es un monorepo con todas las piezas.

> Estado: base del proyecto (spec [001](specs/001-base-proyecto-entorno/spec.md)). Todavía no
> hay mecánicas de juego.

## Mapa del repositorio

| Área | Contenido |
|------|-----------|
| [`client-unity/`](client-unity/README.md) | Proyecto Unity para Android e iOS, con URP. |
| [`server-nakama/`](server-nakama/README.md) | Módulos de servidor autoritativo de Nakama en TypeScript ([ADR-0006](docs/decisions/0006-runtime-nakama-typescript.md)). |
| [`shared/`](shared/README.md) | Solo contratos cliente-servidor versionados, en JSON ([ADR-0008](docs/decisions/0008-contratos-json-versionados.md)). |
| [`deploy/local/`](deploy/local/README.md) | Entorno de desarrollo local: Nakama + PostgreSQL con Docker Compose. |
| [`docs/decisions/`](docs/decisions/README.md) | Registro de decisiones de arquitectura (ADR) y [decisiones pendientes](docs/decisions/pendientes.md). |
| [`tools/`](tools/) | Scripts de verificación y sus pruebas. |
| [`specs/`](specs/) | Especificaciones, planes y tareas de cada funcionalidad (spec-kit). |

Cliente y servidor no dependen el uno del otro: solo comparten los contratos de `shared/`
([ADR-0001](docs/decisions/0001-estructura-monorepo.md)).

## Requisitos previos

| Para | Herramienta | Versión |
|------|-------------|---------|
| Todo | Git, bash | — |
| Backend local | Docker Engine + Docker Compose v2 o posterior | Referencia verificada: Docker 29.2.1 y Compose v5.0.2 |
| Servidor | Node + npm | Node 26 (26.13.1 o posterior en la 26) y npm 11 ([server-nakama/README.md](server-nakama/README.md)) |
| Comprobaciones | `curl` | — |
| Cliente | Unity Hub + editor con *Android Build Support* e *iOS Build Support*; Xcode en macOS para iOS | Unity **6000.3.29f1** (6.3 LTS) ([ADR-0005](docs/decisions/0005-unity-63-lts.md)) |

## Primeros pasos

**Backend local**: sigue [deploy/local/README.md](deploy/local/README.md). Resumen:

```sh
(cd server-nakama && npm ci && npm run build)   # Nakama no arranca sin el módulo
cd deploy/local && cp .env.example .env         # y edita los valores
docker compose up -d --wait
../../tools/check-backend.sh --module
```

¿Algo falla (falta `.env`, Docker detenido, puerto ocupado…)? Consulta la
[resolución de problemas](deploy/local/README.md#resolución-de-problemas).

**Cliente Unity**: sigue [client-unity/README.md](client-unity/README.md) para abrir el
proyecto con la versión exacta, ejecutar las pruebas EditMode y PlayMode y generar las builds de
desarrollo de Android e iOS.

## Comprobaciones

| Comando | Qué comprueba | Necesita |
|---------|---------------|----------|
| `tools/tests/run.sh` | Pruebas de los scripts de `tools/` | bash, git; el CLI de Docker para los casos de Compose (si falta, se marcan como omitidos) |
| `tools/check-repo.sh` | Configuración de Compose válida, ningún `.env` ni dato local en Git, ningún archivo imprescindible ignorado | bash, git, CLI de Docker (no el demonio) |
| `cd server-nakama && npm run check` | Formato, análisis, imports, tipos, pruebas unitarias, contratos y construcción del servidor | Node 26 y npm 11 ([server-nakama/README.md](server-nakama/README.md)) |
| `tools/check-backend.sh --module` | Que el backend local responde y tiene cargado el módulo de servidor | bash, curl; el entorno arrancado |
| `cd server-nakama && npm run test:integration` | El RPC contra el Nakama local | El entorno arrancado con el módulo construido |

`tools/check-repo.sh --only <compose|secrets|ignored>` ejecuta una sola comprobación. Los códigos
de salida de los scripts están en su
[contrato](specs/001-base-proyecto-entorno/contracts/tooling-cli.md).

**Formato de C#**: lo aplica el IDE con [`.editorconfig`](.editorconfig). La comprobación
automática está aplazada ([D-09](docs/decisions/pendientes.md#d-09--comprobación-automática-de-formato-y-análisis-de-c)).

## CI

GitHub Actions ([ADR-0007](docs/decisions/0007-ci-github-actions.md)), definida en
[`.github/workflows/ci.yml`](.github/workflows/ci.yml).

**Estado: configurada.** Validada en el PR #2 (2026-11-25): los tres trabajos pasaron en verde en
52 s y cada uno detectó un fallo deliberado.

- **Cuándo**: en cada PR hacia `main` y en cada integración en `main`.
- **Cómo**: sin secretos del repositorio, con permisos de solo lectura y con las acciones fijadas
  por commit.
- **Qué ejecuta**, en tres trabajos en paralelo:

| Trabajo | Qué ejecuta | Cómo reproducirlo en local |
|---------|-------------|----------------------------|
| `repo` | `tools/tests/run.sh` y `tools/check-repo.sh` | Los mismos comandos desde la raíz |
| `server` | `npm ci` y `npm run check` en `server-nakama/` | `cd server-nakama && npm ci && npm run check` |
| `backend` | Construye el módulo, arranca el entorno local con un `.env` de valores aleatorios, `tools/check-backend.sh --module` y `npm run test:integration` | [deploy/local/README.md](deploy/local/README.md) y los dos comandos anteriores |

**Qué no cubre**: Unity. Las pruebas del cliente se ejecutan en local
([client-unity/README.md](client-unity/README.md)).

**Para que la CI bloquee la integración**, el responsable del repositorio tiene que configurar en
GitHub una regla de protección para `main` (*Settings → Branches* o *Rules*) que exija los
trabajos `repo`, `server` y `backend`. Sin esa regla, la CI informa del resultado pero no impide
integrar.

## Decisiones

- Decisiones tomadas: [docs/decisions/](docs/decisions/README.md).
- Decisiones abiertas, con qué bloquean y qué falta para resolverlas:
  [docs/decisions/pendientes.md](docs/decisions/pendientes.md).

## Flujo de trabajo

- Las normas del proyecto están en la [constitución](.specify/memory/constitution.md).
- Cada funcionalidad se especifica, planifica e implementa con spec-kit en `specs/<NNN>-<nombre>/`.
- Una rama por spec, creada desde `main`: `<tipo>/<NNN>-<nombre-corto>` (p. ej.
  `feat/001-base-proyecto-entorno`). Se integra en `main` mediante un PR revisado.
- Commits atómicos con [Conventional Commits](https://www.conventionalcommits.org/) y ámbito
  cuando ayude: `feat(deploy): …`, `fix(client): …`, `docs(decisions): …`.
- La lógica nueva comprobable se desarrolla con TDD; los secretos nunca entran en Git.
