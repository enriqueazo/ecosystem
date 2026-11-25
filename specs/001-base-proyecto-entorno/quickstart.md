# Quickstart de validación: Base del proyecto y entorno de desarrollo

Guía para demostrar que la funcionalidad cumple la spec. Los comandos y variables están definidos
en [contracts/local-environment.md](contracts/local-environment.md) y
[contracts/tooling-cli.md](contracts/tooling-cli.md); aquí solo se enumeran los escenarios y sus
resultados esperados.

## Requisitos previos

- Git, bash y curl.
- Docker Engine y Compose en ejecución (verificado con 29.2.1 / v5.0.2).
- Solo para los escenarios C: Unity Hub con la versión fijada en ADR-0005, los módulos Android e
  iOS, y Xcode en macOS.

## A. Repositorio (sin Docker ni Unity en marcha)

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| A1 | `tools/tests/run.sh` | Código 0, todas las pruebas pasan. | FR-039, Principio VI |
| A2 | `tools/check-repo.sh` | Código 0. | FR-039, FR-027 |
| A3 | Crear `deploy/local/.env`, hacer `git add -f` y ejecutar `tools/check-repo.sh --only secrets`; después `git restore --staged`. | Código 1 nombrando el archivo. | US5-2, SC-004 |
| A4 | `git ls-files` y revisar las áreas de primer nivel. | Cada área tiene README o contenido; no hay carpetas vacías ni `.gitkeep`. | FR-002, SC-008 |
| A5 | Buscar reglas o archivos de un runtime (Go, TS/JS o Lua) en archivos versionados fuera de `specs/` y `docs/decisions/`. | Ninguna coincidencia. | FR-025, SC-010 |

## B. Backend local (desde `deploy/local/`)

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| B1 | Sin `.env`: `docker compose up -d --wait`. | Aborta sin crear contenedores, nombra la variable e indica copiar `.env.example`. | Edge case, SC-005 |
| B2 | Copiar `.env.example` a `.env` y vaciar `NAKAMA_SERVER_KEY`; arrancar. | Aborta nombrando `NAKAMA_SERVER_KEY`. | FR-018 |
| B3 | Restaurar el valor y arrancar. | Código 0 y ambos servicios sanos. En los registros de `nakama` aparece la migración, y no aparecen la contraseña ni las claves. | US1-1, FR-014/015, riesgo R-4 |
| B4 | `../../tools/check-backend.sh` | Código 0. | US1-2, FR-038 |
| B5 | Listar los puertos en escucha (`lsof -iTCP -sTCP:LISTEN -nP` en macOS) y `docker compose ps`. | Solo `127.0.0.1:<API>` y `127.0.0.1:<consola>`; `postgres` sin puertos publicados. | US1-5, FR-016, SC-003 |
| B6 | Crear una cuenta con la API de cliente: `curl -s -u "$NAKAMA_SERVER_KEY:" -X POST "http://127.0.0.1:<API>/v2/account/authenticate/device?create=true" -H 'Content-Type: application/json' -d '{"id":"verificacion-local-0001"}'` (el id debe tener entre 10 y 128 caracteres) y anotar el `user_id` de la sesión. Después `stop`, `start` y repetir la llamada con `create=false`. Repetir el ciclo 3 veces. | En cada ciclo, la llamada con `create=false` devuelve una sesión del mismo usuario y `check-backend.sh` devuelve 0. | US1-3, SC-002 |
| B7 | `stop` y `../../tools/check-backend.sh` | Código 1 con "backend no disponible". | Edge case |
| B8 | `down -v`, arrancar y repetir la llamada de B6 con `create=false`. | Error de cuenta no encontrada. | US1-4, FR-017 |
| B9 | Con el entorno detenido (`down`), ocupar el puerto de la API con otro proceso y arrancar sin cambiar `NAKAMA_API_PORT`. | El arranque falla mostrando el puerto ocupado. Al cambiar `NAKAMA_API_PORT`, funciona. | Edge case |
| B10 | Detener Docker Desktop y ejecutar `check-backend.sh`. | Código 3, "Docker no está en ejecución". | Edge case |
| B11 | Medir el tiempo desde un clon limpio hasta B4, siguiendo solo el README (con las imágenes ya descargadas). | ≤ 15 min. | SC-001 |

## C. Cliente Unity (bloqueado hasta D-02)

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| C1 | Abrir `client-unity/` desde un clon limpio con la versión fijada. | 0 errores de compilación y consola sin errores. | US2-1, SC-006 |
| C2 | Reproducir `Bootstrap`. | Sin errores ni avisos de scripts ausentes. | US2-2 |
| C3 | Ejecutar las pruebas EditMode y PlayMode (Test Runner o modo batch, R-8). | 100 % en verde. | US2-3, FR-037 |
| C4 | Generar una build de desarrollo para Android. | Se produce un APK sin errores. | US2-4 |
| C5 | Generar la build de iOS. | Se exporta un proyecto Xcode sin errores. | US2-5 |
| C6 | Cerrar el editor y ejecutar `git status`. | Sin cambios. | US2-6, FR-011, SC-007 |

## D. Documentación

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| D1 | Revisar el README raíz contra FR-034. | Contiene todos los apartados; el estado de la CI es "no configurada (D-04)". | US3-1, FR-042 |
| D2 | Buscar cada D-NN. | Su definición completa aparece solo en `docs/decisions/pendientes.md`; el resto de documentos enlaza. | US3-3, FR-033, SC-009 |
| D3 | Revisar `shared/README.md` contra FR-021. | Cubre (a)–(f), con (b) marcado como pendiente de D-05 y sin mensajes de juego. | US4-1, FR-022/023 |

Si un escenario no puede ejecutarse (p. ej. los C sin Unity), se registra como **no verificado**
indicando el motivo (FR-043).
