# 0008 · Formato de los contratos cliente-servidor: JSON versionado

- **Estado**: aceptada
- **Fecha**: 2026-11-25

## Contexto

La constitución (principio II) exige que cada mensaje, RPC y evento tenga una definición
compartida y versionada, y que cliente (C#) y servidor (TypeScript, ADR-0006) interpreten igual
los mensajes (decisión D-05). Los RPC de Nakama transportan cadenas, que en la práctica son JSON.

## Opciones consideradas

- **JSON con JSON Schema 2020-12**: legible, admitido por Nakama sin capas extra y con validadores
  en TypeScript (Ajv) y en C# (bibliotecas de JSON Schema 2020-12).
- **Protocol Buffers**: mensajes más pequeños y tipado fuerte. Necesita compiladores y bibliotecas
  en los dos lados y su ejecución en la VM ES5 de Nakama.
- **Definición en TypeScript** (TypeBox, Zod): cómoda en el servidor, pero no reutilizable desde
  C# sin duplicarla.

## Decisión

Se usa **JSON versionado**:

- Cada contrato es un archivo JSON en `shared/contracts/<tipo>/<id>.json`, validado por un
  metaesquema propio. Contiene: versión, autenticación, tamaño máximo, esquemas de petición y
  respuesta en JSON Schema 2020-12, errores y ejemplos.
- La versión de protocolo está en `shared/contracts/protocol.json`. El cliente la consulta con el
  RPC `protocol_version`. Los mensajes no llevan versión en esta fase.
- `contracts.lock.json` guarda la huella SHA-256 del contenido normativo de cada `id@version`:
  - `npm run contracts:check` falla si un contrato cambia sin subir su versión;
  - `npm run contracts:lock` se niega a sobrescribir una huella existente.
- **Una sola definición**: el servidor genera con Ajv 8.20.0 (modo standalone) los validadores y
  las constantes a partir de los archivos. El cliente C# validará los mismos archivos.

Detalle operativo en [shared/README.md](../../shared/README.md).

## Consecuencias

- **Sin dependencias de ejecución** en el módulo (FR-004):
  - el único ayudante que genera Ajv para los contratos actuales (`ucs2length`, de
    `minLength`/`maxLength`) se sustituye por una implementación ES5 propia equivalente;
  - cualquier otro ayudante hace fallar la generación.

  Por eso Rollup no necesita resolver paquetes de npm.
- **Validadores sin estado**: la VM de Nakama congela las funciones tras `InitModule`, así que los
  validadores generados solo devuelven verdadero o falso, sin `.errors`. Hay esquemas que no se
  admiten: los que necesitan evaluación dinámica (`$dynamicRef`/`unevaluated*`) o que un
  validador lea los errores de otro. Ampliarlos requiere un ADR.
- **Comprobación ES5 solo de sintaxis**: `check:es5` revisa la sintaxis del artefacto. Si el
  código generado usara una API posterior a ES5, lo detectarían las pruebas de integración contra
  Nakama, no la comprobación estática. Las pruebas de integración se ejecutan en la CI.
- JSON es más voluminoso que un formato binario. Si los mensajes de partida lo necesitan, se
  evaluará otro formato para ellos con su propio ADR, sin cambiar los RPC.
