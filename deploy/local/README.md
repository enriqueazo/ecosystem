# Entorno de desarrollo local (Nakama + PostgreSQL)

Este es el procedimiento único para levantar, operar y limpiar el backend en tu máquina. El
resto de documentos enlazan aquí.

**Solo para desarrollo local.** No sirve de base para ningún otro entorno ni debe contener datos
reales. Todavía no carga módulos de servidor: el runtime de Nakama está pendiente
([D-01](../../docs/decisions/pendientes.md#d-01--runtime-de-los-módulos-de-nakama)).

## Qué se levanta

| Servicio | Versión | Accesible desde tu máquina |
|----------|---------|----------------------------|
| PostgreSQL | 16.16 | No: solo desde la red interna del entorno |
| Nakama | 3.42.0 | API de cliente en `127.0.0.1:7350` y consola en `127.0.0.1:7351` |

Versiones, digests y motivos en [ADR-0002](../../docs/decisions/0002-entorno-local-nakama-postgresql.md).
Los datos de PostgreSQL se guardan en un volumen de Docker (`ecosystem-local_postgres-data`), no
en el repositorio.

## Requisitos

- Docker Engine con Docker Compose v2 o posterior, con el demonio en marcha (p. ej. Docker
  Desktop). Versiones de referencia verificadas: Docker Engine 29.2.1 y Docker Compose v5.0.2 en
  macOS (arm64). Las versiones anteriores no se han comprobado.
- `bash` y `curl` para la verificación de conectividad.

## Configuración (una vez)

Desde `deploy/local/`:

```sh
cp .env.example .env
```

Edita `.env` y sustituye cada valor `cambia-esto-…` por uno propio. Por ejemplo, genera cada clave
con `openssl rand -hex 32`. Reglas:

- `POSTGRES_PASSWORD` solo puede tener letras y números, porque forma parte de la dirección de
  conexión de Nakama.
- `.env` no se versiona (está en `.gitignore`). No lo compartas.
- Si cambias `POSTGRES_PASSWORD` cuando ya hay datos, la base existente sigue con la contraseña
  anterior: limpia el entorno (ver más abajo) antes de arrancar.

Variables completas: [contrato del entorno local](../../specs/001-base-proyecto-entorno/contracts/local-environment.md#variables-env-a-partir-de-envexample).

## Operaciones

Todos los comandos se ejecutan desde `deploy/local/`.

| Operación | Comando | Resultado |
|-----------|---------|-----------|
| Arrancar | `docker compose up -d --wait` | Termina con código 0 cuando ambos servicios están sanos. Nakama aplica sus migraciones al arrancar. |
| Verificar | `../../tools/check-backend.sh` | `Backend disponible en 127.0.0.1:7350` y código 0. |
| Estado | `docker compose ps` | Servicios y su estado de salud (`healthy`). |
| Registros | `docker compose logs -f nakama` (o `postgres`) | — |
| Detener | `docker compose stop` | Para los contenedores y **conserva** los datos. |
| Reanudar | `docker compose up -d --wait` | Vuelve a arrancar con los mismos datos. |
| Limpiar | `docker compose down -v` | Borra contenedores, red y **todos los datos** del entorno. |

Estados del entorno: limpio → arrancado ⇄ detenido → limpio
([modelo](../../specs/001-base-proyecto-entorno/data-model.md#entorno-local)).

### Consola de administración

Abre <http://127.0.0.1:7351> y entra con `NAKAMA_CONSOLE_USERNAME` y `NAKAMA_CONSOLE_PASSWORD` de
tu `.env`.

## Resolución de problemas

| Síntoma | Causa | Solución |
|---------|-------|----------|
| `required variable … is missing a value: Falta …` y no se crea ningún contenedor | No existe `.env` o falta un valor | Copia `.env.example` a `.env` y rellena la variable que indica el mensaje. |
| `Cannot connect to the Docker daemon` o `failed to connect to the docker API`; `check-backend.sh` devuelve 3 | Docker no está en marcha | Arranca Docker Desktop (o el servicio de Docker) y repite. |
| `ports are not available … address already in use` | Otro proceso usa el puerto 7350 o 7351 | Libera el puerto o cambia `NAKAMA_API_PORT` / `NAKAMA_CONSOLE_PORT` en `.env`. `check-backend.sh` lee el puerto de `.env`. |
| `check-backend.sh` devuelve 1 | El entorno está detenido o no ha terminado de arrancar | `docker compose up -d --wait` y repite. |
| Error al descargar la imagen (`manifest unknown`, `no matching manifest`) | Sin red, o la arquitectura de tu equipo no está publicada | Comprueba la conexión. Las imágenes fijadas publican `amd64` y `arm64`; con otra arquitectura, abre una decisión para elegir alternativa. |
| `nakama` no llega a `healthy` y los registros muestran un error de migración o de autenticación en la base de datos | Datos locales inconsistentes o `POSTGRES_PASSWORD` cambiada | `docker compose down -v` y arranca de nuevo. **Nunca** se corrige con SQL contra las tablas internas de Nakama. |

## Límites

- No se publica PostgreSQL ni el puerto gRPC de Nakama.
- No se cargan módulos de servidor ni se monta ningún directorio en Nakama hasta resolver D-01.
- Nivel de registro `INFO`: los registros no muestran la contraseña ni las claves.
