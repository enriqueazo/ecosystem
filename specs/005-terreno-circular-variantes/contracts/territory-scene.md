# Contrato: escena del territorio (ampliación de la spec 004)

Comportamiento observable que verifican las pruebas EditMode, PlayMode e integración, y la revisión
manual (quickstart.md). Amplía el
[contrato de la spec 004](../../004-territorio-hexagonal/contracts/territory-scene.md). Lo que no
se menciona aquí **no cambia**:
- las escenas de la build: `Bootstrap` en el índice 0 y `Territory` en el 1;
- el paso desde `Bootstrap`;
- la cámara, la luz y `RenderSettings`;
- la **lista cerrada de componentes**.

No es un contrato de red: ningún mensaje sale del cliente.

## Composición de `Territory.unity`

| Objeto | Configuración obligatoria |
|--------|---------------------------|
| `Territory` | `TerritoryView` con `layout = Data/Territory/InitialTerritory.asset` y `catalog = Data/Territory/TerrainCatalog.asset`. **Ya no tiene** `tilePrefab` |
| `Main Camera`, `Directional Light`, `RenderSettings` | Igual que en la spec 004 |

La lista cerrada de componentes sigue igual: `Transform`, `Camera`,
`UniversalAdditionalCameraData`, `Light`, `UniversalAdditionalLightData`, `TerritoryView`,
`TerritoryCamera`, `MeshFilter` y `MeshRenderer`. Las piezas nuevas solo llevan `MeshFilter` y
`MeshRenderer` (research.md R-5).

## Invariantes en reproducción (PlayMode, sin servidor)

1. Se carga sin ningún registro de error ni de aviso (`LogAssert.NoUnexpectedReceived`).
2. Contiene exactamente **55** piezas. Cada una está en la posición que da `HexMetrics.Center`
   para su celda de `InitialTerritory`, sin giro (`localRotation` identidad).
3. Cada pieza usa los recursos de su entrada en el catálogo:
   - **hierba, nieve, arena y agua**: la malla y los materiales del prefab de su variante;
   - **río**: la malla `River_{Shape}_{rotación canónica}` y los materiales de `RiverTile.prefab`.
4. Para `camera.aspect` ∈ {16/9, 19.5/9, 4/3, 9/16, 9/19.5, 3/4}, tras aplicar el encuadre, todas
   las esquinas de `Bounds` cumplen `WorldToViewportPoint ∈ [0,06; 0,94]` en x e y (igual que en la
   spec 004).
5. Nada se mueve entre los fotogramas 1 y 120 (igual que en la spec 004).
6. Ninguna pieza proyecta ni recibe sombras.

## Datos no válidos

Si la disposición o el catálogo no son válidos:
- se registra un `Debug.LogError` con `[Territorio] ` seguido del mensaje del núcleo (ver
  [data-model.md](../data-model.md) → *TerritoryLayout* y *TerrainCatalogAsset*);
- se crean **0** piezas, también fuera de *Play*.

| Caso de prueba (PlayMode, con `TerritoryView.SetLayout` o `SetCatalog`) | Mensaje que empieza por |
|--------------------------------------------------------------------------|-------------------------|
| Celda repetida | `[Territorio] La disposición repite la celda` |
| Disposición no conexa | `[Territorio] La disposición no es conexa` |
| Variante desconocida | `[Territorio] La celda (columna` … `variante de terreno desconocida` |
| Río sin forma | `[Territorio] La celda (columna` … `datos de río no válidos` |
| Río que acaba en nieve | `[Territorio] El río de la celda (columna` |
| Catálogo sin la variante usada | `[Territorio] El catálogo no tiene recursos para` |

Son errores de configuración del proyecto, no estados del jugador.

## Pruebas que cubren este contrato

| Grupo | Prueba | Qué fija |
|-------|--------|----------|
| EditMode | `TerritoryLayoutTests`, `RiverConnectionTests`, `HexEdgeTests` | Validación, conexión y aristas (núcleo) |
| EditMode | `InitialTerritoryTests` (sustituye las comprobaciones de 4 × 4 de `TerritoryLayoutAssetTests`) | Disco de 55 celdas, 138 parejas, 24 de borde, sin huecos y composición de FR-017 |
| EditMode | `RiverPieceMeshBuilderTests`, `PolygonTriangulatorTests` | Invariantes 1–6 de las mallas de río |
| EditMode | `TerritoryMeshAssetTests` (más `HexTileMeshAssetTests`, sin cambios) | Mallas guardadas al día |
| EditMode | `TerrainCatalogAssetTests`, `TerritoryArtAssetTests` (ampliada) | Catálogo completo, sin repetidas; importación de las 6 texturas nuevas; hierba sin cambios |
| EditMode | `TerritorySceneAssetTests` (actualizada) | Sin *Play* se ven 55 piezas; la escena no guarda instancias |
| PlayMode | `TerritorySceneTests` (actualizada) | Invariantes 1–6 y la tabla de datos no válidos |
| PlayMode | `BootstrapSceneTests.WithoutConfiguration_DoesNotLoadTerritory` | Sin cambios |
| Integración | `BootstrapSceneIntegrationTests.Connected_LoadsTerritoryOnce` (actualizada) | Paso real en ≤ 2 s, con 55 piezas |
