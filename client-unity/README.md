# Cliente Unity

Proyecto Unity del juego para **Android e iOS**, con Universal Render Pipeline (URP). Este
documento es el procedimiento único del cliente; el README raíz enlaza aquí.

> Estado: base del proyecto. Una escena inicial vacía (solo cámara) y pruebas de configuración.
> Todavía no hay código de juego ni conexión con el servidor.

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
├── Scenes/Bootstrap.unity     Escena inicial: solo una cámara. Única escena de la build.
├── Settings/                  Assets de URP: pipeline y renderer móviles, ajustes globales y
│                              perfil de volumen por defecto.
└── Tests/
    ├── EditMode/              Pruebas de configuración del proyecto (ensamblado solo de editor).
    └── PlayMode/              Prueba de carga de la escena inicial.
```

Las carpetas de código de juego (p. ej. `Scripts/` con su definición de ensamblado) se crean con
la primera funcionalidad que aporte código. No se crean carpetas vacías.

## Pruebas

Se usa Unity Test Framework (NUnit):

| Suite | Qué comprueba |
|-------|---------------|
| EditMode (`ProjectConfigurationTests`) | `Bootstrap` es la única escena de la build y está en el índice 0; IL2CPP en Android e iOS; Android solo ARM64; serialización *Force Text*; `.meta` visibles; URP en el pipeline por defecto y en todos los niveles de calidad; identificador provisional |
| PlayMode (`BootstrapSceneTests`) | `Bootstrap` carga, tiene una cámara y no registra errores |

**Desde el editor**: *Window → General → Test Runner*, pestañas *EditMode* y *PlayMode* →
*Run All*.

**En modo batch** (macOS; ajusta la ruta del editor en otros sistemas), desde la raíz del
repositorio:

```sh
UNITY="/Applications/Unity/Hub/Editor/6000.3.29f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics -projectPath client-unity \
  -runTests -testPlatform EditMode -testResults editmode-results.xml -logFile -
"$UNITY" -batchmode -projectPath client-unity \
  -runTests -testPlatform PlayMode -testResults playmode-results.xml -logFile -
```

- No añadas `-quit`: `-runTests` cierra el editor al terminar.
- Código de salida: `0` si todas las pruebas pasan y `2` si alguna falla.
- Los resultados quedan en el XML indicado; no los confirmes en Git.
- El proyecto no puede estar abierto en otro editor a la vez.

Las API de Unity que usan las pruebas (`NamedBuildTarget`, `PlayerSettings`,
`EditorBuildSettings`, `EditorSettings`, `VersionControlSettings`, `GraphicsSettings`,
`QualitySettings`, `SceneManager`, `LogAssert`) se verificaron compilando contra 6000.3.29f1, sin
avisos de obsolescencia.

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

El cliente **no** incluye todavía el SDK de Nakama ni ninguna configuración de conexión
([ADR-0004](../docs/decisions/0004-conectividad-sin-sdk-cliente.md)). Se añadirá, con versión
fijada y su propio ADR, en la primera funcionalidad que lo necesite. El cliente nunca contendrá
claves de servidor.
