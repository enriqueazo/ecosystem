---

description: "Task list for feature 004-territorio-hexagonal"
---

# Tasks: Territorio hexagonal isométrico

**Input**: Design documents from `specs/004-territorio-hexagonal/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: SÍ. La constitución (principio VI) y la spec (FR-023) exigen TDD para la lógica
comprobable:
- disposición, validación, geometría y vecinos;
- malla y encuadre;
- composición de la escena y paso desde `Bootstrap`.

Cada prueba se escribe antes que su implementación y se ve **fallar**. El commit agrupa prueba e
implementación, como en las specs 002 y 003, y el paso Red se anota en el *Registro de
verificación*. El arte y el aspecto se validan a mano con quickstart C (FR-024).

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
- Pruebas de Unity en modo batch con los comandos de [quickstart.md](quickstart.md) (A1, A2 y
  B1). Con el proyecto abierto en otro editor no se pueden ejecutar.
- Las API de Unity se verifican compilando (principio V): `HideFlags`, `Mesh.SetSubMesh`,
  `Screen.safeArea`, `SceneManager` y `TextureImporterPlatformSettings`.
- **Ningún** archivo de `server-nakama/`, `shared/` o `deploy/` ni ajuste de orientación o
  resolución cambia en esta spec (FR-019 y FR-022).

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Comprobar precondiciones y anotarlas en `specs/004-territorio-hexagonal/tasks.md` → *Registro de verificación*:
  - rama `feat/004-territorio-hexagonal` sin cambios pendientes;
  - Unity 6000.3.29f1 instalado;
  - pruebas EditMode y PlayMode actuales en verde (línea base, comandos A1 y A2);
  - `tools/check-repo.sh` → 0.
- [X] T002 Resolver D-08 antes de cualquier binario (FR-015, research.md R-1):
  - crear `docs/decisions/0011-sin-git-lfs.md` con la plantilla `0000-plantilla.md`: contexto, opciones (LFS ahora, sin LFS), decisión y consecuencias, incluido el **umbral de revisión** (5 MB por archivo, primer audio o vídeo, 50 MB en total);
  - añadirlo al índice `docs/decisions/README.md`;
  - en `docs/decisions/pendientes.md`, sustituir la entrada D-08 por una fila en *Resueltas*.
- [X] T003 Crear los ensamblados y carpetas (plan.md → *Project Structure*, research.md R-8):
  - `_Project/Scripts/Territory/Ecosystem.Territory.asmdef`: `noEngineReferences: true`, sin referencias, `autoReferenced: false`;
  - `_Project/Scripts/Territory.View/Ecosystem.Territory.View.asmdef`: referencia a `Ecosystem.Territory`;
  - `_Project/Scripts/Territory.Editor/Ecosystem.Territory.Editor.asmdef`: `includePlatforms: ["Editor"]`, referencias a `Ecosystem.Territory` y `Ecosystem.Territory.View`;
  - añadir `Ecosystem.Territory` y `Ecosystem.Territory.View` a `_Project/Tests/EditMode/Ecosystem.Tests.EditMode.asmdef` y a `_Project/Tests/PlayMode/Ecosystem.Tests.PlayMode.asmdef`;
  - crear las carpetas `_Project/Art/Territory/{Textures,Materials,Meshes,Prefabs}` y `_Project/Data/Territory`.

  Abrir el proyecto en modo batch: 0 errores de compilación y todos los `.meta` nuevos generados.

**Checkpoint**: commits `docs(decisions): …` (T002) y `chore(client): …` (T003).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: el núcleo puro `Ecosystem.Territory` y la malla de la pieza, que necesitan todas las historias.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests (Red) ⚠️

- [X] T004 [P] Escribir `_Project/Tests/EditMode/Territory/HexCellTests.cs`:
  - centro de cada celda según `data-model.md` → `HexCell.Column` y `HexCell.Row` (filas impares desplazadas `W/2`, `z = −1,5·R·Row`) con `HexMetrics.OuterRadius` = 0,5;
  - conversión a axial;
  - las seis direcciones de vecinos;
  - dos vecinas están a distancia `W` (tolerancia ≤ 0,1 % de `W`) y dos no vecinas, a más de `W`.
- [X] T005 [P] Escribir `_Project/Tests/EditMode/Territory/TerritoryLayoutTests.cs`, sobre `data-model.md` → `TerritoryLayout.Cells`:
  - las 16 celdas `{0..3}×{0..3}` dan `Ok` y conservan el orden;
  - `AdjacentPairs() == 33`;
  - ninguna pareja está a menos de `W` (sin solapamientos);
  - cada celda interior tiene sus 6 vecinas y las del borde, las esperadas (sin huecos);
  - lista nula o vacía → `Empty`;
  - celda repetida → `Duplicate(cell)` con la primera repetida;
  - `Bounds` contiene todos los vértices a altura 0 y `HexMetrics.Height`.
- [X] T006 [P] Escribir `_Project/Tests/EditMode/Territory/HexPrismMeshBuilderTests.cs`, según `data-model.md` → *HexPrismMesh*:
  - 30 vértices y 16 triángulos;
  - submalla 0 = cara superior y submalla 1 = laterales;
  - normales planas por cara;
  - caras frontales con orden horario visto desde fuera;
  - sin cara inferior;
  - coordenadas de textura de la cara superior planares y de los laterales `u,v ∈ [0,1]`;
  - las aristas superiores coinciden con `HexMetrics.Corner(i)`.

  Ejecutar A1: las tres fallan (Red). Anotarlo en el registro.

### Implementation (Green)

- [X] T007 Implementar `_Project/Scripts/Territory/Point.cs` (`Point2` y `Point3`, structs inmutables), `HexCell.cs` y `HexMetrics.cs`:
  - `HexMetrics.Default` con `data-model.md` → `HexMetrics.OuterRadius` = 0,5 y `HexMetrics.Height` = 0,35. Es la **única fuente** de `R` y `h`;
  - `Center`, `Corner` y `AreNeighbours`.

  T004 en verde.
- [X] T008 Implementar `_Project/Scripts/Territory/TerritoryLayout.cs`:
  - `Create` devuelve un resultado `Ok`, `Empty` o `Duplicate`, sin excepciones (principio V);
  - `AdjacentPairs` y `Bounds(metrics)`;
  - mensajes en español según `data-model.md` → *TerritoryLayout* (tabla de creación).

  T005 en verde.
- [X] T009 Implementar `_Project/Scripts/Territory/HexPrismMeshBuilder.cs` (research.md R-3). Devuelve arrays planos: posiciones, normales, coordenadas de textura y los índices de las dos submallas. T006 en verde.
- [X] T010 Crear la malla guardada:
  - escribir primero `_Project/Tests/EditMode/Territory/HexTileMeshAssetTests.cs`: carga `_Project/Art/Territory/Meshes/HexTile.asset` con `AssetDatabase` y la compara, vértice a vértice y por índices de submalla, con `HexPrismMeshBuilder(HexMetrics.Default)`. Falla (Red) porque el asset no existe;
  - implementar `_Project/Scripts/Territory.Editor/HexTileMeshMenu.cs`, con el menú *Ecosystem → Territorio → Regenerar malla de pieza*, que crea o sobrescribe el asset;
  - ejecutar el menú para generar `HexTile.asset` y su `.meta`. La prueba pasa a verde.

**Checkpoint**: A1 en verde. Commits `feat(client): add the territory core …` (T004–T005, T007–T008) y `feat(client): generate the hex tile mesh …` (T006, T009–T010).

---

## Phase 3: User Story 1 - Revisar el territorio en el editor sin backend (Priority: P1) 🎯 MVP

**Goal**: abrir `Territory.unity` y ver 16 prismas en 4 filas desplazadas, en edición y en *Play*, sin Nakama.

**Independent Test**: quickstart A2, A3 y A4 con Nakama detenido y sin `connection.local.json`.

### Tests for User Story 1 ⚠️

- [X] T011 [P] [US1] Escribir `_Project/Tests/PlayMode/TerritorySceneTests.cs` con las invariantes 1, 2 y 4 y la composición de [contracts/territory-scene.md](contracts/territory-scene.md):
  - la escena carga con `LogAssert.NoUnexpectedReceived`;
  - hay exactamente 16 piezas, en las posiciones `HexMetrics.Center` de las celdas de `InitialTerritory`;
  - la cámara es ortográfica, con rotación `(θ, 0, 0)`, `SolidColor`, `renderPostProcessing == false` y fondo = constante `ApprovedBackground` (valor de partida `#8E9BAA`);
  - la luz no tiene sombras;
  - `RenderSettings.skybox == null`, `fog == false` y `ambientMode == Flat`;
  - no hay `Animator`, `Animation`, `ParticleSystem`, `Volume`, `AudioSource` ni `ConnectionScreen`;
  - la cámara no cambia entre los fotogramas 1 y 120.

  Carga la escena por ruta con `EditorSceneManager.LoadSceneAsyncInPlayMode`, bajo `#if UNITY_EDITOR`, porque aún no está en la build. Cuando T031 la añada, se puede cargar con `SceneManager`.
- [X] T012 [P] [US1] Escribir dos pruebas EditMode:
  - `_Project/Tests/EditMode/Territory/TerritorySceneAssetTests.cs` comprueba que las piezas no se serializan en la escena (research.md R-7). En el YAML de `_Project/Scenes/Territory.unity`:
    - no hay bloques `PrefabInstance` (`--- !u!1001`) cuyo `m_SourcePrefab` sea el GUID de `HexTile.prefab`;
    - ningún `MeshFilter` referencia el GUID de `HexTile.asset`.

    La referencia al prefab en el campo `tilePrefab` de `TerritoryView` **sí** es válida y no cuenta.
  - `_Project/Tests/EditMode/Territory/TerritoryLayoutAssetTests.cs`, primer caso: `InitialTerritory.asset` → `ToLayout()` da `Ok` con exactamente las 16 celdas `{0..3}×{0..3}` de `data-model.md` → `TerritoryLayoutAsset.cells` y `TerritoryLayout.Cells`. Los casos no válidos llegan en US5 (T034).

  Ejecutar A1 y A2: fallan (Red).

### Implementation for User Story 1

- [X] T013 [P] [US1] Crear los materiales y el prefab:
  - `_Project/Art/Territory/Materials/M_Grass_Top.mat` y `M_Earth_Side.mat`: `Universal Render Pipeline/Simple Lit`, *Specular Highlights* desactivado, *Receive Shadows* desactivado y color base de partida (`#6FA34A`, `#8A5A3B`; research.md R-5). Las texturas llegan en US2;
  - `_Project/Art/Territory/Prefabs/HexTile.prefab`: `MeshFilter(HexTile.asset)` y `MeshRenderer` con los materiales en el orden de las submallas, `Cast Shadows = Off`.
- [X] T014 [P] [US1] Implementar `_Project/Scripts/Territory.View/TerritoryLayoutAsset.cs`, un ScriptableObject con `data-model.md` → `TerritoryLayoutAsset.cells` (`{column,row}`) y `ToLayout()` que delega en `TerritoryLayout.Create`. Crear `_Project/Data/Territory/InitialTerritory.asset` con las 16 celdas `{0..3}×{0..3}` de `data-model.md` → `TerritoryLayout.Cells` (disposición inicial). `TerritoryLayoutAssetTests` (T012) en verde.
- [X] T015 [US1] Implementar `_Project/Scripts/Territory.View/TerritoryView.cs` según `data-model.md` → *Componentes de escena* → `TerritoryView` y research.md R-7:
  - `[ExecuteAlways]`;
  - en `OnEnable`, instancia `tilePrefab` en `HexMetrics.Center` por celda;
  - `HideFlags.DontSave` fuera de *Play*;
  - en `OnDisable`, destruye las instancias;
  - `OnValidate` marca la vista como pendiente y la reconstruye en el siguiente `Update`;
  - expone `Bounds` para el encuadre.

  Los errores de disposición quedan para US5 (T034).
- [X] T016 [US1] Crear `_Project/Scenes/Territory.unity` con la composición de [contracts/territory-scene.md](contracts/territory-scene.md):
  - `Territory`, con `TerritoryView`, `InitialTerritory` y `HexTile.prefab`;
  - `Main Camera`: ortográfica, rotación (35°, 0, 0), `SolidColor` `#8E9BAA` y *Post Processing* desactivado, con un `orthographicSize` fijo provisional que encuadre en 16:9 (el encuadre adaptativo llega en US3);
  - `Directional Light`: rotación (50°, −30°, 0°) y `Shadows = None`;
  - *Lighting*: sin *skybox*, sin niebla y ambiente plano, sin hornear.

  T011 y T012 en verde.
- [X] T017 [US1] Validar a mano quickstart A3 (se ve sin *Play* y la escena no queda modificada) y A4 (10 *Play* sin errores ni avisos). Anotarlo en el registro.

**Checkpoint**: MVP visible. Commit `feat(client): add the territory scene …` (T011–T017).

---

## Phase 4: User Story 2 - Terreno de tierra con césped legible y sereno (Priority: P1)

**Goal**: dirección visual aprobada con Claude Design, texturas de césped y tierra integradas, y valores visuales definitivos.

**Independent Test**: quickstart C (criterios C3, C4, C5, C6, C8, C9 y C11) en 16:9.

- [ ] T018 [US2] Crear la dirección visual con Claude Design:
  - llamar a la herramienta de artefactos con `action: "quickstart"` e `intent: "design"`, y cargar los skills que indique (`artifact-design`; `artifact-capabilities` para el botón de descarga);
  - el artefacto incluye todo lo de [contracts/visual-direction.md](contracts/visual-direction.md) §1: paleta, pieza, cámara y luz, referencias 16:9 y 9:19.5, notas de estilo, y `T_Grass_Top` y `T_Earth_Side` con su botón «Descargar PNG» de 512 × 512;
  - los requisitos de las texturas de §1 son obligatorios: banda de borde hexagonal alineada con el mapeo planar de `data-model.md` → *HexPrismMesh*, tierra que se repite sin costura en horizontal y borde de césped en la banda alta.
- [ ] T019 [US2] **Bloqueante, lo hace el responsable**: revisar y aprobar el artefacto. Puede iterar en T018. Anotar en [contracts/visual-direction.md](contracts/visual-direction.md) §2 los valores aprobados, el enlace y la fecha, y cambiar el *Estado* a «aprobado».
- [ ] T020 [P] [US2] Exportar los dos PNG desde el artefacto a `_Project/Art/Territory/Textures/T_Grass_Top.png` y `T_Earth_Side.png`:
  - importación según [contracts/visual-direction.md](contracts/visual-direction.md) §3: 512, sRGB, *mipmaps*, trilineal, *wrap* por textura y *override* ASTC 6×6 en Android e iOS;
  - comprobar en el inspector que miden 512 × 512;
  - `git status` no muestra SVG, capturas ni temporales.
- [ ] T021 [US2] Aplicar los valores aprobados, empezando por las pruebas:
  - actualizar `ApprovedBackground` en `TerritorySceneTests.cs` y la inclinación esperada (Red si cambian);
  - después, asignar las texturas a `M_Grass_Top` y `M_Earth_Side` y ajustar su color base;
  - ajustar fondo, ambiente, luz e inclinación en `Territory.unity`;
  - si cambia la altura, actualizar `data-model.md` → `HexMetrics.Height` en `HexMetrics.Default`, regenerar con el menú y comprobar que `HexTileMeshAssetTests` pasa.

  A1 y A2 en verde.
- [ ] T022 [US2] Revisión manual quickstart C en 16:9: criterios C2 a C6, C8, C9 y C11. Si C2 detecta líneas en las aristas, aplicar el solape documentado de plan.md → *Riesgos*, ≤ 0,1 % del ancho, y anotarlo. Registrar el resultado.

**Checkpoint**: commit `feat(client): add the approved territory art …` (T020–T022, con `contracts/visual-direction.md`).

---

## Phase 5: User Story 3 - El territorio se ve entero en cualquier orientación y proporción (Priority: P2)

**Goal**: encuadre adaptativo dentro del área segura en las seis proporciones, recalculado al cambiar la pantalla.

**Independent Test**: A1, A2 y quickstart C7 y C10, con la vista *Simulator*.

### Tests for User Story 3 ⚠️

- [X] T023 [P] [US3] Escribir `_Project/Tests/EditMode/Territory/TerritoryFramingTests.cs` con las entradas de `data-model.md` → `TerritoryFraming.boardCorners`, `.tiltDegrees`, `.screen`, `.safeArea` y `.marginFraction`:
  - para 16:9, 19.5:9, 4:3, 9:16, 9:19.5 y 3:4 a pantalla completa, todas las esquinas proyectadas caen dentro del área segura menos el margen de 0,06, y el tablero ocupa ≥ 70 % de la dimensión que limita;
  - con un área segura ficticia (muesca arriba en vertical, laterales en horizontal), el tablero queda centrado en ella y fuera de la zona no segura;
  - un área segura igual a la pantalla da `CameraOffset` 0.
- [X] T024 [P] [US3] Ampliar `_Project/Tests/PlayMode/TerritorySceneTests.cs` con la invariante 3 de [contracts/territory-scene.md](contracts/territory-scene.md): para cada `camera.aspect` de referencia, tras aplicar el encuadre, `WorldToViewportPoint` de las esquinas de `Bounds` ∈ [0,06; 0,94].

  Ejecutar A1 y A2: fallan (Red).

### Implementation for User Story 3

- [X] T025 [US3] Implementar `_Project/Scripts/Territory/TerritoryFraming.cs` (research.md R-6), con salida `data-model.md` → `TerritoryFraming.OrthographicSize` y `.CameraOffset`. T023 en verde.
- [X] T026 [US3] Implementar `_Project/Scripts/Territory.View/TerritoryCamera.cs` según `data-model.md` → *Componentes de escena* → `TerritoryCamera`:
  - `[ExecuteAlways]`;
  - en `LateUpdate` recalcula solo si cambian `Screen.width`, `Screen.height` o `Screen.safeArea`;
  - expone un método interno `Apply(width, height, safeArea)` para la prueba PlayMode.

  Añadirlo a `Main Camera` en `Territory.unity` y quitar el `orthographicSize` provisional de T016. T024 en verde, y las invariantes de T011 siguen en verde.
- [ ] T027 [US3] *(Pendiente del responsable: necesita la vista Simulator del editor; las partes automatizables están verificadas, ver el registro)* Validar a mano quickstart C7, en las seis proporciones y en *Simulator* con un iPhone con muesca en vertical y horizontal, y C10, cambiando de proporción durante *Play*. Registrarlo.

**Checkpoint**: commit `feat(client): frame the territory for any aspect and safe area …` (T023–T027).

---

## Phase 6: User Story 4 - Ver el territorio tras conectarse como invitado (Priority: P2)

**Goal**: `Bootstrap` carga el territorio una sola vez al llegar a `Connected`. En los demás estados, nada cambia.

**Independent Test**: A1, A2, B1, B2 y B3.

### Tests for User Story 4 ⚠️

- [ ] T028 [P] [US4] En `_Project/Tests/EditMode/ProjectConfigurationTests.cs`, sustituir `BuildScenes_ContainOnlyBootstrapSceneAtIndexZero` por `BuildScenes_AreBootstrapThenTerritory`, con la lista exacta `[Bootstrap, Territory]` (FR-021). Justificarlo en un comentario que cite la spec 004 FR-021 y la spec 003 FR-027.
- [ ] T029 [P] [US4] En `_Project/Tests/PlayMode/BootstrapSceneTests.cs`, añadir `WithoutConfiguration_DoesNotLoadTerritory`: con `ECOSYSTEM_NAKAMA_CONFIG=none`, tras 60 fotogramas la escena activa sigue siendo `Bootstrap` y no hay ninguna `TerritoryView`.
- [ ] T030 [P] [US4] En `_Project/Tests/Integration/BootstrapSceneIntegrationTests.cs`, añadir `Connected_LoadsTerritoryOnce`:
  - con el entorno local, al llegar a `Connected` la escena activa pasa a `Territory.unity` en ≤ 2 s;
  - hay 16 piezas;
  - durante 2 s más, `sceneLoaded` se dispara una sola vez para esa escena;
  - no se registran errores ni avisos.

  Ejecutar A1 y B1: T028 y T030 fallan (Red). T029 puede pasar ya y se anota como prueba de no regresión.

### Implementation for User Story 4

- [ ] T031 [US4] Añadir `_Project/Scenes/Territory.unity` en el índice 1 de `client-unity/ProjectSettings/EditorBuildSettings.asset`, con `Bootstrap` en el 0. T028 en verde.
- [ ] T032 [US4] Modificar `_Project/Scripts/Presentation/ConnectionScreen.cs` según `data-model.md` → *Componentes de escena* → `ConnectionScreen` y [contracts/territory-scene.md](contracts/territory-scene.md) → *Paso desde Bootstrap*:
  - `[SerializeField] string sceneOnConnected` y bandera `_navigated`;
  - cuando `Run()` termina con `State == Connected` y `!_navigated`, carga en `Single`;
  - si la escena está vacía o no está en la build (`SceneUtility.GetBuildIndexByScenePath < 0`), registra `[Conexión] Escena tras conectar no disponible: …` como error y se queda en `Bootstrap`;
  - sin referencias a `Ecosystem.Territory*`.

  Asignar `sceneOnConnected = Assets/_Project/Scenes/Territory.unity` en `_Project/Scenes/Bootstrap.unity`. T030 en verde, y las pruebas de la spec 003 siguen en verde.
- [ ] T033 [US4] Validar a mano quickstart B2 (≤ 2 s) y B3 («Reintentar» tras «Sin conexión» carga el territorio una vez). Registrarlo.

**Checkpoint**: commit `feat(client): show the territory after a guest connection …` (T028–T033).

---

## Phase 7: User Story 5 - Disposición del territorio definida por datos (Priority: P3)

**Goal**: la disposición sale solo del asset de datos, y una disposición no válida se rechaza con un error claro.

**Independent Test**: A1, A2 y quickstart A5.

### Tests for User Story 5 ⚠️

- [ ] T034 [P] [US5] Ampliar `_Project/Tests/EditMode/Territory/TerritoryLayoutAssetTests.cs` (el caso de `InitialTerritory` ya existe desde T012):
  - un `TerritoryLayoutAsset` creado con `ScriptableObject.CreateInstance` con 15 celdas da `Ok` con 15;
  - vacío → `Empty`;
  - con una repetida → `Duplicate`.

  Todo según `data-model.md` → `TerritoryLayoutAsset.cells` y `TerritoryLayout.Cells`.
- [ ] T035 [P] [US5] Ampliar `_Project/Tests/PlayMode/TerritorySceneTests.cs`: un `TerritoryView` con un asset de 15 celdas instancia 15 piezas, y con uno vacío, con uno con repetidas o con `layout` nulo:
  - `LogAssert.Expect(LogType.Error, ^"\[Territorio\] ")` con la causa;
  - 0 piezas.

  Ejecutar A1 y A2: fallan los casos no válidos (Red).

### Implementation for User Story 5

- [ ] T036 [US5] En `_Project/Scripts/Territory.View/TerritoryView.cs`, tratar los resultados `Empty` y `Duplicate` y el `layout` nulo:
  - `Debug.LogError("[Territorio] " + mensaje)`, con los mensajes de `data-model.md` → *TerritoryLayout*;
  - no instancia nada;
  - sin excepciones.

  T034 y T035 en verde.
- [ ] T037 [US5] Validar a mano quickstart A5 (15 piezas, luego el error con repetidas) y deshacer los cambios del asset. Registrarlo.

**Checkpoint**: commit `feat(client): reject invalid territory layouts …` (T034–T037).

---

## Phase 8: Polish & Cross-Cutting Concerns

- [ ] T038 [P] Actualizar `client-unity/README.md` (FR-026):
  - estructura con `Art/Territory`, `Data/Territory`, `Scripts/Territory*` y `Territory.unity`;
  - dependencias `Territory.Editor → Territory.View → Territory`;
  - «Revisar el territorio sin backend»;
  - menú para regenerar la malla;
  - nuevas pruebas en cada grupo;
  - «Bootstrap pasa al territorio al conectar»;
  - quitar «Única escena de la build».
- [ ] T039 [P] Actualizar `README.md` (raíz): la línea de estado (spec 004, primer territorio visible) y la fila de pruebas de Unity.
- [ ] T040 Ejecutar los tres grupos en modo batch (A1, A2 y B1) con el entorno local arrancado. Anotar los códigos de salida y los totales en el registro.
- [ ] T041 Generar las builds de desarrollo de Android e iOS según `client-unity/README.md` → *Builds de desarrollo*:
  - las dos compilan;
  - tras cerrar, ningún archivo versionado cambia salvo los esperados, que se justifican en el registro.
- [ ] T042 Ejecutar quickstart D (higiene) y `tools/tests/run.sh`. Las cuatro comprobaciones de D salen vacías o con 0. Anotarlo.
- [ ] T043 Revisión visual manual **completa** de quickstart C (tabla C1–C11 en las seis proporciones y *Simulator*), hecha o confirmada por el responsable. Registrar la tabla con fecha y persona en `specs/004-territorio-hexagonal/spec.md` → nueva sección *Revisión visual* (FR-024 y SC-005).
- [ ] T044 Push y PR hacia `main` **solo con autorización explícita** del responsable (constitución → Git). Anotar el número del PR y el resultado de la CI.

**Checkpoint**: commits `docs: …` (T038–T039) y `docs(spec): log the verification for spec 004` (T040–T043).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (1)**: sin dependencias. T002 va antes de cualquier binario (T020).
- **Foundational (2)**: depende de T003 y bloquea todas las historias.
- **US1 (3)**: depende de la fase 2. Es el MVP: escena, prefab, materiales y vista.
- **US2 (4)**: depende de US1, porque usa los materiales y la escena. T019 es una espera humana bloqueante; T018 puede empezar en cuanto acaba la fase 2.
- **US3 (5)**: depende de US1. Es independiente de US2 y puede ir en paralelo, aunque los dos tocan `Territory.unity`, así que mejor en secuencia o con cuidado al fusionar.
- **US4 (6)**: depende de US1 (la escena existe). Es independiente de US2, US3 y US5.
- **US5 (7)**: depende de US1 (`TerritoryView`). Es independiente de US2 a US4.
- **Polish (8)**: depende de todas. T043 necesita las valoraciones del responsable y T044, su autorización.

### Within Each User Story

- Pruebas (Red) → núcleo (Green) → vista y escena → validación manual.
- Las pruebas EditMode del núcleo no cargan escenas. Las PlayMode no necesitan Nakama. Solo T030 necesita el entorno local.

### Parallel Opportunities

- Fase 2: T004 ∥ T005 ∥ T006.
- US1: T011 ∥ T012, y T013 ∥ T014.
- US2: T018 puede empezar durante US1. T020 ∥ el registro de valores tras T019.
- US3: T023 ∥ T024.
- US4: T028 ∥ T029 ∥ T030.
- US5: T034 ∥ T035.
- US3, US4 y US5 en paralelo tras US1 (cuidado con `Territory.unity` y `TerritorySceneTests.cs`).
- Polish: T038 ∥ T039.

---

## Parallel Example: Foundational

```bash
Task: "T004 [P] HexCellTests.cs"
Task: "T005 [P] TerritoryLayoutTests.cs"
Task: "T006 [P] HexPrismMeshBuilderTests.cs"
```

## Parallel Example: User Story 4

```bash
Task: "T028 [P] [US4] ProjectConfigurationTests.BuildScenes_AreBootstrapThenTerritory"
Task: "T029 [P] [US4] BootstrapSceneTests.WithoutConfiguration_DoesNotLoadTerritory"
Task: "T030 [P] [US4] BootstrapSceneIntegrationTests.Connected_LoadsTerritoryOnce"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Fases 1 y 2.
2. US1: territorio de 16 piezas visible sin backend, con los colores de partida.
3. **STOP and VALIDATE**: A2, A3 y A4.

### Incremental Delivery

1. US1 → territorio visible en el editor.
2. US2 → arte aprobado con Claude Design.
3. US3 → encuadre en cualquier orientación y área segura.
4. US4 → integrado en el flujo tras la conexión.
5. US5 → disposición por datos validada.
6. Polish → documentación, builds, revisión completa y PR.

---

## Notes

- [P] = archivos distintos y sin dependencias pendientes.
- Ninguna verificación se marca hecha sin haberse ejecutado (constitución, principio VI). Si algo
  no se puede ejecutar, se anota qué falta y por qué.
- Los recursos nuevos llevan su `.meta`. No se confirman XML de pruebas, builds, `Library/`,
  `Temp/` ni archivos del diseño.

## Registro de verificación

| Fecha | Tarea | Comando o comprobación | Resultado |
|-------|-------|------------------------|-----------|
| 2026-11-25 | T001 | `git status`, rama | `feat/004-territorio-hexagonal`, sin cambios pendientes |
| 2026-11-25 | T001 | Unity instalado | 6000.3.29f1 en `/Applications/Unity/Hub/Editor/` |
| 2026-11-25 | T001 | A1 (EditMode, línea base) | Salida 0 · 113/113 |
| 2026-11-25 | T001 | A2 (PlayMode, línea base) | Salida 0 · 2/2 |
| 2026-11-25 | T001 | `tools/check-repo.sh` | Salida 0 (compose, secrets, ignored) |
| 2026-11-25 | T002 | ADR-0011, índice y `pendientes.md` | D-08 en *Resueltas*. Sin otras referencias vivas que actualizar (las de la spec 001 son históricas) |
| 2026-11-25 | T003 | Apertura en modo batch tras crear los tres asmdef y referenciarlos en las pruebas | Salida 0, 0 errores de compilación (solo el aviso esperado de «asmdef sin scripts»). Desviación: las carpetas `Art/Territory/*`, `Data/Territory` y `Tests/EditMode/Territory` se crean con su primer archivo y no vacías, porque Git no versiona carpetas vacías y un `.meta` de carpeta sin la carpeta haría que Unity lo borrara en cada clon |
| 2026-11-25 | T004–T006 | A1 con las pruebas nuevas, sin implementación (Red) | Salida 1: el ensamblado de pruebas no compila porque faltan `HexMetrics`, `TerritoryLayout`, `HexPrismMeshData` y `Point3` |
| 2026-11-25 | T007–T009 | A1 (Green) | Salida 0 · 151/151 (113 previas + 38 nuevas) |
| 2026-11-25 | T010 | A1 con `HexTileMeshAssetTests` y sin el asset (Red) | Salida 2 · 151/152, falla `SavedMesh_MatchesTheGeneratorOutput` (falta la malla) |
| 2026-11-25 | T010 | `-executeMethod Ecosystem.Territory.Editor.HexTileMeshMenu.Regenerate`, y A1 | Malla guardada (6,7 KB, texto YAML). Salida 0 · 152/152. Regenerar otra vez da un archivo idéntico |
| 2026-11-25 | T011–T012 | A1 y A2 con las pruebas de US1 y sin la vista (Red) | Salida 1 en los dos: faltan `TerritoryView` y `Ecosystem.Territory.View.TerritoryLayoutAsset` |
| 2026-11-25 | T013–T016 | Materiales, prefab, `InitialTerritory.asset` y `Territory.unity` creados con un script de editor temporal (borrado, no se versiona) | Simple Lit con `_SpecularHighlights = 1` (sin especular) y `_RECEIVE_SHADOWS_OFF`. La escena no guarda piezas (`grep "Tile ("` → 0) |
| 2026-11-25 | T011, T016 | Ajuste de la prueba de composición | En lugar de buscar `Animator`, `ParticleSystem` y `AudioSource` (sus módulos no están en el proyecto y no compila), la prueba exige una **lista cerrada** de componentes. Se actualiza `contracts/territory-scene.md` |
| 2026-11-25 | T011–T016 | A1 y A2 (Green) | EditMode: salida 0 · 155/155. PlayMode: salida 0 · 9/9 |
| 2026-11-25 | T017 | A3, automatizado en `TerritorySceneAssetTests.TerritoryScene_OpenedInTheEditor_ShowsSixteenPiecesAndStaysClean` | Abierta sin *Play*: 16 piezas `DontSave` y `scene.isDirty == false` |
| 2026-11-25 | T017 | A4: 10 ejecuciones seguidas de `TerritorySceneTests` en modo batch (incluye `LogAssert.NoUnexpectedReceived`) | 10/10 sin errores ni avisos. `-repeat 10` del Test Framework no repite la prueba, así que se lanzan 10 procesos |
| 2026-11-25 | T017 | Captura en *Play* (prueba temporal, no versionada), 16:9 y 9:19.5 | 16 prismas con césped verde arriba y tierra en los laterales frontales, sin huecos ni parpadeo visibles. Sin borde en la textura, las caras superiores se leen como una sola superficie (riesgo previsto, se resuelve en US2). En 9:19.5 el tablero se recorta con el tamaño fijo provisional (se resuelve en US3). Nota: con `camera.Render()` desde el editor, la cara superior sale con el material de tierra; en *Play* es correcta, así que las capturas se hacen siempre en *Play* |
| 2026-11-25 | T023–T024 | A1 con `TerritoryFramingTests` y A2 con el encuadre por aspecto (Red) | Salida 1: faltan `TerritoryFramingResult`, `TerritoryFraming` y `TerritoryCamera` |
| 2026-11-25 | T025–T026 | A1 y A2 (Green), con `TerritoryCamera` añadido a `Main Camera` mediante un script de editor temporal (no versionado) | EditMode: salida 0 · 164/164 (6 proporciones a pantalla completa, muesca en vertical, muescas laterales en horizontal, proyección). PlayMode: 15/15 de las pruebas del proyecto (incluidas las 6 proporciones con `WorldToViewportPoint ∈ [0,06; 0,94]`) |
| 2026-11-25 | T027 | Captura en *Play* a 9:19.5 tras el encuadre | Tablero entero, centrado, con el margen del 6 % a los lados (88 % del ancho). Quedan pendientes para el responsable la vista *Simulator* con muesca (C7) y el cambio de proporción durante *Play* (C10) |
