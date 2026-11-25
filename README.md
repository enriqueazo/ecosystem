# Ecosystem

Juego móvil multiplataforma (Android e iOS) de estrategia: cliente en Unity y backend
autoritativo en Nakama con PostgreSQL. Este repositorio es un monorepo con todas las piezas.

> Estado: base del proyecto en construcción (spec
> [001](specs/001-base-proyecto-entorno/spec.md)). Todavía no hay mecánicas de juego.

## Mapa del repositorio

| Área | Contenido |
|------|-----------|
| `client-unity/` | Proyecto Unity para Android e iOS. *Pendiente de crear hasta fijar la versión de Unity (D-02).* |
| [`server-nakama/`](server-nakama/README.md) | Módulos de servidor autoritativo de Nakama. *Runtime pendiente (D-01).* |
| [`shared/`](shared/README.md) | Solo contratos cliente-servidor versionados. |
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
| Comprobaciones | `curl` | — |
| Cliente | Unity Hub + editor con *Android Build Support* e *iOS Build Support*; Xcode en macOS para iOS | Pendiente: [D-02](docs/decisions/pendientes.md#d-02--versión-exacta-de-unity-y-módulos) |

## Primeros pasos

**Backend local**: sigue [deploy/local/README.md](deploy/local/README.md). Resumen:

```sh
cd deploy/local && cp .env.example .env   # y edita los valores
docker compose up -d --wait
../../tools/check-backend.sh
```

**Cliente Unity**: todavía no está disponible. Se creará cuando se resuelva
[D-02](docs/decisions/pendientes.md#d-02--versión-exacta-de-unity-y-módulos); entonces sus
instrucciones estarán en `client-unity/README.md`.

## Comprobaciones

| Comando | Qué comprueba | Necesita |
|---------|---------------|----------|
| `tools/tests/run.sh` | Pruebas de los scripts de `tools/` | bash, git; el CLI de Docker para los casos de Compose (si falta, se marcan como omitidos) |
| `tools/check-repo.sh` | Configuración de Compose válida, ningún `.env` ni dato local en Git, ningún archivo imprescindible ignorado | bash, git, CLI de Docker (no el demonio) |
| `tools/check-backend.sh` | Que el backend local responde | bash, curl; el entorno arrancado |

Ejecuta `tools/tests/run.sh` y `tools/check-repo.sh` antes de cada PR. `tools/check-repo.sh --only
<compose|secrets|ignored>` ejecuta una sola comprobación. Los códigos de salida de cada script
están en su [contrato](specs/001-base-proyecto-entorno/contracts/tooling-cli.md).

**Formato de C#**: lo aplica el IDE con [`.editorconfig`](.editorconfig). La comprobación
automática está aplazada ([D-09](docs/decisions/pendientes.md#d-09--comprobación-automática-de-formato-y-análisis-de-c)).

**Estado de la CI: no configurada.** El proveedor está pendiente de decidir
([D-04](docs/decisions/pendientes.md#d-04--proveedor-de-ci)). Cuando se decida, el flujo mínimo
ejecutará `tools/tests/run.sh` y `tools/check-repo.sh` en cada PR.

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
