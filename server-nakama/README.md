# Servidor Nakama

Aquí vivirán los módulos de servidor de Nakama: la lógica autoritativa sobre identidad,
progreso, economía, recompensas y partidas (constitución, principio I), con sus pruebas y su
configuración de compilación.

> **Estado**: vacío a propósito. El runtime de los módulos (Go, TypeScript/JavaScript o Lua)
> está pendiente: [D-01](../docs/decisions/pendientes.md#d-01--runtime-de-los-módulos-de-nakama).
> No se añade código ni configuración de ningún runtime hasta decidirlo.

## Límites

- No depende de código del cliente Unity ni de ninguna clase de Unity.
- Se comunica con el cliente solo mediante los contratos de [`shared/`](../shared/README.md).
- No accede directamente a las tablas internas de Nakama: usa las interfaces soportadas por su
  runtime. Las migraciones propias de la aplicación se documentarán aquí.
- Para ejecutar Nakama en local se usa el entorno de [`deploy/local/`](../deploy/local/README.md).

## Qué se añadirá al resolver D-01

En el **mismo cambio** que el ADR de la decisión:

1. El código fuente y su estructura, con las convenciones oficiales del runtime elegido.
2. Las pruebas, con el framework fijado para ese runtime (constitución, principio VI).
3. Las comprobaciones de formato y análisis estático del runtime.
4. Las reglas de `.gitignore` propias del runtime (artefactos de compilación, dependencias), sin
   ignorar los archivos de bloqueo.
5. La carga de los módulos en `deploy/local/compose.yaml` y la actualización de
   `deploy/local/README.md`.
6. Las instrucciones de compilación y pruebas en este README.
