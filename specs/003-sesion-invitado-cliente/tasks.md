---

description: "Task list for feature 003-sesion-invitado-cliente"
---

# Tasks: Sesión de invitado y conexión del cliente

**Input**: Design documents from `specs/003-sesion-invitado-cliente/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: SÍ. La constitución (principio VI) y la spec (FR-024 a FR-027) exigen TDD para la
lógica comprobable: el generador C#, los validadores C#, el núcleo de conexión, la configuración y
los adaptadores (pruebas de integración). Cada prueba se escribe antes que su implementación y se
ve **fallar**. El commit agrupa prueba e implementación, como en la spec 002: el paso Red se anota
en el *Registro de verificación* y en el mensaje del commit. La escena IMGUI se valida a mano y su
carga, con PlayMode.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5 de spec.md)
- Rutas relativas a la raíz del repositorio. `client-unity/Assets/_Project/` se abrevia como
  `_Project/`.

### Convenciones

- `data-model.md` → `Entidad.campo` remite a la regla canónica de [data-model.md](data-model.md).
  Las interfaces remiten a [contracts/](contracts/) y las decisiones, a [research.md](research.md)
  (R-n).
- Las pruebas de Unity se ejecutan en modo batch con los comandos de
  [contracts/unity-tests-cli.md](contracts/unity-tests-cli.md). Con el proyecto abierto en otro
  editor no se pueden ejecutar.
- Los nombres de la API del SDK se verifican compilando (principio V). Si difieren de lo previsto,
  se ajusta el adaptador, no el núcleo.

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Comprobar precondiciones y anotarlas en `specs/003-sesion-invitado-cliente/tasks.md` → *Registro de verificación*:
  - rama `feat/003-sesion-invitado-cliente`;
  - Unity 6000.3.29f1 instalado;
  - Node 26 y npm 11;
  - Docker en marcha;
  - `tools/tests/run.sh` y `cd server-nakama && npm run check` en verde;
  - pruebas EditMode y PlayMode de la spec 001 en verde (línea base).
- [X] T002 Añadir los paquetes a `client-unity/Packages/manifest.json` (research.md R-1 y R-2):
  - `com.heroiclabs.nakama-unity` con la URL de Git fijada al commit `704f5da244a49d942f383965f5549420522eb8e0`;
  - `com.unity.nuget.newtonsoft-json` `3.2.2`.

  Abrir en modo batch para regenerar `client-unity/Packages/packages-lock.json`. Comprobar: 0 errores de compilación, las pruebas de la línea base en verde y, tras cerrar, solo cambian `manifest.json` y `packages-lock.json`.
- [X] T003 [P] Preparar la configuración local del cliente (contracts/client-config.md):
  - crear `client-unity/LocalConfig/connection.example.json` con valores ficticios (`serverKey: "cambia-esto-server-key"`), según `data-model.md` → `Configuración de conexión.host`, `.port`, `.serverKey` y `.useSsl`;
  - añadir `/client-unity/LocalConfig/connection.local.json` a `.gitignore`;
  - comprobar con `git check-ignore --no-index` que el ejemplo no queda ignorado.

**Checkpoint**: commit `chore(client): …` (T002–T003).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: validadores C# generados (los usa US3) y los tipos base del núcleo de conexión (todas las historias).

**⚠️ CRITICAL**: las historias dependen de `Ecosystem.Contracts` y de los puertos de `Ecosystem.Connection`.

- [X] T004 Escribir en `server-nakama/test/unit/generate-csharp.test.ts` (Red) las pruebas de la salida C# del generador, según contracts/csharp-contracts.md:
  - genera `ProtocolContracts.ProtocolVersion` y, por contrato, `Id`, `Version`, `MaxRequestBytes`, `ValidateRequest`, `ValidateResponse` y `Examples`;
  - cubre cada palabra clave del subconjunto (`type`, `properties`, `required`, `additionalProperties` booleano, `minimum`, `maximum`, `minLength`, `maxLength` en caracteres, `pattern`, `enum` y `const` primitivos);
  - **falla** con una palabra clave no admitida (p. ej. `oneOf`, `additionalProperties` como objeto) o con un `pattern` fuera de lo admitido, nombrando el contrato y la palabra clave;
  - la salida es determinista (dos generaciones iguales).
- [X] T005 Implementar la salida C# en `server-nakama/scripts/generate.ts` (Green):
  - una función `generateCSharp` que escribe `client-unity/Assets/_Project/Scripts/Contracts/Generated/Contracts.g.cs` con la cabecera «Generado… No editar»;
  - `npm run generate` escribe la salida JS (existente) y la C#.
- [X] T006 Añadir a `server-nakama/scripts/contracts.ts` la comprobación «C# al día»: genera en memoria y compara con el archivo versionado, con el mensaje de contracts/csharp-contracts.md. Añadir sus casos a `server-nakama/test/unit/contracts.test.ts` (Red y después Green). Validar A1 y A2 de quickstart.md.
- [X] T007 Crear `_Project/Scripts/Contracts/Ecosystem.Contracts.asmdef`:
  - referencia a Newtonsoft (`Newtonsoft.Json.dll` como referencia precompilada del paquete de Unity);
  - `autoReferenced: false`.

  Generar `Contracts.g.cs` con `npm run generate`, abrir Unity para crear los `.meta` y confirmar que compila.
- [X] T008 Escribir `_Project/Tests/EditMode/ContractExamplesTests.cs`:
  - recorre `<Id>Contract.Examples` con los validadores C#: los ejemplos válidos deben pasar y los inválidos fallar;
  - añadir la referencia `Ecosystem.Contracts` al ensamblado `Ecosystem.Tests.EditMode`.

  Red: antes de T007 no compila. Green: pasa en modo batch.
- [X] T009 Crear el núcleo `_Project/Scripts/Connection/` con `Ecosystem.Connection.asmdef` (`noEngineReferences: true`; sin referencia a Nakama; referencia a `Ecosystem.Contracts`) y sus tipos base, sin lógica todavía (research.md R-7):
  - `ConnectionState.cs`, con los estados de `data-model.md` → *Estado de conexión* y los textos de contracts/connection-states.md;
  - los puertos `IGameServer.cs`, `ISessionStore.cs`, `IInstallationIdStore.cs`, `IDelay.cs` e `IAuthenticator.cs`;
  - los tipos de resultado (éxito, fallo transitorio, rechazo definitivo y respuesta inválida), sin excepciones como flujo normal (principio V).

  Añadir la referencia al ensamblado `Ecosystem.Tests.EditMode` y comprobar que compila.

**Checkpoint**: commits `feat(server): …` (T004–T006, con `Contracts.g.cs`) y `feat(client): …` (T007–T009).

---

## Phase 3: User Story 1 - Entrar como invitado sin registrarse (Priority: P1) 🎯 MVP

**Goal**: Al abrir `Bootstrap` con el servidor disponible, el jugador queda «conectado» con una cuenta nueva asignada por el servidor.

**Independent Test**: quickstart.md, escenarios A5 y B1. Pruebas EditMode de US1 y de integración «alta».

### Tests for User Story 1 ⚠️

- [X] T010 [P] [US1] Escribir `_Project/Tests/EditMode/Connection/InstallationIdTests.cs` (Red), según `data-model.md` → `Identificador de instalación.Valor` y `Identificador de instalación.Ciclo de vida`:
  - se genera si no existe y luego se reutiliza;
  - se regenera solo si es inválido;
  - nunca se regenera por un error.

  Usa un almacén en memoria.
- [X] T011 [P] [US1] Escribir `_Project/Tests/EditMode/Connection/GuestConnectionTests.cs` (Red), con un doble de `IGameServer`:
  - el primer arranque autentica como invitado con el identificador de instalación y `create=true`;
  - el estado final es `Connected`, con el `userId` del servidor como identidad (`data-model.md` → `Cuenta de jugador.userId`);
  - el identificador de instalación nunca se usa como identidad (FR-003).
- [X] T012 [P] [US1] Escribir `_Project/Tests/EditMode/Connection/ConnectionConfigTests.cs` (Red) para el cargador de configuración (contracts/client-config.md):
  - las variables `ECOSYSTEM_NAKAMA_*` tienen precedencia sobre el archivo;
  - archivo válido;
  - sin configuración → `ConfigurationError` con el nombre del archivo;
  - campo inválido → `ConfigurationError` nombrando el campo, nunca su valor (`data-model.md` → `Configuración de conexión.serverKey`);
  - `ECOSYSTEM_NAKAMA_CONFIG=none` → `ConfigurationError` aunque exista el archivo.

  La lógica de análisis vive en el núcleo: recibe el texto y las variables como datos.
- [X] T013 [US1] Crear el ensamblado `_Project/Tests/Integration/Ecosystem.Tests.Integration.asmdef` (EditMode, solo Editor) y `_Project/Tests/Integration/GuestSignInIntegrationTests.cs` (Red):
  - verificar primero si Unity Test Framework 1.6 ejecuta pruebas `async Task` en EditMode (una prueba mínima); si no, usar `[UnityTest]` + `IEnumerator`, y anotar el resultado;
  - comprueba primero que el servidor está disponible y falla en ≤ 15 s con «Entorno local no disponible…» (FR-025);
  - con un identificador nuevo, crea una cuenta y obtiene `Connected`;
  - dos identificadores dan dos cuentas distintas (US1-2).

  Toma la configuración de contracts/client-config.md.

### Implementation for User Story 1

- [X] T014 [US1] Implementar en el núcleo (Green: T010–T012):
  - `InstallationIdentity.cs`, que cumple `data-model.md` → `Identificador de instalación.Valor` y `Identificador de instalación.Ciclo de vida`;
  - `ConnectionConfig.cs` y su análisis, según `data-model.md` → `Configuración de conexión.host`, `.port`, `.serverKey` y `.useSsl`;
  - `GuestDeviceAuthenticator.cs` (implementa `IAuthenticator`, FR-004; única credencial según `data-model.md` → `Cuenta de jugador.Credenciales vinculadas`);
  - en `ConnectionFlow.cs`, el camino del primer arranque: configuración, autenticación de invitado y `Connected`, con la identidad en `data-model.md` → `Cuenta de jugador.userId`.
- [X] T015 [US1] Implementar los adaptadores en `_Project/Scripts/Connection.Nakama/` (`Ecosystem.Connection.Nakama.asmdef`, que referencia el SDK, `Ecosystem.Connection` y `Ecosystem.Contracts`):
  - `NakamaGameServer.cs`: autenticación por dispositivo y desactivación del reintento global del SDK (R-6);
  - `PlayerPrefsInstallationIdStore.cs`, con la clave de `data-model.md` → `Identificador de instalación.Clave`;
  - `ConnectionConfigLoader.cs`, que lee `client-unity/LocalConfig/connection.local.json` y las variables;
  - el registro solo muestra los 6 primeros caracteres del identificador (`data-model.md` → `Identificador de instalación.Exposición`).

  Green: T013 pasa con el entorno local arrancado.
- [X] T016 [US1] Crear `_Project/Scripts/Presentation/ConnectionScreen.cs` (`Ecosystem.Presentation.asmdef`):
  - `MonoBehaviour` con IMGUI que monta las dependencias y arranca el flujo con `destroyCancellationToken`;
  - muestra los estados de contracts/connection-states.md (de momento `Connecting`, `Connected` con el id corto y `ConfigurationError`);
  - `ConfigurationError` se registra como aviso, no como error.

  Añadir un GameObject con `ConnectionScreen` a `_Project/Scenes/Bootstrap.unity`, que sigue siendo la única escena de la build (FR-027).
- [X] T017 [US1] Validar:
  - PlayMode (A4): la prueba `BootstrapSceneTests` fija `ECOSYSTEM_NAKAMA_CONFIG=none` (y lo restaura al terminar), comprueba que `ConnectionScreen` está en `ConfigurationError` y que no hay errores registrados, con o sin `connection.local.json` ni servidor;
  - EditMode completo;
  - quickstart.md A5 y B1 en el editor (≤ 5 s, cuenta nueva en la consola de Nakama).

  Anotar los resultados.

**Checkpoint**: commits `feat(client)` (T010–T014), `feat(client)` (T013, T015) y `feat(client)` (T016–T017).

---

## Phase 4: User Story 2 - Volver a entrar con la misma cuenta (Priority: P1)

**Goal**: Sesión persistente, renovable y, si no se puede renovar, reautenticación con la misma cuenta; error definido si la cuenta está deshabilitada.

**Independent Test**: quickstart.md, escenarios B2–B5. Pruebas EditMode de sesión y de integración de refresco.

### Tests for User Story 2 ⚠️

- [X] T018 [P] [US2] Escribir `_Project/Tests/EditMode/Connection/SessionLifecycleTests.cs` (Red), con dobles:
  - sesión vigente → se usa, sin autenticar;
  - caducada → se refresca;
  - refresco rechazado → una sola reautenticación con el mismo identificador y la misma cuenta;
  - rechazo durante el uso → mismo orden, una sola vez (FR-007);
  - autenticación rechazada o cuenta deshabilitada → `AccountError`, sin bucle (FR-008); clave de servidor rechazada → `ConfigurationError` (spec, *Edge Cases*);
  - tokens ilegibles → se descartan y se reautentica (`data-model.md` → `Sesión.Datos ilegibles`);
  - margen de 60 s en la validez local (`Sesión.Validez local`);
  - nunca se crea una cuenta nueva si el identificador es el mismo.
- [X] T019 [P] [US2] Escribir `_Project/Tests/Integration/SessionIntegrationTests.cs` (Red):
  - reabrir con la sesión guardada → misma cuenta;
  - sin `authToken` → refresco y misma cuenta;
  - sin ningún token → reautenticación y misma cuenta.

  Además, el caso «clave de servidor errónea»: identificar la respuesta de Nakama y comprobar que se distingue de una cuenta deshabilitada.

  Anotar los códigos HTTP reales que devuelve Nakama al rechazar un refresco o una autenticación.

### Implementation for User Story 2

- [X] T020 [US2] Implementar en el núcleo el ciclo de vida de la sesión en `ConnectionFlow.cs` y `SessionTokens.cs` (Green: T018), según `data-model.md` → `Sesión.authToken`, `Sesión.refreshToken`, `Sesión.Validez local` y `Sesión.Datos ilegibles`.
- [X] T021 [US2] Implementar el refresco y la restauración en `NakamaGameServer.cs` y crear `PlayerPrefsSessionStore.cs` (claves de `data-model.md` → `Sesión.authToken` y `Sesión.refreshToken`; nunca en registros). Green: T019. Si los códigos HTTP reales difieren de contracts/connection-states.md, corregir el contrato. En `ConnectionScreen.cs`, mostrar el estado `AccountError` con su texto de contracts/connection-states.md, sin «Reintentar».
- [X] T022 [US2] Añadir `--session.refresh_token_expiry_sec 604800` en `deploy/local/compose.yaml` y documentar las dos caducidades en `deploy/local/README.md` (FR-009, research.md R-6). Reiniciar el entorno y comprobar con `tools/check-backend.sh --module` y `npm run test:integration` que la spec 002 sigue en verde.
- [X] T023 [US2] Validar quickstart.md B2–B5 en el editor (10 reaperturas, caducidad forzada, sin tokens y cuenta deshabilitada desde la consola; en B5, la escena muestra «No se puede acceder a la cuenta») y anotar los resultados.

**Checkpoint**: commits `feat(client)` (T018, T020), `feat(client)` (T019, T021) y `feat(deploy)` (T022).

---

## Phase 5: User Story 3 - Saber que hay que actualizar el juego (Priority: P1)

**Goal**: Comprobación de la versión de protocolo con la respuesta validada contra el contrato y bloqueos diferenciados.

**Independent Test**: quickstart.md, escenario B9. Pruebas EditMode de compatibilidad.

### Tests for User Story 3 ⚠️

- [X] T024 [P] [US3] Escribir `_Project/Tests/EditMode/Connection/ProtocolCompatibilityTests.cs` (Red):
  - versión igual → `Connected`;
  - servidor mayor → `UpdateRequired`;
  - servidor menor → `ServerIncompatible`;
  - respuesta que no cumple el contrato (falta un campo, tipo incorrecto, campo extra) → `ServerIncompatible`, sin reintento;
  - el error del RPC se interpreta por `code` y `message`, ignorando `error` (FR-012);
  - la versión del cliente sale de `ProtocolContracts.ProtocolVersion`.
- [X] T025 [P] [US3] Añadir a `_Project/Tests/Integration/GuestSignInIntegrationTests.cs` (Red) la llamada real a `protocol_version`: respuesta validada y `Connected` con la versión vigente.

### Implementation for User Story 3

- [X] T026 [US3] Implementar `ProtocolCompatibility.cs` y el paso de versión en `ConnectionFlow.cs` (Green: T024), con `ProtocolVersionContract.ValidateResponse` sobre el `JToken` de la respuesta.
- [X] T027 [US3] Implementar `GetProtocolVersion` en `NakamaGameServer.cs` (RPC `protocol_version`, con la carga como JSON), devolviendo el texto JSON tal cual para que lo valide el núcleo (Green: T025). Mostrar los estados `UpdateRequired` y `ServerIncompatible` en `ConnectionScreen.cs`, sin reintento en el primero (contracts/connection-states.md).
- [X] T028 [US3] Validar quickstart.md B9:
  - subir temporalmente `protocolVersion` del servidor a 2 → `UpdateRequired`;
  - cliente en 2 y servidor en 1 → `ServerIncompatible`.

  Revertir los dos cambios, regenerar y comprobar que `git status` queda limpio. Anotar los resultados.

**Checkpoint**: commits `feat(client)` (T024, T026) y `feat(client)` (T025, T027).

---

## Phase 6: User Story 4 - Comportamiento predecible sin conexión (Priority: P2)

**Goal**: Reintentos limitados, tiempo límite, cancelación, descarte de resultados tardíos y «Reintentar».

**Independent Test**: quickstart.md, escenarios B6–B8. Pruebas EditMode con reloj falso.

### Tests for User Story 4 ⚠️

- [X] T029 [P] [US4] Escribir `_Project/Tests/EditMode/Connection/RetryPolicyTests.cs` (Red), según `data-model.md` → *Política de reintentos*:
  - 3 reintentos con esperas de 1, 2 y 4 s (±20 %);
  - qué se reintenta y qué no;
  - el peor caso queda en ≤ 60 s;
  - con `IDelay` falso y variación aleatoria determinista.
- [X] T030 [P] [US4] Escribir `_Project/Tests/EditMode/Connection/ResilienceTests.cs` (Red):
  - tiempo límite por petición → fallo transitorio;
  - reintentos agotados → `Offline`;
  - «Reintentar» desde `Offline` sigue el flujo completo;
  - la cancelación detiene todo sin cambiar a un estado de error ni registrar errores (FR-017);
  - un resultado tardío de una generación superada se descarta (`data-model.md` → *Generación de intento*): una sola sesión y una sola cuenta.

### Implementation for User Story 4

- [X] T031 [US4] Implementar `RetryPolicy.cs` y la gestión de generaciones, tiempo límite y cancelación en `ConnectionFlow.cs` (Green: T029–T030), según `data-model.md` → `Política de reintentos.Reintentos automáticos`, `.Peor caso hasta Offline`, `.Se reintenta`, `.No se reintenta` y *Generación de intento*.
- [X] T032 [US4] En `NakamaGameServer.cs`:
  - tiempo límite de 10 s por petición (`data-model.md` → `Política de reintentos.Tiempo límite por petición`);
  - pasar el `CancellationToken` a cada llamada;
  - traducir los errores del SDK a fallo transitorio o rechazo definitivo según contracts/connection-states.md.

  En `ConnectionScreen.cs`, el estado `Offline` con el botón «Reintentar».
- [X] T033 [US4] Validar quickstart.md B6–B8 en el editor (entorno detenido → `Offline` en ≤ 60 s; «Reintentar» tras arrancar; salir del modo reproducción a mitad sin errores) y anotar los tiempos medidos.

**Checkpoint**: commits `feat(client)` (T029–T031) y `feat(client)` (T032–T033).

---

## Phase 7: User Story 5 - Probar el cliente en local y estar listos para la CI (Priority: P2)

**Goal**: Configuración local documentada, grupos de pruebas separados en modo batch y la configuración local protegida en Git.

**Independent Test**: quickstart.md, escenarios A3, A6, C1, C2 y C4.

- [ ] T034 [P] [US5] Añadir a `tools/tests/run.sh` (Red) el caso «`client-unity/LocalConfig/connection.local.json` en el índice → `--only secrets` falla nombrándolo». Ampliar `tools/check-repo.sh` (Green) y actualizar su contrato de referencia en `specs/001-base-proyecto-entorno/contracts/tooling-cli.md`.
- [ ] T035 [US5] Ejecutar los tres grupos en modo batch con los comandos de contracts/unity-tests-cli.md:
  - A3: EditMode con el entorno **apagado**, 100 %;
  - PlayMode con el entorno apagado;
  - C1: integración con el entorno arrancado, 100 %;
  - C2: integración con el entorno apagado, falla en ≤ 15 s con el mensaje.

  Anotar los resultados (SC-006, FR-025).
- [ ] T036 [US5] Actualizar `client-unity/README.md` (FR-026):
  - configuración local (copiar el ejemplo y tomar `NAKAMA_SERVER_KEY` del `.env`);
  - la escena de conexión y sus estados;
  - los tres grupos de pruebas con sus comandos;
  - cómo llevarlos a la CI (sin configurarla);
  - la tabla de paquetes con el SDK y Newtonsoft (FR-023);
  - la arquitectura (ensamblados y dirección de dependencias);
  - la limitación de reinstalación (cuenta nueva);
  - sustituir la sección «Conexión con el servidor» (ya no «sin SDK»).

**Checkpoint**: commits `feat(tools)` (T034) y `docs(client)` (T036).

---

## Phase 8: Polish & Cross-Cutting Concerns

- [ ] T037 [P] Crear `docs/decisions/0009-sdk-nakama-unity.md` (aceptada, research.md R-1, R-4 y R-7):
  - SDK 3.23.0 fijado por commit;
  - `PlayerPrefs` y su revisión obligatoria al añadir cuentas;
  - el punto de extensión `IAuthenticator` (FR-004);
  - verificación solo en el editor.

  Cambiar el estado de `docs/decisions/0004-conectividad-sin-sdk-cliente.md` a «sustituida por 0009». Actualizar `docs/decisions/README.md`.
- [ ] T038 [P] Crear `docs/decisions/0010-validadores-csharp-generados.md` (aceptada, research.md R-2 y R-3, con la desviación de FR-022 confirmada por el responsable) y añadir una referencia a él en `docs/decisions/0008-contratos-json-versionados.md` sin cambiar su decisión. Actualizar `shared/README.md` → *g* con la salida C# y su comprobación.
- [ ] T039 Actualizar el README raíz:
  - el mapa (`client-unity` conecta con Nakama);
  - los primeros pasos del cliente (configuración local);
  - la tabla de comprobaciones (grupos de Unity, solo en local).
- [ ] T040 Validar quickstart.md C3:
  - build de desarrollo de Android y exportación de iOS con los paquetes nuevos (IL2CPP), con 0 errores;
  - las pruebas de configuración de la spec 001 siguen en verde;
  - C5: `git status` limpio tras cerrar el editor y tras las builds. Si el SDK o las builds tocan ajustes de primera vez, se versionan aparte, como en la spec 001.
- [ ] T041 [P] Auditar secretos y registros (SC-005, B10):
  - `tools/check-repo.sh`;
  - `git log -p main..HEAD` sin claves reales;
  - la consola del editor y `Editor.log` de una sesión completa sin tokens, sin la clave y sin el identificador completo.
- [ ] T042 Ejecutar la validación completa de quickstart.md (A–C). Medir SC-007 con un clon limpio: configurar y ejecutar siguiendo solo `client-unity/README.md`. Anotar lo no verificado y su motivo.
- [ ] T043 Con autorización explícita del responsable: subir la rama y abrir el PR hacia `main`. Comprobar que la CI (`repo`, `server`, `backend`) pasa en verde, incluida la comprobación de que el C# está al día.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (1)**: sin dependencias.
- **Foundational (2)**: depende de Setup y bloquea todas las historias.
- **US1 (3)**: depende de la fase 2. Es el MVP: crea los adaptadores y la escena.
- **US2 (4)**: depende de US1 (flujo, adaptadores y escena).
- **US3 (5)**: depende de US1. Es independiente de US2 y puede ir en paralelo, pero toca los mismos archivos (`ConnectionFlow.cs`, `NakamaGameServer.cs`), así que mejor en secuencia.
- **US4 (6)**: depende de US1 a US3 (envuelve todo el flujo).
- **US5 (7)**: T034 es independiente; T035 y T036 dependen de US1 a US4.
- **Polish (8)**: depende de todas. T043 necesita autorización.

### Within Each User Story

- Pruebas (Red) → implementación del núcleo (Green) → adaptador o escena → validación manual.
- Las pruebas EditMode del núcleo no necesitan Nakama. Las de `Ecosystem.Tests.Integration`, sí.

### Parallel Opportunities

- Fase 1: T003 en paralelo con T002.
- US1: T010 ∥ T011 ∥ T012.
- US2: T018 ∥ T019.
- US3: T024 ∥ T025.
- US4: T029 ∥ T030.
- US5: T034 en paralelo con las fases 3 a 6.
- Polish: T037 ∥ T038 ∥ T041.

---

## Parallel Example: User Story 1

```bash
Task: "T010 [P] [US1] InstallationIdTests.cs"
Task: "T011 [P] [US1] GuestConnectionTests.cs"
Task: "T012 [P] [US1] ConnectionConfigTests.cs"
```

## Parallel Example: User Story 4

```bash
Task: "T029 [P] [US4] RetryPolicyTests.cs"
Task: "T030 [P] [US4] ResilienceTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Fases 1 y 2.
2. US1: el jugador entra como invitado.
3. **STOP and VALIDATE**: A5 y B1.

### Incremental Delivery

1. US1 → entrar como invitado.
2. US2 → misma cuenta siempre.
3. US3 → bloqueo de versiones incompatibles.
4. US4 → resiliencia.
5. US5 → pruebas listas para la CI.
6. Polish → ADR, documentación, builds y PR.

---

## Notes

- [P] = archivos distintos y sin dependencias pendientes.
- Commits atómicos con Conventional Commits. Cada commit agrupa la prueba con su implementación y menciona el paso Red. No se hace push ni se abre PR sin autorización (T043).
- Ninguna tarea se marca como completada sin haber ejecutado su verificación. Lo que no se pueda verificar (p. ej. dispositivos reales, fuera de alcance) se anota como tal.

## Registro de verificación

| Escenario | Fecha | Resultado (✅ / ❌ / no verificado) | Evidencia o motivo |
|-----------|-------|------------------------------------|--------------------|
| T001 | 2026-11-25 | ✅ | Rama `feat/003-sesion-invitado-cliente`; Unity 6000.3.29f1; Node v26.13.1 / npm 11.20.0; Docker 29.9.3; `tools/tests/run.sh` 33/33; `npm run check` OK; línea base de Unity: EditMode 10/10 y PlayMode 1/1 |
| T002 | 2026-11-25 | ✅ | `nakama-unity` por URL de Git fijada al commit `704f5da…` (el lockfile registra el hash) y `newtonsoft-json` 3.2.2. Solo cambian `manifest.json` y `packages-lock.json`. Una sonda temporal en el Editor (borrada) compila contra las API usadas: `Client(…, UnityWebRequestAdapter.Instance)`, `Timeout`, `GlobalRetryConfiguration = new RetryConfiguration(ms, 0)`, `AuthenticateDeviceAsync(id, null, true, null, null, ct)`, `Session.Restore`, `HasExpired`, `HasRefreshExpired`, `SessionRefreshAsync(s, null, null, ct)`, `RpcAsync(s, id, payload, null, ct)`, `ApiResponseException.StatusCode` / `.GrpcStatusCode` y `JToken.Parse`. Sin errores ni avisos de obsolescencia. EditMode 10/10 y PlayMode 1/1 con los paquetes nuevos |
| T003 | 2026-11-25 | ✅ | `client-unity/LocalConfig/connection.example.json` versionado; `connection.local.json` ignorado (comprobado con `git check-ignore --no-index`) |
| T004 Red | 2026-11-25 | ✅ | `generate-csharp.test.ts` falla: `generateCSharpSource` no existe |
| T005 Green | 2026-11-25 | ✅ | 12/12. **Ampliación sobre el plan**: contrato de prueba `server-nakama/test/fixtures/contracts/rpc/keyword_coverage.json` con todas las palabras clave del subconjunto (y emoji en `pattern` y `maxLength`), generado como `Tests/EditMode/Generated/ContractFixtures.g.cs`; Ajv valida sus ejemplos en Node. **Ajustes**: Node en modo de solo eliminar tipos rechaza las propiedades de parámetro (`erasableSyntaxOnly`) → campo explícito. Ajv estricto no admite `type` como lista → se retira del subconjunto y el generador C# lo rechaza también (cliente y servidor admiten exactamente lo mismo). Generación en `scripts/generate-csharp.ts` |
| T006 / A1–A2 | 2026-11-25 | ✅ | Red: las pruebas de `checkGeneratedCSharp` fallaban (no existía). Green: `contracts.test.ts` 20/20. Antes de generar, `contracts:check` → «Contracts.g.cs desactualizado» y «ContractFixtures.g.cs desactualizado» (A2); tras `npm run generate`, OK (A1). Unitarias 83/83. **Corrección**: al hacer el commit `c89fd95`, `npm run check` devolvía 1 por la regla `noShadowRestrictedNames` de Biome (variable `escape` en `generate-csharp.ts`) y se registró por error como «en verde». Se corrigió en el commit siguiente; desde entonces `npm run check` → 0 |
| T007 / T008 | 2026-11-25 | ✅ | `Ecosystem.Contracts` (sin referencias a Unity, con Newtonsoft). `ContractExamplesTests`: los **42 ejemplos** (11 de `protocol_version` y 31 de `keyword_coverage`) dan el mismo resultado que Ajv → EditMode 53/53. **Red de la prueba**: alterar el C# generado (`maxLength` 64 → 3) hace fallar 1 prueba en Unity **y** `contracts:check`; se restauró con `npm run generate` |
| T009 | 2026-11-25 | ✅ | Núcleo `Ecosystem.Connection` (`noEngineReferences`, sin Nakama): `ConnectionState` y textos, `ServerResult`/`FailureKind` (sin excepciones como flujo), `SessionTokens`/`AuthenticatedSession` (identidad = `UserId`) y los puertos `IGameServer`, `ISessionStore`, `IInstallationIdStore`, `IDelay`, `IClock` e `IAuthenticator`. Compila y EditMode sigue en 53/53 |
| T010–T012 Red | 2026-11-25 | ✅ | Las pruebas no compilan: faltan `ConfigResult`, `ConnectionFlow` e `InstallationIdentity` |
| T014 Green | 2026-11-25 | ✅ | EditMode 77/77 (24 nuevas: identificador de instalación, primer arranque como invitado, identidad = `userId`, configuración con precedencia, `ECOSYSTEM_NAKAMA_CONFIG=none` y errores sin el valor de la clave) |
| T013 sonda | 2026-11-25 | ✅ | Unity Test Framework 1.6 **sí** ejecuta `async Task` en `[Test]` de EditMode (el `Assert.Fail` posterior al `await` se ejecutó) |
| T013 / T015 | 2026-11-25 | ✅ tras corrección | Red: no compilaba sin los adaptadores. **1.ª ejecución colgada (> 10 min)**: el `UnityWebRequestAdapter` del SDK es un `MonoBehaviour` con corrutinas y `DontDestroyOnLoad`, y no funciona en EditMode. Al matar Unity quedó corrupta la caché de Burst (*segmentation fault* al arrancar); se resolvió borrando `Temp/` y `Library/BurstCache`. **Corrección**: integración en **PlayMode** con `[UnityTest]` + `[Timeout(60000)]`, sin `OneTimeSetUp` asíncrono (documentado en research.md R-9, plan y contracts/unity-tests-cli.md). El ensamblado del adaptador referencia `NakamaRuntime`. Green: 2/2 contra Nakama real (alta y dos instalaciones con cuentas distintas) |
| T016 | 2026-11-25 | ✅ | `ConnectionScreen` (IMGUI) añadido a `Bootstrap` con un script de editor temporal ya eliminado; sigue siendo la única escena de la build. Red: la prueba PlayMode no compilaba. `LogAssert.NoUnexpectedReceived` también falla con registros normales, así que la prueba declara los dos esperados (`Connecting` y el aviso de `ConfigurationError`) y exige que no haya más. Green: PlayMode 2/2, fijando `ECOSYSTEM_NAKAMA_CONFIG=none` |
| T017 / A4 / A5 / B1 / SC-001 | 2026-11-25 | ✅ | PlayMode 2/2, también con `connection.local.json` presente (A4, determinista). A5: sin configuración, `ConfigurationError` con el aviso que nombra el archivo y la variable. B1 **automatizado** (no puedo pulsar Play en la interfaz): `BootstrapSceneIntegrationTests` carga la escena real con `PlayerPrefs` y configuración reales → «Conectado» en **0,10 s** con una cuenta nueva. El log no contiene tokens ni la clave. La comprobación visual en el editor queda para el responsable |
| Regresión US1 | 2026-11-25 | ✅ | EditMode 77/77, PlayMode 2/2, integración 3/3, `check-repo` 0 y `npm run check` 0 |
| T018 Red | 2026-11-25 | ✅ | `SessionLifecycleTests`: 6 de 10 fallan (el flujo siempre se reautenticaba). El caso «rechazo durante el uso» (FR-007) depende del RPC y se cubre en US3 (T024) |
| T020 Green | 2026-11-25 | ✅ | `EstablishSessionAsync`: sesión vigente → se usa; caducada (margen de 60 s) → refresco; refresco rechazado, caducado o tokens ilegibles → reautenticación con la misma instalación; cuenta rechazada → `AccountError` sin bucle. EditMode 87/87 |
| T019 / T021 | 2026-11-25 | ✅ | Integración en PlayMode contra Nakama: reabrir con sesión → misma cuenta y sin refresco; caducidad forzada (reloj +3 h) → refresco real y misma cuenta; sin tokens → reautenticación y misma cuenta; clave errónea → `ConfigurationError`. El adaptador de refresco ya existía desde T015, así que estas pruebas no pasaron por Red. **Códigos HTTP reales** (curl contra Nakama 3.42.0): clave de servidor errónea → **401**; refresco inválido → **401**; cuenta deshabilitada (ban desde la API de la consola local, reactivada después) → **403** «User account banned.». Coinciden con la clasificación del adaptador y con contracts/connection-states.md. **Corrección en la propia prueba**: la aserción comparaba tokens, era inestable (un refresco en el mismo segundo da un token idéntico) **e imprimía un token** en el resultado (solo en el log temporal local de una cuenta desechable). Ahora cuenta refrescos con un envoltorio y nunca pone tokens en las aserciones; 2 ejecuciones seguidas en verde y 0 apariciones de `eyJ` en logs y XML. `AccountError` ya se mostraba: la escena pinta `ConnectionTexts.For(state)` y no ofrece «Reintentar» |
| T022 | 2026-11-25 | ✅ | `--session.refresh_token_expiry_sec 604800` en `compose.yaml` (documentado en `deploy/local/README.md`). Medido en los JWT emitidos: sesión 7200 s y refresco 604 800 s. Spec 002: `check-backend --module` OK e integración 13/13 |
| T023 / B2–B5 / SC-002 | 2026-11-25 | ✅ (B5 en parte) | B2 automatizado: 10 reaperturas, alternando sesión vigente y caducidad forzada → misma cuenta las 10 veces (integración 8/8). B3 y B4 cubiertos por T019. B5: clasificación 403 → `AccountError` verificada con el servidor real y con la prueba unitaria; **no verificado** visualmente en el editor (requiere la interfaz) |
| Incidencia de herramientas | 2026-11-25 | — | Un vigilante de tiempo en segundo plano (`sleep 300; pkill`) de una ejecución anterior mató una ejecución posterior. Se eliminó y no se vuelve a usar; el `[Timeout]` de las pruebas limita el tiempo |
| T024 Red | 2026-11-25 | ✅ | `ProtocolCompatibilityTests` y `StandardErrorTests` no compilaban: faltaban `ClientProtocolVersion`, `ProtocolCompatibility` y `StandardError`. Incluye el caso FR-007 pendiente de US2 (rechazo de sesión por el RPC → una sola reautenticación) |
| T026 Green | 2026-11-25 | ✅ | EditMode 102/102. `ProtocolCompatibility.Evaluate`: valida la respuesta con `ProtocolVersionContract.ValidateResponse`; igual → `Connected`, servidor mayor → `UpdateRequired`, menor o respuesta fuera del contrato (falta un campo, tipo erróneo, campo extra, no JSON) → `ServerIncompatible`, sin reintento. `StandardError.TryParseCode` lee el código del contrato en `message` e ignora el campo `error` de Nakama (FR-012). Versión del cliente: `ProtocolContracts.ProtocolVersion` por defecto |
| T025 / T027 | 2026-11-25 | ✅ | El adaptador `GetProtocolVersionAsync` ya existía desde T015 (sin Red real). Prueba de integración nueva: la respuesta real del RPC valida contra el contrato generado y su versión es la del cliente. Integración 9/9. Los estados `UpdateRequired` y `ServerIncompatible` se muestran con `ConnectionTexts`; «Reintentar» solo en `ServerIncompatible` |
| T028 / B9 / SC-004 | 2026-11-25 | ✅ | Automatizado contra el servidor real inyectando la versión del cliente (sin cambiar archivos versionados): cliente por detrás → `UpdateRequired`; cliente por delante → `ServerIncompatible`. Integración 10/10 y PlayMode 2/2; `git status` sin cambios en contratos |
| T029–T030 Red | 2026-11-25 | ✅ | No compilaban: faltaba `RetryPolicy`. **Corrección de la propia prueba**: en «resultado tardío», el orden de la cola del doble estaba invertido (el intento superado consume su resultado al final) |
| T031 Green | 2026-11-25 | ✅ | `RetryPolicy` (3 reintentos con 1, 2 y 4 s ±20 %; solo se reintentan los transitorios; peor caso teórico 48,4 s) y `ConnectionFlow` reestructurado: intentos por generación (un resultado superado no cambia el estado ni guarda la sesión), reintentos con espera cancelable, cancelación sin pasar por estados de error, y la sesión se guarda al aplicar el resultado vigente. EditMode 113/113 |
| T032 | 2026-11-25 | ✅ | El tiempo límite de 10 s y la cancelación por petición estaban en el adaptador desde T015. Los errores del SDK se traducen a transitorio o definitivo. La escena muestra `Offline` con «Reintentar» (`ConnectionTexts.CanRetry`) y no permite ejecuciones simultáneas |
| T033 / B6–B8 / SC-003 | 2026-11-25 | ✅ (B7 en parte) | Automatizado con red real, sin Nakama: puerto cerrado → `Offline` en **6,2 s**; servidor TCP que acepta y nunca responde → los 4 intentos agotan el tiempo límite y `Offline` en **41,5 s** (≤ 60 s); salir de `Bootstrap` a mitad de conexión → sin errores ni excepciones (B8, `LogAssert`). Integración 13/13 y PlayMode 2/2. B7 («Reintentar» tras volver el servidor) está cubierto por la prueba unitaria; la pulsación real en el editor queda para el responsable |
