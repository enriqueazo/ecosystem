---

description: "Task list for feature 005-terreno-circular-variantes"
---

# Tasks: Variantes de terreno y territorio circular

**Input**: Design documents from `specs/005-terreno-circular-variantes/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: SÍ. La constitución (principio VI) y la spec (FR-026) exigen TDD para la lógica
comprobable:
- celdas, aristas y validación;
- conexiones de río;
- triangulación y mallas;
- disposición inicial, catálogo y escena.

Cada prueba se escribe antes que su implementación y se ve **fallar**. El commit agrupa prueba e
implementación, como en la spec 004, y el paso Red se anota en el *Registro de verificación*. El
arte y el aspecto se validan a mano con quickstart C (FR-027).

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
- Las pruebas de Unity se ejecutan en modo batch con los comandos de [quickstart.md](quickstart.md)
  (A1, A2 y B1). No se pueden ejecutar con el proyecto abierto en otro editor.
- Los recursos de hierba de la spec 004 **no se modifican**:
  - `T_Grass_Top`, `T_Earth_Side`;
  - `M_Grass_Top`, `M_Earth_Side`;
  - `HexTile.asset`, `HexTile.prefab`.

  Tampoco cambian `server-nakama/`, `shared/`, `deploy/`, `ProjectSettings/` ni la CI (FR-025,
  SC-010).
- Un commit solo incluye pruebas en verde. Una prueba escrita en rojo antes de tiempo se confirma
  en el commit que la pone en verde (constitución, *Git*: commits compilables y con pruebas
  pasando cuando sea posible).
- Los assets de Unity (materiales, prefabs, catálogo y escena) se crean o editan con un **script de
  editor temporal** que no se versiona, o a mano en el editor, como en la spec 004. Se anota en el
  registro.

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Comprobar las precondiciones y anotarlas en `specs/005-terreno-circular-variantes/tasks.md` → *Registro de verificación*:
  - rama `feat/005-terreno-circular-variantes` sin cambios pendientes;
  - Unity 6000.3.29f1 instalado;
  - línea base en verde de A1 (EditMode) y A2 (PlayMode), con el número de pruebas;
  - `tools/check-repo.sh` → 0;
  - tamaño actual de los binarios de `client-unity/Assets` (quickstart D).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: los tipos del núcleo, la validación estructural, el dato ampliado y la vista con
catálogo. Al terminar, la escena sigue mostrando el tablero 4 × 4 de hierba de la spec 004, ya a
través del catálogo.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests (Red) ⚠️

- [X] T002 [P] Crear `_Project/Tests/EditMode/Territory/HexEdgeTests.cs`:
  - orden y nombres de data-model.md → `HexEdge`;
  - `Opposite`;
  - `HexCell.Neighbour(e)` igual al vecino `e` de `Neighbours()`, en filas pares e impares;
  - la cara lateral `k` de `HexPrismMeshBuilder` corresponde a la arista `(k+1) mod 6`
    (research.md R-3).
- [X] T003 [P] Crear `_Project/Tests/EditMode/Territory/RiverPieceTests.cs`:
  - `RiverPiece.OpenEdges` y `RiverPiece.Canonical` para cada `RiverShape` y cada giro 0–5
    (data-model.md → `RiverShape`, `RiverPiece.OpenEdges`, `RiverPiece.Canonical`);
  - el recto con giro `r` y `r+3` abre las mismas aristas.
- [X] T004 [P] Ampliar `_Project/Tests/EditMode/Territory/TerritoryLayoutTests.cs` con los estados 1–4 de data-model.md → `TerritoryLayout` (tabla de creación):
  - `Create(IEnumerable<TerritoryCell>)`: `Empty`, `Duplicate` y `UnknownTerrain` (valor 9 en `TerritoryCell.Terrain`);
  - `InvalidRiverPiece` con cada causa de `TerritoryCell.River` y `TerritoryCell.Rotation`;
  - el orden fijo (el primer error gana, research.md R-6) y el texto exacto de cada `Message`;
  - `Entries` y `Cells` conservan el orden del dato;
  - `Create(IEnumerable<HexCell>)` da todo hierba;
  - sustituir `DuplicateCell` por `Cell`;
  - `RequiredVisuals()` con rotación canónica.
- [X] T005 [P] Ampliar `_Project/Tests/EditMode/Territory/TerritoryLayoutAssetTests.cs`:
  - una entrada con el formato de la spec 004 (solo `column` y `row`) se lee como hierba
    (data-model.md → `TerritoryLayoutAsset`);
  - `terrain`, `river` y `rotation` llegan a `TerritoryCell`;
  - `DuplicateCell` → `Cell`.
- [X] T006 [P] Crear `_Project/Tests/EditMode/Territory/TerrainCatalogAssetTests.cs`, sobre catálogos en memoria:
  - con un catálogo incompleto, `TerritoryView` registra `[Territorio] El catálogo no tiene recursos para …` con el texto de data-model.md → `TerrainCatalogAsset` y no crea piezas;
  - con un catálogo completo, resuelve el prefab de cada `TerrainKind` y la malla de cada (forma, giro canónico);
  - las entradas repetidas se resuelven con la primera.

### Implementation (Green)

- [X] T007 Crear en `_Project/Scripts/Territory/`:
  - `HexEdge.cs`, con `HexEdge`, `Opposite` y `HexCell.Neighbour`;
  - `TerrainKind.cs`, con `TerrainKind`, `RiverShape` y `TerritoryCell`;
  - `RiverPiece.cs`, con `OpenEdges` y `Canonical`.

  Valores según data-model.md → `HexEdge`, `TerrainKind`, `RiverShape` y `TerritoryCell`. T002 y
  T003 pasan a verde.
- [X] T008 Ampliar `_Project/Scripts/Territory/TerritoryLayout.cs`:
  - añadir `Entries` y conservar `Cells` (data-model.md → `TerritoryLayout.Entries`, `TerritoryLayout.Cells`), `Create(IEnumerable<TerritoryCell>)` y `RequiredVisuals()`;
  - `TerritoryLayoutResult` con `Cell`, `Edge` y los estados `UnknownTerrain` e `InvalidRiverPiece`, con sus mensajes (data-model.md → `TerritoryLayout`, órdenes 1–4);
  - mantener `Create(IEnumerable<HexCell>)` como todo hierba;
  - renombrar `DuplicateCell` a `Cell`.

  T004 pasa a verde.
- [X] T009 Ampliar `_Project/Scripts/Territory.View/TerritoryLayoutAsset.cs` con `terrain`, `river` y `rotation` en cada `Cell`, con valores por defecto 0 (data-model.md → `TerritoryLayoutAsset.terrain`, `TerritoryLayoutAsset.river`, `TerritoryLayoutAsset.rotation`). `ToLayout()` construye `TerritoryCell`, y `SetCells` admite `TerritoryCell`. T005 pasa a verde.
- [X] T010 Crear `_Project/Scripts/Territory.View/TerrainCatalogAsset.cs`, un ScriptableObject con `terrains`, `riverPrefab` y `riverMeshes` (data-model.md → `TerrainCatalogAsset.terrains`, `.riverPrefab` y `.riverMeshes`). Incluye la resolución por clave y la lista de claves que faltan para un `RequiredVisuals()`.
- [X] T011 Cambiar `_Project/Scripts/Territory.View/TerritoryView.cs` (research.md R-5):
  - `tilePrefab` → `catalog`, con `SetCatalog` interno para las pruebas;
  - antes de instanciar, comprobar `RequiredVisuals()` contra el catálogo y, si falta algo, `LogError` y 0 piezas;
  - instanciar el prefab de cada variante;
  - en el río, instanciar `riverPrefab` y asignar `sharedMesh` (preparado para US3);
  - el ciclo de vida no cambia (`[ExecuteAlways]` y `DontSave`).

  T006 pasa a verde.
- [X] T012 Crear `_Project/Data/Territory/TerrainCatalog.asset` con la entrada `Grass` → `_Project/Art/Territory/Prefabs/HexTile.prefab`. En `_Project/Scenes/Territory.unity`, asignar `catalog` y quitar `tilePrefab` (contracts/territory-scene.md → *Composición*).
- [X] T013 Actualizar `_Project/Tests/PlayMode/TerritorySceneTests.cs` y `_Project/Tests/EditMode/Territory/TerritorySceneAssetTests.cs` a `catalog`, en lugar de `tilePrefab`. Ejecutar A1 y A2: todo en verde con el tablero 4 × 4 aún vigente. Anotarlo en el registro.
- [X] T014 Commit `feat(client): add terrain variants, river pieces and a terrain catalog to the territory core and view` (T002–T013).

**Checkpoint**: el núcleo conoce variantes y piezas de río. La escena se construye a través del
catálogo y no cambia nada visible.

---

## Phase 3: User Story 1 - Territorio circular en la escena existente (Priority: P1) 🎯 MVP

**Goal**: el tablero 4 × 4 se sustituye por el disco de 55 celdas de hierba, con la disposición
validada como conexa.

**Independent Test**: A1 (`InitialTerritoryTests` y validación de no conexa), A2 (55 piezas), A3 y
A4 de quickstart.md, y el criterio C1 de la revisión manual.

### Tests for User Story 1 ⚠️

- [X] T015 [P] [US1] Ampliar `_Project/Tests/EditMode/Territory/TerritoryLayoutTests.cs` con `Disconnected`:
  - una celda suelta, dos grupos y el mensaje de data-model.md → `TerritoryLayout` (orden 5);
  - un hueco interior sigue siendo válido (spec FR-003);
  - `BorderCells()` y `FrontBorderCells()` sobre disposiciones pequeñas.
- [X] T016 [P] [US1] Crear `_Project/Tests/EditMode/Territory/InitialTerritoryTests.cs`, sobre `InitialTerritory.asset`:
  - exactamente las 55 celdas de data-model.md → *Disposición inicial*, sin falta ninguna con `q² + q·r + r² ≤ 13` respecto a (4,4);
  - `AdjacentPairs() == 138`;
  - `BorderCells().Count == 24`;
  - una sola componente conexa.

  Sustituye las comprobaciones de 4 × 4 de `TerritoryLayoutAssetTests` (FR-028, sin perder
  comprobaciones).
- [X] T017 [P] [US1] Actualizar las pruebas que fijan 16 piezas para que esperen el disco de 55 celdas (FR-028), con la lista esperada del disco en `TerritoryTestGeometry`:
  - `_Project/Tests/EditMode/Territory/TerritoryTestGeometry.cs`;
  - `_Project/Tests/EditMode/Territory/TerritoryLayoutTests.cs`, recuento;
  - `_Project/Tests/EditMode/Territory/TerritorySceneAssetTests.cs`: 55 piezas sin *Play* y la escena sigue limpia;
  - `_Project/Tests/PlayMode/TerritorySceneTests.cs`: 55 piezas en sus posiciones, sin giro (contracts/territory-scene.md, invariante 2);
  - `_Project/Tests/Integration/BootstrapSceneIntegrationTests.cs`: 55 piezas.

  Las pruebas que construyen disposiciones de prueba de 15 celdas siguen valiendo. Ejecutar A1 y
  A2: Red.

### Implementation for User Story 1

- [X] T018 [US1] Añadir a `_Project/Scripts/Territory/TerritoryLayout.cs` la validación `Disconnected` (recorrido por vecinos desde la primera celda), `BorderCells()` y `FrontBorderCells()` (data-model.md → `TerritoryLayout`, orden 5 y *Derivados*). T015 pasa a verde.
- [X] T019 [US1] Reescribir `_Project/Data/Territory/InitialTerritory.asset`, conservando el GUID, con las 55 celdas de data-model.md → *Disposición inicial*, todas de hierba, en orden de fila y columna. T016 y T017 pasan a verde. Ejecutar A1 y A2 y anotarlo.
- [X] T020 [US1] Validar A3 (sin *Play*, escena sin `*`), A4 (10 reproducciones con 0 errores y 0 avisos) y una captura temporal (no versionada) en 16:9 y 9:19.5 para comprobar el contorno (C1). Anotarlo en el registro.
- [X] T021 [US1] Commit `feat(client): replace the 4×4 board with a 55-cell circular territory`.

**Checkpoint**: MVP. Territorio circular de hierba visible sin backend y en el flujo normal.

---

## Phase 4: User Story 2 - Hierba, nieve, arena y agua distinguibles (Priority: P1)

**Goal**: las variantes nieve, arena y agua, con arte aprobado en Claude Design, y una composición
que las muestra. La hierba no cambia.

**Independent Test**: A1 (mallas al día, catálogo, texturas y hierba sin cambios), A2 (cada pieza
con los recursos de su variante) y los criterios C2–C5, C8, C10, C11 y C13 de quickstart.md.

### Tests for User Story 2 ⚠️

- [X] T022 [P] [US2] Ampliar `_Project/Tests/EditMode/Territory/HexPrismMeshBuilderTests.cs` con el mapeo superior en red (`u = x/W`, `v = z/(3R)`), según data-model.md → *Mallas generadas* (`WaterTile`). Las pruebas existentes del mapeo por defecto **no cambian**: comprueban que `HexTile` sigue igual.
- [X] T023 [P] [US2] Crear `_Project/Tests/EditMode/Territory/RiverMetricsTests.cs`: valores de partida y restricciones de data-model.md → `RiverMetrics.WaterLevel`, `.ChannelWidth` y `.PoolRadius` (`0 < hw < h`, `0 < w < W/2`, `w/2 < rp < W/2`). Fuera de rango, `ArgumentOutOfRangeException`.
- [X] T024 [P] [US2] Crear `_Project/Tests/EditMode/Territory/TerritoryMeshAssetTests.cs`: `Art/Territory/Meshes/WaterTile.asset` coincide con `HexPrismMeshBuilder.Build(new HexMetrics(R, RiverMetrics.Default.WaterLevel), TopUv.Lattice)`, con la misma técnica que `HexTileMeshAssetTests`, que no cambia.
- [X] T025 [P] [US2] Ampliar `_Project/Tests/EditMode/Territory/TerritoryArtAssetTests.cs`:
  - importación de las 6 texturas nuevas según contracts/visual-direction.md §3 (512, sRGB, mipmaps, trilineal, *wrap* de cada una y ASTC 6×6 en Android e iOS);
  - materiales `Simple Lit` sin especular ni sombras;
  - prefabs `SnowTile`, `SandTile` y `WaterTile` con su malla, sus materiales y sin sombras;
  - las comprobaciones actuales de hierba se mantienen tal cual.
- [X] T026 [P] [US2] Ampliar `_Project/Tests/EditMode/Territory/TerrainCatalogAssetTests.cs`: `TerrainCatalog.asset` tiene una sola entrada por cada `TerrainKind` que no es río (data-model.md → `TerrainCatalogAsset.terrains`), y Grass apunta a `HexTile.prefab`.
- [X] T027 [P] [US2] Ampliar `_Project/Tests/PlayMode/TerritorySceneTests.cs` (invariantes 3 y 6 de contracts/territory-scene.md): cada pieza de hierba, nieve, arena o agua usa la malla y los materiales del prefab de su variante, y ninguna proyecta ni recibe sombras.
- [X] T028 [P] [US2] Ampliar `_Project/Tests/EditMode/Territory/InitialTerritoryTests.cs` con los mínimos de FR-017 para las variantes:
  - cada una con al menos 4 celdas;
  - al menos una celda de cada variante en `FrontBorderCells()`.

  Ejecutar A1 y A2: Red.

### Implementation for User Story 2

- [X] T029 [US2] Añadir el mapeo superior en red como opción a `_Project/Scripts/Territory/HexPrismMeshBuilder.cs` (`TopUv.Inscribed` por defecto y `TopUv.Lattice`, research.md R-4). Crear `_Project/Scripts/Territory/RiverMetrics.cs` con `RiverMetrics.Default` (data-model.md → `RiverMetrics.WaterLevel`, `.ChannelWidth`, `.PoolRadius`, `.ArcSegments` y `.PoolSegments`). T022 y T023 pasan a verde, y `HexTileMeshAssetTests` sigue en verde.
- [X] T030 [US2] Sustituir `_Project/Scripts/Territory.Editor/HexTileMeshMenu.cs` por `TerritoryMeshesMenu.cs` (research.md R-2):
  - menú *Ecosystem → Territorio → Regenerar mallas del territorio* y método `TerritoryMeshesMenu.Regenerate`;
  - regenera `HexTile.asset` y `WaterTile.asset` (en US3, también las de río) con `CopySerialized` sobre los existentes, para conservar el GUID;
  - en `_Project/Tests/EditMode/Territory/HexTileMeshAssetTests.cs`, cambiar solo los textos que remiten a *Regenerar malla de pieza* (comentario y mensajes de error) por *Regenerar mallas del territorio*. Lo que comprueba la prueba no cambia.

  Generar `_Project/Art/Territory/Meshes/WaterTile.asset` en modo batch. Comprobar que
  `HexTile.asset` no cambia (`git diff` vacío). T024 pasa a verde.
- [X] T031 [US2] Commit `feat(client): generate the water tile mesh and the territory meshes command` (T022–T024 y T029–T030). T025–T028 siguen en rojo hasta T036 y **no** se incluyen: entran en T038.
- [X] T032 [US2] **Diseño** (sin commit de código): crear el artefacto de Claude Design con la herramienta de artefactos, intención *design* (FR-019, research.md R-7, contracts/visual-direction.md §1).
  - Contenido: paletas de nieve, arena y agua, vista aislada de cada variante, la composición de las variantes, referencias en 16:9 y 9:19.5 y notas de estilo.
  - Texturas: pintar las 6 de research.md R-7 con un script determinista **fuera del repositorio** (`$CLAUDE_JOB_DIR/tmp` o equivalente). `T_Water_Top` tiene que ser periódica en `(½, ½)`.
  - Referencias: renders reales en *Play* con una importación temporal, revertida sin confirmar.
  - Comprobar el contraste exigido (nieve frente al fondo ≥ 25 L*).
  - Anotar el enlace y las desviaciones en el registro.
- [X] T033 [US2] **Espera bloqueante**: el responsable aprueba el artefacto o pide cambios. Si cambia `RiverMetrics.WaterLevel`, actualizar `RiverMetrics.Default`, regenerar `WaterTile.asset` y repetir A1. Anotar la fecha de aprobación.
- [X] T034 [US2] Medir el umbral de ADR-0011 con los PNG aprobados: ninguno de más de 5 MB y el total por debajo de 50 MB (quickstart D). Importar:
  - texturas: `_Project/Art/Territory/Textures/T_Snow_Top.png`, `T_Snow_Side.png`, `T_Sand_Top.png`, `T_Sand_Side.png`, `T_Water_Top.png` y `T_Water_Side.png`, con la configuración de contracts/visual-direction.md §3;
  - materiales: `_Project/Art/Territory/Materials/M_Snow_Top.mat`, `M_Snow_Side.mat`, `M_Sand_Top.mat`, `M_Sand_Side.mat`, `M_Water_Top.mat` y `M_Water_Side.mat`;
  - prefabs: `_Project/Art/Territory/Prefabs/SnowTile.prefab`, `SandTile.prefab` y `WaterTile.prefab`.

  T025 pasa a verde.
- [X] T035 [US2] Completar `specs/005-terreno-circular-variantes/contracts/visual-direction.md` §2 con los valores aprobados de las variantes y de `hw`, el enlace al artefacto y la fecha. Marcar el estado como aprobado para las variantes.
- [X] T036 [US2] Añadir a `_Project/Data/Territory/TerrainCatalog.asset` las entradas Snow, Sand y Water. En `_Project/Data/Territory/InitialTerritory.asset`, aplicar la composición aprobada de las variantes (data-model.md → *Composición de partida*, o la ajustada en T033). Las celdas de río siguen siendo hierba hasta US3. T026, T027 y T028 pasan a verde. Ejecutar A1 y A2 y anotarlo.
- [X] T037 [US2] Captura temporal en *Play* (16:9 y 9:19.5) frente a las referencias aprobadas: variantes reconocibles, laterales frontales, agua más baja con orillas sin huecos y hierba idéntica a la de la spec 004. Anotarlo en el registro.
- [X] T038 [US2] Commit `feat(client): add the approved snow, sand and water terrain art` (T025–T028 y T034–T037).

**Checkpoint**: cuatro variantes con arte aprobado. La hierba no cambia.

---

## Phase 5: User Story 3 - Un río continuo formado por piezas conectadas (Priority: P2)

**Goal**: las tres piezas de río con cauce hundido, sus reglas de conexión y el río de la
composición.

**Independent Test**: A1 (conexiones, triangulación, mallas de río y composición), A2 (piezas de
río con su malla), A5 y los criterios C6 y C7 de quickstart.md.

### Tests for User Story 3 ⚠️

- [X] T039 [P] [US3] Crear `_Project/Tests/EditMode/Territory/RiverConnectionTests.cs` con la regla de data-model.md → `TerritoryLayout` (orden 6 y *Regla de conexión*):
  - **aceptan**: conexión recíproca, desembocadura en agua, salida por el borde, río cerrado sin manantial y dos ríos vecinos sin conectar;
  - **rechazan** con `BrokenRiver`, celda, arista y el mensaje exacto: vecina de hierba, nieve o arena, y vecina de río con la arista cerrada (vista desde las dos piezas);
  - `Disconnected` va antes que `BrokenRiver`.
- [X] T040 [P] [US3] Crear `_Project/Tests/EditMode/Territory/PolygonTriangulatorTests.cs`:
  - convexo, cóncavo y en forma de «U»;
  - la suma de las áreas de los triángulos es igual al área del polígono;
  - todos los triángulos tienen la misma orientación;
  - el número de triángulos es `n − 2`;
  - un polígono degenerado devuelve un resultado definido, no una excepción.
- [X] T041 [P] [US3] Crear `_Project/Tests/EditMode/Territory/RiverPieceMeshBuilderTests.cs` con los invariantes 1–6 de data-model.md → *Mallas generadas*, para las 15 combinaciones (forma, giro canónico):
  - cortes de anchura `w` centrados en el punto medio de cada arista abierta;
  - muescas hasta `hw` y laterales completos sin huecos;
  - agua del cauce a `y = hw`;
  - suma de áreas igual a la del hexágono;
  - la malla con giro `r` es la de giro 0 girada (salvo las coordenadas de textura);
  - caras orientadas hacia fuera;
  - 3 submallas.

  También, con una tolerancia de ≤ 0,1 % de `W` (FR-013, FR-014 y SC-005):
  - para cada pareja de piezas con conexión recíproca, los cortes coinciden;
  - el corte de la desembocadura casa con la arista de `WaterTile`.
- [X] T042 [P] [US3] Ampliar `_Project/Tests/EditMode/Territory/TerritoryMeshAssetTests.cs` con las 15 mallas `Art/Territory/Meshes/River/River_{Shape}_{r}.asset`, frente a `RiverPieceMeshBuilder`.
- [X] T043 [P] [US3] Ampliar `_Project/Tests/EditMode/Territory/TerrainCatalogAssetTests.cs` y `TerritoryArtAssetTests.cs`:
  - `riverPrefab` es `RiverTile.prefab`, con los materiales `M_Grass_Top`, `M_Earth_Side` y `M_Water_Top` y sin sombras;
  - `riverMeshes` tiene exactamente las 15 claves de data-model.md → `TerrainCatalogAsset.riverMeshes`, sin repetidas.
- [X] T044 [P] [US3] Ampliar `_Project/Tests/EditMode/Territory/InitialTerritoryTests.cs` con el río de FR-017:
  - al menos 5 piezas;
  - un manantial, al menos un recto y al menos una curva;
  - el curso desemboca en agua.

  Ampliar `_Project/Tests/PlayMode/TerritorySceneTests.cs` (invariante 3): cada pieza de río usa
  `River_{Shape}_{rotación canónica}` y los materiales de `RiverTile.prefab`. Ejecutar A1 y A2:
  Red.

### Implementation for User Story 3

- [X] T045 [US3] Añadir a `_Project/Scripts/Territory/TerritoryLayout.cs` la validación `BrokenRiver` con `RiverPiece.OpenEdges` y `HexCell.Neighbour` (data-model.md → `TerritoryLayout`, orden 6). T039 pasa a verde.
- [X] T046 [US3] Crear `_Project/Scripts/Territory/PolygonTriangulator.cs` (*ear clipping*, research.md R-2). T040 pasa a verde.
- [X] T047 [US3] Crear `_Project/Scripts/Territory/RiverPieceMeshBuilder.cs` (research.md R-2):
  - trazado recto, curva (arco de 60° con centro a `√3·R` y radio `1,5·R`, `ArcSegments`) y manantial con poza (`PoolSegments`);
  - orillas trianguladas;
  - paredes y laterales con muesca, con `v = y/h`;
  - agua en red;
  - mallas giradas por construcción.

  T041 pasa a verde.
- [X] T048 [US3] Ampliar `_Project/Scripts/Territory.Editor/TerritoryMeshesMenu.cs` para generar las 15 mallas en `_Project/Art/Territory/Meshes/River/`. Generarlas en modo batch y comprobar que regenerar dos veces da archivos idénticos. T042 pasa a verde.
- [X] T049 [US3] Commit `feat(client): add river connection rules and generated river piece meshes` (T039–T042 y T045–T048). T043–T044 siguen en rojo hasta T051 y **no** se incluyen: entran en T054.
- [X] T050 [US3] **Diseño**: ampliar el artefacto de T032 con las tres piezas de río (`w`, `rp` y `hw`), el detalle de una unión y de la desembocadura, la composición completa con el río y referencias nuevas en 16:9 y 9:19.5 (contracts/visual-direction.md §1). **Espera bloqueante**: aprobación del responsable. Si cambian `RiverMetrics.ChannelWidth` o `.PoolRadius`, actualizar `RiverMetrics.Default`, regenerar las mallas y repetir A1.
- [X] T051 [US3] Crear `_Project/Art/Territory/Prefabs/RiverTile.prefab` (`MeshFilter` sin malla y `MeshRenderer[M_Grass_Top, M_Earth_Side, M_Water_Top]`, sin sombras). En `_Project/Data/Territory/TerrainCatalog.asset`, añadir `riverPrefab` y las 15 `riverMeshes`. En `_Project/Data/Territory/InitialTerritory.asset`, aplicar el río aprobado (data-model.md → *Composición de partida*: `TerritoryCell.Terrain = River`, `.River` y `.Rotation`). T043 y T044 pasan a verde. Ejecutar A1 y A2 y anotarlo.
- [X] T052 [US3] Completar `specs/005-terreno-circular-variantes/contracts/visual-direction.md` §2 con `w`, `rp`, la composición aprobada y la fecha. Marcar el estado como aprobado.
- [X] T053 [US3] Validar A5 (río que acaba en nieve: error con celda y arista y 0 piezas) y hacer una captura temporal en *Play* para C6 y C7 (zoom 2× en las uniones y la desembocadura). Anotarlo en el registro.
- [X] T054 [US3] Commit `feat(client): add the river tile and the approved river to the territory` (T043–T044 y T050–T053).

**Checkpoint**: territorio completo: disco, cuatro variantes y río continuo.

---

## Phase 6: User Story 4 - El territorio ampliado se ve entero y en el flujo normal (Priority: P2)

**Goal**: confirmar que el encuadre, el área segura y el paso desde `Bootstrap` siguen cumpliendo
con 55 piezas. No se espera código nuevo (research.md R-8).

**Independent Test**: A2 (encuadre en 6 proporciones), B1–B3 y C9 y C12 de quickstart.md.

- [X] T055 [P] [US4] Ejecutar A2 y comprobar, en `_Project/Tests/PlayMode/TerritorySceneTests.cs`, que la prueba de encuadre por proporción (invariante 4 de contracts/territory-scene.md) recorre las 6 proporciones con 55 piezas, y `TerritoryFramingTests` con área segura. Si la ocupación cae por debajo del 70 %, parar y plantearlo al responsable (plan.md → *Riesgos*). Anotarlo en el registro.
- [X] T056 [P] [US4] Con el entorno local arrancado, ejecutar B1 (`_Project/Tests/Integration/BootstrapSceneIntegrationTests.cs`, con 55 piezas en ≤ 2 s), B2 y B3. Anotarlo en el registro.
- [ ] T057 [US4] En el editor, vista *Simulator* con un iPhone con muesca, en vertical y en horizontal: comprobar C9 (55 piezas fuera de la muesca, legibles en 9:19.5) y C12. Anotarlo en el registro. Los resultados definitivos los da la revisión del responsable (T064).

**Checkpoint**: el territorio ampliado funciona en cualquier proporción y en el flujo de invitado.

---

## Phase 7: User Story 5 - Disposición, variantes y río definidos por datos y validados (Priority: P3)

**Goal**: cada tipo de dato no válido produce un error claro y una escena sin piezas.

**Independent Test**: A2 (tabla de datos no válidos de contracts/territory-scene.md) y A5–A6 de
quickstart.md.

- [X] T058 [P] [US5] Ampliar `_Project/Tests/PlayMode/TerritorySceneTests.cs` con cada fila de contracts/territory-scene.md → *Datos no válidos*, con `SetLayout` y `SetCatalog`:
  - celda repetida, disposición no conexa, variante desconocida, río sin forma, río que acaba en nieve y catálogo sin la variante usada;
  - cada caso: `LogAssert.Expect` con el prefijo del mensaje y 0 piezas.

  Añadir una disposición válida distinta de la inicial (por ejemplo, 7 celdas con un río de
  borde a borde) que coloque exactamente sus piezas con sus recursos (spec US5, escenario 1).
  Ejecutar A2. Si algún caso ya pasa por el trabajo de las fases 2–5, anotar que el Red no
  aplica y por qué.
- [X] T059 [US5] Corregir en `_Project/Scripts/Territory.View/TerritoryView.cs` lo que T058 descubra, por ejemplo errores registrados más de una vez o piezas a medias. A2 en verde.
- [X] T060 [US5] Validar A6 (catálogo sin nieve) en el editor. Anotar A5 y A6 en el registro (SC-006).
- [X] T061 [US5] Commit `test(client): cover invalid territory data in the territory scene` (o `fix(client): …` si T059 cambió código).

**Checkpoint**: datos incoherentes → error claro y 0 piezas.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [X] T062 [P] Actualizar `client-unity/README.md` (FR-029):
  - estado;
  - estructura: `Data/Territory/TerrainCatalog.asset` y `Art/Territory/Meshes/River/`;
  - sección *Territorio*: disco de 55 celdas, variantes, río, catálogo, cómo cambiar la composición y el comando *Regenerar mallas del territorio* con su línea en modo batch (`TerritoryMeshesMenu.Regenerate`, que sustituye a `HexTileMeshMenu.Regenerate`);
  - pruebas nuevas en la tabla de grupos.
- [X] T063 [P] Actualizar la línea de estado de `README.md` (raíz).
- [ ] T064 Revisión visual manual completa (quickstart C, C1–C13) por el responsable en las 6 proporciones y el simulador, incluida la prueba SC-003 de 8 celdas y el tiempo (≤ 15 min, SC-009). Registrar los resultados en `specs/005-terreno-circular-variantes/spec.md` → nueva sección *Revisión visual*, con fecha y persona. Un ❌ vuelve a la fase que corresponda.
- [X] T065 Higiene (quickstart D, SC-004 y SC-010):
  - `git diff` vacío en `server-nakama/`, `shared/`, `deploy/`, `.github/`, `ProjectSettings/` y los recursos de hierba;
  - ningún GUID de `.meta` cambiado;
  - todos los recursos con `.meta`;
  - umbral de ADR-0011;
  - `tools/check-repo.sh` → 0.

  Anotarlo en el registro.
- [X] T066 Ejecución final de A1, A2 y B1, y builds de desarrollo de Android e iOS que compilan, según `client-unity/README.md` → *Builds de desarrollo*. Anotar los resultados y las salidas en el registro. Si una build no se puede ejecutar, anotar qué falta y por qué.
- [ ] T067 Commit `docs: describe the circular territory, terrain variants and rivers in the READMEs` (T062–T063) y commit `docs(spec): log the visual review, tests, builds and repository checks for spec 005` (T064–T066).
- [X] T068 Con la autorización explícita del responsable: push y PR hacia `main`. La CI tiene que estar en verde. Anotar el PR y el resultado de la CI en el registro y confirmarlo con `docs(spec): log the PR and CI result for spec 005`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (1)**: sin dependencias.
- **Foundational (2)**: depende de T001 y bloquea todas las historias.
- **US1 (3)**: depende de la fase 2. Es el MVP.
- **US2 (4)**: depende de US1, porque la composición se escribe sobre el disco. T033 es una espera humana bloqueante. T032 puede empezar en cuanto termina T030.
- **US3 (5)**: depende de US2: necesita `WaterTile`, `M_Water_Top`, `RiverMetrics` y celdas de agua para la desembocadura. T050 es una espera humana bloqueante. T039–T048 (núcleo y mallas) pueden adelantarse tras T029, en paralelo con el diseño de US2.
- **US4 (6)**: depende de US3 para validar con el territorio final. Las comprobaciones de encuadre pueden adelantarse tras US1.
- **US5 (7)**: depende de US3, porque prueba los errores de río y de catálogo en escena.
- **Polish (8)**: depende de todas. T064 necesita al responsable y T068, su autorización.

### Within Each User Story

- Pruebas (Red) → núcleo (Green) → recursos y datos → escena → validación manual.
- Las pruebas EditMode del núcleo no cargan escenas. Las PlayMode no necesitan Nakama. Solo T056
  necesita el entorno local.

### Parallel Opportunities

- Fase 2: T002 ∥ T003 ∥ T004 ∥ T005 ∥ T006.
- US1: T015 ∥ T016 ∥ T017.
- US2: T022 ∥ T023 ∥ T024 ∥ T025 ∥ T026 ∥ T027 ∥ T028. El diseño (T032) en paralelo con las pruebas de US3.
- US3: T039 ∥ T040 ∥ T041 ∥ T042 ∥ T043 ∥ T044. T045, T046 y T047 tocan archivos distintos (T047 depende de T046).
- US4: T055 ∥ T056.
- Polish: T062 ∥ T063.
- Cuidado: varias historias tocan `TerritorySceneTests.cs`, `TerritoryLayout.cs` y `InitialTerritory.asset`. Mejor en secuencia.

---

## Parallel Example: Foundational

```bash
Task: "T002 [P] HexEdgeTests.cs"
Task: "T003 [P] RiverPieceTests.cs"
Task: "T004 [P] TerritoryLayoutTests.cs (estados 1–4)"
Task: "T005 [P] TerritoryLayoutAssetTests.cs"
Task: "T006 [P] TerrainCatalogAssetTests.cs"
```

## Parallel Example: User Story 3

```bash
Task: "T039 [P] [US3] RiverConnectionTests.cs"
Task: "T040 [P] [US3] PolygonTriangulatorTests.cs"
Task: "T041 [P] [US3] RiverPieceMeshBuilderTests.cs"
Task: "T042 [P] [US3] TerritoryMeshAssetTests.cs (mallas de río)"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Fases 1 y 2: núcleo y catálogo, sin cambio visible.
2. US1: disco de 55 celdas de hierba.
3. **STOP and VALIDATE**: A1, A2, A3, A4 y C1.

### Incremental Delivery

1. US1 → territorio circular.
2. US2 → nieve, arena y agua con arte aprobado.
3. US3 → río continuo con arte aprobado.
4. US4 → encuadre y flujo confirmados con el territorio final.
5. US5 → datos no válidos cubiertos en escena.
6. Polish → documentación, revisión completa, builds y PR.

---

## Notes

- [P] = archivos distintos y sin dependencias pendientes.
- Ninguna verificación se marca hecha sin haberse ejecutado (constitución, principio VI). Si algo
  no se puede ejecutar, se anota qué falta y por qué.
- Los recursos nuevos llevan su `.meta`. No se confirman XML de pruebas, builds, `Library/`,
  `Temp/`, scripts de editor temporales, el script de texturas ni archivos del diseño.

---

## Registro de verificación

| Fecha | Tarea | Comando o comprobación | Resultado |
|-------|-------|------------------------|-----------|
| 2026-11-25 | T001 | Rama y `git status` | `feat/005-terreno-circular-variantes`, sin cambios pendientes. El editor estaba abierto con el proyecto; el responsable lo cerró antes de empezar |
| 2026-11-25 | T001 | Unity instalado | 6000.3.29f1 en `/Applications/Unity/Hub/Editor/` |
| 2026-11-25 | T001 | A1 y A2 (línea base) | EditMode: salida 0 · 171/171. PlayMode: salida 0 · 20/20 |
| 2026-11-25 | T001 | `tools/check-repo.sh` y binarios de `client-unity/Assets` | Salida 0. Total 1,2 MB (ADR-0011) |
| 2026-11-25 | T002–T006 | A1 con las pruebas nuevas, sin implementación (Red) | Salida 1: el ensamblado de pruebas no compila porque faltan `HexEdge`, `RiverShape`, `TerritoryCell`, `RiverPiece`, `VisualKey` y `TerrainCatalogAsset` |
| 2026-11-25 | T007–T011 | A1 (Green, salvo la escena) | Salida 2 · 235/236: solo falla `TerritoryScene_OpenedInTheEditor_…` (la escena aún no tiene catálogo). Ajuste: `Create(null)` pasa a ser ambiguo con las dos sobrecargas, así que la prueba lo tipa como `(HexCell[])null`. Las piezas se nombran ahora `‹prefab› (columna, fila)` en lugar de `Tile (columna, fila)`; nada dependía del nombre |
| 2026-11-25 | T012 | `TerrainCatalog.asset` y su `.meta` escritos a mano en YAML (GUID nuevo) con Grass → `HexTile.prefab`. En `Territory.unity`, `tilePrefab` → `catalog` | La escena solo cambia en esa línea |
| 2026-11-25 | T013 | A1 y A2 (Green) | EditMode: salida 0 · 236/236 (171 previas + 65 nuevas). PlayMode: salida 0 · 20/20. Unity no reescribe la escena ni los datos |
| 2026-11-25 | T015–T017 | A1 con las pruebas de US1 y sin implementación (Red) | Salida 1: no compila porque faltan `TerritoryLayoutStatus.Disconnected`, `BorderCells` y `FrontBorderCells`. El Red de PlayMode no se vio por separado: con el ensamblado EditMode sin compilar, Unity aborta también la ejecución PlayMode |
| 2026-11-25 | T018–T019 | A1 y A2 (Green) con `InitialTerritory.asset` reescrito: 55 celdas de hierba en orden de fila y columna, mismo GUID | EditMode: salida 0 · 245/245. PlayMode: salida 0 · 20/20. Ajuste: `Does.Not.Contain` solo admite texto en NUnit; se usa `Has.No.Member`. La prueba de integración ya espera 55 piezas; se ejecuta en T056 |
| 2026-11-25 | T020 | A3, automatizado en `TerritorySceneAssetTests.TerritoryScene_OpenedInTheEditor_ShowsFiftyFivePiecesAndStaysClean` | Sin *Play*: 55 piezas `DontSave` y `scene.isDirty == false` |
| 2026-11-25 | T020 | A4: 10 procesos de `TerritorySceneTests` en modo batch (incluye `LogAssert.NoUnexpectedReceived`) | 10/10 con 17/17 y salida 0. Las ejecuciones 6–10 se repitieron: la primera vez fallaron porque se estaban escribiendo las pruebas de US2 en el mismo proyecto (errores de compilación ajenos a la escena) |
| 2026-11-25 | T020 | Captura en *Play* con una prueba temporal no versionada (16:9 y 9:19.5) | Disco redondo en escorzo, sin huecos ni piezas sueltas, con los laterales del borde frontal visibles. En 9:19.5 ocupa el 88 % del ancho. Observación para C1: las filas 3 y 5 (8 piezas) sobresalen media pieza respecto a la 4 a izquierda y derecha, lo que deja una pequeña muesca en el contorno; es propio del disco K = 13 acordado |
| 2026-11-25 | T022–T028 | A1 con las pruebas de US2 y sin implementación (Red) | Salida 1: no compila porque faltan `TopUv`, `RiverMetrics`, `TerrainCatalogAsset.TerrainEntryCount` y `TerritoryView.Catalog` |
| 2026-11-25 | T029–T030 | `TerritoryMeshesMenu` sustituye a `HexTileMeshMenu` (`git mv`, mismo `.meta`). `-executeMethod Ecosystem.Territory.Editor.TerritoryMeshesMenu.Regenerate` | Salida 0. `WaterTile.asset` nuevo (6,7 KB). `HexTile.asset` regenerado **sin cambios** (`git status` limpio) |
| 2026-11-25 | T029–T030 | A1 | Salida 2 · 263/284: pasan T022–T024 (mapeo en red, `RiverMetrics` y `WaterTile` al día). Fallan solo las 21 comprobaciones de arte, catálogo y composición de T025, T026 y T028 (faltan las texturas, materiales, prefabs y composición que llegan con T034–T036) |
| 2026-11-25 | T031 | Commit con T022–T024 y T029–T030 | T025–T028 quedan fuera del commit, en rojo, y entran en T038 |
| 2026-11-25 | T032 | Artefacto de Claude Design creado: https://claude.ai/artifact/98VB8yMnzYHcMT252Mm8wp (privado) | Cuatro paneles: variantes y valores (paletas, medidas, lo que no cambia y lo que se evita), composición (mapa de las 55 celdas y río previsto), las 6 texturas y referencias en Unity (16:9, 9:19.5 y dos detalles, renders reales en *Play*). Igual que en la spec 004: (1) las texturas se pintan con un script determinista (Python sin dependencias, fuera del repositorio) que reutiliza el ruido del de la 004; son **los mismos bytes** que se importan; (2) las referencias se renderizan con una importación temporal sin confirmar; (3) no se usa el sistema de diseño «Vocablo», que es de otro producto. `T_Water_Top` es periódica en (½, ½) (diferencia máxima 0). Contraste nieve frente al fondo: 93,4 − 63,4 = 30 L* (≥ 25). Con la importación temporal: A1 284/284 y A2 21/21 |
| 2026-11-25 | T033 | Aprobación del artefacto por el responsable | **Aprobado tal cual** el 2026-11-25: paletas de nieve, arena y agua, `hw` = 0,25, composición de las variantes y las 6 texturas. `RiverMetrics.Default` no cambia |
| 2026-11-25 | T034 | Umbral de ADR-0011 y bytes importados | Ningún binario de más de 5 MB. Total de `client-unity/Assets` en Git, con los nuevos: 2,2 MB (texturas del territorio: 1,1 MB). Las 6 texturas del proyecto son idénticas (`cmp`) a las del artefacto. Materiales y prefabs copiados de los de la hierba con GUID nuevos (script temporal, no versionado); los de la hierba no cambian |
| 2026-11-25 | T035 | `contracts/visual-direction.md` §2 | Valores aprobados de las variantes y de `hw`, enlace y fecha. El río sigue pendiente (T050) |
| 2026-11-25 | T036 | Catálogo (Snow, Sand y Water) y composición de las variantes en `InitialTerritory.asset`; celdas del río aún de hierba | A1: salida 0 · 284/284. A2: salida 0 · 21/21. T025–T028 pasan a verde |
| 2026-11-25 | T037 | Capturas en *Play* (16:9, 9:19.5, detalle del lago y de la nieve) frente a las referencias aprobadas | Son las mismas capturas del artefacto: variantes reconocibles, laterales frontales distintos, agua sin costura entre piezas y más baja que la tierra con orillas sin huecos, y hierba igual que en la spec 004 |
| 2026-11-25 | T039–T044 | A1 con las pruebas de US3 y sin implementación (Red) | Salida 1: no compila porque faltan `RiverPieceMeshData`, `RiverPieceMeshBuilder`, `PolygonTriangulator` y `TerritoryLayoutStatus.BrokenRiver`. Ajuste previo: una prueba del triangulador que construía un polígono que se cruzaba consigo mismo se sustituye por una de ojo de cerradura, la forma de la orilla del manantial |
| 2026-11-25 | T045–T047 | A1 (Green del núcleo) | Salida 2 · 407/427. Pasan conexiones, triangulador y los invariantes 1–6 y las uniones de las 15 mallas (≤ 0,1 % de W). Dos pruebas de la fase 2 tenían datos de río que ahora son, con razón, un río roto: se corrigen los datos (un recto NO–SE que desemboca en agua; un recto con giro 3, canónico 0, unido a una curva) sin cambiar lo que comprueban |
| 2026-11-25 | T048 | `TerritoryMeshesMenu.Regenerate` en modo batch, dos veces | 15 mallas en `Meshes/River/` (276 KB en texto YAML), idénticas en las dos ejecuciones (`shasum`). `HexTile.asset` y `WaterTile.asset` sin cambios. A1: salida 2 · 424/427; solo fallan las 3 comprobaciones de T043–T044 (faltan `RiverTile.prefab`, las mallas en el catálogo y el río en la composición, que llegan con T051) |
| 2026-11-25 | T049 | Commit con T039–T042 y T045–T048 | T043–T044 quedan fuera del commit, en rojo, y entran en T054 |
| 2026-11-25 | T050 | Artefacto ampliado con el panel «Río: piezas y uniones» (las tres piezas dibujadas desde la misma geometría, medidas y lo que no hay) y referencias nuevas: territorio con el río en 16:9 y 9:19.5, uniones, manantial y desembocadura (renders reales con una importación temporal) | **Aprobado tal cual** por el responsable el 2026-11-25: `w` = 0,26, `rp` = 0,17 y el río de 6 piezas. `RiverMetrics.Default` no cambia |
| 2026-11-25 | T051 | `RiverTile.prefab` (copia de `HexTile.prefab` sin malla y con `M_Grass_Top`, `M_Earth_Side` y `M_Water_Top`), `riverPrefab` y las 15 `riverMeshes` en el catálogo, y el río en `InitialTerritory.asset` (script temporal, no versionado) | A1: salida 0 · 427/427. A2: salida 0 · 22/22. T043–T044 pasan a verde |
| 2026-11-25 | T052 | `contracts/visual-direction.md` | Estado aprobado; `w`, `rp` y la composición del río registrados |
| 2026-11-25 | T053 | A5 con un cambio temporal (la celda (4,6) pasa a nieve) y `TerritorySceneTests.Scene_LoadsWithoutErrorsOrWarnings` | Consola: `[Territorio] El río de la celda (columna 4, fila 5) no continúa por la arista SO: la vecina es de nieve.`, el texto exacto de quickstart A5. Cambio deshecho |
| 2026-11-25 | T053 | C6 y C7 con capturas en *Play* (río entero, unión entre curvas de cerca, manantial y desembocadura) | Cauce continuo de una pieza a otra, sin escalones, huecos ni cambios de anchura; la desembocadura se une al lago al mismo nivel; la poza se lee como un nacimiento. Sin parpadeo ni líneas de fondo en las uniones |
| 2026-11-25 | T055 | A2 (encuadre por proporción con 55 piezas) y A1 (`TerritoryFramingTests`, ya sobre el disco) | Las 6 proporciones con todas las esquinas en [0,06; 0,94] y las áreas seguras ficticias de la spec 004, en verde. Con el margen del 6 %, el territorio ocupa el 88 % de la dimensión que limita (≥ 70 %). `TerritoryFraming` y `TerritoryCamera` sin cambios |
| 2026-11-25 | T056 | B1 con el entorno local (`tools/check-backend.sh --module` → 0) | Salida 0 · 14/14, incluido `Connected_LoadsTerritoryOnce` con 55 piezas en ≤ 2 s. B2 y B3 (reproducir `Bootstrap` en el editor y parar el entorno a mano) no se ejecutaron: necesitan el editor con interfaz; pasan a la revisión del responsable (T064) |
| 2026-11-25 | T057 | Vista *Simulator* con muesca (C9, C12) | **No ejecutado**: necesita el editor con interfaz. La parte automática (área segura ficticia en EditMode y 6 proporciones en PlayMode) está en verde. Queda para la revisión del responsable (T064) |
| 2026-11-25 | T058 | A2 con las pruebas de datos no válidos de contracts/territory-scene.md y una disposición válida de 7 celdas con un río de borde a borde | Salida 2 · 23/28: los errores se registran bien, pero en *Play* las 55 piezas anteriores siguen en la escena durante el fotograma, porque `Destroy` espera al final del fotograma. Ajuste en la prueba: tipar la disposición vacía (`new HexCell[0]`), porque `AssetWith()` pasa a ser ambiguo |
| 2026-11-25 | T059 | `TerritoryView.Clear`: en *Play* desactiva cada pieza antes de `Destroy`. A2 y A1 | PlayMode: salida 0 · 28/28. EditMode: salida 0 · 427/427. Ninguna pieza visible junto a un error ni junto a las nuevas |
| 2026-11-25 | T060 | A6: catálogo sin nieve | Comprobado con `View_WithACatalogMissingAUsedVariant_LogsItAndShowsNoPieces` en la escena en *Play*: `[Territorio] El catálogo no tiene recursos para variante nieve.` y 0 piezas visibles. No se repitió a mano en el editor |
| 2026-11-25 | T062–T063 | `client-unity/README.md` y README raíz | Estado, estructura, sección *Territorio* (disco, variantes, río, catálogo, cómo cambiar la composición y `TerritoryMeshesMenu.Regenerate`) y pruebas. Commit `680caaa` |
| 2026-11-25 | T065 | Higiene (quickstart D) | Sin cambios en `server-nakama/`, `shared/`, `deploy/`, `.github/` ni `ProjectSettings/`. Recursos de hierba idénticos a `main`. Ningún GUID cambiado: el único `.meta` modificado es el renombrado de `HexTileMeshMenu.cs.meta` a `TerritoryMeshesMenu.cs.meta` (R100, mismo GUID). Todo con `.meta`, carpetas incluidas. Sin temporales en Git. Binarios: 2,6 MB en total, ninguno de más de 5 MB (ADR-0011). `tools/check-repo.sh` → 0 |
| 2026-11-25 | T066 | Ejecución final: A1, A2 y B1 sobre el código final | EditMode: salida 0 · 427/427. PlayMode: salida 0 · 28/28. Integración: salida 0 · 14/14 |
| 2026-11-25 | T066 | Builds de desarrollo en modo batch con `BuildPipeline.BuildPlayer` y las escenas de la build (script de editor temporal, no versionado) | Android: correcta, 0 errores, `Builds/Android/Ecosystem-dev.apk`. iOS: correcta, 0 errores, proyecto Xcode en `Builds/iOS/`. Las dos con `Bootstrap` y `Territory`. Ningún archivo versionado cambió. iOS queda como plataforma activa del editor (`Library/`, no versionado) |
| 2026-11-25 | T068 | Push y PR con la autorización del responsable | PR #5 (https://github.com/enriqueazo/ecosystem/pull/5) hacia `main`. CI en verde: `repo`, `server` y `backend`. Las pruebas de Unity no corren en la CI (ADR-0007). La revisión visual (T057 y T064) sigue pendiente de registrar |
