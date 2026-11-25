# Research: Sesión de invitado y conexión del cliente

**Fecha**: 2026-11-25 · **Spec**: [spec.md](spec.md) · **Plan**: [plan.md](plan.md)

Fuentes:
- repositorio `heroiclabs/nakama-unity`: releases, `package.json`, `CHANGELOG.md` y `README.md`
  de v3.23.0;
- metadatos de NuGet de JsonSchema.Net, NJsonSchema y Corvus.Json;
- registro de paquetes de Unity;
- documentación y código de Nakama ya verificados en la spec 002.

## R-1 · SDK de Nakama para Unity (P-01) — RESUELTA

- **Decisión**: `com.heroiclabs.nakama-unity` **3.23.0** (2026-11-08, basada en Nakama .NET
  3.23.0).
  - Se instala por UPM con URL de Git fijada **por commit**, en `client-unity/Packages/manifest.json`:
    `https://github.com/heroiclabs/nakama-unity.git?path=/Packages/Nakama#704f5da244a49d942f383965f5549420522eb8e0`.
    Ese es el commit de la etiqueta anotada `v3.23.0`; `packages-lock.json` registra el hash.
  - Exige Unity `2022.3` o posterior (`package.json`), así que admite 6000.3.
  - Su única dependencia es `com.unity.modules.unitywebrequest`.
- **Lo que usamos del SDK** (confirmado en su README):
  - autenticación por dispositivo;
  - `Session.Restore` y refresco de sesión;
  - RPC;
  - `RetryConfiguration` (global y por petición) y cancelación por petición.

  Los nombres exactos de las API se verifican al compilar contra el paquete (principio V).
- **IL2CPP**: el SDK va como DLL (`Runtime/Plugins/Nakama.dll`). La compatibilidad con
  Android e iOS se verifica generando las builds (quickstart C3), como en la spec 001.
- **Alternativas**:
  - instalar desde la tarball de la release (menos cómodo de revisar que un commit);
  - usar `.unitypackage` (no lo gestiona UPM, nada de lockfile).
- **Registro**: ADR-0009, que **sustituye** a ADR-0004.

## R-2 · Validación de contratos en C# (P-02) — RESUELTA (desviación justificada, confirmada por el responsable el 2026-11-25)

Bibliotecas evaluadas:

| Opción | Problema |
|--------|----------|
| JsonSchema.Net 9.7.0 | Tiene versión para .NET Standard 2.0, pero arrastra `JsonPointer.Net`, `Json.More.Net`, `Humanizer.Core` y System.Text.Json: varias DLL que habría que gestionar a mano en Unity, con dudas sobre IL2CPP y el *stripping*, para validar respuestas pequeñas. |
| NJsonSchema 11.6.2 | No admite JSON Schema 2020-12 completo; depende de `Namotion.Reflection` (reflexión pesada, arriesgada con IL2CPP). |
| Corvus.Json | Genera código con un amplio grafo de dependencias de *CodeGeneration*; no está pensado para Unity. |

- **Decisión**: **generar validadores C#** a partir de los mismos archivos de `shared/contracts/`,
  con el generador que ya existe en `server-nakama/scripts/generate.ts` (spec 002).
  - Admite un **subconjunto declarado** de JSON Schema 2020-12: `type`, `properties`,
    `required`, `additionalProperties` (booleano), `minimum` y `maximum`, `minLength` y
    `maxLength` (en caracteres), `pattern`, `enum` y `const` de valores primitivos.
  - Si un contrato usa otra palabra clave, la generación **falla** con un mensaje claro: como
    hoy con Ajv, ampliarlo requiere un ADR.
- **Análisis del JSON en el cliente**: el paquete oficial de Unity
  `com.unity.nuget.newtonsoft-json` **3.2.2**, mantenido por Unity y preparado para IL2CPP. Los
  validadores generados recorren un `JToken`.
- **Mismo comportamiento que el servidor**: el generador también escribe los ejemplos `valid` e
  `invalid` de cada contrato. Una prueba EditMode los recorre con los validadores C#, igual que
  `contracts:check` hace con Ajv, así que una divergencia entre lenguajes rompe una prueba.
- **Por qué cumple la spec**:
  - FR-011: la definición compartida es la única fuente, sin copias a mano;
  - FR-013: si el código C# generado está desactualizado, falla la comprobación (R-3);
  - FR-022: compatible con IL2CPP, sin generación de código en ejecución.

  Se aparta de la *forma* propuesta en la spec («elegir biblioteca») y se registra como
  ampliación de ADR-0008, en ADR-0010.
- **Coste aceptado**: el generador C# es código propio con su propio conjunto de pruebas. Además
  de las pruebas en Node, la prueba EditMode de los ejemplos valida el resultado en C#.

## R-3 · Cómo llegan los contratos al cliente (FR-010, FR-013) — RESUELTA

- `npm run generate` (en `server-nakama/`) escribe además
  `client-unity/Assets/_Project/Scripts/Contracts/Generated/Contracts.g.cs`. Contiene
  `ProtocolVersion` (desde `protocol.json`), los validadores, las constantes de cada contrato y
  los ejemplos.
- El archivo **se versiona**, porque Unity debe abrir y compilar sin Node. Su `.meta` también se
  versiona, con un GUID estable.
- `npm run contracts:check` añade una comprobación: genera el C# en memoria y **falla** si
  difiere del versionado. Mensaje: «ejecuta npm run generate y confirma el cambio». La ejecuta el
  trabajo `server` de la CI.
- **Alternativa descartada**: leer los JSON en el editor en tiempo de ejecución. Obligaría a
  empaquetar los esquemas en la build y a validar con una biblioteca genérica (R-2).

## R-4 · Almacenamiento de la sesión (P-03) — RESUELTA

- **Decisión**: `PlayerPrefs` para el identificador de instalación y los tokens, que es el patrón
  que documenta el SDK. Claves con prefijo propio (`ecosystem.session.*`) y nunca en registros.
- **Fundamento**:
  - la sesión de invitado no da más acceso que el propio identificador de instalación, que en
    cualquier caso debe guardarse en la app para recuperar la cuenta;
  - un almacenamiento seguro (Keychain o Keystore) exigiría plugins nativos y probar cada
    plataforma, algo que no se puede verificar sin dispositivos (Clarifications de la spec).
- **Revisión obligatoria**: al añadir cuentas registradas (vinculación), sus credenciales
  justificarán un almacenamiento seguro. Queda en ADR-0009 como consecuencia.
- **Datos corruptos**: si los tokens no se pueden leer, se descartan. El identificador de
  instalación solo se regenera si no cumple el formato (`^[0-9a-f]{32}$`).

## R-5 · Identificador de instalación (FR-002) — RESUELTA

- `Guid.NewGuid().ToString("N")`: 32 caracteres hexadecimales. Nakama exige entre 10 y 128,
  como se vio en la spec 001.
- Se genera la primera vez y no se deriva del hardware: no se usa
  `SystemInfo.deviceUniqueIdentifier` ni identificadores de publicidad.
- En los registros solo aparecen sus 6 primeros caracteres.

## R-6 · Caducidades y reintentos (P-04) — RESUELTA

- **Servidor** (`deploy/local/compose.yaml`):
  - se mantiene `--session.token_expiry_sec 7200` (2 h);
  - se añade `--session.refresh_token_expiry_sec 604800` (7 días).

  Así el refresco dura más que la sesión: hoy dura menos, porque se queda con el valor por
  defecto de Nakama. Si el jugador vuelve tras más de 7 días, el cliente se reautentica por
  dispositivo y recupera la misma cuenta (FR-006), así que la caducidad larga no añade riesgo
  frente al identificador de instalación.
- **Cliente**:
  - **tiempo límite** de 10 s por petición;
  - **reintentos automáticos**: 3 tras el primer intento, con espera de 1 s, 2 s y 4 s (±20 % de
    variación aleatoria);
  - **peor caso**: 4 × 10 s + 8,4 s ≈ 48 s, dentro de los 60 s de SC-003.
- Los reintentos los gestiona **nuestra lógica de conexión**, que se puede probar con un reloj
  falso. El reintento global del SDK se **desactiva** para no contar intentos dos veces.
- **No se reintentan**: los rechazos de autenticación (401 o 403 al autenticar), las
  incompatibilidades de versión, las respuestas que no cumplen el contrato y los errores de
  configuración.
- **Errores de red y de disponibilidad** (se reintentan): sin conexión, tiempo agotado, 5xx y 404
  del RPC (módulo no cargado). El 404 se reintenta porque durante un reinicio de Nakama es
  transitorio; si persiste, el estado final es «sin conexión».

## R-7 · Arquitectura del cliente (FR-019, FR-004) — RESUELTA

- **Núcleo sin Unity ni Nakama**, en el ensamblado `Ecosystem.Connection`:
  - máquina de estados `ConnectionFlow`;
  - `RetryPolicy`;
  - `ProtocolCompatibility`;
  - puertos `IGameServer`, `ISessionStore`, `IInstallationIdStore` e `IDelay`.

  Se prueba en EditMode con dobles.
- **Punto de extensión para cuentas** (FR-004): `IAuthenticator` con un único método
  implementado, `GuestDeviceAuthenticator`. Las futuras vinculaciones serán otros autenticadores
  u operaciones sobre la misma cuenta (userId), sin tocar el flujo ni el almacenamiento.
- **Adaptadores**, en el ensamblado `Ecosystem.Connection.Nakama`:
  - el SDK detrás de `IGameServer` (R-03 de la spec);
  - `PlayerPrefs` detrás de los almacenes;
  - el cargador de configuración.
- **Presentación**:
  - un componente `ConnectionScreen` en la escena **Bootstrap**, que pasa a ser el punto de
    entrada de conexión;
  - UI provisional con **IMGUI** (`OnGUI`), que no necesita paquetes ni assets: uGUI se retiró en
    la spec 001 y UI Toolkit exigiría `PanelSettings` y temas. Se sustituirá cuando haya diseño de
    interfaz.
- **No hace falta otra escena**: la conexión vive en `Bootstrap`. Así se mantiene el invariante
  «única escena de la build» de la spec 001 (FR-027), sin cambiar sus pruebas.
- **Ciclo de vida**: el flujo recibe un `CancellationToken` ligado a `destroyCancellationToken`
  del componente (Unity 2022.2+), que se cancela al salir de la escena o cerrar la app (FR-017).
  Cada intento lleva un número de generación, y los resultados de generaciones superadas se
  descartan.

## R-8 · Configuración de conexión (FR-020) — RESUELTA

- Archivo local `client-unity/LocalConfig/connection.local.json`, **ignorado**. Ejemplo
  versionado: `client-unity/LocalConfig/connection.example.json`. Campos: `host`, `port`,
  `serverKey`, `useSsl`.
- Está fuera de `Assets/` a propósito: no se empaqueta en las builds ni genera `.meta`.
- **Orden de precedencia**:
  1. variables de entorno `ECOSYSTEM_NAKAMA_HOST`, `ECOSYSTEM_NAKAMA_PORT`,
     `ECOSYSTEM_NAKAMA_SERVER_KEY` y `ECOSYSTEM_NAKAMA_USE_SSL` (las usaría la CI en el futuro);
  2. el archivo local.
  3. Si no hay ninguno, el estado es «error de configuración» con el nombre del archivo que hay
     que crear.
- **Builds**: en esta spec no llevan configuración de servidor, así que al ejecutarse mostrarían
  «error de configuración». Inyectar la configuración de publicación queda fuera de alcance.
- `tools/check-repo.sh` añade `connection.local.json` a la comprobación `secrets`, y
  `.gitignore` lo ignora.

## R-9 · Pruebas de Unity listas para la CI (FR-024 a FR-026) — RESUELTA

| Ensamblado | Plataforma | Necesita servidor | Contenido |
|-----------|-----------|-------------------|-----------|
| `Ecosystem.Tests.EditMode` (existente) | EditMode | No | Configuración del proyecto y, además, `ConnectionFlow`, `RetryPolicy`, compatibilidad, validadores C# con los ejemplos de los contratos y cargador de configuración |
| `Ecosystem.Tests.PlayMode` (existente) | PlayMode | No | Carga de `Bootstrap`. Sin configuración, el estado es «error de configuración» con un aviso, no un error |
| `Ecosystem.Tests.Integration` (nuevo) | **PlayMode** (corregido en la implementación) | **Sí** | Flujo real contra Nakama local: alta, misma cuenta al volver, refresco y versión de protocolo |

- **Corrección durante la implementación (T013)**: `UnityWebRequestAdapter` del SDK es un
  `MonoBehaviour` que usa corrutinas y `DontDestroyOnLoad`, y no funciona en EditMode (la primera
  ejecución se quedó colgada). Las pruebas de integración son `[UnityTest]` en **PlayMode**, que
  esperan a la `Task` cediendo fotogramas, con `[Timeout]`. Unity Test Framework 1.6 sí admite
  `async Task` en `[Test]` (verificado), pero no se usa en `[OneTimeSetUp]`.
- **Comandos en modo batch**: `-runTests -testPlatform EditMode -assemblyNames <ensamblado>`,
  documentados en `client-unity/README.md`.
- **Pruebas de integración**: comprueban primero la disponibilidad del servidor y fallan de
  inmediato con un mensaje claro (FR-025).
- **Registro en las pruebas**: las de PlayMode usan `LogAssert`, así que el estado «error de
  configuración» se registra como aviso (`LogWarning`), no como error.
- **CI**: no se configura. El README describe los pasos necesarios: licencia de Unity como
  secreto, una imagen o acción de Unity fijada por versión, y el trabajo `backend` reutilizado
  para las pruebas de integración.
