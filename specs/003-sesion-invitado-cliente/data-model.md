# Data Model: Sesión de invitado y conexión del cliente

## Identificador de instalación (cliente, `PlayerPrefs`)

| Campo | Regla |
|-------|-------|
| Valor | 32 caracteres hexadecimales en minúscula (`^[0-9a-f]{32}$`), de `Guid.NewGuid().ToString("N")`. Nunca derivado del hardware ni de la publicidad. |
| Clave | `ecosystem.session.installationId` |
| Ciclo de vida | Se crea en la primera apertura y no se regenera, salvo que no exista o no cumpla el formato. **Nunca** por un error de red o de autenticación. |
| Exposición | En los registros, como mucho, los 6 primeros caracteres. Nunca en la UI. |

## Cuenta de jugador (servidor)

| Campo | Regla |
|-------|-------|
| `userId` | Lo asigna Nakama. Es la **única** identidad del jugador en el cliente y en el servidor. |
| Credenciales vinculadas | En esta spec, solo el dispositivo (el identificador de instalación). En el futuro, otras sobre la misma cuenta (FR-004). |

## Sesión (cliente, `PlayerPrefs`)

| Campo | Regla |
|-------|-------|
| `authToken` | Clave `ecosystem.session.authToken`. Caduca a las 2 h (servidor). |
| `refreshToken` | Clave `ecosystem.session.refreshToken`. Caduca a los 7 días (servidor). |
| Validez local | Se considera caducada 60 s antes de su caducidad, como margen. Si el servidor la rechaza, prevalece el servidor. |
| Datos ilegibles | Se borran los dos tokens y se reautentica. El identificador de instalación no se toca. |

## Configuración de conexión

Ver [contracts/client-config.md](contracts/client-config.md).

| Campo | Regla |
|-------|-------|
| `host` | Obligatorio. En esta spec, `127.0.0.1`. |
| `port` | Obligatorio, entero entre 1 y 65535. |
| `serverKey` | Obligatorio, no vacío. Nunca se versiona su valor real ni aparece en registros. |
| `useSsl` | Booleano. `false` en local. |

## Estado de conexión

Estados y transiciones en [contracts/connection-states.md](contracts/connection-states.md).

| Estado | ¿Terminal? |
|--------|-----------|
| `Connecting` | No |
| `Connected` | Sí, hasta cerrar |
| `Offline` | No (reintento manual) |
| `UpdateRequired` | Sí |
| `ServerIncompatible` | Sí (reintento manual) |
| `AccountError` | Sí |
| `ConfigurationError` | Sí |

## Política de reintentos

| Campo | Valor |
|-------|-------|
| Tiempo límite por petición | 10 s |
| Reintentos automáticos | 3, con esperas de 1, 2 y 4 s (±20 %) |
| Peor caso hasta `Offline` | ≈ 48 s (≤ 60 s, SC-003) |
| Se reintenta | Sin red, tiempo agotado, HTTP 5xx, 404 del RPC (módulo ausente) |
| No se reintenta | Autenticación rechazada, incompatibilidad, contrato inválido, configuración |

## Generación de intento

Cada ejecución del flujo, automática o manual, recibe un número creciente. Un resultado cuyo
número no es el vigente se descarta (FR-017).
