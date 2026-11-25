# Cliente Unity

Proyecto Unity del juego para **Android e iOS**, con Universal Render Pipeline (URP). Este
documento es el procedimiento único del cliente; el README raíz enlaza aquí.

> Estado: conexión como invitado con Nakama (spec 003) y primer territorio visible: 16 piezas
> hexagonales en vista isométrica (spec 004). Todavía no hay mecánicas de juego.

## Versión y requisitos

| Requisito | Detalle |
|-----------|---------|
| Unity | **6000.3.29f1** (Unity 6.3 LTS), la revisión fijada en `ProjectSettings/ProjectVersion.txt` ([ADR-0005](../docs/decisions/0005-unity-63-lts.md)) |
| Módulos del editor | *Android Build Support* (con OpenJDK, Android SDK y NDK) e *iOS Build Support* |
| iOS | macOS con Xcode, para abrir el proyecto exportado |
| Licencia | Unity Personal (provisional, ver ADR-0005) activada en Unity Hub |

Instala la revisión exacta desde Unity Hub (*Installs → Install Editor → Archive*) con los dos
módulos.

## Abrir el proyecto

1. En Unity Hub: *Add → Add project from disk* y elige la carpeta `client-unity/`.
2. Ábrelo **con la revisión 6000.3.29f1**. Si Hub propone otra versión, no la aceptes: cambiar de
   versión es una decisión explícita que requiere un ADR nuevo (constitución, principio VIII).
3. La primera importación tarda unos minutos y crea `Library/` (ignorada por Git).

Abrir y cerrar el proyecto sin cambiar nada no debe modificar ningún archivo versionado. Si
`git status` muestra cambios que no has hecho, revísalos antes de confirmarlos.

## Estructura

```text
Assets/_Project/
├── Art/Territory/             Texturas, materiales (URP Simple Lit), malla GENERADA y prefab de la pieza
├── Data/Territory/            InitialTerritory.asset: disposición inicial (16 celdas)
├── Scenes/
│   ├── Bootstrap.unity        Escena inicial y pantalla de conexión. Índice 0 de la build.
│   └── Territory.unity        Territorio hexagonal. Índice 1; se abre también sin backend.
├── Settings/                  Assets de URP (pipeline y renderer móviles, ajustes globales, volumen).
├── Scripts/
│   ├── Contracts/             Ecosystem.Contracts: validadores C# GENERADOS desde shared/contracts/
│   ├── Connection/            Ecosystem.Connection: núcleo de conexión, sin Unity ni Nakama
│   ├── Connection.Nakama/     Ecosystem.Connection.Nakama: adaptadores (SDK de Nakama, PlayerPrefs, config)
│   ├── Presentation/          Ecosystem.Presentation: ConnectionScreen (IMGUI provisional)
│   ├── Territory/             Ecosystem.Territory: rejilla hexagonal, disposición, malla y encuadre, sin Unity
│   ├── Territory.View/        Ecosystem.Territory.View: TerritoryLayoutAsset, TerritoryView y TerritoryCamera
│   └── Territory.Editor/      Ecosystem.Territory.Editor: comando para regenerar la malla (solo editor)
└── Tests/
    ├── EditMode/              Configuración, conexión, contratos y núcleo del territorio
    ├── PlayMode/              Bootstrap y la escena del territorio
    └── Integration/           Contra el Nakama local (PlayMode)
```

Dependencias en una sola dirección, en dos ramas independientes:
`Presentation → Connection.Nakama → Connection → Contracts` y
`Territory.Editor → Territory.View → Territory`. `Presentation` solo conoce la ruta de la escena del
territorio, no sus ensamblados. El núcleo (`Connection`) no conoce
Unity ni Nakama: todo pasa por puertos (`IGameServer`, `ISessionStore`, `IInstallationIdStore`,
`IDelay`, `IClock`, `IAuthenticator`), así que se prueba con dobles.

`Scripts/Contracts/Generated/Contracts.g.cs` y `Tests/EditMode/Generated/ContractFixtures.g.cs`
los genera `npm run generate` en `server-nakama/`. **No se editan a mano**, y la CI falla si están
desactualizados ([shared/README.md](../shared/README.md#g-validación-y-comprobación)).

## Pruebas

Unity Test Framework (NUnit), en tres grupos que se ejecutan por separado:

| Grupo | Ensamblado | Necesita | Qué comprueba |
|-------|-----------|----------|---------------|
| EditMode | `Ecosystem.Tests.EditMode` | Nada | Configuración del proyecto (escenas de la build: `Bootstrap` y `Territory`); núcleo de conexión (sesión, reintentos, compatibilidad, cancelación, resultados tardíos) con dobles; los ejemplos de los contratos dan el mismo resultado con los validadores C# que con Ajv; territorio: celdas y vecinos, disposición y su validación, malla de la pieza y su asset al día, encuadre en 6 proporciones y con área segura, escena sin piezas guardadas |
| PlayMode | `Ecosystem.Tests.PlayMode` | Nada | `Bootstrap` carga sin errores, muestra «Configuración ausente» y no pasa al territorio. La prueba fija `ECOSYSTEM_NAKAMA_CONFIG=none`, así que no depende de tu configuración local. `Territory` carga sin errores con 16 piezas, cámara ortográfica inclinada sin giro, fondo liso, sin sombras ni posprocesado, encuadre en las 6 proporciones y rechazo de disposiciones no válidas |
| Integración | `Ecosystem.Tests.Integration` (PlayMode) | Entorno local arrancado con el módulo (`tools/check-backend.sh --module` → 0) y la configuración local | Alta de invitado, misma cuenta al volver (sesión, refresco, reautenticación), versión de protocolo, clave errónea, sin conexión, salida de la escena a mitad y paso al territorio al conectar (una vez, en ≤ 2 s). Si el entorno no responde, falla al instante con «Entorno local no disponible» |

Las de integración son PlayMode porque el adaptador HTTP del SDK (`UnityWebRequestAdapter`) usa
corrutinas y necesita el bucle del reproductor.

**Desde el editor**: *Window → General → Test Runner*, pestañas *EditMode* y *PlayMode*.

**En modo batch** (macOS; ajusta la ruta del editor en otros sistemas), desde la raíz del
repositorio:

```sh
UNITY="/Applications/Unity/Hub/Editor/6000.3.29f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics -projectPath client-unity -runTests -testPlatform EditMode \
  -assemblyNames Ecosystem.Tests.EditMode -testResults editmode.xml -logFile -
"$UNITY" -batchmode -projectPath client-unity -runTests -testPlatform PlayMode \
  -assemblyNames Ecosystem.Tests.PlayMode -testResults playmode.xml -logFile -
"$UNITY" -batchmode -projectPath client-unity -runTests -testPlatform PlayMode \
  -assemblyNames Ecosystem.Tests.Integration -testResults integration.xml -logFile -
```

- No añadas `-quit`: `-runTests` cierra el editor al terminar.
- Código de salida: `0` si todas pasan y `2` si alguna falla. No confirmes los XML en Git.
- El proyecto no puede estar abierto en otro editor a la vez.
- Si interrumpes Unity a mitad de una ejecución y el siguiente arranque falla en el compilador de
  Burst, borra `client-unity/Temp/` y `client-unity/Library/BurstCache/` (se regeneran).

**Para llevarlas a la CI** (todavía no se ejecutan en GitHub Actions, ADR-0007): los mismos
comandos con una licencia de Unity como secreto del repositorio y una acción o imagen de Unity
fijada por versión. Los grupos EditMode y PlayMode no necesitan servidor. El de integración
encajaría en el trabajo `backend`, que ya arranca el entorno, pasando la configuración por las
variables `ECOSYSTEM_NAKAMA_*` (ver *Conexión con el servidor*).

## Builds de desarrollo

Sin firma ni publicación (fuera de alcance). Las salidas van a `client-unity/Builds/`, que está
ignorada por Git.

- **Android**: *File → Build Profiles → Android → Switch Platform*, marca *Development Build* y
  pulsa *Build*. Destino: `Builds/Android/Ecosystem-dev.apk`.
- **iOS** (macOS): *File → Build Profiles → iOS → Switch Platform*, marca *Development Build* y
  pulsa *Build*. Destino: `Builds/iOS/`. El resultado es un proyecto Xcode
  (`Unity-iPhone.xcodeproj`); compilarlo y firmarlo en Xcode queda fuera de esta fase.

La primera build de cada plataforma completa algunos ajustes de URP y del proyecto (datos de
filtrado de shaders y *batching* por plataforma). Ya están versionados, así que las builds
siguientes no deben cambiar ningún archivo versionado.

## Paquetes

Solo se declaran paquetes con una necesidad documentada (constitución, principio IV). Sus
dependencias indirectas las fija `Packages/packages-lock.json`, que se versiona.

| Paquete | Versión | Necesidad |
|---------|---------|-----------|
| `com.unity.render-pipelines.universal` | 17.3.0 | Pipeline de render del proyecto ([ADR-0003](../docs/decisions/0003-pipeline-render-urp.md)) |
| `com.unity.test-framework` | 1.6.0 | Pruebas EditMode y PlayMode (constitución, principio VI) |
| `com.unity.ide.rider` | 3.1.0 | Integración con JetBrains Rider (generación de proyectos y depuración) |
| `com.unity.ide.visualstudio` | 2.0.26 | Integración con Visual Studio y VS Code (generación de proyectos y depuración) |
| `com.heroiclabs.nakama-unity` | 3.23.0 (commit `704f5da…`) | SDK de Nakama: autenticación, sesión y RPC ([ADR-0009](../docs/decisions/0009-sdk-nakama-unity.md)) |
| `com.unity.nuget.newtonsoft-json` | 3.2.2 | Leer JSON para los validadores de contratos generados ([ADR-0010](../docs/decisions/0010-validadores-csharp-generados.md)) |

El proyecto se creó con la plantilla **«3D URP»**, que es el nombre de la plantilla *Universal
3D* en Unity 6.3. Se le retiraron los paquetes sin uso: Input System, uGUI, Timeline, Visual
Scripting, AI Navigation, Multiplayer Center, Collab Proxy y los módulos declarados
explícitamente. Input System volverá con la primera funcionalidad de controles, con su necesidad
documentada.

## Valores provisionales

Hasta resolver [D-06](../docs/decisions/pendientes.md#d-06--nombre-de-producto-e-identificador-de-aplicación-definitivos):

| Ajuste | Valor provisional |
|--------|-------------------|
| Identificador de aplicación (Android e iOS) | `com.ecosystem.dev` |
| Nombre de producto | `Ecosystem` |
| Compañía | `Ecosystem` |

## Conexión con el servidor

Al abrir `Bootstrap`, el cliente entra como **invitado** en el Nakama configurado
([ADR-0009](../docs/decisions/0009-sdk-nakama-unity.md)):

1. **Identidad**: un identificador de instalación aleatorio, guardado en `PlayerPrefs` y nunca
   derivado del hardware, sirve para la autenticación por dispositivo. La identidad del jugador es
   su **cuenta en el servidor** (`userId`). Más adelante se podrán vincular otras credenciales a
   esa misma cuenta (`IAuthenticator`).
2. **Sesión**: se guarda en `PlayerPrefs`. Si sigue vigente, se reutiliza; si ha caducado (2 h),
   se refresca (el refresco dura 7 días); si no se puede, se vuelve a autenticar la misma
   instalación y se recupera **la misma cuenta**.
3. **Versión de protocolo**: llama al RPC `protocol_version` y valida la respuesta con el
   contrato generado. Si el servidor tiene una versión mayor, «actualiza el juego»; si es menor o
   la respuesta no cumple el contrato, «servidor incompatible».
4. **Fallos de red**: hasta 3 reintentos (1, 2 y 4 s) con 10 s de tiempo límite por petición y,
   si no hay forma, «Sin conexión» con «Reintentar» en menos de 60 s. Al salir de la escena se
   cancela todo sin errores.

Estados y textos completos: [contrato de estados](../specs/003-sesion-invitado-cliente/contracts/connection-states.md).

Al llegar a «Conectado», `Bootstrap` carga una vez la escena del territorio (`sceneOnConnected` en
`ConnectionScreen`). En cualquier otro estado, la pantalla de conexión se queda como está.

## Territorio

Primer elemento visual del juego (spec 004): 16 piezas hexagonales de tierra con césped, en 4 filas
de 4 con las filas impares desplazadas media pieza, vistas con una cámara ortográfica inclinada 35°
sobre un fondo gris azulado liso. Dirección visual y valores:
[contrato de dirección visual](../specs/004-territorio-hexagonal/contracts/visual-direction.md).

- **Revisarlo sin backend**: abre `Assets/_Project/Scenes/Territory.unity`. Se ve ya sin pulsar
  *Play* (las piezas no se guardan en la escena) y en *Play* no necesita Nakama ni configuración
  local. La revisión visual completa está en el
  [quickstart de la spec 004](../specs/004-territorio-hexagonal/quickstart.md#c-revisión-visual-manual-fr-024).
- **Disposición**: `Assets/_Project/Data/Territory/InitialTerritory.asset`, una lista de celdas
  `{column, row}` en coordenadas offset *odd-r*. Una disposición vacía o con celdas repetidas no
  muestra piezas y registra un error `[Territorio] …`.
- **Encuadre**: `TerritoryCamera` mantiene el tablero entero dentro del área segura, con un 6 % de
  margen por lado, en vertical y en horizontal (la rotación automática del proyecto no cambia).
- **Malla de la pieza**: la genera el código (`HexPrismMeshBuilder`). Si cambias la altura en
  `HexMetrics.Default`, ejecuta *Ecosystem → Territorio → Regenerar malla de pieza*; si no, la
  prueba `HexTileMeshAssetTests` falla. En modo batch:
  `"$UNITY" -batchmode -nographics -quit -projectPath client-unity -executeMethod Ecosystem.Territory.Editor.HexTileMeshMenu.Regenerate`.
- **Binarios**: las texturas van en Git normal, sin LFS, con el umbral de revisión de
  [ADR-0011](../docs/decisions/0011-sin-git-lfs.md).

### Configuración local

1. Copia `client-unity/LocalConfig/connection.example.json` a
   `client-unity/LocalConfig/connection.local.json`. **No se versiona**: está en `.gitignore` y
   `tools/check-repo.sh` lo rechaza si alguien lo añade.
2. Pon en `serverKey` el valor de `NAKAMA_SERVER_KEY` de tu `deploy/local/.env`. Si cambiaste
   `NAKAMA_API_PORT`, cámbialo también en `port`.
3. Arranca el backend ([deploy/local/README.md](../deploy/local/README.md)) y reproduce
   `Bootstrap`.

En su lugar puedes usar las variables `ECOSYSTEM_NAKAMA_HOST`, `ECOSYSTEM_NAKAMA_PORT`,
`ECOSYSTEM_NAKAMA_SERVER_KEY` y `ECOSYSTEM_NAKAMA_USE_SSL`, que tienen precedencia sobre el
archivo. `ECOSYSTEM_NAKAMA_CONFIG=none` desactiva la configuración, y lo usan las pruebas.

La carpeta `LocalConfig/` está fuera de `Assets/`, así que **las builds no llevan configuración**
y muestran «Configuración de conexión ausente». Configurar builds de publicación queda para una
spec posterior. La clave de servidor de Nakama es la clave pública de cliente; la `http_key` nunca
va en el cliente.

### Limitaciones conocidas

- **Reinstalar la app o borrar sus datos crea una cuenta de invitado nueva**: se pierde el
  identificador de instalación. Se resolverá con la vinculación de cuentas.
- Cada dispositivo es una cuenta de invitado distinta.
- Los tokens se guardan en `PlayerPrefs`. Al añadir cuentas registradas habrá que pasar a un
  almacenamiento seguro (ADR-0009).
- Solo se ha verificado en el editor contra `127.0.0.1`. Conectar dispositivos o emuladores al
  backend local queda fuera de alcance.
