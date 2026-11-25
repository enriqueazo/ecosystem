# Contrato: escena del territorio y paso desde `Bootstrap`

Comportamiento observable que verifican las pruebas PlayMode e integración y la revisión manual
(quickstart.md). No es un contrato de red: ningún mensaje sale del cliente.

## Escenas de la build

| Índice | Escena | Prueba |
|--------|--------|--------|
| 0 | `Assets/_Project/Scenes/Bootstrap.unity` | `ProjectConfigurationTests.BuildScenes_AreBootstrapThenTerritory` exige exactamente esta lista |
| 1 | `Assets/_Project/Scenes/Territory.unity` | Ídem |

## Composición de `Territory.unity`

| Objeto | Configuración obligatoria |
|--------|---------------------------|
| `Territory` | `TerritoryView` con `layout = Data/Territory/InitialTerritory.asset` y `tilePrefab = Art/Territory/Prefabs/HexTile.prefab` |
| `Main Camera` | `orthographic = true` y rotación `(θ, 0, 0)` con `θ` = inclinación aprobada (giro y alabeo 0). `clearFlags = SolidColor`, `backgroundColor` = fondo aprobado, `renderPostProcessing = false` y `TerritoryCamera` |
| `Directional Light` | Rotación aprobada, `shadows = None` |
| `RenderSettings` | `skybox = null`, `fog = false`, `ambientMode = Flat` con el color ambiente aprobado |

Además, la escena solo puede contener estos componentes (lista cerrada): `Transform`, `Camera`,
`UniversalAdditionalCameraData`, `Light`, `UniversalAdditionalLightData`, `TerritoryView`,
`TerritoryCamera`, `MeshFilter` y `MeshRenderer`. Así quedan fuera animación, partículas, audio,
volúmenes de posprocesado y componentes de conexión. Los módulos de animación, partículas y audio ni
siquiera están en el proyecto (`Packages/manifest.json`).

## Invariantes en reproducción (PlayMode, sin servidor)

1. Se carga sin ningún registro de error ni de aviso (`LogAssert.NoUnexpectedReceived`).
2. Contiene exactamente **16** instancias de pieza, con las posiciones que da
   `HexMetrics.Center` para las celdas de `InitialTerritory`.
3. Para `camera.aspect` ∈ {16/9, 19.5/9, 4/3, 9/16, 9/19.5, 3/4}, tras aplicar el encuadre,
   todas las esquinas de `Bounds` cumplen `WorldToViewportPoint ∈ [0,06; 0,94]` en x e y.
4. La posición, la rotación y el `orthographicSize` de la cámara no cambian entre el fotograma 1 y
   el 120 si el tamaño de pantalla no cambia (nada se mueve).

## Disposición no válida

Si `TerritoryView.layout` es nulo, está vacío o repite una celda:
- `Debug.LogError` registra un mensaje que empieza por `[Territorio] ` seguido de la causa (ver
  data-model.md → *TerritoryLayout*);
- no se instancia ninguna pieza.

Es un error de configuración del proyecto, no un estado del jugador.

## Paso desde `Bootstrap`

| Estado final de la conexión (spec 003) | Resultado |
|----------------------------------------|-----------|
| `Connected` | `ConnectionScreen` carga `sceneOnConnected` (`Assets/_Project/Scenes/Territory.unity`) en modo `Single`, **una sola vez** por apertura. El territorio se ve en ≤ 2 s |
| `Offline`, `UpdateRequired`, `ServerIncompatible`, `AccountError`, `ConfigurationError` | Sin cambio de escena. La pantalla de conexión se comporta como en la spec 003 |
| `Offline` → «Reintentar» → `Connected` | Se carga el territorio, una vez |

- `ConnectionScreen` no referencia ningún tipo de `Ecosystem.Territory*`.
- Si `sceneOnConnected` está vacío o no está en la build, se registra un error
  `[Conexión] Escena tras conectar no disponible: …` y se sigue en `Bootstrap`.

## Pruebas que cubren este contrato

| Grupo | Prueba | Qué fija |
|-------|--------|----------|
| EditMode | `ProjectConfigurationTests.BuildScenes_AreBootstrapThenTerritory` | Lista de escenas |
| PlayMode | `TerritorySceneTests.*` | Invariantes 1 a 4 y composición (cámara, fondo, luz, sin componentes animados) |
| PlayMode | `BootstrapSceneTests.WithoutConfiguration_DoesNotLoadTerritory` | Sin `Connected` no hay paso al territorio |
| Integración | `BootstrapSceneIntegrationTests.Connected_LoadsTerritoryOnce` | Paso real en ≤ 2 s, con 16 piezas |
