# 0002 · Entorno local con Docker Compose: Nakama 3.42.0 y PostgreSQL 16.16

- **Estado**: aceptada
- **Fecha**: 2026-11-25

## Contexto

El equipo necesita levantar Nakama y PostgreSQL en local de forma reproducible, sin secretos en
Git, sin exponer servicios innecesarios y sin elegir todavía el runtime de módulos (D-01). La
constitución exige fijar versiones exactas antes de la primera compilación (decisión D-03).

## Opciones consideradas

- **Orquestación**:
  - *Docker Compose* (disponible: Docker 29.2.1, Compose v5.0.2): un único archivo, estándar y
    con comprobaciones de salud.
  - *Instalación nativa de PostgreSQL y Nakama*: más difícil de reproducir entre equipos.
- **Base de datos**: PostgreSQL 16 (es la que usa la plantilla oficial de Heroic Labs, y el foro
  oficial confirma compatibilidad con 16 o superior), 17/18 (sin respaldo explícito en la
  plantilla), o CockroachDB (descartada: la constitución fija PostgreSQL).
- **Fijación de imágenes**: solo etiqueta (legible, pero la etiqueta puede reconstruirse) o
  etiqueta + digest (reproducible).

## Decisión

- Docker Compose en `deploy/local/compose.yaml`, con dos servicios.
- Imágenes, fijadas por etiqueta + digest del índice multiarquitectura (publican `amd64` y
  `arm64`), obtenidos el **2026-11-25**:

  | Imagen | Digest |
  |--------|--------|
  | `postgres:16.16-alpine` | `sha256:f2f98726820d0342055351c6e9d36e705f5bfffb3dd53ff556325114b08afb6d` |
  | `registry.heroiclabs.com/heroiclabs/nakama:3.42.0` | `sha256:009bfadc8264e4c4e82f1c1bd8b800a74e4dfeb03be59e0faab79117d394cfb8` |

- Nakama 3.42.0 es la última versión publicada (2026-11-03). PostgreSQL 16.16 es la última
  versión menor de la rama 16 en la fecha de la decisión.
- Partimos de la plantilla oficial de Heroic Labs, con estos cambios:
  - solo se publican la API de cliente y la consola, en `127.0.0.1`; ni PostgreSQL ni gRPC;
  - no se monta `/nakama/data` ni ningún directorio de módulos;
  - los secretos son obligatorios y vienen de `.env`; si falta alguno, el arranque aborta;
  - se usa un usuario propio de base de datos (`nakama`), sin `container_name` fijo y con
    `restart: unless-stopped`;
  - Nakama aplica sus migraciones con `nakama migrate up` antes de arrancar y espera a que
    PostgreSQL esté sano.

## Consecuencias

- Docker es un requisito previo para trabajar con el backend.
- Un `docker pull` posterior obtiene exactamente las mismas imágenes en cualquier equipo.
- **Actualizar versiones** es un cambio propio y explícito (principio VIII):
  1. Elegir la nueva etiqueta y comprobar la compatibilidad Nakama ↔ PostgreSQL en las notas de
     versión.
  2. Obtener el digest con `docker buildx imagetools inspect <imagen>:<etiqueta>` y confirmar que
     publica `amd64` y `arm64`.
  3. Actualizar `deploy/local/compose.yaml` y crear un ADR que sustituya a este.
  4. Validar con los escenarios B del quickstart de la spec 001.
- Al decidir el runtime (D-01) habrá que montar los módulos en Nakama, en el mismo cambio que su
  ADR.
