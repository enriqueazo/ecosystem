# 0001 · Estructura del monorepo

- **Estado**: aceptada
- **Fecha**: 2026-11-25

## Contexto

La constitución fija un monorepo Git con carpetas diferenciadas para cliente Unity, servidor
Nakama, contratos compartidos, despliegue y documentación, y exige que cliente y servidor solo se
comuniquen mediante contratos. Hacía falta concretar nombres y límites de cada área, y evitar
carpetas vacías o anticipadas que nadie mantiene.

## Opciones consideradas

- **Áreas de primer nivel por responsabilidad, con los nombres ya presentes** (`client-unity`,
  `server-nakama`, `shared`, `deploy`, `docs`, `tools`): coherente con lo existente y fácil de
  recorrer.
- **Renombrar `shared/` a `contracts/`**: más explícito, pero rompe con lo existente. El riesgo
  que evita (que `shared/` se use como cajón de código común) se cubre igual con una regla
  escrita en su README.
- **Estructura anidada por capas** (`apps/`, `packages/`…): sobredimensionada para dos proyectos
  y un área de contratos.

## Decisión

| Área | Responsabilidad |
|------|-----------------|
| `client-unity/` | Proyecto Unity (raíz del proyecto). |
| `server-nakama/` | Módulos de servidor autoritativo, sus pruebas y su compilación. |
| `shared/` | **Solo** contratos cliente-servidor versionados, con su documentación y pruebas. |
| `deploy/local/` | Entorno de desarrollo local. Por ahora no hay otros entornos. |
| `docs/` | Documentación transversal y registro de decisiones (`docs/decisions/`). |
| `tools/` | Scripts de desarrollo y verificación. |

Reglas:
- Ninguna carpeta versionada está vacía ni contiene solo marcadores; un área sin contenido
  técnico se justifica con un `README.md` que explica propósito, límites, estado y qué la
  desbloquea. No se usan `.gitkeep`.
- No se crean subcarpetas anticipadas (producción, plataformas adicionales, mensajes de juego).
- `client-unity/` y `server-nakama/` no se referencian entre sí; su único punto común es
  `shared/`.
- Cada procedimiento se documenta en un único lugar: el del backend en `deploy/local/README.md`,
  el del cliente en `client-unity/README.md`, y las decisiones pendientes en
  `docs/decisions/pendientes.md`.

## Consecuencias

- Un área nueva de primer nivel requiere un ADR que sustituya o amplíe este.
- Al decidir el runtime (D-01), su código vive en `server-nakama/` sin cambiar la estructura.
- La revisión de PR comprueba que `shared/` no recibe código que no sea de contratos.
