# Data Model: Base del servidor TypeScript, CI y contratos JSON

No hay datos persistentes. Las entidades son definiciones versionadas, el módulo y la CI.
Formato detallado en [contracts/contract-format.md](contracts/contract-format.md).

## Contrato (`shared/contracts/<kind>/<id>.json`)

| Campo | Regla |
|-------|-------|
| `id` | `snake_case` ASCII, de 3 a 64 caracteres, único en `shared/contracts/`. Coincide con el nombre del archivo y con el identificador del RPC en Nakama. |
| `kind` | Enumerado: `rpc` (otros tipos, como los mensajes de partida, se añadirán con su spec). |
| `version` | Entero ≥ 1. Sube con **cualquier** cambio del contenido normativo (ver `Huella`). |
| `auth` | Enumerado: `session` (exige sesión de cliente). Otros valores requieren ADR. |
| `maxRequestBytes` | Entero entre 1 y 65 536; tamaño máximo de la petición en bytes UTF-8. |
| `request` / `response` | Esquemas JSON Schema 2020-12. `request.type` = `object`. `response` con `additionalProperties: false`. |
| `errors` | Lista no vacía de `{code, grpcStatus, when}`. `code` en `UPPER_SNAKE_CASE` y único dentro del contrato; `grpcStatus` es un entero de 1 a 16. |
| `examples.request.valid` / `.invalid` | Al menos un ejemplo en cada lista. Los válidos validan contra `request` y los inválidos no. |
| `examples.response.valid` / `.invalid` | Lo mismo contra `response`. |
| `description` | Texto breve, no normativo: no forma parte de la huella. |

### Huella (`shared/contracts/contracts.lock.json`)

| Campo | Regla |
|-------|-------|
| Clave | `"<id>@<version>"` |
| Valor | `"sha256:<hex>"` del JSON canónico (claves ordenadas, sin espacios) de `{kind, version, auth, maxRequestBytes, request, response, errors}`. |

Transiciones:
- Si cambia el contenido normativo y la versión no sube, la huella no coincide y
  `contracts:check` falla.
- Al subir la versión, `contracts:lock` añade la nueva clave. La entrada anterior se conserva
  como historial.

## Versión de protocolo (`shared/contracts/protocol.json`)

| Campo | Regla |
|-------|-------|
| `protocolVersion` | Entero ≥ 1. Valor inicial `1`. Sube con un cambio incompatible en cualquier contrato (`shared/README.md` → *Versionado*). |

## Módulo de servidor

| Campo | Regla |
|-------|-------|
| `moduleVersion` | `version` de `server-nakama/package.json`, en semver `MAJOR.MINOR.PATCH`. Valor inicial `0.1.0`. Se inyecta al generar. |
| Artefacto | `server-nakama/build/index.js`, ignorado por Git. Debe pasar `check:es5`. |
| RPC registrados | Exactamente uno en esta fase: `protocol_version`. |

Estados del módulo en el entorno local, observados con `check-backend.sh --module`:

```text
[sin construir] --npm run build--> [construido] --up -d --wait--> [cargado]
[cargado] --cambio de código + build + reinicio de nakama--> [cargado (nueva versión)]
```

## Error estándar

| Campo | Regla |
|-------|-------|
| `code` | Uno de los `errors[].code` del contrato. Estable. |
| `message` | Texto breve para depuración, sin datos de la petición, secretos ni trazas. |

Transporte en Nakama: `{code: <grpcStatus>, message: JSON.stringify(errorEstándar)}`
([contracts/rpc-protocol-version.md](contracts/rpc-protocol-version.md)).

## Trabajo de CI

| Campo | Regla |
|-------|-------|
| Nombre | `repo`, `server` o `backend`. Estable, porque la protección de rama los referencia. |
| Disparadores | `pull_request` → `main` y `push` → `main`. |
| Límite | `repo` 10 min, `server` 15 min y `backend` 20 min. |
| Permisos | `contents: read`. |
| Secretos | Ninguno. |
