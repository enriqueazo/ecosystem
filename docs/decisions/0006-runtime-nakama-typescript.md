# 0006 · Runtime de los módulos de Nakama: TypeScript

- **Estado**: aceptada
- **Fecha**: 2026-11-25

## Contexto

La lógica autoritativa del servidor (constitución, principio I) necesita un runtime de módulos
para Nakama 3.42.0 (decisión D-01). Hay tres opciones: Go, TypeScript/JavaScript o Lua. La
documentación oficial de Heroic Labs sobre el runtime JavaScript establece:

- la VM es **goja** y solo admite **ES5**;
- no hay APIs de Node ni de la web, sistema de archivos, hilos ni procesos;
- las variables globales no son estado fiable;
- `InitModule` y las funciones registradas deben ser funciones globales.

## Opciones consideradas

- **Go**: máximo rendimiento y tipado. El plugin debe compilarse con la versión exacta de Nakama
  y de su toolchain, lo que acopla cada actualización.
- **TypeScript/JavaScript**: tipado (TS) y herramientas conocidas. Requiere transpilar a ES5 y
  empaquetar en un archivo, y las pruebas en Node no equivalen al runtime real.
- **Lua**: sencillo, pero con menos herramientas y sin tipado.

## Decisión

Se usa **TypeScript**, compilado y empaquetado para la VM JavaScript de Nakama. Las pruebas en
Node se complementan con pruebas de integración contra Nakama. Cadena de herramientas
(research de la spec 002, R-1 a R-5):

| Pieza | Elección |
|-------|----------|
| Node / npm | 26.13.1 (LTS) / 11.20.0, impuestos con `engines` y `engine-strict` |
| Comprobación de tipos | TypeScript 7.0.2 (`tsc --noEmit`, `lib: ["es5"]`). Plan B si dejara de aceptar las definiciones de Nakama: TypeScript 6.0.3, solo para tipos |
| Transpilación y empaquetado | Rollup 4.68.8 + `@rollup/plugin-babel` 7.1.0 + Babel 7.29 (`preset-env` con `ie 11` y `preset-typescript`) → `build/index.js` |
| Verificaciones del artefacto | `check:es5` (acorn con `ecmaVersion: 5`) y `check:imports` (FR-004) |
| Formato y análisis | Biome 2.6.9 |
| Pruebas | `node:test` (unitarias e integración) |
| Tipos del runtime | `nakama-runtime` del commit de `nakama-common` v1.49.0 (exigida por Nakama 3.42.0), como tarball HTTPS con `integrity` |

Por qué se separa la comprobación de tipos de la transpilación:
- TypeScript 7 ya no emite ES5;
- `@rollup/plugin-babel` solo admite Babel 7;
- la guía oficial (`@rollup/plugin-typescript` emitiendo ES5) obligaría a quedarse en TypeScript ≤ 5.9.

## Consecuencias

- **Diferencias Node ↔ Nakama**, encontradas con las pruebas de integración (spec 002):
  - Nakama **congela** los objetos globales tras `InitModule`. Una función que guarda estado en sí
    misma falla con «object is not extensible». El generador de validadores elimina ese estado y
    una prueba congela las funciones en Node para detectarlo antes.
  - `nkruntime.Codes` es un `const enum` y no existe en ejecución: el módulo usa constantes propias,
    comparadas con las oficiales en una prueba.
- **Limitación conocida de Nakama 3.42.0**: el runtime JavaScript adjunta `error.stackTrace` a
  **toda** excepción lanzada por un módulo, también a los errores definidos en el contrato, y no
  hay opción para desactivarlo (`server/runtime_javascript.go`, `invokeFunction`). El módulo solo
  lanza objetos planos y convierte lo inesperado en `INTERNAL` (`runSafely`), así que la traza no
  incluye mensajes de excepción, datos de la petición ni secretos, solo nombres de función y
  posiciones del artefacto. Los clientes deben ignorar ese campo. Hay que revisarlo al actualizar
  Nakama.
- Cada cambio de Nakama obliga a revisar la versión de `nakama-common`.
- La forma del código queda condicionada: lógica pura en `src/rpc/` (probada en Node) y una capa
  fina en `src/main.ts` (probada contra Nakama).
- **Pregunta abierta (P-03)**: si las partidas en directo serán exigentes en rendimiento o por
  turnos. No afecta a esta base, pero hay que responderla antes de la spec de partidas. Si el
  rendimiento de la VM JavaScript no bastara, habría que reevaluar esta decisión para esos
  módulos.
