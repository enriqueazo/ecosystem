# Implementation Plan: Sesión de invitado y conexión del cliente

**Branch**: `feat/003-sesion-invitado-cliente` | **Date**: 2026-11-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/003-sesion-invitado-cliente/spec.md`

## Summary

Conectar el cliente Unity con Nakama mediante una sesión de invitado:

- **Identidad**: autenticación por dispositivo con un identificador de instalación aleatorio. La
  identidad del jugador es su userId en el servidor.
- **Sesión**: se guarda, se renueva con el token de refresco y, si no se puede, se reautentica
  con la misma cuenta.
- **Compatibilidad**: el cliente llama a `protocol_version` y valida la respuesta con validadores
  C# **generados** desde `shared/contracts/`.
- **Resiliencia**: reintentos limitados (≤ 48 s en el peor caso), tiempo límite y cancelación.
- **Escena**: la de conexión es la propia `Bootstrap`, con una UI provisional en IMGUI.
- **Arquitectura**:
  - un núcleo sin Unity ni Nakama, probado en EditMode con dobles;
  - adaptadores para el SDK de Nakama Unity 3.23.0 y `PlayerPrefs`;
  - pruebas de integración en su propio ensamblado, preparadas para la CI.
- **Servidor**: se fija la caducidad del token de refresco en 7 días y se generan los validadores
  C#, con su comprobación de que están al día en la CI.

## Technical Context

**Language/Version**:
- C# en Unity 6000.3.29f1 (IL2CPP en Android e iOS).
- TypeScript y Node 26 para ampliar el generador de contratos (`server-nakama/scripts/`).

**Primary Dependencies**:
- `com.heroiclabs.nakama-unity` 3.23.0, fijado por commit `704f5da…` (research.md R-1).
- `com.unity.nuget.newtonsoft-json` 3.2.2, paquete oficial de Unity (R-2).
- Sin cambios de dependencias en el servidor.

**Storage**: `PlayerPrefs` en el cliente para el identificador de instalación y los tokens (R-4).
Las cuentas, en Nakama sobre PostgreSQL, que ya existe.

**Testing**:
- Unity Test Framework con tres ensamblados (R-9): EditMode sin servidor, PlayMode sin servidor e
  integración con servidor.
- `node:test` para el generador C#.
- `contracts:check`, que comprueba que el C# generado está al día.

**Target Platform**:
- Editor de Unity en macOS contra Nakama local en `127.0.0.1` (Clarifications).
- Las builds de Android e iOS deben seguir compilando.

**Project Type**: cliente móvil (Unity) y servidor de juego (Nakama) del monorepo existente.

**Performance Goals**:
- «Conectado» en ≤ 5 s (SC-001).
- «Sin conexión» en ≤ 60 s en el peor caso (SC-003; con la política de R-6 son ≈ 48 s).

**Constraints**:
- Ningún token, clave ni identificador completo en registros ni en Git.
- La identidad es el userId, nunca el identificador de instalación.
- Sin red fuera de `127.0.0.1`.
- Sin generación de código en ejecución (IL2CPP).
- Unity no se ejecuta en la CI.

**Scale/Scope**: 3 ensamblados de código nuevos, 1 de pruebas nuevo, 2 paquetes, 1 archivo C#
generado, 2 ADR y 1 cambio en `compose.yaml`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio / norma | Cumplimiento | Estado |
|-------------------|--------------|--------|
| I. Servidor autoritativo | La identidad la asigna Nakama (userId). El cliente no decide nada crítico; solo refleja estados. | ✅ |
| II. Contratos versionados | El cliente valida las respuestas con validadores generados desde la definición compartida; el C# desactualizado hace fallar `contracts:check`; la versión de protocolo sale de `protocol.json`; los campos desconocidos se toleran según cada esquema. | ✅ |
| III. Resiliencia | Desconexión, reintentos limitados, tiempo límite, cancelación, resultados tardíos descartados por generación y reautenticación sin duplicar cuentas, con pruebas para cada caso. | ✅ |
| IV. Desacoplado y simple | El núcleo no conoce Unity ni Nakama (puertos). La escena no es una dependencia global: el componente recibe sus dependencias al iniciarse. Los paquetes se justifican (R-1, R-2). IMGUI, sin paquetes, para una UI provisional. | ✅ |
| V. Calidad | Las API del SDK se verifican al compilar. Cancelación y ciclo de vida explícitos. Sin excepciones como flujo normal: los resultados son valores de estado. | ✅ |
| VI. TDD | Pruebas primero para el núcleo (EditMode con dobles y reloj falso), el generador C# (Node) y los validadores C# (ejemplos). Las de integración, antes de montar el adaptador real. La escena IMGUI se valida a mano y su carga, con PlayMode. | ✅ |
| VII. Seguridad | Identificador aleatorio, no de hardware. Tokens solo en `PlayerPrefs`, con su riesgo asumido y una revisión obligatoria al añadir cuentas (R-4). Configuración local ignorada y comprobada por `check-repo`. Registros sin datos sensibles. | ✅ |
| VIII. Reproducibilidad | SDK fijado por commit; Newtonsoft por versión; lockfile versionado; caducidades explícitas en `compose.yaml`; comandos de prueba en modo batch documentados. | ✅ |
| Una rama por spec / commits atómicos | Rama `feat/003-sesion-invitado-cliente`. | ✅ |

**Resultado**: PASA.

**Desviación respecto a la spec**: FR-022 pedía «biblioteca de JSON Schema». Se usan validadores
**generados** (R-2), que cumplen el propósito de FR-011, FR-013 y FR-022 con menos dependencias.
Se registra en ADR-0010. **Confirmada por el responsable el 2026-11-25.**

**Re-check tras la fase 1**: PASA.

## Project Structure

### Documentation (this feature)

```text
specs/003-sesion-invitado-cliente/
├── spec.md · plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   ├── connection-states.md      # Estados, transiciones, errores → estado y UI
│   ├── client-config.md          # Configuración de conexión: archivo, variables y precedencia
│   ├── csharp-contracts.md       # C# generado: API, subconjunto de JSON Schema y comprobación
│   └── unity-tests-cli.md        # Grupos de pruebas y comandos en modo batch
└── checklists/requirements.md
```

### Source Code (repository root)

```text
client-unity/
├── LocalConfig/
│   ├── connection.example.json            # Versionado (ejemplo)
│   └── connection.local.json              # Ignorado (cada desarrollador)
├── Packages/manifest.json                 # + nakama-unity (commit) y newtonsoft-json
└── Assets/_Project/
    ├── Scenes/Bootstrap.unity             # + GameObject con ConnectionScreen
    ├── Scripts/
    │   ├── Contracts/                     # Ecosystem.Contracts.asmdef (Newtonsoft)
    │   │   └── Generated/Contracts.g.cs   # Generado y versionado
    │   ├── Connection/                    # Ecosystem.Connection.asmdef (sin Unity ni Nakama)
    │   │   ├── ConnectionFlow.cs, ConnectionState.cs, RetryPolicy.cs
    │   │   ├── ProtocolCompatibility.cs, IAuthenticator.cs, GuestDeviceAuthenticator.cs
    │   │   └── Ports: IGameServer.cs, ISessionStore.cs, IInstallationIdStore.cs, IDelay.cs
    │   ├── Connection.Nakama/             # Ecosystem.Connection.Nakama.asmdef (SDK y PlayerPrefs)
    │   │   ├── NakamaGameServer.cs, PlayerPrefsSessionStore.cs, PlayerPrefsInstallationIdStore.cs
    │   │   └── ConnectionConfigLoader.cs
    │   └── Presentation/                  # Ecosystem.Presentation.asmdef
    │       └── ConnectionScreen.cs        # MonoBehaviour con IMGUI
    └── Tests/
        ├── EditMode/                      # + pruebas del núcleo, validadores y configuración
        ├── PlayMode/                      # Bootstrap carga (estado de configuración sin errores)
        └── Integration/                   # Nuevo: Ecosystem.Tests.Integration (necesita Nakama)
server-nakama/scripts/
├── generate.ts                            # + salida C# (subconjunto de JSON Schema)
└── contracts.ts                           # + comprobación de que el C# generado está al día
deploy/local/compose.yaml                  # + --session.refresh_token_expiry_sec 604800
tools/check-repo.sh                        # + connection.local.json en secrets
docs/decisions/0009-sdk-nakama-unity.md    # Sustituye a 0004
docs/decisions/0010-validadores-csharp-generados.md
```

**Structure Decision**: las dependencias van en una sola dirección:

```text
Presentation → Connection.Nakama → Connection → Contracts
```

`Connection` no referencia `UnityEngine` (`noEngineReferences: true`) ni Nakama, así que el
núcleo se puede probar sin Unity en ejecución. La escena `Bootstrap` sigue siendo la única de la
build (FR-027).

## Orden de entrega (commits atómicos, prueba e implementación juntas)

1. `feat(server)`: generador C# (subconjunto y ejemplos) con pruebas en Node, y comprobación de
   que está al día en `contracts:check`. Incluye `Contracts.g.cs` generado.
2. `feat(client)`: paquetes (SDK y Newtonsoft), `Ecosystem.Contracts` y prueba EditMode de los
   ejemplos.
3. `feat(client)`: núcleo `Ecosystem.Connection` con TDD (estados, reintentos, compatibilidad,
   sesión, reautenticación, cancelación y generaciones).
4. `feat(deploy)`: caducidad del token de refresco.
5. `feat(client)`: adaptadores de Nakama y `PlayerPrefs` con su configuración. Pruebas de
   integración primero (Red), después el adaptador (Green). Incluye `.gitignore` y `check-repo`.
6. `feat(client)`: `ConnectionScreen` en `Bootstrap`, con PlayMode y validación manual.
7. `docs`: ADR-0009 y ADR-0010, `client-unity/README.md`, README raíz y quickstart.

## Riesgos

| Riesgo | Mitigación |
|--------|------------|
| Una API del SDK distinta de la documentada (`RetryConfiguration`, refresco) | Verificar al compilar en el bloque 2; ajustar el adaptador, no el núcleo |
| `Regex` de .NET frente a `RegExp` de JS en `pattern` | El generador solo admite patrones ASCII sencillos (clases, cuantificadores y anclas) y las pruebas de ejemplos en los dos lados lo verifican |
| El *stripping* de IL2CPP elimina tipos de Newtonsoft o del SDK | El paquete de Unity ya incluye `link.xml`; se comprueba con las builds de Android e iOS (quickstart C3) |
| Con el SDK, la prueba PlayMode registra errores sin configuración | Sin configuración, el estado es «error de configuración» y se registra como aviso |
| Errores de sesión distintos de lo previsto (401 o 403 al refrescar) | Las pruebas de integración fijan los códigos reales y se documentan en `connection-states.md` |

## Complexity Tracking

| Elemento | Por qué hace falta | Alternativa más simple descartada |
|----------|--------------------|-----------------------------------|
| Generador de validadores C# propio | Una sola definición para cliente y servidor (FR-011), compatible con IL2CPP y sin DLL de terceros | Validar a mano en C# rompería FR-011 y FR-013; las bibliotecas evaluadas en R-2 son pesadas o incompatibles |
