# Quickstart de validación: Sesión de invitado y conexión del cliente

Detalle de comandos, estados y configuración en [contracts/](contracts/). Aquí solo van los
escenarios y sus resultados esperados.

## Requisitos previos

- Los de las specs 001 y 002: Unity 6000.3.29f1 con módulos Android e iOS, Docker, Node 26 y npm 11.
- El entorno local construido y arrancado para los escenarios B y C.

## A. Sin servidor

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| A1 | `cd server-nakama && npm run check` | En verde, incluida la comprobación de que el C# generado está al día | FR-013 |
| A2 | Cambiar un contrato y no regenerar | `contracts:check` falla con «Contracts.g.cs desactualizado» | FR-013 |
| A3 | EditMode sin servidor (entorno apagado) | 100 % en verde: núcleo, validadores con los ejemplos y configuración | FR-024, SC-006 |
| A4 | PlayMode sin servidor | `Bootstrap` carga; sin configuración, estado `ConfigurationError` con un aviso y sin errores | FR-027 |
| A5 | Abrir `Bootstrap` en el editor sin `connection.local.json` | Muestra «Configuración de conexión ausente» y el registro nombra el archivo | FR-020 |
| A6 | `git add -f client-unity/LocalConfig/connection.local.json` y `tools/check-repo.sh --only secrets` | Falla nombrando el archivo | FR-020, SC-005 |

## B. Con servidor (editor)

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| B1 | Borrar los `PlayerPrefs` del proyecto y reproducir `Bootstrap` | «Conectado» en ≤ 5 s y una cuenta nueva en la consola de Nakama | US1, SC-001 |
| B2 | Detener y volver a reproducir 10 veces | La misma cuenta las 10 veces y ninguna nueva en la consola | US2-1, SC-002 |
| B3 | Forzar la caducidad (borrar `authToken` o usar un token caducado) y reproducir | Se refresca y sigue la misma cuenta | US2-2 |
| B4 | Borrar `authToken` y `refreshToken` y reproducir | Se reautentica y sigue la misma cuenta | US2-3 |
| B5 | Deshabilitar la cuenta en la consola y reproducir | `AccountError`, sin cuenta nueva | US2-4 |
| B6 | Detener el entorno y reproducir | Reintentos y, después, «Sin conexión» en ≤ 60 s | US4-1, SC-003 |
| B7 | Arrancar el entorno y pulsar «Reintentar» | «Conectado» con la misma cuenta | US4-2 |
| B8 | Salir del modo reproducción en mitad de la conexión | Sin errores ni excepciones en la consola | US4-4, FR-017 |
| B9 | Subir `protocolVersion` del servidor a 2 (temporal, `npm run build` y recarga) y reproducir | `UpdateRequired`. Con el servidor en 1 y el cliente en 2, `ServerIncompatible`. Se revierte | US3, SC-004 |
| B10 | Revisar la consola del editor y `Editor.log` | Ni tokens, ni la clave, ni el identificador completo | SC-005 |

## C. Pruebas de integración y builds

| # | Paso | Esperado | Requisitos |
|---|------|----------|------------|
| C1 | Integración en modo batch con el entorno arrancado | 100 % en verde | FR-024, SC-006 |
| C2 | Integración en modo batch con el entorno apagado | Falla en ≤ 15 s con «Entorno local no disponible» | FR-025 |
| C3 | Build de desarrollo de Android y exportación de iOS | Sin errores; las pruebas de configuración de la spec 001 siguen en verde | FR-021, FR-027 |
| C4 | Clon limpio: configurar y ejecutar siguiendo solo `client-unity/README.md` | ≤ 15 min sin contar instalaciones | SC-007 |
| C5 | Tras cerrar el editor, `git status` | Sin cambios inesperados en archivos versionados | Spec 001, SC-007 |

Un escenario que no pueda ejecutarse se registra como **no verificado**, con su motivo.
