# Contrato: validadores C# generados (`Contracts.g.cs`)

Lo genera `npm run generate` (en `server-nakama/`) desde `shared/contracts/` y se escribe en
`client-unity/Assets/_Project/Scripts/Contracts/Generated/Contracts.g.cs`. **Se versiona** y no
se edita a mano.

## API generada

Espacio de nombres `Ecosystem.Contracts`:

| Miembro | Descripción |
|---------|-------------|
| `ProtocolContracts.ProtocolVersion` (`int`) | Valor de `shared/contracts/protocol.json` |
| `<Id>Contract.Id`, `.Version`, `.MaxRequestBytes` | Constantes por contrato (p. ej. `ProtocolVersionContract`) |
| `<Id>Contract.ValidateRequest(JToken)` / `ValidateResponse(JToken)` → `bool` | Validadores generados |
| `<Id>Contract.Examples` | Ejemplos `valid` e `invalid` de petición y respuesta, como texto JSON, para las pruebas |

## Subconjunto admitido de JSON Schema 2020-12

| Palabra clave | Semántica en C# |
|---------------|-----------------|
| `type` | `object`, `array`, `string`, `integer` (número sin parte fraccionaria), `number`, `boolean`, `null` |
| `properties`, `required` | Recursivo |
| `additionalProperties` | Solo booleano |
| `minimum`, `maximum` | Numéricos inclusivos |
| `minLength`, `maxLength` | En caracteres (un par sustituto cuenta como 1), igual que Ajv |
| `pattern` | `Regex` de .NET con `RegexOptions.CultureInvariant`. Solo se admiten patrones con clases ASCII, cuantificadores, grupos y anclas; el generador rechaza los demás |
| `enum`, `const` | Solo valores primitivos |
| `$schema` | Se ignora |

Cualquier otra palabra clave hace **fallar** la generación, con un mensaje que nombra el contrato
y la palabra clave (ADR-0010).

## Comprobaciones

- `npm run contracts:check` genera el C# en memoria y **falla** si difiere del archivo versionado:
  «Contracts.g.cs desactualizado: ejecuta npm run generate y confirma el cambio». Se ejecuta en el
  trabajo `server` de la CI.
- La prueba EditMode `ContractExamplesTests` valida todos los ejemplos con los validadores C#. Los
  ejemplos válidos deben pasar y los inválidos fallar, como en `contracts:check` con Ajv.
