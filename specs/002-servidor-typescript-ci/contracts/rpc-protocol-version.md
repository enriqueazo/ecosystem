# Contrato: RPC `protocol_version` (v1)

Definición normativa: `shared/contracts/rpc/protocol_version.json`. Este documento describe su
comportamiento observable y la forma en que viaja por Nakama.

## Propósito

Permitir que un cliente autenticado sepa qué versión de protocolo y qué versión del módulo sirve
el servidor, para detectar incompatibilidades (principio II). No tiene lógica de juego.

## Llamada

| Aspecto | Valor |
|---------|-------|
| Identificador | `protocol_version` |
| Autenticación | Sesión de cliente (`Authorization: Bearer <token>`) |
| Petición | Cuerpo vacío o un objeto JSON. Se ignoran los campos desconocidos. |
| Tamaño máximo de petición | 1024 bytes UTF-8 |
| HTTP (API de Nakama) | `POST /v2/rpc/protocol_version?unwrap`, con el cuerpo JSON tal cual; la respuesta correcta es el objeto JSON. Sin `unwrap`, el cuerpo es una cadena JSON con el contenido escapado (`"{}"`) y la respuesta llega como `{"payload":"<JSON escapado>"}` (verificado). |

## Respuesta correcta

```json
{ "protocolVersion": 1, "moduleVersion": "0.1.0" }
```

| Campo | Tipo | Regla |
|-------|------|-------|
| `protocolVersion` | entero ≥ 1 | `shared/contracts/protocol.json` |
| `moduleVersion` | cadena semver, ≤ 64 caracteres | `version` de `server-nakama/package.json` |

No hay más campos (`additionalProperties: false`).

## Errores

| Situación | Código de contrato | gRPC | HTTP esperado* |
|-----------|--------------------|------|----------------|
| Sin credenciales o token inválido | — (lo rechaza Nakama antes del módulo) | 16 UNAUTHENTICATED | 401 |
| Llamada con `http_key` de servidor, sin sesión de usuario | `SESSION_REQUIRED` | 7 PERMISSION_DENIED | 403 |
| La petición supera 1024 bytes | `PAYLOAD_TOO_LARGE` | 3 INVALID_ARGUMENT | 400 |
| La petición no es JSON, o no es un objeto | `INVALID_ARGUMENT` | 3 INVALID_ARGUMENT | 400 |
| Excepción inesperada en el módulo | `INTERNAL` | 13 INTERNAL | 500 |

\* Códigos HTTP **verificados** con las pruebas de integración contra Nakama 3.42.0 (2026-11-25),
salvo `INTERNAL`, que no se puede provocar desde fuera.

Forma del error en el módulo: se lanza `{ code: <gRPC>, message: "<JSON>" }`. `<JSON>` es el
error estándar de `shared/README.md`, p. ej. `{"code":"PAYLOAD_TOO_LARGE","message":"La petición
supera 1024 bytes."}`. El mensaje no incluye el contenido de la petición. Cualquier otra excepción
la convierte `runSafely` en `INTERNAL`, con un mensaje genérico, y su detalle solo se registra en
el servidor.

Forma HTTP real del cuerpo de error (verificada):

```json
{"code":7,"error":{"stackTrace":"[object Object]\n\tat runSafely (index.js:45:7(16))\n…"},"message":"{\"code\":\"SESSION_REQUIRED\",\"message\":\"Este RPC requiere una sesión de usuario.\"}"}
```

- El cliente lee `code` (gRPC) y analiza `message` como el error estándar.
- **Limitación conocida de Nakama**: el runtime JavaScript adjunta `error.stackTrace` a toda
  excepción lanzada por un módulo, también a los errores del contrato, y no se puede desactivar
  (`server/runtime_javascript.go`, `invokeFunction`, en v3.42.0). Como el módulo solo lanza
  objetos planos, esa traza no incluye mensajes de excepción, datos de la petición ni secretos;
  solo nombres de función y posiciones del artefacto. Los clientes deben ignorar el campo
  `error`. Registrado en ADR-0006.

## Orden de validación

1. Sesión: si `ctx.userId` está vacío → `SESSION_REQUIRED`.
2. Tamaño en bytes UTF-8 → `PAYLOAD_TOO_LARGE`.
3. Análisis JSON. La cadena vacía equivale a `{}`; un error de análisis da `INVALID_ARGUMENT`.
4. Validación contra `request` → `INVALID_ARGUMENT`.
5. Construcción de la respuesta. En las pruebas se valida contra `response`.

## Casos compartidos (unitarias ↔ integración)

`server-nakama/test/cases/protocol-version.cases.ts` enumera, como mínimo:

| Caso | Entrada | Resultado |
|------|---------|-----------|
| vacía | `""` | respuesta correcta |
| objeto vacío | `{}` | respuesta correcta |
| campos desconocidos | `{"foo":1,"bar":"x"}` | respuesta correcta |
| no JSON | `{no-json` | `INVALID_ARGUMENT` |
| no objeto | `[]`, `42`, `"x"`, `null` | `INVALID_ARGUMENT` |
| demasiado grande | objeto de 1025 bytes | `PAYLOAD_TOO_LARGE` |
| multibyte en el límite | 1024 bytes con caracteres de 2–4 bytes | respuesta correcta; con 1 byte más, `PAYLOAD_TOO_LARGE` |
| sin sesión | `{}` con `http_key` | `SESSION_REQUIRED` |
