# Implementation Plan: Territorio hexagonal isométrico

**Branch**: `feat/004-territorio-hexagonal` | **Date**: 2026-11-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/004-territorio-hexagonal/spec.md`

## Summary

Mostrar el primer elemento visual del juego: un territorio de 16 prismas hexagonales con vértice
arriba, de tierra con césped, en 4 filas desplazadas, visto con una cámara ortográfica inclinada
frontal sobre un fondo gris azulado liso.

- **Disposición**: un ScriptableObject en texto con celdas offset *odd-r*. La valida un núcleo
  puro (`Ecosystem.Territory`, sin Unity) que también calcula posiciones, vecinos, límites, la
  malla del prisma y el encuadre. Todo se prueba en EditMode con TDD.
- **Arte**: la dirección visual y las dos texturas (césped y tierra) se crean con Claude Design.
  La malla se genera desde el núcleo y se guarda como asset, con una prueba que comprueba que
  está al día.
- **Escena**: `Territory.unity` se abre y se ve sin backend, también fuera de *Play*, y se
  reencuadra en cualquier proporción y área segura.
- **Flujo normal**: `ConnectionScreen` carga la escena una vez al llegar a `Connected`.
- **Git LFS (D-08)**: se resuelve en ADR-0011, sin LFS y con un umbral de revisión.
- **Fuera del cliente**: sin cambios en servidor, contratos ni orientación de pantalla.

## Technical Context

**Language/Version**: C# en Unity 6000.3.29f1 (IL2CPP en Android e iOS; API .NET Standard).

**Primary Dependencies**:
- URP 17.3.0, que ya está en el proyecto (shader `Simple Lit`).
- Sin paquetes nuevos (R-01 de la spec).

**Storage**: assets del proyecto en texto YAML (`ForceText`): disposición, malla, materiales,
prefab y escena. Más dos PNG de 512 × 512 en Git normal (R-1).

**Testing**: Unity Test Framework 1.6.0:
- **EditMode**: núcleo, malla al día y lista de escenas de la build.
- **PlayMode**: escena del territorio, y que sin configuración `Bootstrap` no pasa al territorio.
- **Integración**: paso real a `Connected`.
- **Revisión visual manual**: quickstart C.

**Target Platform**:
- Editor de Unity en macOS (vista *Game* y *Simulator*).
- Las builds de desarrollo de Android e iOS deben seguir compilando.

**Project Type**: cliente móvil Unity del monorepo existente. Solo `client-unity/` y `docs/`.

**Performance Goals**:
- Territorio visible en ≤ 2 s tras `Connected` (SC-006).
- Escena estática: 16 instancias de 30 vértices, 2 materiales, sin sombras ni posprocesado.

**Constraints**:
- Sin red, contratos ni cambios de servidor (FR-022).
- La orientación y la resolución del proyecto no cambian (FR-019).
- Sin paquetes nuevos.
- Texturas de 512 como máximo con ASTC.
- `Presentation` no depende de `Territory*`.

**Scale/Scope**:
- código: 3 ensamblados nuevos (núcleo, vista y editor), 1 escena y 1 cambio pequeño en
  `ConnectionScreen` y `Bootstrap`;
- arte: 2 texturas, 2 materiales, 1 malla y 1 prefab;
- datos y documentación: 1 asset de datos y 1 ADR.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio / norma | Cumplimiento | Estado |
|-------------------|--------------|--------|
| I. Servidor autoritativo | Solo presentación. La disposición es un dato local de vista, sin valor de juego. Cuando el territorio tenga reglas o estado, la autoridad será el servidor (spec futura). | ✅ |
| II. Contratos versionados | No hay mensajes nuevos. La disposición **no** es un contrato de red y no va en `shared/` (R-2). | ✅ N/A |
| III. Resiliencia | No hay funcionalidad en directo. El paso de escena ocurre después de que `RunAsync` termine en `Connected`, una sola vez, y la cancelación de la spec 003 sigue igual (R-4). Lo que pase al perder la conexión con el territorio visible está fuera de alcance de forma explícita. | ✅ |
| IV. Desacoplado y simple | El núcleo no conoce Unity. La vista depende del núcleo y el editor de la vista. `Presentation` solo conoce una ruta de escena. Ni escenas ni singletons como dependencias globales. Sin paquetes nuevos ni servicios de navegación sin consumidores. | ✅ |
| V. Calidad | La validación devuelve resultados, no excepciones. El ciclo de vida `[ExecuteAlways]` es explícito (`OnEnable`/`OnDisable`, `DontSave` fuera de *Play*). Se verifican las API de Unity usadas: `Screen.safeArea`, `HideFlags`, `Mesh.SetSubMesh`, `SceneManager`. | ✅ |
| VI. TDD | Pruebas primero para la disposición, la geometría, los vecinos, el encuadre y la malla al día. PlayMode para la escena y el paso. El arte y el aspecto se validan a mano con la revisión de quickstart C, documentada (excepción prevista por la constitución). | ✅ |
| VII. Seguridad | Sin secretos ni datos personales. El artefacto de diseño es privado y solo se enlaza desde la spec. | ✅ |
| VIII. Reproducibilidad | Sin versiones nuevas. La malla se genera con un comando documentado y se comprueba. Los valores visuales están en `contracts/visual-direction.md`. Se actualizan los README. | ✅ |
| Decisiones abiertas | D-08 se resuelve antes del primer binario de arte (ADR-0011, R-1). | ✅ |
| Una rama por spec / commits atómicos | Rama `feat/004-territorio-hexagonal`. | ✅ |

**Resultado**: PASA, sin desviaciones respecto a la spec.

**Re-check tras la fase 1**: PASA. El diseño (data-model y contratos) no añade dependencias,
mensajes ni estado global.

## Project Structure

### Documentation (this feature)

```text
specs/004-territorio-hexagonal/
├── spec.md · plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   ├── territory-scene.md        # Escenas de la build, composición, invariantes y paso desde Bootstrap
│   └── visual-direction.md       # Qué entrega Claude Design, valores aprobados e importación en Unity
└── checklists/requirements.md
```

### Source Code (repository root)

```text
client-unity/Assets/_Project/
├── Art/Territory/                              # Nuevo (P-06)
│   ├── Textures/T_Grass_Top.png, T_Earth_Side.png
│   ├── Materials/M_Grass_Top.mat, M_Earth_Side.mat
│   ├── Meshes/HexTile.asset                    # Generada desde el núcleo
│   └── Prefabs/HexTile.prefab
├── Data/Territory/InitialTerritory.asset       # Nuevo: 16 celdas odd-r
├── Scenes/
│   ├── Bootstrap.unity                         # ConnectionScreen.sceneOnConnected = Territory
│   └── Territory.unity                         # Nueva (índice 1 de la build)
├── Scripts/
│   ├── Presentation/ConnectionScreen.cs        # + carga de escena una vez en Connected
│   ├── Territory/                              # Ecosystem.Territory.asmdef (noEngineReferences)
│   │   ├── HexCell.cs, HexMetrics.cs, Point.cs
│   │   ├── TerritoryLayout.cs                  # Create → Ok | Empty | Duplicate
│   │   ├── HexPrismMeshBuilder.cs
│   │   └── TerritoryFraming.cs
│   ├── Territory.View/                         # Ecosystem.Territory.View.asmdef
│   │   ├── TerritoryLayoutAsset.cs             # ScriptableObject
│   │   ├── TerritoryView.cs                    # [ExecuteAlways] instancia piezas
│   │   └── TerritoryCamera.cs                  # [ExecuteAlways] encuadre al cambiar pantalla
│   └── Territory.Editor/                       # Ecosystem.Territory.Editor.asmdef (solo editor)
│       └── HexTileMeshMenu.cs                  # Ecosystem → Territorio → Regenerar malla de pieza
└── Tests/
    ├── EditMode/Territory/                     # Layout, HexMetrics/vecinos, malla, encuadre, malla al día
    ├── EditMode/ProjectConfigurationTests.cs   # BuildScenes_AreBootstrapThenTerritory
    ├── PlayMode/TerritorySceneTests.cs         # Nuevo
    ├── PlayMode/BootstrapSceneTests.cs         # + no pasa al territorio sin configuración
    └── Integration/BootstrapSceneIntegrationTests.cs   # + Connected_LoadsTerritoryOnce
client-unity/ProjectSettings/EditorBuildSettings.asset  # + Territory.unity (índice 1)
client-unity/README.md                                  # Estructura, escena, pruebas y regenerar malla
README.md                                               # Línea de estado
docs/decisions/0011-sin-git-lfs.md                      # Resuelve D-08
docs/decisions/README.md, pendientes.md                 # Índice; D-08 → Resueltas
```

**Structure Decision**: las dependencias van en una sola dirección, en dos ramas independientes:

```text
Territory.Editor → Territory.View → Territory
Presentation → Connection.Nakama → Connection → Contracts
```

`Presentation` solo conoce la ruta de la escena del territorio, que está en la build. Las pruebas
EditMode y PlayMode añaden referencias a `Ecosystem.Territory` y `Ecosystem.Territory.View`.

## Orden de entrega (commits atómicos, prueba e implementación juntas)

1. `docs(decisions)`: ADR-0011, sin Git LFS. D-08 pasa a *Resueltas* (antes de cualquier
   binario).
2. `feat(client)`: núcleo `Ecosystem.Territory` con TDD: `HexCell`, `HexMetrics`, vecinos (33
   parejas), `TerritoryLayout` con validación, límites y `TerritoryFraming` (6 proporciones y área
   segura).
3. `feat(client)`: `HexPrismMeshBuilder` con TDD (30 vértices, 16 triángulos, 2 submallas y
   normales planas), el comando del editor, `HexTile.asset` y la prueba de malla al día.
4. **Diseño** (sin commit de código): artefacto de Claude Design con paleta, pieza, cámara, luz,
   referencias y texturas. Lo aprueba el responsable.
5. `feat(client)`: arte del territorio (texturas con su importación, materiales y prefab) y
   `contracts/visual-direction.md` con los valores aprobados y el enlace. Si cambia `h`, se
   regenera la malla en este commit.
6. `feat(client)`: vista (`TerritoryLayoutAsset`, `TerritoryView`, `TerritoryCamera`),
   `InitialTerritory.asset` y `Territory.unity`, con `TerritorySceneTests` primero (Red).
7. `feat(client)`: paso desde `Bootstrap` en `Connected`, la escena en la build y la actualización
   justificada de `ProjectConfigurationTests`, con las pruebas PlayMode e integración del paso.
8. `docs`: `client-unity/README.md`, README raíz y registro de la revisión manual (quickstart C)
   en la spec.

## Riesgos

| Riesgo | Mitigación |
|--------|------------|
| Líneas finas en las aristas compartidas por redondeo (sin MSAA, `renderScale` 0,8) | Los laterales de tierra tapan cualquier rendija, así que no se ve el fondo. Si la revisión C2 aún ve líneas, se aplica un solape intencionado documentado de ≤ 0,1 % del ancho (dentro de SC-003) |
| Las 16 caras de césped se leen como una sola superficie | Banda de borde hexagonal en `T_Grass_Top` (R-5). La valida la revisión C3 |
| Las instancias `[ExecuteAlways]` marcan la escena como modificada o se guardan | `HideFlags.DontSave` fuera de *Play*. La revisión A3 y una prueba EditMode comprueban que el asset de la escena no contiene instancias |
| El artefacto de diseño no exporta el PNG con el tamaño exacto | El botón exporta con `canvas` a 512 × 512. Si falla, se exporta desde el navegador y se comprueba el tamaño en el inspector de Unity antes de confirmar |
| ASTC no es el formato por defecto en alguna de las dos plataformas | Se fija explícitamente en el *override* por plataforma de cada textura |
| `Screen.safeArea` en el editor es la pantalla entera | El área segura se prueba con valores ficticios en EditMode y a mano en la vista *Simulator* |
| La carga de escena coincide con la destrucción de `ConnectionScreen` | Se carga después de que `RunAsync` termine. La bandera `_navigated` y `destroyCancellationToken` evitan una segunda carga |

## Complexity Tracking

| Elemento | Por qué hace falta | Alternativa más simple descartada |
|----------|--------------------|-----------------------------------|
| Malla generada y guardada como asset, con prueba de que está al día | Aristas exactas por construcción (FR-004) y prefab autosuficiente (FR-012), sin binario FBX | Un modelo FBX añade herramienta y binario. Generarla solo en ejecución deja el prefab sin malla |
| Ensamblado de editor `Ecosystem.Territory.Editor` | Escribir assets (`AssetDatabase`) solo puede hacerse en el editor | `#if UNITY_EDITOR` dentro de la vista mezcla código de editor y de ejecución |
