# Quickstart de validación: Base del servidor TypeScript, CI y contratos JSON

Escenarios que demuestran que la funcionalidad cumple la spec. Los comandos están definidos en
[contracts/](contracts/); aquí solo se enumeran los pasos y los resultados esperados.

## Requisitos previos

- Los de la spec 001: Docker y Compose, git, bash y curl.
- Node 26.13.1 (o superior dentro de la 26) con npm 11.

## A. Servidor (sin Docker)

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| A1 | `cd server-nakama && npm ci` | Instala el lockfile sin cambios en `package-lock.json` | US1-1, FR-001 |
| A2 | `npm ci` con Node 24 (p. ej. con un gestor de versiones) | Falla por `engine-strict`, indicando la versión requerida (también con npm fuera de la rama 11) | US1-5 |
| A3 | `npm run check` | Código 0. Genera `build/index.js`, que pasa `check:es5` | US1-2/4, FR-002/003 |
| A4 | Fallo de tipos, de formato y de análisis introducidos por separado | `typecheck`, `format:check` o `lint` fallan indicando archivo y línea | US1-3 |
| A5 | Usar `Array.prototype.includes` en `src/` | `typecheck` falla (`lib: es5`) | Caso límite de APIs de Node o posteriores a ES5 |
| A6 | `git status` tras A3 | Sin `node_modules/`, `build/` ni `src/generated/` | FR-007 |
| A7 | `npm test` | Todas las pruebas unitarias pasan, incluida la tabla de casos de [rpc-protocol-version.md](contracts/rpc-protocol-version.md) | FR-006, FR-014 |

## B. Contratos

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| B1 | `npm run contracts:check` | Código 0 | FR-020 |
| B2 | Cambiar `maxRequestBytes` del contrato sin subir `version` | `contracts:check` falla por la huella de `protocol_version@1` | FR-020 |
| B3 | Añadir a `examples.request.valid` un ejemplo que no valida | Falla nombrando el ejemplo | FR-013/020 |
| B4 | Revisar `shared/README.md` | Secciones de formato, codificación de versión, validación, compatibilidad y organización completas, sin marcas de pendiente de D-05 | US4-1, FR-018 |

## C. Backend local con módulo

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| C1 | Sin `server-nakama/build/`: `docker compose up -d --wait` y `tools/check-backend.sh --module` | Nakama arranca o falla, según R-8 (anotar cuál); `--module` devuelve 4 con el mensaje de construir | FR-016/017 |
| C2 | `npm run build`, reiniciar el entorno y `--module` | Código 0; los registros muestran el módulo cargado y el RPC registrado | US2-1 |
| C3 | Revisar `docker compose logs nakama` | Ni `NAKAMA_RUNTIME_HTTP_KEY` ni otras claves aparecen en los registros | Riesgo R-8, principio VII |
| C4 | `npm run test:integration` | Pasan: respuesta correcta con sesión, 401 sin credenciales, `SESSION_REQUIRED` con `http_key`, campos desconocidos tolerados, `INVALID_ARGUMENT` y `PAYLOAD_TOO_LARGE` | US2-2…5, SC-002, SC-008 |
| C5 | Comparar los resultados de C4 con los de A7 sobre la tabla de casos | Mismo resultado en cada caso | US2-6 |
| C6 | `tools/tests/run.sh` y `tools/check-repo.sh` | Pruebas nuevas en verde; `check-repo` incluye el lockfile y `.node-version` del servidor | FR-017, FR-031 |

## D. CI (PR real)

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| D1 | Abrir el PR de la rama hacia `main` | Los trabajos `repo`, `server` y `backend` en verde, en ≤ 15 min en total | US3-1, SC-004 |
| D2 | Commits temporales con un fallo por trabajo (p. ej. romper una prueba de `tools/`, un error de tipos y un RPC sin registrar), revertidos después | Cada trabajo falla y su registro muestra la causa | US3-2, SC-003, FR-028 |
| D3 | Revisar `ci.yml` y los registros | Acciones fijadas por commit, `contents: read`, sin `secrets.` y sin valores de `.env` impresos | US3-3, SC-005, SC-006 |
| D4 | Revisar el README raíz | Describe qué ejecuta la CI, cuándo, cómo reproducirla y el paso manual de protección de rama | US3-5, FR-027 |

## E. Decisiones

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| E1 | Revisar `docs/decisions/` | ADR 0006, 0007 y 0008 aceptados. D-01, D-04 y D-05 en *Resueltas* de `pendientes.md` y en el índice | US4-3, FR-029, SC-007 |

Un escenario que no pueda ejecutarse se registra como **no verificado**, con su motivo, en el
*Registro de verificación* de `tasks.md`.
