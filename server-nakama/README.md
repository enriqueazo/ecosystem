# Servidor Nakama (TypeScript)

Módulos de servidor de Nakama: la lógica autoritativa sobre identidad, progreso, economía,
recompensas y partidas (constitución, principio I). Escritos en **TypeScript** y transpilados a
un único archivo **ES5** para la VM JavaScript (goja) de Nakama 3.42.0
([ADR-0006](../docs/decisions/0006-runtime-nakama-typescript.md)).

> **Estado**: base técnica. Un único RPC, `protocol_version`, sin lógica de juego.

## Límites

- No depende de código del cliente Unity ni de ninguna clase de Unity.
- Se comunica con el cliente solo mediante los contratos de [`shared/`](../shared/README.md).
- No accede directamente a las tablas internas de Nakama: usa las interfaces soportadas por el
  runtime. Las migraciones propias de la aplicación se documentarán aquí.
- Para ejecutar Nakama en local se usa [`deploy/local/`](../deploy/local/README.md).

## Requisitos

- **Node 26** (26.13.1 o posterior dentro de la 26; la versión exacta está en `.node-version`) con
  **npm 11** (11.20.0 o posterior dentro de la 11). `npm ci` falla con `EBADENGINE` fuera de esos
  rangos (`engine-strict`).
- Para las pruebas de integración: el entorno local arrancado (Docker), ver
  [deploy/local/README.md](../deploy/local/README.md).

## Instalación

Desde `server-nakama/`:

```sh
npm ci
```

Instala exactamente lo que fija `package-lock.json` (todas son dependencias de desarrollo). Los
tipos del runtime (`nakama-runtime`) vienen del commit de la etiqueta `v1.49.0` de
`heroiclabs/nakama-common`, la versión que exige Nakama 3.42.0.

## Comandos

| Comando | Qué hace |
|---------|----------|
| `npm run check` | Todo, en el mismo orden que la CI: formato, análisis, imports, tipos, pruebas unitarias, contratos y construcción. |
| `npm run build` | Genera `src/generated/`, empaqueta `build/index.js` y comprueba que es ES5. |
| `npm test` | Pruebas unitarias (`node:test`, sin Nakama). |
| `npm run test:integration` | Pruebas contra el Nakama local (ver *Pruebas*). |
| `npm run format` | Aplica el formato de Biome. |

Lista completa y códigos de salida en
[el contrato de comandos](../specs/002-servidor-typescript-ci/contracts/server-scripts.md). Los
comandos de contratos están descritos en [shared/README.md](../shared/README.md#g-validación-y-comprobación).

## Estructura

```text
server-nakama/
├── src/
│   ├── main.ts               InitModule y funciones RPC globales (capa fina sobre Nakama)
│   ├── errors.ts             Códigos gRPC, error estándar y runSafely
│   ├── payload.ts            Tamaño UTF-8 y análisis JSON de la carga
│   ├── rpc/                  Lógica pura de cada RPC (sin dependencias de Nakama)
│   └── generated/            Validadores y constantes desde shared/contracts/ (ignorado)
├── test/
│   ├── cases/                Tablas de casos compartidas entre unitarias e integración
│   ├── unit/                 Pruebas en Node
│   └── integration/          Pruebas contra Nakama local
├── scripts/                  generate, contracts, check-es5 y check-imports
└── build/index.js            Artefacto que carga Nakama (ignorado)
```

## Restricciones de la VM de Nakama

Lo que funciona en Node no siempre funciona en Nakama. Estas reglas se imponen con herramientas
siempre que se puede:

| Restricción | Cómo se impone |
|-------------|----------------|
| Solo ES5: sin `let`/`const`, clases ni flechas en la salida | Babel transpila a ES5 y `check:es5` analiza el artefacto |
| Sin APIs posteriores a ES5 (`includes`, `Map`, `Object.entries`…) | `tsconfig.json` con `lib: ["es5"]` |
| Sin paquetes de npm ni APIs de Node/web en ejecución | `check:imports`: `src/` solo importa módulos relativos y tipos de `nakama-runtime` |
| `InitModule` y las funciones registradas con `registerRpc` deben ser `function` globales, y el identificador del RPC, un literal | Convención en `src/main.ts` |
| `nkruntime.Codes` es un `const enum`: no existe en ejecución | Constantes propias en `src/errors.ts`, comparadas con las oficiales en una prueba |
| Nakama **congela** los objetos globales tras `InitModule`: no se puede guardar estado en funciones ni en variables globales | El generador elimina el estado de los validadores y falla si un esquema lo necesitaría; una prueba congela las funciones en Node |
| Toda excepción llega al cliente con una traza (`error.stackTrace`) | `runSafely` lanza solo objetos planos y convierte lo inesperado en `INTERNAL` |

## Pruebas

- **Unitarias** (`npm test`): la lógica pura de `src/` y los scripts, sin servicios externos.
- **Integración** (`npm run test:integration`): contra el Nakama de `deploy/local/`.
  1. Construye el módulo (`npm run build`) y arranca el entorno (ver
     [deploy/local/README.md](../deploy/local/README.md)).
  2. Las pruebas toman las claves y el puerto de `deploy/local/.env` (o de las variables
     `NAKAMA_SERVER_KEY`, `NAKAMA_RUNTIME_HTTP_KEY` y `NAKAMA_API_PORT`). Crean una cuenta de
     prueba por dispositivo en la base de datos local.
  3. Si el módulo no está cargado, fallan de inmediato con «Entorno local no disponible».
- Las tablas de `test/cases/` se recorren tanto en las unitarias como en las de integración, para
  demostrar que el resultado es el mismo en Node que en Nakama.
- TDD: la prueba se escribe primero y se ve fallar (constitución, principio VI).

## Añadir un RPC

1. Define su contrato en `shared/contracts/` y registra la huella (ver
   [shared/README.md](../shared/README.md)).
2. Escribe los casos en `test/cases/` y las pruebas unitarias y de integración.
3. Implementa la lógica pura en `src/rpc/` y regístrala en `src/main.ts` con una `function` global
   envuelta en `runSafely`.
4. `npm run check` y `npm run test:integration` en verde.
