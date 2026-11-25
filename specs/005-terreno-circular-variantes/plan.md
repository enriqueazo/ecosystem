# Implementation Plan: Variantes de terreno y territorio circular

**Branch**: `feat/005-terreno-circular-variantes` | **Date**: 2026-11-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/005-terreno-circular-variantes/spec.md`

## Summary

Ampliar la escena `Territory` de la spec 004:
- **Forma**: el tablero 4 × 4 pasa a ser un **disco de 55 celdas**.
- **Variantes**: además de la hierba, **nieve**, **arena** y **agua**.
- **Río**: un **río demostrativo** de piezas con cauce hundido (manantial, tramo recto y curva
  suave).

Enfoque técnico:
- **Datos**: cada celda de `InitialTerritory.asset` pasa a llevar variante, forma de río y giro
  (mismo asset y mismo GUID).
- **Núcleo** sin Unity (`Ecosystem.Territory`), ampliado con TDD:
  - aristas;
  - validación (conexa, variantes, datos de río y conexiones recíprocas);
  - el generador de mallas de río, con un triangulador de polígonos.
- **Geometría**: el agua y las 15 mallas de río (una por giro distinto) se **generan desde el
  núcleo** y se guardan como assets, con una prueba de que están al día, como `HexTile`.
- **Catálogo**: un `TerrainCatalogAsset` asocia cada variante y cada pieza de río con su prefab y
  su malla. `TerritoryView` lo usa en lugar de un único prefab.
- **Hierba**: conserva su prefab, malla, materiales y texturas, byte a byte.
- **Diseño**: la dirección visual y las 6 texturas nuevas se hacen con Claude Design y se aprueban
  antes de importarlas.
- **Sin cambios** en el servidor, los contratos, la orientación, las escenas de la build ni la
  cámara.

## Technical Context

**Language/Version**: C# en Unity 6000.3.29f1 (IL2CPP en Android e iOS; API .NET Standard).

**Primary Dependencies**: URP 17.3.0 (`Simple Lit`), que ya está en el proyecto. Sin paquetes
nuevos (R-01 de la spec).

**Storage**: assets en texto YAML (`ForceText`): disposición, catálogo, mallas, materiales y
prefabs. Más 6 PNG de 512 × 512 en Git normal, unos 0,9 MB (ADR-0011).

**Testing**: Unity Test Framework 1.6.0:
- **EditMode**: núcleo (TDD), mallas al día, catálogo, importación de texturas y disposición
  inicial.
- **PlayMode**: escena, encuadre y datos no válidos.
- **Integración**: paso a `Connected` con 55 piezas.
- **Revisión visual manual**: quickstart C.

**Target Platform**: editor de Unity en macOS (vistas *Game* y *Simulator*). Las builds de
desarrollo de Android e iOS deben seguir compilando.

**Project Type**: cliente móvil Unity del monorepo. Solo cambian `client-unity/` y la
documentación.

**Performance Goals**:
- Territorio visible en ≤ 2 s tras `Connected` (SC-008).
- Escena estática: 55 instancias. Las mallas de río tienen un orden de cientos de vértices; el
  resto, 30.
- 8 materiales en total, sin sombras, transparencias ni posprocesado.

**Constraints**:
- Sin red, contratos ni cambios de servidor (FR-025).
- Los recursos de hierba no cambian (FR-006).
- La lista cerrada de componentes de la escena no cambia (R-5).
- Texturas de 512 como máximo con ASTC 6×6.
- Sin shaders propios.

**Scale/Scope**:
- código: núcleo ampliado (unos 6 tipos nuevos), cambio en `TerritoryView`, un ScriptableObject
  nuevo y el comando del editor ampliado;
- arte y datos: 6 texturas, 6 materiales, 16 mallas generadas, 4 prefabs, 1 catálogo y la
  disposición reescrita;
- pruebas: unas 8 clases nuevas o actualizadas.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio / norma | Cumplimiento | Estado |
|-------------------|--------------|--------|
| I. Servidor autoritativo | Solo presentación. Variantes y río son datos locales de vista, sin valor de juego (FR-005). Cuando tengan reglas, la autoridad será el servidor. | ✅ |
| II. Contratos versionados | No hay mensajes nuevos. La disposición y el catálogo **no** son contratos de red ni van en `shared/`. | ✅ N/A |
| III. Resiliencia | No hay funcionalidad en directo. El paso desde `Bootstrap` no cambia. | ✅ N/A |
| IV. Desacoplado y simple | El núcleo sigue sin Unity, y las dependencias van en una dirección: `Territory.Editor → Territory.View → Territory`. Sin componentes nuevos en la escena, sin singletons ni paquetes. El catálogo tiene un consumidor claro (`TerritoryView`). La sobrecarga `Create(IEnumerable<HexCell>)` se mantiene porque la usan las pruebas. | ✅ |
| V. Calidad | La validación devuelve resultados con un orden fijo, sin excepciones como flujo. Solo lanzan excepción los constructores de medidas fuera de rango (`RiverMetrics`), igual que `HexMetrics`, porque es un error de programación. Las API de Unity usadas ya se usan en la spec 004 (`Mesh.SetTriangles` con varias submallas, `EditorUtility.CopySerialized`). | ✅ |
| VI. TDD | Primero las pruebas de: aristas, validación, conexiones, triangulador, mallas de río (cortes, nivel y áreas) y disposición inicial. La escena y los recursos se cubren con PlayMode y EditMode. El arte se valida a mano (quickstart C), una excepción documentada que prevé la constitución. Las 6 pruebas que fijan 16 piezas se actualizan sin perder comprobaciones (FR-028). | ✅ |
| VII. Seguridad | Sin secretos ni datos personales. El artefacto de diseño es privado y solo se enlaza. | ✅ |
| VIII. Reproducibilidad | Sin versiones nuevas. Las mallas se regeneran con un comando documentado (también en modo batch) y se comprueban. Los valores visuales están en `contracts/visual-direction.md`. Se actualizan los README. | ✅ |
| ADR-0011 (binarios) | Unos 0,9 MB nuevos, por debajo de los umbrales. Se mide en quickstart D antes de confirmar. | ✅ |
| Una rama por spec / commits atómicos | Rama `feat/005-terreno-circular-variantes`, creada desde `main` en `0918fbf`. | ✅ |

**Resultado**: PASA, sin desviaciones respecto a la spec.

**Re-check tras la fase 1**: PASA. El diseño no añade dependencias, componentes de escena,
mensajes ni estado global.
- El catálogo es un dato referenciado por la escena, no una dependencia global.
- El triangulador y los generadores son funciones puras del núcleo, con un solo consumidor cada
  uno: el comando del editor y sus pruebas.

## Project Structure

### Documentation (this feature)

```text
specs/005-terreno-circular-variantes/
├── spec.md · plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   ├── territory-scene.md        # Composición, invariantes con 55 piezas, datos no válidos y pruebas
│   └── visual-direction.md       # Qué entrega Claude Design, valores (de partida y aprobados) y recursos
└── checklists/requirements.md
```

### Source Code (repository root)

```text
client-unity/Assets/_Project/
├── Art/Territory/
│   ├── Textures/       + T_Snow_Top, T_Snow_Side, T_Sand_Top, T_Sand_Side, T_Water_Top, T_Water_Side (.png)
│   ├── Materials/      + M_Snow_Top, M_Snow_Side, M_Sand_Top, M_Sand_Side, M_Water_Top, M_Water_Side (.mat)
│   ├── Meshes/         + WaterTile.asset, River/River_*.asset (15)      (HexTile.asset sin cambios)
│   └── Prefabs/        + SnowTile, SandTile, WaterTile, RiverTile (.prefab)  (HexTile.prefab sin cambios)
├── Data/Territory/
│   ├── InitialTerritory.asset                  # Reescrito: 55 celdas con variante y río (mismo GUID)
│   └── TerrainCatalog.asset                    # Nuevo
├── Scenes/Territory.unity                      # TerritoryView: tilePrefab → catalog
├── Scripts/
│   ├── Territory/                              # Ecosystem.Territory (noEngineReferences)
│   │   ├── HexEdge.cs                          # Nuevo: aristas, opuesta, vecina por arista
│   │   ├── TerrainKind.cs                      # Nuevo: TerrainKind, RiverShape, TerritoryCell
│   │   ├── RiverPiece.cs                       # Nuevo: OpenEdges, Canonical
│   │   ├── TerritoryLayout.cs                  # Ampliado: validación R-6, BorderCells, RequiredVisuals
│   │   ├── RiverMetrics.cs                     # Nuevo: hw, w, rp (única fuente)
│   │   ├── PolygonTriangulator.cs              # Nuevo: ear clipping
│   │   ├── HexPrismMeshBuilder.cs              # + mapeo superior en red (opcional; por defecto el de 004)
│   │   └── RiverPieceMeshBuilder.cs            # Nuevo: 3 submallas por (forma, giro)
│   ├── Territory.View/
│   │   ├── TerritoryLayoutAsset.cs             # + terrain, river, rotation por celda
│   │   ├── TerrainCatalogAsset.cs              # Nuevo
│   │   └── TerritoryView.cs                    # catalog; comprueba RequiredVisuals; asigna la malla de río
│   └── Territory.Editor/
│       └── TerritoryMeshesMenu.cs              # Sustituye a HexTileMeshMenu: regenera las 17 mallas
└── Tests/
    ├── EditMode/Territory/
    │   ├── HexEdgeTests.cs, RiverConnectionTests.cs, PolygonTriangulatorTests.cs     # Nuevas
    │   ├── RiverPieceMeshBuilderTests.cs, InitialTerritoryTests.cs                   # Nuevas
    │   ├── TerritoryMeshAssetTests.cs, TerrainCatalogAssetTests.cs                   # Nuevas
    │   ├── TerritoryLayoutTests.cs, TerritoryLayoutAssetTests.cs                     # Ampliadas (Cell en vez de DuplicateCell)
    │   ├── TerritoryArtAssetTests.cs, TerritorySceneAssetTests.cs, TerritoryTestGeometry.cs   # Actualizadas
    │   ├── HexPrismMeshBuilderTests.cs                 # Ampliada con el mapeo en red; las pruebas existentes no cambian
    │   └── HexTileMeshAssetTests.cs                    # Solo cambia el nombre del menú en sus mensajes
    ├── PlayMode/TerritorySceneTests.cs                                               # 55 piezas, recursos, datos no válidos
    └── Integration/BootstrapSceneIntegrationTests.cs                                 # 16 → 55
client-unity/README.md                          # Estado, estructura, sección Territorio y pruebas
README.md                                       # Línea de estado
```

**Structure Decision**: se mantienen los tres ensamblados del territorio de la spec 004, sin
ensamblados nuevos. Todo lo comprobable sin Unity (validación, conexiones y geometría) vive en
`Ecosystem.Territory`. La vista solo traduce datos y asigna recursos, y el editor solo escribe
assets.

## Orden de entrega (commits atómicos, prueba e implementación juntas)

1. `feat(client)`: aristas (`HexEdge`), `TerrainKind`, `RiverShape`, `TerritoryCell` y
   `RiverPiece` con TDD.
2. `feat(client)`: validación ampliada de `TerritoryLayout` (R-6): no conexa, variante, datos de
   río y conexiones, más `BorderCells` y `RequiredVisuals`, con TDD. `DuplicateCell` pasa a ser
   `Cell`.
3. `feat(client)`: `PolygonTriangulator`, `RiverMetrics` y `RiverPieceMeshBuilder` con TDD
   (invariantes 1–6 de data-model), más el mapeo en red de `HexPrismMeshBuilder` sin cambiar el
   resultado por defecto.
4. `feat(client)`: comando *Regenerar mallas del territorio*, `WaterTile.asset`, las 15 mallas de
   río y `TerritoryMeshAssetTests`. `HexTile.asset` queda idéntico.
5. **Diseño** (sin commit de código): artefacto de Claude Design con las paletas, las variantes, las
   piezas de río, la composición, las referencias y las 6 texturas. Lo aprueba el responsable. Si
   cambian `hw`, `w` o `rp`, se actualiza `RiverMetrics.Default` y se regeneran las mallas en el
   commit 6.
6. `feat(client)`: texturas, materiales y prefabs de nieve, arena, agua y río, con
   `contracts/visual-direction.md` aprobado y `TerritoryArtAssetTests` ampliada. Antes, se mide
   ADR-0011.
7. `feat(client)`: `TerrainCatalogAsset` y `TerrainCatalog.asset`, `TerritoryView` con catálogo,
   los campos nuevos de `TerritoryLayoutAsset`, `InitialTerritory.asset` con 55 celdas y
   composición, y la escena actualizada. Las pruebas PlayMode, EditMode e integración se
   actualizan primero (Red).
8. `docs`: `client-unity/README.md`, README raíz y registro de la revisión manual (quickstart C) en
   la spec.

## Riesgos

| Riesgo | Mitigación |
|--------|------------|
| Rendijas o parpadeo en las uniones del cauce y en las orillas | Cortes calculados con la misma fórmula desde las dos piezas, probados con ≤ 0,1 % de `W`. Las caras coincidentes tienen normales opuestas y se descarta una por *backface culling*. Revisión C7 con zoom 2× |
| El *ear clipping* falla con polígonos casi degenerados (puntos del arco muy cerca de un vértice del hexágono) | Las restricciones de `RiverMetrics` (`w < W/2`, `rp < W/2`) y las pruebas de área para las 15 mallas. El arco de la curva no pasa cerca de los vértices: su radio interior es 0,62 frente a 0,43 hasta la arista intermedia |
| Costura del agua entre celdas | Mapeo en red y textura periódica en `(½, ½)` (research.md R-4). Revisión C5 |
| La nieve se confunde con el fondo gris azulado | Diferencia de luminosidad exigida (≥ 25 L*) en la dirección visual. Revisión C2 con las 8 celdas al azar |
| 55 piezas poco legibles en 9:19.5 vertical | El encuadre ya ocupa el 88 % de la dimensión que limita. Si la revisión C9 falla, se plantea al responsable reducir el margen o el tamaño antes de cambiar nada |
| Reescribir `InitialTerritory.asset` o el comando del editor cambia GUID | Se edita el asset existente y el comando usa `CopySerialized` sobre los existentes. Comprobación de GUID en quickstart D |
| La hierba cambia sin querer al tocar `HexPrismMeshBuilder` | El mapeo nuevo es opcional y por defecto da el resultado de la spec 004. Las comprobaciones de `HexTileMeshAssetTests` y las pruebas existentes de `HexPrismMeshBuilderTests` no cambian y deben pasar. `git diff` de los recursos de hierba vacío (quickstart D) |
| El visor de artefactos bloquea la descarga de PNG (como en la spec 004) | Las texturas se generan con el script local y se suben al artefacto. Son los mismos archivos que se importan (research.md R-7) |

## Complexity Tracking

| Elemento | Por qué hace falta | Alternativa más simple descartada |
|----------|--------------------|-----------------------------------|
| 15 mallas de río ya giradas en vez de 3 girando la instancia | Las texturas planares (banda de borde de la hierba y agua sin costura) tienen que casar con las vecinas (research.md R-2) | Girar la instancia rompe la continuidad de las texturas en cada giro distinto de 0 |
| `PolygonTriangulator` (*ear clipping*) en el núcleo | Las orillas del cauce son polígonos no convexos | Triangular a mano cada forma no escala a 3 formas × giros, y es más difícil de probar |
| Mapeo en red y textura de agua periódica | Agua sin costura entre piezas sin un shader propio (FR-009) | Un shader de coordenadas de mundo añade código de render que mantener |
