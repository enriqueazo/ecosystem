# Contrato: estados de conexión del cliente

Comportamiento observable de `ConnectionFlow` (núcleo) y de lo que muestra `ConnectionScreen`.

## Flujo

```text
inicio ─► [config válida?] ─no─► ConfigurationError
              │sí
              ▼
          Connecting
              │ 1. Sesión: guardada y vigente → usar
              │            caducada → refrescar
              │            sin sesión o no renovable → autenticar invitado (create=true)
              │ 2. RPC protocol_version → validar contra el contrato → comparar versión
              ▼
   ┌──────────┼─────────────┬──────────────────┬──────────────────┐
Connected  UpdateRequired  ServerIncompatible  AccountError   Offline ──Reintentar──► Connecting
```

Desde `ServerIncompatible` también se puede pulsar «Reintentar», porque el servidor puede
actualizarse. `UpdateRequired`, `AccountError` y `ConfigurationError` no ofrecen reintento.

## De resultado a estado

| Situación | Reintento automático | Estado final | Texto (provisional) |
|-----------|---------------------|--------------|---------------------|
| Todo correcto y `protocolVersion` igual | — | `Connected` | «Conectado» (+ id corto de cuenta) |
| `protocolVersion` del servidor **mayor** | No | `UpdateRequired` | «Hay una versión nueva. Actualiza el juego para seguir.» |
| `protocolVersion` del servidor **menor** | No | `ServerIncompatible` | «El servidor aún no admite esta versión. Inténtalo más tarde.» |
| Respuesta del RPC que no cumple el contrato | No | `ServerIncompatible` | Igual que la anterior |
| Sin red, tiempo agotado, HTTP 5xx o 404 del RPC | Sí (hasta 3) | `Offline` si se agotan | «Sin conexión con el servidor.» + «Reintentar» |
| El refresco falla con 401 (refresco caducado o revocado) | Una vez, como reautenticación | Se sigue con la reautenticación | — |
| Autenticación de invitado rechazada (401 o 403, cuenta deshabilitada) | No | `AccountError` | «No se puede acceder a la cuenta.» |
| Clave de servidor incorrecta (401 al autenticar sin sesión previa) | No | `AccountError`; el registro de desarrollo indica que se revise `serverKey` | Igual |
| Falta configuración o no es válida | No | `ConfigurationError` | «Configuración de conexión ausente.» El registro (aviso) nombra `client-unity/LocalConfig/connection.local.json` |

Los códigos HTTP exactos de Nakama para refresco y autenticación rechazados se confirman con las
pruebas de integración y se corrigen aquí si difieren.

## Garantías

- Nunca hay más de una cuenta por identificador de instalación: el flujo solo usa `create=true`
  con el identificador guardado.
- Un único flujo activo. «Reintentar» incrementa la generación y descarta los resultados
  anteriores.
- Al destruir `ConnectionScreen` (salir de la escena o cerrar la app), todo se cancela sin
  registrar errores.
- La UI y los registros nunca muestran tokens, la clave, el identificador completo ni el
  `error.stackTrace` de Nakama.
