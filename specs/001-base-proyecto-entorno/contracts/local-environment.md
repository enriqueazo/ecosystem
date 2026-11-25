# Contrato: entorno local (`deploy/local/`)

Interfaz que el entorno local ofrece a las personas desarrolladoras y a los scripts de `tools/`.
Cambiar cualquier nombre o valor de este contrato obliga a actualizar en el mismo cambio
`.env.example`, `compose.yaml`, `deploy/local/README.md`, los scripts y sus pruebas.

## Servicios

| Servicio | Imagen (fijada) | Publicado en el anfitrión | Comprobación de salud |
|----------|-----------------|---------------------------|-----------------------|
| `postgres` | `postgres:16.16-alpine` | Nada | `pg_isready` sobre la base `nakama` |
| `nakama` | `registry.heroiclabs.com/heroiclabs/nakama:3.42.0` | API de cliente y consola, solo en `127.0.0.1` | `nakama healthcheck` |

En `compose.yaml` cada imagen se fija con etiqueta + digest; el digest vigente consta en
ADR-0002.

- Arranque de `nakama`: primero `migrate up` y después el servidor. Depende de que `postgres`
  esté sano.
- Persistencia: un volumen con nombre para los datos de PostgreSQL.
- No se montan módulos ni `/nakama/data` (D-01).

## Variables (`.env`, a partir de `.env.example`)

| Variable | Obligatoria | Secreta | Valor por defecto | Uso |
|----------|-------------|---------|-------------------|-----|
| `POSTGRES_PASSWORD` | Sí | Sí | — | Contraseña del usuario de base de datos `nakama` (usuario y base fijos en `compose.yaml`); solo alfanumérica. |
| `NAKAMA_SERVER_KEY` | Sí | Sí | — | `--socket.server_key`: la clave que usarán los clientes. |
| `NAKAMA_RUNTIME_HTTP_KEY` | Sí | Sí | — | `--runtime.http_key` |
| `NAKAMA_CONSOLE_USERNAME` | Sí | No | — | `--console.username` |
| `NAKAMA_CONSOLE_PASSWORD` | Sí | Sí | — | `--console.password` |
| `NAKAMA_CONSOLE_SIGNING_KEY` | Sí | Sí | — | `--console.signing_key` |
| `NAKAMA_SESSION_ENCRYPTION_KEY` | Sí | Sí | — | `--session.encryption_key` |
| `NAKAMA_SESSION_REFRESH_ENCRYPTION_KEY` | Sí | Sí | — | `--session.refresh_encryption_key` |
| `NAKAMA_API_PORT` | No | No | `7350` | Puerto del anfitrión para la API de cliente. |
| `NAKAMA_CONSOLE_PORT` | No | No | `7351` | Puerto del anfitrión para la consola. |

Reglas:
- Cada variable obligatoria usa `${VAR:?…}`. El mensaje nombra la variable y dice "copia
  `deploy/local/.env.example` a `deploy/local/.env`".
- Los valores de `.env.example` empiezan por `cambia-esto-` (o un equivalente alfanumérico para
  la contraseña) para que nadie los confunda con valores reales.
- Los valores por defecto de los puertos coinciden con los de Nakama para que la documentación
  sea predecible. Los puertos internos del contenedor no cambian.

## Operaciones (desde `deploy/local/`)

| Operación | Comando | Resultado esperado |
|-----------|---------|--------------------|
| Arrancar | `docker compose up -d --wait` | Código 0 con ambos servicios sanos. Distinto de 0 ante cualquier fallo. |
| Estado | `docker compose ps` | Lista de servicios y su estado de salud. |
| Registros | `docker compose logs [-f] [servicio]` | — |
| Detener | `docker compose stop` | Contenedores parados; los datos se conservan. |
| Reanudar | `docker compose up -d --wait` | Vuelve al estado arrancado con los mismos datos. |
| Limpiar | `docker compose down -v` | Elimina contenedores, red y volumen. |
| Verificar | `../../tools/check-backend.sh` | Ver [tooling-cli.md](tooling-cli.md). |
