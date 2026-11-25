# Contratos cliente-servidor

Esta área contiene **solo** los contratos versionados entre el cliente Unity y el servidor
Nakama: definiciones de mensajes, RPC y eventos, más su documentación. No es un lugar para código
de utilidad compartido. Cliente y servidor dependen de `shared/`, nunca el uno del otro
([ADR-0001](../docs/decisions/0001-estructura-monorepo.md)).

Formato decidido: **JSON versionado** con esquemas JSON Schema 2020-12
([ADR-0008](../docs/decisions/0008-contratos-json-versionados.md)).

> **Estado**: un único contrato, técnico: `protocol_version`. No se inventan mensajes de juego
> antes de que una funcionalidad los defina.

## a. Organización

```text
shared/contracts/
├── contract.schema.json      Metaesquema: valida cada definición de contrato
├── protocol.json             {"protocolVersion": <entero>}
├── contracts.lock.json       Huella de cada id@version (no se edita a mano)
└── rpc/
    └── protocol_version.json Un archivo por contrato; el nombre es su id
```

- Cada contrato vive en `<tipo>/<id>.json`. El `id` va en `snake_case`, es único y coincide con el
  nombre del archivo y con el identificador del RPC en Nakama.
- Las herramientas que comprueban los contratos y generan validadores viven en `server-nakama/`,
  el único proyecto con Node (comandos en la sección *g*). No hay un proyecto aparte para
  `shared/`.

## b. Formato de definición

Cada contrato es un objeto JSON validado por `contract.schema.json`. Sus campos son:

| Campo | Contenido |
|-------|-----------|
| `id`, `kind`, `version` | Identificador, tipo (de momento solo `rpc`) y versión del contrato (entero ≥ 1). |
| `description` | Texto breve. No es normativo: cambiarlo no obliga a subir la versión. |
| `auth` | Autenticación exigida (de momento solo `session`, la sesión de usuario de Nakama). |
| `maxRequestBytes` | Tamaño máximo de la petición en bytes UTF-8 (1–65 536). |
| `request` / `response` | Esquemas JSON Schema 2020-12. La petición es un objeto y la respuesta un objeto con `additionalProperties: false`. |
| `errors` | Lista de `{code, grpcStatus, when}`. Códigos estables en `UPPER_SNAKE_CASE`, únicos en el contrato. |
| `examples` | Ejemplos `valid` e `invalid` de petición y de respuesta (al menos uno de cada). |

Reglas completas en el
[modelo de datos de la spec 002](../specs/002-servidor-typescript-ci/data-model.md#contrato-sharedcontractskindidjson).

Restricciones de los esquemas, porque el servidor los ejecuta en la VM de Nakama:
- no deben necesitar estado entre validadores (`$dynamicRef`, `unevaluated*` ni composiciones
  que lean errores de otro validador);
- no deben usar palabras clave que requieran ayudantes de ejecución distintos de `minLength` y
  `maxLength`.

Si un esquema lo necesita, la generación falla y hay que ampliar el generador con un ADR.

## c. Versionado

- **Versión de protocolo** (`protocol.json`): un entero que sube cuando hay un cambio
  incompatible en cualquier contrato. El cliente la consulta al iniciar sesión con el RPC
  `protocol_version` y, si no coincide con la suya, sabe que debe actualizarse.
- **Versión de contrato** (`version` en cada definición): sube con **cualquier** cambio del
  contenido normativo (todo salvo `description` y `examples`).
- **Cambio compatible**: añadir un campo opcional o un valor de enumeración que los receptores
  antiguos pueden ignorar. Sube la versión del contrato, no la del protocolo.
- **Cambio incompatible**: eliminar o renombrar un campo, cambiar un tipo, hacer obligatorio un
  campo opcional, endurecer un límite o cambiar el significado de un valor. Sube la versión del
  contrato **y** la del protocolo.
- **En los mensajes**: en esta fase los mensajes no llevan ni la versión de contrato ni la de
  protocolo. Cómo se codificará la versión en los mensajes de tiempo real se decidirá con la spec
  de partidas.

## d. Errores

Toda respuesta de error del módulo usa la misma estructura:

| Campo | Regla |
|-------|-------|
| `code` | Uno de los `errors[].code` del contrato (p. ej. `INVALID_ARGUMENT`). Estable: nunca cambia de significado. |
| `message` | Descripción breve para depuración. No se muestra tal cual al jugador y no contiene datos de la petición, datos personales, secretos, trazas ni detalles internos. |

El cliente decide qué mostrar al jugador a partir de `code`, no de `message`.

**Transporte en Nakama**: el módulo lanza `{code: <gRPC>, message: "<error estándar en JSON>"}`.
La API HTTP de Nakama responde con el estado HTTP correspondiente y un cuerpo
`{"code":<gRPC>,"message":"<error estándar en JSON>","error":{…}}`. El cliente lee `code` y
analiza `message`. Debe **ignorar** el campo `error`: Nakama 3.42.0 adjunta siempre ahí una traza
del módulo, sin datos de la petición
([ADR-0006](../docs/decisions/0006-runtime-nakama-typescript.md)). Cualquier excepción inesperada
se responde como `INTERNAL` (gRPC 13), con un mensaje genérico.

## e. Compatibilidad

Reglas de la constitución (principio II):

- Los receptores **toleran campos desconocidos** y los ignoran (los esquemas de petición no los
  prohíben).
- Los receptores **rechazan mensajes inválidos** (formato, tamaño, límites, permisos o estado)
  con un error definido; nunca los procesan parcialmente.
- Las respuestas llevan exactamente los campos del contrato y nunca objetos internos completos.
- No se envían actualizaciones de red por fotograma: la frecuencia se define según el diseño.
- El servidor es la autoridad: valida todo valor que llegue del cliente (principio I), con los
  validadores generados a partir de la definición.

## f. Checklist para añadir o cambiar un contrato

En **un mismo cambio**:

- [ ] Definición del contrato con versión, campos obligatorios, límites, errores y ejemplos.
- [ ] Clasificación del cambio (compatible o incompatible) y versiones actualizadas según *c*.
- [ ] Huella actualizada con `npm run contracts:lock` (solo tras subir `version`).
- [ ] Implementación en el servidor, con validación completa de la entrada.
- [ ] Implementación en el cliente.
- [ ] Pruebas unitarias y de integración que demuestran que cliente y servidor interpretan igual
      los mensajes.
- [ ] Comportamiento ante desconexión, reintento y duplicados, si el contrato es de tiempo real
      (principio III).
- [ ] Documentación actualizada.

## g. Validación y comprobación

Desde `server-nakama/` (requisitos en [server-nakama/README.md](../server-nakama/README.md)):

| Comando | Qué hace |
|---------|----------|
| `npm run contracts:check` | Comprueba el metaesquema, que los esquemas compilen, los ejemplos, los `id` y códigos de error únicos, `protocol.json` y que la huella de cada `id@version` coincida con `contracts.lock.json`. Cambiar un contrato sin subir su versión hace que falle. |
| `npm run contracts:lock` | Registra la huella de cada `id@version` nuevo. **Se niega** a sobrescribir la de una versión ya registrada. |
| `npm run generate` | Genera en `server-nakama/src/generated/` (ignorado) los validadores y las constantes del módulo de servidor, y los validadores **C# del cliente** (`client-unity/…/Contracts.g.cs`, versionado; ADR-0010). Lo ejecutan solos `build`, `typecheck` y `test`. |

La CI ejecuta `contracts:check` en cada PR; también falla si el C# generado del cliente está
desactualizado. El cliente valida con esos validadores generados, que admiten el subconjunto
declarado en ADR-0010; una palabra clave fuera de él hace fallar la generación.
