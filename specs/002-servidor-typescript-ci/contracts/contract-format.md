# Contrato: formato de los contratos JSON (`shared/contracts/`)

Interfaz que consumen el servidor (generación de validadores), el futuro cliente C# y la
comprobación automática. Campos y reglas canónicas: [data-model.md](../data-model.md) →
*Contrato*.

## Archivos

| Archivo | Contenido |
|---------|-----------|
| `contract.schema.json` | Metaesquema (JSON Schema 2020-12) que valida cada definición de contrato. |
| `protocol.json` | `{"protocolVersion": <entero>}` |
| `contracts.lock.json` | Objeto `{"<id>@<version>": "sha256:<hex>"}`, ordenado por clave. |
| `<kind>/<id>.json` | Una definición por contrato, p. ej. `rpc/protocol_version.json`. |

## Estructura de una definición

```json
{
  "$schema": "../contract.schema.json",
  "id": "protocol_version",
  "kind": "rpc",
  "version": 1,
  "description": "…",
  "auth": "session",
  "maxRequestBytes": 1024,
  "request":  { "$schema": "https://json-schema.org/draft/2020-12/schema", "type": "object" },
  "response": { "$schema": "https://json-schema.org/draft/2020-12/schema", "type": "object",
                "additionalProperties": false, "required": ["…"], "properties": { } },
  "errors": [ { "code": "INVALID_ARGUMENT", "grpcStatus": 3, "when": "…" } ],
  "examples": {
    "request":  { "valid": [ {} ], "invalid": [ [] ] },
    "response": { "valid": [ { } ], "invalid": [ { } ] }
  }
}
```

## Reglas de versión en los mensajes (fase actual)

- Los mensajes **no** llevan la versión de contrato ni la de protocolo en cada petición.
- El cliente consulta la versión de protocolo con el RPC `protocol_version` al establecer la
  sesión. Si no coincide con la suya, sabe que es incompatible.
- La codificación de la versión en mensajes de tiempo real se decidirá con la spec de partidas.

## Comprobaciones (`npm run contracts:check`, en `server-nakama/`)

Falla (código distinto de 0, indicando archivo y motivo) si:

1. una definición no cumple `contract.schema.json`;
2. `request` o `response` no compilan como JSON Schema 2020-12;
3. un ejemplo válido no valida, o uno inválido valida;
4. dos definiciones comparten `id`, o el `id` no coincide con el nombre de archivo;
5. la huella del contenido normativo no coincide con `contracts.lock.json[id@version]`, o falta
   esa entrada;
6. `protocol.json` no existe o no es válido.

`npm run contracts:lock` añade o actualiza la entrada `id@version` de cada definición. Solo se
ejecuta de forma explícita al subir una versión, y su resultado se revisa en el PR.

## Generación (`npm run generate`)

A partir de los archivos anteriores, escribe en `server-nakama/src/generated/` (ignorado):

- validadores *standalone* de Ajv para la petición y la respuesta de cada contrato;
- constantes: `PROTOCOL_VERSION`, `MODULE_VERSION` y, por contrato, `MAX_REQUEST_BYTES` y sus
  códigos de error.

`build`, `typecheck` y `test` ejecutan antes la generación.
