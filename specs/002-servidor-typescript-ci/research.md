# Research: Base del servidor TypeScript, CI y contratos JSON

**Fecha**: 2026-11-25 · **Spec**: [spec.md](spec.md) · **Plan**: [plan.md](plan.md)

Fuentes principales:
- documentación oficial *TypeScript Runtime* de Heroic Labs (leída el 2026-11-25; la página de
  *setup* redirige a ella);
- metadatos del registro npm (`npm view`);
- índice de versiones de nodejs.org;
- API de GitHub (etiquetas de acciones y de `nakama-common`).

## R-1 · Versión de Node y gestor de paquetes (P-01) — RESUELTA

- **Decisión**: **Node 26.13.1** con el **npm 11.20.0** que trae.
  - `server-nakama/.node-version` contiene `26.13.1` y lo leen la CI y los gestores de versiones.
  - `package.json` declara `engines.node: ">=26.13.1 <27"` y `engines.npm: ">=11.20.0 <12"`.
  - `.npmrc` usa `engine-strict=true`, así que la instalación falla fuera de la rama 26 de Node o de la 11 de npm.
- **Fundamento**: 26.13.1 es la última versión de la rama 26, publicada el 2026-11-10 con la
  etiqueta LTS «Lithium» según el índice de nodejs.org. La rama 26 entró en *Active LTS* a
  finales de octubre de 2026 y la 24 pasó a *Maintenance*. Además, es la versión instalada en la
  máquina de trabajo.
- **Alternativas**: Node 24 (en mantenimiento, pierde soporte antes) y pnpm o Yarn (otra
  herramienta que instalar, sin una necesidad que lo justifique: npm viene con Node y su
  `package-lock.json` cumple con la reproducibilidad).
- **Por qué no se fija la revisión exacta en `engines`**: obligar a cada desarrollador a tener
  exactamente 26.13.1 bloquearía en cuanto se publique un parche. La revisión exacta la fijan
  `.node-version` (para la CI) y el lockfile (para las dependencias). Fuera de la rama 26 la
  instalación falla (FR-001 y el caso límite de versión distinta).

## R-2 · Cadena de construcción para la VM de Nakama (FR-003) — RESUELTA

Restricciones verificadas en la documentación oficial:
- «The JavaScript runtime is powered by the goja VM which currently supports the JavaScript ES5
  spec» y «we don't support versions past ES5».
- No hay soporte para bibliotecas que requieran Node, APIs web o código nativo. Tampoco hay
  acceso a sistema de archivos, E/S, hilos ni procesos, y las variables globales no son estado
  fiable.
- `InitModule` es una función global. Las funciones que se pasan a `initializer.registerRpc`
  deben declararse en ámbito global, no como variables. Con Rollup, la guía añade
  `!InitModule && InitModule.bind(null);` para que el *tree-shaking* no las elimine.
- El artefacto se carga con `runtime.js_entrypoint`.

Conflictos con las versiones actuales del ecosistema:
- **TypeScript 7.0.2** (la versión nativa) ya no ofrece `target: "es5"`, que TypeScript 6
  declaró obsoleto.
- **`@rollup/plugin-babel` 7.1.0** declara `peerDependencies: @babel/core ^7`; Babel 8 no es
  compatible.
- La guía oficial usa `@rollup/plugin-typescript` para emitir ES5, lo que obligaría a usar TS ≤
  5.9.

**Decisión**: separar la comprobación de tipos de la transpilación.

| Función | Herramienta | Versión |
|---------|-------------|---------|
| Comprobación de tipos (`tsc --noEmit`) | `typescript` | 7.0.2 |
| Empaquetado en un único archivo | `rollup` | 4.68.8 |
| Resolución de módulos y CJS (helpers de Ajv) | `@rollup/plugin-node-resolve`, `@rollup/plugin-commonjs` | 16.0.3, 29.0.3 |
| Transpilación TS → ES5 | `@rollup/plugin-babel` + `@babel/core` + `@babel/preset-env` + `@babel/preset-typescript` | 7.1.0, 7.29.13, 7.29.14, 7.29.7 |
| Verificación de que la salida es ES5 | `acorn` (análisis con `ecmaVersion: 5`) | la última 8.x al instalar, fijada en el lockfile |

- `tsconfig.json` usa `"lib": ["es5"]`, de modo que la comprobación de tipos rechaza APIs
  posteriores a ES5 (`Array.prototype.includes`, `Object.entries`, `Map`…). Babel solo
  transforma sintaxis y no añade polyfills.
- `@babel/preset-env` usa `targets: { "ie": "11" }` para forzar la salida ES5 de forma explícita.
  La guía oficial no indica `targets` y depende de los valores por defecto, que cambian entre
  versiones.
- Formato de salida de Rollup `es` sin exportaciones: las declaraciones quedan en el ámbito
  global. `InitModule` y las funciones RPC se declaran con `function` en `src/main.ts`, con el
  truco de referencia de la guía.
- **Enums del runtime**: `nkruntime.Codes` es un `const enum` en las definiciones. Babel
  transpila archivo a archivo y no puede sustituirlo por su valor, así que el código usará
  constantes numéricas propias con los códigos gRPC (3 = INVALID_ARGUMENT, 7 =
  PERMISSION_DENIED…), declaradas en `src/errors.ts` y comprobadas por las pruebas.
- **Comprobación final**: `npm run check:es5` analiza `build/index.js` con acorn (`ecmaVersion:
  5`). Si se cuela sintaxis ES2015+, falla antes de que Nakama la rechace.
- **Alternativas descartadas**:
  - fijar TypeScript 5.9.3 con `@rollup/plugin-typescript` (la cadena oficial): ata el proyecto a
    una rama antigua del compilador;
  - esbuild: no puede transformar a ES5 `let`/`const`, clases ni desestructuración, y su
    *tree-shaking* elimina funciones globales no referenciadas;
  - `tsc --outFile` sin empaquetador: no puede incluir los validadores generados ni las
    dependencias.
- **Riesgo**: que TypeScript 7 no acepte las definiciones de `nakama-common` o `lib: ["es5"]`.
  Plan B documentado: TypeScript 6.0.3 solo para la comprobación de tipos, sin afectar a la
  transpilación. Se verifica en la primera tarea de implementación.

## R-3 · Definiciones de tipos del runtime — RESUELTA

- **Decisión**: el paquete `nakama-runtime` se instala desde GitHub, fijado a la etiqueta
  **`v1.49.0`** de `heroiclabs/nakama-common` (commit `41fa72eb631c3152d36510126f7660864c115955`).
  Las notas de la versión 3.42.0 de Nakama exigen `nakama-common` v1.49.0. Entrada en
  `package.json`: `"nakama-runtime": "github:heroiclabs/nakama-common#v1.49.0"`. El lockfile
  registra el commit resuelto.
- Es una dependencia de desarrollo (solo tipos) y Rollup la marca como `external`.

## R-4 · Formato y análisis (P-02) — RESUELTA

- **Decisión**: **Biome 2.6.9** (`@biomejs/biome`) como formateador y analizador de TS, JS y
  JSON, en una sola herramienta.
  - Configuración en `server-nakama/biome.json`, con `formatter.useEditorconfig: true` para
    coherencia con `.editorconfig`.
  - Comandos: `format:check` (`biome format`), `lint` (`biome lint`) y `format` (aplica cambios).
- **Alternativas**: ESLint + Prettier (dos herramientas y más dependencias, sin reglas
  necesarias que Biome no cubra en esta fase).
- La regla de no usar APIs de Node en `src/` se impone con `lib: ["es5"]`, sin `@types/node` en
  el `tsconfig` de `src/`, y con la comprobación ES5. Las pruebas tienen su propio `tsconfig`
  con los tipos de Node.

## R-5 · Pruebas (FR-006, FR-014) — RESUELTA

- **Unitarias**: el ejecutor integrado `node:test` y la eliminación nativa de tipos de Node 26
  (sin dependencias). Las pruebas importan los `.ts` directamente.
  - `tsconfig` con `allowImportingTsExtensions` y `erasableSyntaxOnly` para usar solo sintaxis
    que Node puede eliminar (sin `enum`, `namespace` ni propiedades de parámetro).
  - Comando: `npm test` → `node --test test/unit/`.
- **Integración**: también `node:test`, en `test/integration/`, contra el Nakama del entorno
  local por HTTP (`fetch` nativo).
  - Usan las claves de `deploy/local/.env` o variables de entorno.
  - Comando: `npm run test:integration`. Requieren el entorno arrancado con el módulo construido;
    si no lo está, fallan de inmediato con un mensaje claro.
- **Equivalencia Node ↔ Nakama** (US2, escenario 6): una tabla de casos compartida
  (`test/cases/protocol-version.cases.ts`). La recorren las pruebas unitarias, contra la función
  pura, y las de integración, contra el RPC real, comparando resultado o código de error.
- **Alternativas descartadas**: Jest (es la guía de pruebas de Heroic Labs, pero añade muchas
  dependencias y transformación propia) y Vitest (innecesario teniendo `node:test`).

## R-6 · Contratos JSON (D-05, FR-018–FR-020) — RESUELTA

- **Formato**: un archivo JSON por contrato en `shared/contracts/`, validado por un metaesquema
  propio (`shared/contracts/contract.schema.json`). Cada contrato declara:
  - identificador, tipo (`rpc`), versión, requisito de autenticación y tamaño máximo de petición;
  - esquemas de petición y respuesta en **JSON Schema 2020-12**;
  - errores y ejemplos válidos e inválidos.

  Detalle en [contracts/contract-format.md](contracts/contract-format.md).
- **Una sola definición para cliente y servidor (FR-019)**:
  - **servidor**: un script de generación (`npm run generate`) compila los esquemas con **Ajv
    8.20.0** en modo *standalone* a funciones de validación JavaScript, además de las
    constantes (versión de protocolo, versión del módulo y límites), en `src/generated/`
    (ignorado). Rollup las empaqueta y Babel las baja a ES5;
  - **cliente C#**: futuro, fuera de alcance. Validará con una biblioteca de JSON Schema 2020-12
    los mismos archivos.

  Nadie reescribe la definición a mano.
- **Dependencias de ejecución (FR-004)**: el código *standalone* de Ajv puede requerir pequeños
  ayudantes de `ajv/dist/runtime/` (p. ej. `ucs2length` para `maxLength`), que se empaquetan en
  el artefacto. Es la única dependencia de ejecución y se justifica en ADR-0008. Ajv como
  compilador es solo de desarrollo.
- **Versiones**:
  - la **versión de protocolo** está en `shared/contracts/protocol.json` (`{"protocolVersion":
    1}`);
  - la **versión de contrato** está en cada definición;
  - los mensajes **no** llevan la versión de contrato en esta fase. El cliente consulta la
    versión de protocolo con el RPC técnico al iniciar sesión y compara.
  - La codificación de versiones en mensajes de tiempo real se decidirá con la spec de partidas.
- **Comprobación automática (FR-020)**: `npm run contracts:check` hace estas comprobaciones:
  1. cada contrato cumple el metaesquema;
  2. los esquemas compilan;
  3. los ejemplos válidos validan y los inválidos no;
  4. los identificadores son únicos;
  5. la huella SHA-256 del contenido normativo de cada contrato coincide con la registrada en
     `shared/contracts/contracts.lock.json` para su `id@version`.

  Cambiar un contrato sin subir su versión rompe el paso 5. Al subir la versión hay que
  regenerar la huella de forma explícita (`npm run contracts:lock`), y el cambio queda visible
  en la revisión.

  Es más estricto que «solo si es incompatible»: cualquier cambio exige subir la versión, y la
  clasificación como compatible o incompatible se hace en la revisión con la lista de
  `shared/README.md`.
- **Dónde viven las herramientas**: en `server-nakama/` (scripts de npm), porque es el único
  proyecto con Node. Se documentan en `shared/README.md`. No se crea un proyecto Node aparte
  para `shared/`, porque supondría otro lockfile sin una necesidad que lo justifique.
- **Alternativas descartadas**: Protocol Buffers (D-05 ya decidió JSON), TypeBox/Zod (definición
  en TypeScript, no reutilizable desde C#) y un validador escrito a mano (duplica la definición).

## R-7 · RPC técnico «protocol_version» y errores — RESUELTA

- **Autenticación**:
  - sin credenciales, Nakama responde 401 antes de llegar al módulo;
  - con sesión, `ctx.userId` no está vacío;
  - con la clave HTTP de servidor (`http_key`), el módulo recibe `ctx.userId` vacío y rechaza
    con `PERMISSION_DENIED` (código gRPC 7) y el código de contrato `SESSION_REQUIRED` (FR-011).
- **Tamaño**: se mide en bytes UTF-8 sobre la cadena de entrada, con una función pura en ES5,
  antes de analizar el JSON. Si supera `maxRequestBytes`, devuelve `PAYLOAD_TOO_LARGE`.
- **Cuerpo vacío**: una cadena vacía equivale a `{}`.
- **Errores**: Nakama solo transporta un código gRPC numérico y un mensaje. El módulo lanza
  `{ code: <gRPC>, message: <JSON del error estándar> }`, con el error estándar de
  `shared/README.md`: `{"code":"INVALID_ARGUMENT","message":"…"}`. Las pruebas de integración
  verifican la forma HTTP real (código de estado y cuerpo) y la documentan en el contrato.
- **Versión del módulo**: la `version` de `server-nakama/package.json` (inicial `0.1.0`). El
  script de generación la inyecta como constante, para no empaquetar `package.json`.
- **Registro**: el módulo escribe una línea `info` al cargarse con el nombre y la versión, sin
  datos de la petición.

## R-8 · Carga en el entorno local y verificación (FR-015–FR-017) — RESUELTA (con verificación pendiente)

- `deploy/local/compose.yaml`:
  - monta `../../server-nakama/build` en `/nakama/data/modules` en **solo lectura**;
  - añade `--runtime.js_entrypoint index.js` (ruta relativa a `runtime.path`, por defecto
    `/nakama/data/modules`).
- **Artefacto ausente**: hay que verificar en la implementación qué hace Nakama 3.42.0 si falta
  `index.js`. Si arranca sin módulo en silencio, `check-backend.sh --module` lo detecta igual.
  Docker crea la carpeta vacía si no existe; está ignorada por Git.
- **`tools/check-backend.sh --module`** (nueva opción; sin ella, el comportamiento y los códigos
  no cambian):
  - llama al RPC con la clave HTTP de `NAKAMA_RUNTIME_HTTP_KEY` (entorno o `.env`). La clave va
    en la cadena de consulta, el único mecanismo que admite Nakama para `http_key`;
  - HTTP 403 con `SESSION_REQUIRED` → módulo cargado (código 0);
  - HTTP 404 (función no encontrada) → **código 4**, «módulo no cargado: ejecuta `npm run
    build` en server-nakama y reinicia el entorno».
  - Hay que verificar que la clave en la URL no aparece en los registros con nivel `INFO`, igual
    que en la spec 001.

## R-9 · CI con GitHub Actions (D-04, FR-021–FR-028) — RESUELTA

- **Archivo**: `.github/workflows/ci.yml`.
  - **Disparadores**: `pull_request` hacia `main` y `push` a `main`.
  - **Concurrencia**: cancela ejecuciones anteriores de la misma rama.
  - **Permisos**: `permissions: contents: read`.
- **Ejecutor**: `ubuntu-24.04`, fijado (no `ubuntu-latest`) por reproducibilidad. Incluye Docker
  y Compose v2.
- **Acciones**, fijadas por commit con la versión en un comentario:
  - `actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1  # v7.0.1`
  - `actions/setup-node@949feb2413d6458794dcd2491c4babbbce0c15c1  # v7.1.0`, con
    `node-version-file: server-nakama/.node-version` y caché de npm por
    `server-nakama/package-lock.json` (R-02 de la spec).
- **Trabajos**:
  1. `repo` (límite de 10 min): `tools/tests/run.sh` y `tools/check-repo.sh`.
  2. `server` (límite de 15 min): `npm ci`, `npm run check` (formato, lint, tipos, pruebas
     unitarias, contratos, construcción y ES5).
  3. `backend` (límite de 20 min):
     - `npm ci` y `npm run build`;
     - generación de `deploy/local/.env` con valores aleatorios de `openssl rand` en un paso que
       no los imprime;
     - `docker compose up -d --wait`, `tools/check-backend.sh --module` y
       `npm run test:integration`;
     - si falla, `docker compose logs`; siempre, `docker compose down -v`.
- **Sin secretos del repositorio**. Los PR desde forks funcionan con permisos de solo lectura.
- **Validación (FR-028)**: un PR real con la CI en verde y, en commits temporales que se revierten
  antes de integrar, un fallo deliberado por trabajo.
- **Paso manual del responsable**: en GitHub, *Settings → Branches →* regla para `main` con los
  tres trabajos obligatorios. Se documenta en el README raíz y en ADR-0007.
- **Alternativas**: un único trabajo secuencial (peor diagnóstico y sin paralelismo) o acciones
  por etiqueta (mutables; prohibido por FR-022).

## R-10 · Reglas de Git y del repositorio — RESUELTA

- `.gitignore`: `/server-nakama/node_modules/`, `/server-nakama/build/`,
  `/server-nakama/src/generated/` y `*.tsbuildinfo`. **No** se ignoran `package-lock.json`,
  `.node-version`, `.npmrc`, `biome.json`, `tsconfig*.json`, `babel.config.json` ni
  `rollup.config.mjs`.
- `.editorconfig`: `*.{ts,mjs}` con 2 espacios, coherente con Biome.
- `tools/check-repo.sh` (FR-031): añade `server-nakama/package-lock.json` y
  `server-nakama/.node-version` a la lista de imprescindibles cuando existe
  `server-nakama/package.json`. Lleva su prueba en `tools/tests/run.sh`.
