# Research: Variantes de terreno y territorio circular

Decisiones de la fase 0. Resuelven las decisiones pendientes P-01 a P-05 de la spec y las dudas
técnicas del plan. Formato: decisión, motivo y alternativas descartadas. Todo amplía lo que hizo la
spec 004 ([research.md](../004-territorio-hexagonal/research.md)); lo que no se menciona aquí no
cambia.

## R-1 · Dato de la disposición con variante y pieza de río (P-01)

**Decisión**:
- **Mismo recurso**: `TerritoryLayoutAsset` y `InitialTerritory.asset` (mismo GUID). Cada celda
  serializada pasa de `{column, row}` a `{column, row, terrain, river, rotation}`:
  - `terrain`: enumerado `TerrainKind`. El valor por defecto es `Grass` (0), así que una celda
    escrita con el formato de la spec 004 se lee como hierba;
  - `river`: enumerado `RiverShape`, con `None` (0) por defecto;
  - `rotation`: entero, 0 por defecto.
- **Núcleo**: el asset solo convierte datos. `TerritoryLayout.Create` valida todo (data-model.md).
  Se mantiene la sobrecarga `Create(IEnumerable<HexCell>)` para las pruebas y disposiciones de
  solo hierba.
- **Migración**: `InitialTerritory.asset` se reescribe con las 55 celdas y la composición. No hace
  falta código de migración, porque no hay más disposiciones en el proyecto.

**Motivo**:
- Una celda tiene exactamente una variante o pieza de río (FR-005). Tenerlo en la misma entrada
  hace imposible que una celda tenga datos en dos sitios, y la validación de repetidas ya existente
  sigue sirviendo.
- Se conservan el GUID, la referencia de la escena y el inspector por defecto: no hace falta un
  editor.

**Alternativas descartadas**:
- **Recurso aparte para variantes y ríos**, indexado por celda: dos fuentes que pueden
  desincronizarse y una validación cruzada más.
- **Variante como texto** (`"snow"`): no se detecta el error al editar y el inspector no ofrece
  lista. El enumerado sigue permitiendo detectar valores desconocidos si se edita el YAML a mano
  (`Enum.IsDefined`).

## R-2 · Geometría: agua, cauce y giros (P-02, P-03)

**Decisión**: toda la geometría nueva se **genera por código en el núcleo**, como `HexTile`, y se
guarda como assets de malla, con una prueba que comprueba que están al día.

- **Pieza de agua**: `HexPrismMeshBuilder` con una altura `hw` (nivel del agua) y el mapeo de la
  cara superior **en red** (R-4). Malla `WaterTile.asset`. Para no tocar `HexTile.asset`, el
  mapeo por defecto sigue siendo el de la spec 004.
- **Piezas de río**: un generador nuevo, `RiverPieceMeshBuilder`, con 3 submallas:
  - `0` · orillas de hierba a altura `h` (material `M_Grass_Top`, mismo mapeo que la hierba);
  - `1` · laterales y paredes del cauce (material `M_Earth_Side`, `v = y / h` en todos);
  - `2` · lámina de agua del cauce a altura `hw` (material `M_Water_Top`, mapeo en red).

  El cauce está **abierto por arriba**: su fondo visible es la lámina de agua, y las paredes van de
  `hw` a `h`. En una arista abierta, el lateral lleva una muesca hasta `hw`. Las paredes usan la
  textura de tierra con `v = y/h`, así que la banda de césped aparece en el borde superior de la
  orilla sin textura nueva.
- **Trazado del cauce** (en el plano XZ, centrado en la pieza):
  - **recto**: una franja de anchura `w` entre los puntos medios de dos aristas opuestas;
  - **curva suave** (aristas `a` y `a+2`): un arco de 60° con centro en la intersección de las
    rectas de esas dos aristas (a distancia `√3·R` del centro de la pieza, en la dirección de la
    arista `a+1`) y radio `1,5·R`. Es tangente a la normal de las dos aristas en sus puntos
    medios, así que el cauce entra y sale perpendicular a la arista, igual que el recto. Se
    discretiza con 8 segmentos;
  - **manantial**: una franja desde el punto medio de la arista hasta el centro, que termina en
    una poza circular de radio `rp`, con 16 segmentos.
- **Triangulación** de las orillas, que son polígonos simples no convexos: *ear clipping* en el
  núcleo (`PolygonTriangulator`), probado por área (la suma de los triángulos es igual al área del
  polígono) y por orientación.
- **Giros**: se generan **mallas ya giradas**, una por cada giro geométricamente distinto, en vez
  de girar la instancia. Son 6 de manantial, 3 de recto y 6 de curva: **15 mallas**. Así, las
  coordenadas de textura planar se calculan siempre en el sistema de la celda sin girar, y:
  - la hierba de las orillas coincide con la de las piezas de hierba, con la banda de borde en su
    sitio;
  - el agua del cauce continúa sin costura con la de las celdas de agua (R-4).
- **Comando del editor**: *Ecosystem → Territorio → Regenerar mallas del territorio* regenera
  `HexTile`, `WaterTile` y las 15 de río, conservando los GUID existentes, como ahora. Sustituye
  al comando de la spec 004, que solo regeneraba `HexTile`. El método para modo batch es
  `Ecosystem.Territory.Editor.TerritoryMeshesMenu.Regenerate`.

**Exactitud de las uniones (FR-013, FR-014, SC-005)**: los puntos de corte del cauce en cada arista
son `m ± (w/2)·t`, donde `m` es el punto medio y `t` la dirección de la arista. La pieza vecina
calcula los mismos puntos desde el otro lado. Una prueba EditMode lo comprueba para cada pareja
(forma, giro) con conexión recíproca y para la unión con la pieza de agua, con una tolerancia de
≤ 0,1 % de `W`.

**Parpadeo**: las caras que coinciden entre vecinas tienen normales opuestas y el *backface
culling* descarta una, como en la spec 004. La lámina del cauce y la del agua vecina no se solapan:
se tocan en la arista.

**Alternativas descartadas**:
- **Modelos FBX hechos a mano**: un binario y una herramienta externa más, y la exactitud de FR-013
  dependería del modelado.
- **Girar la instancia** (`Quaternion.Euler(0, −60·r, 0)`) con 3 mallas: las texturas planares
  giran con la pieza, y la banda de borde de la hierba y el agua del cauce dejarían de casar con
  las piezas vecinas.
- **Generar las mallas solo en ejecución**, sin assets: las piezas no tendrían malla en el
  inspector y habría un componente nuevo en la escena (la lista cerrada de la spec 004).
- **Cauce como calco plano**: descartado en *Clarifications* de la spec.
- **Lámina de agua y fondo de cauce separados**, con agua translúcida: FR-009 pide agua opaca, sin
  transparencia.

## R-3 · Convención de aristas y giros (P-03)

**Decisión**: las aristas se numeran en el orden de las direcciones axiales de `HexCell`, en
sentido **antihorario visto desde arriba**:

| Índice | Arista | Dirección axial | Normal (desde +x hacia +z) |
|--------|--------|-----------------|----------------------------|
| 0 | E | (+1, 0) | 0° |
| 1 | NE (fondo derecha) | (+1, −1) | 60° |
| 2 | NO (fondo izquierda) | (0, −1) | 120° |
| 3 | O | (−1, 0) | 180° |
| 4 | SO (frente izquierda) | (−1, +1) | 240° |
| 5 | SE (frente derecha) | (0, +1) | 300° |

- La arista opuesta a `e` es `(e + 3) mod 6`.
- La cara lateral `k` de `HexPrismMeshBuilder` (entre los vértices `k` y `k+1`) corresponde a la
  arista `(k + 1) mod 6`.
- **Aristas abiertas con giro 0**: manantial `{E}`, recto `{E, O}` y curva `{E, NO}`. Con giro
  `r`, cada arista abierta pasa a ser `(e + r) mod 6`.
- Se aceptan giros de 0 a 5 en todas las piezas. El recto con giro `r` es igual al de giro `r − 3`,
  y el catálogo lo guarda con giro canónico `r mod 3`.

**Motivo**: coincide con el orden de `HexCell.Neighbours()`, así que la vecina por la arista `e`
es el vecino `e`, sin otra tabla. Los nombres E, NE… son los de la spec (FR-012).

## R-4 · Texturas de agua sin costura entre piezas

**Decisión**: la cara superior del agua (en `WaterTile` y en el cauce) usa un mapeo **en red**:
`u = x / W` y `v = z / (3R)`, con *wrap* `Repeat`. La textura `T_Water_Top` MUST ser **periódica en
la red hexagonal**: invariante al desplazar media textura en los dos ejes a la vez, `(½, ½)`.

**Motivo**: los centros de todas las celdas están en la red generada por `(W, 0)` y `(W/2, 1,5R)`.
En este mapeo, esos vectores son `(1, 0)` y `(½, ½)`. Si la textura es periódica en ellos, el
mapeo local de cada pieza da el mismo resultado que uno en coordenadas de mundo. Así, el agua
continúa sin costura entre celdas de agua y en la desembocadura, sin un shader propio (FR-009: «sin
borde de pieza entre celdas de agua»).

**Coste**: la textura ocupa en el mundo un rectángulo de `W × 3R` (0,866 × 1,5). Se pinta con la
relación de aspecto compensada y con la periodicidad por construcción, en el mismo script
determinista que la spec 004 usó para las texturas (fuera del repositorio).

**Alternativas descartadas**:
- **Shader con coordenadas de mundo**: un shader propio que mantener, frente a `Simple Lit`.
- **Una textura plana sin detalle**: FR-009 pide una textura sutil, no un color plano.

## R-5 · Catálogo de variantes y representación en la vista (P-04)

**Decisión**:
- **`TerrainCatalogAsset`** (ScriptableObject en `Data/Territory/TerrainCatalog.asset`):
  - `terrains`: lista de `{kind, prefab}`. Hierba → `HexTile.prefab` (el de la spec 004, sin
    cambios), nieve → `SnowTile.prefab`, arena → `SandTile.prefab` y agua → `WaterTile.prefab`;
  - `riverPrefab`: `RiverTile.prefab`, con los tres materiales de R-2 y sin malla;
  - `riverMeshes`: lista de `{shape, rotation, mesh}` con las 15 mallas.
- **`TerritoryView`**: el campo `tilePrefab` se sustituye por `catalog`. Por cada celda, instancia
  el prefab de su variante. En las de río, instancia `riverPrefab` y le asigna la malla de su
  (forma, giro canónico).
- **Antes de crear nada**, la vista comprueba que el catálogo tiene todo lo que pide la
  disposición. Si falta algo, registra `[Territorio] El catálogo no tiene recursos para …` y crea
  0 piezas. La lista de lo que hace falta la calcula el núcleo (`TerritoryLayout.RequiredVisuals()`).
- **La escena** sigue con los mismos tipos de componente: las piezas solo llevan `MeshFilter` y
  `MeshRenderer`. **La lista cerrada del contrato de la spec 004 no cambia.**

**Motivo**:
- La hierba conserva su prefab, malla y materiales tal cual (FR-006, SC-004).
- Nieve y arena reutilizan `HexTile.asset` con otros materiales (R-02 de la spec).
- Añadir una variante es añadir un prefab y una entrada, sin tocar la lógica (FR-011).

**Alternativas descartadas**:
- **Un prefab por pieza de río y giro** (15 prefabs): mucha repetición para cambiar solo la malla.
- **Un prefab común con materiales por variante en el catálogo**: obligaría a cambiar
  `HexTile.prefab` y a asignar materiales en ejecución.
- **Un componente nuevo en cada pieza** para elegir la malla: amplía la lista cerrada de
  componentes sin necesidad.

## R-6 · Validación de la disposición

**Decisión**: `TerritoryLayout.Create` devuelve el **primer** error, en este orden fijo, y recorre
las celdas en el orden del dato:

1. `Empty`: lista nula o vacía.
2. `Duplicate`: celda repetida.
3. `UnknownTerrain`: valor de `terrain` fuera del enumerado.
4. `InvalidRiverPiece`:
   - celda de río sin forma;
   - forma de río en una celda que no es de río;
   - giro fuera de 0–5;
   - forma desconocida;
   - giro distinto de 0 en una celda que no es de río.
5. `Disconnected`: la primera celda (en orden del dato) que no se alcanza desde la primera por
   vecinos.
6. `BrokenRiver`: la primera arista abierta que incumple FR-016. Indica la celda, la arista y qué
   hay al otro lado.

El resultado lleva `Status`, `Cell`, `Edge` (cuando aplica) y `Message`. `Cell` sustituye a
`DuplicateCell`, y sus pruebas se actualizan. Sigue sin haber excepciones como flujo normal
(principio V). Los textos están en data-model.md.

**Motivo**: los errores estructurales (forma de los datos) van antes que los de conjunto
(conexión y río), para que el mensaje apunte a la causa más básica. Un orden fijo hace las pruebas
deterministas.

**Alternativas descartadas**:
- **Devolver todos los errores**: no aporta nada mientras no haya un editor de territorios, y
  complica el resultado.
- **Validar la forma de disco**: el disco es la disposición inicial, no una regla (FR-002). Se
  comprueba con una prueba sobre el asset.

## R-7 · Dirección visual con Claude Design y valores de partida (P-05)

**Decisión**: el proceso es el de la spec 004 (research.md R-5 y el registro de T018 en su
[contrato visual](../004-territorio-hexagonal/contracts/visual-direction.md)):
1. Se crea un **artefacto de Claude** con intención *design*. Lleva paletas, vista de cada
   variante y de las tres piezas de río, la composición y notas de estilo.
2. Las texturas se pintan con un **script determinista** (Python, sin dependencias y **fuera del
   repositorio**) a partir de la paleta, y se suben al artefacto. Son los mismos archivos que se
   importan en Unity.
3. Las referencias 16:9 y 9:19.5 son **renders reales** de la escena en *Play*, hechos con una
   importación temporal que se revierte sin confirmar.
4. El responsable aprueba el artefacto. Después, los valores y el enlace se copian en
   [contracts/visual-direction.md](contracts/visual-direction.md) y se importan las texturas.

**Valores de partida** (no definitivos):

| Parámetro | Valor |
|-----------|-------|
| Nieve: base, luz y sombra | `#E6EDF2`, `#F7FAFC` y `#C3D1DD` |
| Nieve: borde de nieve del lateral | 20 % superior, `#DCE5EC` sobre la tierra de la spec 004 |
| Arena: base, luz y sombra | `#D8BF8A`, `#E8D4A4` y `#BC9E68` |
| Arena: capas del lateral | `#C9AA74`, `#B08E5A` y `#DCC596` |
| Agua: base, luz y sombra | `#4E8E9F`, `#6DA8B5` y `#3D7585` |
| Agua: lecho del lateral y lámina | lecho `#7A6A55` y `#5E5040`; lámina en el 12 % superior, `#4E8E9F` |
| Nivel del agua `hw` | `0,25` (≈ 71 % de `h`; escalón de orilla de 0,10) |
| Anchura del cauce `w` | `0,26` (≈ 30 % de `W`) |
| Radio de la poza del manantial `rp` | `0,17` |
| Textura de las orillas del cauce | La de tierra de la spec 004 (sin textura nueva) |

**Contraste exigido**: la nieve se distingue del fondo `#8E9BAA` por luminosidad (≥ 25 puntos de
L* en CIELAB) y de la arena por tono. El agua se distingue del fondo por saturación y luminosidad.
El revisor lo comprueba en quickstart C con las referencias.

**Texturas nuevas** (6, 512 × 512, la misma importación que la spec 004):

| Textura | Mapeo | *Wrap* | Requisito |
|---------|-------|--------|-----------|
| `T_Snow_Top` | Inscrito (como `T_Grass_Top`) | Clamp | Banda de borde hexagonal sutil |
| `T_Snow_Side` | Lateral (`v = y/h`) | Repeat U / Clamp V | Sin costura en horizontal; borde de nieve en el 20 % superior |
| `T_Sand_Top` | Inscrito | Clamp | Banda de borde sutil |
| `T_Sand_Side` | Lateral | Repeat U / Clamp V | Sin costura en horizontal; capas, sin borde de césped |
| `T_Water_Top` | En red (R-4) | Repeat | Periódica en `(½, ½)`, sin banda de borde |
| `T_Water_Side` | Lateral (`v = y/hw`) | Repeat U / Clamp V | Sin costura en horizontal; lámina en la banda superior |

**Volumen**: unos 6 × 150 KB ≈ 0,9 MB, frente a los 0,3 MB actuales. Muy por debajo de los
umbrales de ADR-0011, así que no hace falta un ADR nuevo. Se mide en quickstart D.

## R-8 · Encuadre y legibilidad con 55 piezas

**Decisión**: sin cambios en `TerritoryFraming` ni en `TerritoryCamera`. El encuadre ya se calcula
a partir de los límites de la disposición, y estos no crecen en altura, porque el agua y el cauce
quedan por debajo de `h`.

**Comprobación**: el disco mide unas 8 piezas de ancho y 9 filas de fondo (`8·W ≈ 6,9` × `≈ 6,5`
unidades). En 9:19.5 vertical limita el ancho: cada pieza ocupa en torno a 1/9 del ancho del área
segura útil. La prueba de las 6 proporciones (≥ 70 %) se mantiene. La legibilidad la juzga la
revisión manual (FR-027).

**Alternativa descartada**: **cámara más cercana con desplazamiento**: es interacción, fuera de
alcance.
