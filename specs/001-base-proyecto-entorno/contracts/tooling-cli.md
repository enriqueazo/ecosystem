# Contrato: scripts de `tools/`

Todos los scripts:
- son bash y se pueden ejecutar desde cualquier directorio. La raíz del repo es el directorio
  padre de `tools/`, calculado a partir de la ruta del propio script. No hay variable de entorno
  para cambiarla: las pruebas copian el script dentro del repositorio temporal;
- usan `set -euo pipefail`;
- escriben los diagnósticos en stderr;
- no necesitan dependencias aparte de `bash`, `git`, `curl` y `docker` (este último, solo donde
  se indica).

## `tools/check-backend.sh`

Comprueba que la API de cliente de Nakama responde en el anfitrión.

| Entrada | Descripción |
|---------|-------------|
| `NAKAMA_API_PORT` (entorno, opcional) | Si no está definida, se lee de `deploy/local/.env`; si tampoco está ahí, se usa `7350`. |
| `--module` (opcional, spec 002) | Tras la salud, comprueba que el módulo de servidor está cargado: llama a `POST /v2/rpc/protocol_version?unwrap&http_key=<clave>` y espera HTTP 403 con `SESSION_REQUIRED`. |
| `NAKAMA_RUNTIME_HTTP_KEY` (con `--module`) | Del entorno o, si no, de `deploy/local/.env`. Nunca se imprime. |

| Código | Significado | Mensaje (stderr) incluye |
|--------|-------------|--------------------------|
| 0 | Nakama responde a su comprobación de salud y, con `--module`, el módulo está cargado. | `Backend disponible en 127.0.0.1:<puerto>` y, con `--module`, `Módulo cargado: RPC protocol_version disponible` (stdout) |
| 1 | No hay respuesta en el puerto. | "backend no disponible", y si Docker está disponible: "¿has arrancado el entorno? (`docker compose up -d --wait` en deploy/local)" |
| 2 | La respuesta no es correcta (código HTTP inesperado). | Código HTTP recibido. |
| 3 | El demonio de Docker no está disponible (solo se comprueba tras fallar la conexión). | "Docker no está en ejecución" |
| 4 | Con `--module`: salud correcta, pero el módulo no está cargado (HTTP 404 «function not found» u otra respuesta inesperada). | "módulo no cargado" y "Ejecuta npm run build en server-nakama y reinicia el entorno" |
| 64 | Uso incorrecto (argumentos no admitidos), `NAKAMA_API_PORT` no es un puerto válido (1–65535) o, con `--module`, falta `NAKAMA_RUNTIME_HTTP_KEY`. | Uso, o la variable y su valor (nunca el de la clave). |

Opción: `-h` / `--help` muestra la ayuda y devuelve 0. El archivo `.env` se lee sin ejecutarlo (no se usa `source`).

## `tools/check-repo.sh`

Comprobaciones estáticas del repositorio (FR-039). No necesitan el editor de Unity ni el demonio
de Docker, pero sí el CLI de Docker para validar la configuración de Compose.

| Comprobación | Falla si |
|--------------|----------|
| `compose` | `docker compose config --quiet` con `--env-file .env.example` devuelve error. |
| `secrets` | Hay en el índice de Git un archivo `.env` o `.env.*` (salvo `.env.example`), datos de PostgreSQL (un archivo `PG_VERSION` o una ruta bajo `postgres-data/`) o la configuración local del cliente `client-unity/LocalConfig/connection.local.json` (spec 003). |
| `ignored` | Algún archivo imprescindible está ignorado (`.env.example`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/ProjectVersion.txt`, o `client-unity/Assets/_Project.meta`). Las rutas del cliente solo se comprueban si existe `client-unity/ProjectSettings/`. |

| Código | Significado |
|--------|-------------|
| 0 | Todas las comprobaciones pasan. |
| 1 | Alguna comprobación falla; cada fallo se informa con su nombre y causa. |
| 64 | Uso incorrecto. |

Opción: `--only <compose\|secrets\|ignored>` ejecuta solo una comprobación (útil en pruebas).
Si `docker` no está instalado, `compose` falla con un mensaje explícito en lugar de omitirse en
silencio.

## `tools/tests/run.sh`

Ejecuta las pruebas de los dos scripts anteriores en repositorios Git temporales.
- Devuelve 0 si todas pasan y 1 si alguna falla, con el nombre de la prueba fallida.
- No necesita el demonio de Docker ni red. Para el caso `compose` usa el CLI de Docker si está
  disponible; si no lo está, la prueba se marca como omitida de forma explícita, sin darla por
  superada.
