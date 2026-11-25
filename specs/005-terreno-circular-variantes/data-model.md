# Data Model: Variantes de terreno y territorio circular

Todos los datos son **locales del cliente**: nada viaja por red ni se valida en el servidor
(FR-025). Amplía el [data-model de la spec 004](../004-territorio-hexagonal/data-model.md). Las
convenciones geométricas (unidades, ejes, `R = 0,5`, `W = √3·R`, offset *odd-r*) **no cambian**.

## HexEdge (núcleo, nuevo)

Enumerado de las seis aristas, en el orden de `HexCell.Neighbours()` (research.md R-3):

| Valor | Nombre | Vecina | Normal |
|-------|--------|--------|--------|
| 0 | `E` | axial (+1, 0) | 0° |
| 1 | `NE` | (+1, −1), fondo | 60° |
| 2 | `NW` (NO) | (0, −1), fondo | 120° |
| 3 | `W` (O) | (−1, 0) | 180° |
| 4 | `SW` (SO) | (−1, +1), frente | 240° |
| 5 | `SE` | (0, +1), frente | 300° |

- `Opposite(e) = (e + 3) mod 6`.
- `HexCell.Neighbour(e)`: la vecina por esa arista.
- En los mensajes se usan los nombres en español: E, NE, NO, O, SO, SE.

## TerrainKind y RiverShape (núcleo, nuevos)

| `TerrainKind` | Valor | Geometría | Prefab (catálogo) |
|---------------|-------|-----------|-------------------|
| `Grass` | 0 (por defecto) | `HexTile` | `HexTile.prefab` (spec 004, sin cambios) |
| `Snow` | 1 | `HexTile` | `SnowTile.prefab` |
| `Sand` | 2 | `HexTile` | `SandTile.prefab` |
| `Water` | 3 | `WaterTile` (altura `hw`) | `WaterTile.prefab` |
| `River` | 4 | Una de las 15 mallas de río | `RiverTile.prefab` + malla del catálogo |

| `RiverShape` | Valor | Aristas abiertas con giro 0 | Giros canónicos |
|--------------|-------|-----------------------------|-----------------|
| `None` | 0 (por defecto) | — | — |
| `Spring` (manantial) | 1 | `{E}` | 0–5 |
| `Straight` (tramo recto) | 2 | `{E, W}` | 0–2 (`r mod 3`) |
| `Curve` (curva suave) | 3 | `{E, NW}` | 0–5 |

- **`RiverPiece.OpenEdges(shape, rotation)`** = `{ (e + rotation) mod 6 | e ∈ aristas con giro 0 }`.
- **`RiverPiece.Canonical(shape, rotation)`** = `rotation mod 3` para el recto; si no, `rotation`.

## TerritoryCell (núcleo, nuevo)

| Campo | Tipo | Reglas (validadas por `TerritoryLayout.Create`) |
|-------|------|------------------------------------------------|
| `Cell` | `HexCell` | Única en la disposición |
| `Terrain` | `TerrainKind` | Valor definido |
| `River` | `RiverShape` | `≠ None` si y solo si `Terrain == River`; valor definido |
| `Rotation` | `int` | 0–5 si es de río; 0 si no |

## TerritoryLayout (núcleo, ampliado)

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Entries` | lista de solo lectura de `TerritoryCell` | Válida según la tabla de abajo; conserva el orden del dato |
| `Cells` | lista de solo lectura de `HexCell` | Las celdas de `Entries`, en el mismo orden (se mantiene la API de la spec 004) |

**Creación**:
- `Create(IEnumerable<TerritoryCell>)`.
- `Create(IEnumerable<HexCell>)`, que se mantiene: todas las celdas son hierba.

Las dos devuelven un `TerritoryLayoutResult` con `Status`, `Layout`, `Cell`, `Edge` y `Message`.
Sigue la regla del primer error, en este orden (research.md R-6):

| Orden | `Status` | Cuándo | `Message` (tras `[Territorio] ` en la consola) |
|-------|----------|--------|-----------------------------------------------|
| 1 | `Empty` | Lista nula o vacía | «La disposición del territorio está vacía.» |
| 2 | `Duplicate` | Primera celda repetida | «La disposición repite la celda (columna C, fila F).» |
| 3 | `UnknownTerrain` | `Terrain` no definido | «La celda (columna C, fila F) tiene una variante de terreno desconocida (N).» |
| 4 | `InvalidRiverPiece` | Río sin forma; forma desconocida; forma o giro ≠ 0 en una celda que no es de río; giro fuera de 0–5 | «La celda (columna C, fila F) tiene datos de río no válidos: ‹causa›.» Causas: «es de río y no tiene forma», «forma de río desconocida (N)», «no es de río y tiene forma de río», «no es de río y tiene giro N», «giro N fuera de 0–5» |
| 5 | `Disconnected` | Alguna celda no se alcanza desde la primera | «La disposición no es conexa: la celda (columna C, fila F) no está unida al resto.» |
| 6 | `BrokenRiver` | Arista abierta que incumple FR-016 | «El río de la celda (columna C, fila F) no continúa por la arista X: ‹causa›.» Causas: «la vecina es de hierba», «… de nieve», «… de arena», «la vecina es una pieza de río con esa arista cerrada» |
| — | `Ok` | Ninguna de las anteriores | — |

**Regla de conexión (FR-016)**: para cada celda de río `c` y cada arista `e` de
`OpenEdges(c.River, c.Rotation)`, sea `n` la celda vecina por `e`. Es válido si:
- `n` no está en la disposición (salida por el borde);
- `n.Terrain == Water` (desembocadura);
- `n.Terrain == River` y `Opposite(e) ∈ OpenEdges(n.River, n.Rotation)`.

En cualquier otro caso, el resultado es `BrokenRiver`. No se validan manantiales, ríos cerrados,
sentido ni longitud.

**Derivados**:
- `Bounds(metrics)`: **sin cambios**. Usa `h`, que es la altura máxima, porque el agua y el cauce
  quedan por debajo.
- `AdjacentPairs()`: **138** en la disposición inicial.
- `BorderCells()`: celdas con al menos un vecino fuera. **24** en la disposición inicial.
- `FrontBorderCells()`: celdas cuyo vecino `SW` o `SE` está fuera. Se usa para FR-017.
- `RequiredVisuals()`: conjunto de claves de catálogo que pide la disposición:
  - `Terrain(kind)` para cada variante usada que no es río;
  - `River(shape, canonicalRotation)` para cada pieza de río usada.

## Disposición inicial (`Data/Territory/InitialTerritory.asset`)

Disco de 55 celdas (spec FR-001), con centro en `(columna 4, fila 4)`:

| Fila | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|------|---|---|---|---|---|---|---|---|---|
| Columnas | 3–5 | 1–6 | 1–7 | 0–7 | 1–7 | 0–7 | 1–7 | 1–6 | 3–5 |

**Composición de partida** (se ajusta al aprobar el diseño; tiene que seguir cumpliendo FR-017):

| Variante | Celdas `(columna, fila)` | En el borde frontal |
|----------|--------------------------|---------------------|
| Nieve (8) | (3,0) (4,0) (1,1) (2,1) (3,1) (1,2) (2,2) (0,3) | (0,3) |
| Arena (6) | (7,5) (6,6) (7,6) (5,7) (6,7) (5,8) | (7,5) (7,6) (5,7) (6,7) (5,8) |
| Agua (6) | (3,6) (2,7) (3,7) (4,7) (3,8) (4,8) | (2,7) (3,8) (4,8) |
| Río (6) | Ver la tabla siguiente | — |
| Hierba (29) | El resto | (0,5) (1,6) (1,7) (7,3) |

| Orden | Celda | Pieza | Giro | Aristas abiertas |
|-------|-------|-------|------|------------------|
| 1 | (5,1) | Manantial | 4 | SO |
| 2 | (5,2) | Recto | 1 | NE, SO |
| 3 | (4,3) | Recto | 1 | NE, SO |
| 4 | (4,4) | Curva | 5 | SE, NE |
| 5 | (4,5) | Curva | 2 | NO, SO |
| 6 | (4,6) | Recto | 1 | NE, SO → agua en (3,7) |

Se comprobó con un script (fuera del repositorio) que esta composición cumple las reglas de
conexión y los mínimos de FR-017: un manantial, 3 rectos y 2 curvas, desembocadura en agua y cada
variante con al menos 4 celdas y una en el borde frontal. Las pruebas EditMode lo vuelven a
comprobar sobre el asset.

## TerritoryLayoutAsset (vista, ampliado)

Cada entrada serializada pasa a ser `{ int column; int row; TerrainKind terrain; RiverShape river;
int rotation; }`. Los campos nuevos valen 0 por defecto, así que el formato de la spec 004 se lee
como hierba. Solo guarda datos: `ToLayout()` llama a `TerritoryLayout.Create`.

## TerrainCatalogAsset (vista, nuevo)

| Campo | Tipo | Contenido inicial |
|-------|------|-------------------|
| `terrains` | lista de `{TerrainKind kind; GameObject prefab}` | Grass → `HexTile.prefab`; Snow → `SnowTile.prefab`; Sand → `SandTile.prefab`; Water → `WaterTile.prefab` |
| `riverPrefab` | `GameObject` | `RiverTile.prefab` (materiales `M_Grass_Top`, `M_Earth_Side` y `M_Water_Top`) |
| `riverMeshes` | lista de `{RiverShape shape; int rotation; Mesh mesh}` | 15 entradas: Spring 0–5, Straight 0–2, Curve 0–5 |

La vista comprueba `RequiredVisuals()` contra el catálogo antes de crear piezas. Si una entrada
está repetida, gana la primera, y una prueba EditMode exige que el catálogo no tenga repetidas.
Si falta algo:

> `[Territorio] El catálogo no tiene recursos para ‹variante nieve | pieza de río curva, giro 3›.`

Ese error deja 0 piezas en la escena.

## Medidas visuales (núcleo)

`HexMetrics.Default` (`R = 0,5`, `h = 0,35`) **no cambia**. Se añade **`RiverMetrics.Default`**,
la única fuente de estos valores en el código:

| Campo | De partida | Origen |
|-------|-----------|--------|
| `WaterLevel` (`hw`) | 0,25 | Dirección visual |
| `ChannelWidth` (`w`) | 0,26 | Dirección visual |
| `PoolRadius` (`rp`) | 0,17 | Dirección visual |
| `ArcSegments` | 8 | Fijo |
| `PoolSegments` | 16 | Fijo |

Restricciones (validadas en el constructor):
- `0 < hw < h`;
- `0 < w < W/2`;
- `w/2 < rp < W/2`.

## Mallas generadas

| Malla | Generador | Submallas | Coordenadas de textura |
|-------|-----------|-----------|------------------------|
| `HexTile.asset` | `HexPrismMeshBuilder.Build(HexMetrics.Default)` | **Sin cambios** | **Sin cambios** |
| `WaterTile.asset` | `HexPrismMeshBuilder.Build(new HexMetrics(R, hw), TopUv.Lattice)` | 0 · agua arriba, 1 · lecho en los laterales | Arriba en red: `u = x/W`, `v = z/(3R)`. Laterales: `v = y/hw` |
| `Meshes/River/River_{Shape}_{r}.asset` (15) | `RiverPieceMeshBuilder.Build(HexMetrics.Default, RiverMetrics.Default, shape, r)` | 0 · orillas de hierba (`y = h`), 1 · laterales y paredes (tierra), 2 · agua del cauce (`y = hw`) | 0: inscrito, como `HexTile`. 1: `v = y/h`, y `u` a lo largo de la cara con la misma escala que los laterales de `HexTile`. 2: en red |

**Invariantes de las piezas de río** (pruebas EditMode):
1. Cada arista abierta tiene un corte de anchura `w` centrado en su punto medio: los vértices del
   cauce en la arista están en `m ± (w/2)·t`. Las aristas cerradas son un lateral completo de
   `0` a `h`.
2. En una arista abierta, el lateral tiene la muesca hasta `hw` y, a cada lado, laterales de `0`
   a `h`, sin huecos: la suma de sus anchuras es la longitud de la arista.
3. Todos los vértices del agua del cauce están a `y = hw`, igual que la cara superior de
   `WaterTile`.
4. La suma del área de las orillas y del agua del cauce (proyectadas en XZ) es igual al área del
   hexágono (tolerancia de 1e‑4). No hay solapes ni huecos en la cara superior.
5. La malla con giro `r` es la de giro 0 girada `60°·r` en sentido antihorario visto desde arriba,
   salvo las coordenadas de textura planares, que no giran.
6. Todos los triángulos tienen la cara frontal hacia fuera (orientación de Unity).

**Malla al día**: una prueba EditMode compara cada asset con la salida del generador, como
`HexTileMeshAssetTests`.

## Componentes de escena

| Componente | Cambio |
|------------|--------|
| `TerritoryView` | `tilePrefab` → `catalog` (`TerrainCatalogAsset`). Comprueba el catálogo, instancia por variante y asigna la malla de río. El resto del ciclo de vida no cambia (`[ExecuteAlways]` y `DontSave` fuera de *Play*) |
| `TerritoryCamera` | Sin cambios |
| `ConnectionScreen` | Sin cambios |

No hay transiciones de estado de juego. Las piezas no tienen estado ni comportamiento.
