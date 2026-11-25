# 0010 · Validadores C# generados desde los contratos

- **Estado**: aceptada
- **Fecha**: 2026-11-25
- **Amplía**: [0008](0008-contratos-json-versionados.md)

## Contexto

ADR-0008 fijó contratos JSON versionados con JSON Schema 2020-12 y una sola definición para
cliente y servidor. La spec 003 necesita validar en el cliente Unity (C#, IL2CPP) las respuestas
del servidor con esa misma definición (FR-011, FR-013, FR-022).

## Opciones consideradas

| Opción | Problema |
|--------|----------|
| JsonSchema.Net 9.7.0 | Arrastra `JsonPointer.Net`, `Json.More.Net`, `Humanizer.Core` y System.Text.Json: varias DLL que habría que gestionar a mano en Unity, con dudas sobre IL2CPP y el *stripping* |
| NJsonSchema 11.6.2 | No admite JSON Schema 2020-12 completo; depende de reflexión pesada (`Namotion.Reflection`) |
| Corvus.Json | Amplio grafo de generación de código, no pensado para Unity |
| **Generar validadores C#** con el generador existente | Código propio que hay que probar |

## Decisión

**Validadores C# generados**, opción confirmada por el responsable el 2026-11-25. Se apartan de la
forma que proponía la spec («elegir biblioteca»), pero cumplen su propósito.

- `npm run generate` (en `server-nakama/`) genera dos archivos, que se versionan porque Unity debe
  compilar sin Node:
  - `client-unity/Assets/_Project/Scripts/Contracts/Generated/Contracts.g.cs`, con
    `ProtocolContracts.ProtocolVersion` y, por contrato, sus constantes, `ValidateRequest`,
    `ValidateResponse` y ejemplos;
  - `ContractFixtures.g.cs` (pruebas), a partir de un contrato de prueba que usa todas las palabras
    clave admitidas.
- **Subconjunto admitido**: `type` (un solo tipo), `properties`, `required`,
  `additionalProperties` (booleano), `minimum` y `maximum`, `minLength` y `maxLength` (en
  caracteres; un par sustituto cuenta como 1), `pattern` (escapes compatibles con JS y .NET
  `RegexOptions.ECMAScript`, sin grupos `(?…)`), y `enum` y `const` primitivos. Cualquier otra
  palabra clave **falla** en la generación.
- **Análisis del JSON**: `com.unity.nuget.newtonsoft-json` 3.2.2, el paquete oficial de Unity,
  preparado para IL2CPP. Los validadores no generan código en ejecución.
- **Comprobaciones**:
  - `contracts:check` falla si el C# versionado está desactualizado; lo ejecuta la CI en el
    trabajo `server`;
  - una prueba EditMode recorre todos los ejemplos con los validadores C#, y deben dar el mismo
    resultado que Ajv en Node.

## Consecuencias

- **Equivalencia probada**: 42 ejemplos (`protocol_version` y `keyword_coverage`) dan el mismo
  resultado en C# y en Ajv. Si se altera el C# generado, fallan la prueba de Unity y
  `contracts:check`.
- **Mismo subconjunto en los dos lados**: Ajv en modo estricto (servidor) no admite listas de
  tipos, así que el generador C# tampoco.
- **Ampliar el subconjunto** (`oneOf`, `items`, `$ref`…) exige ampliar el generador y el contrato
  de prueba, con su ADR.
- **Coste aceptado**: el generador C# es código propio, con pruebas en Node y en Unity.
