# Data Model: Territorio hexagonal isométrico

Todos los datos son **locales del cliente**. Nada viaja por red ni se valida en el servidor
(FR-001 y FR-022).

## Convenciones geométricas (núcleo `Ecosystem.Territory`)

- **Unidades**: unidades de mundo de Unity. Circunradio de la pieza `R = 0,5`, así que el ancho es
  `W = √3·R ≈ 0,8660` y la profundidad de vértice a vértice `2R = 1`.
- **Ejes**:
  - `x` crece a la derecha de la pantalla;
  - `y` crece hacia arriba;
  - `z` crece hacia el fondo, alejándose de la cámara.

  El frente del tablero está en `z` negativo.
- **Hexágono con vértice arriba** (*pointy-top*): vértices a 30° + 60°·k en el plano XZ, medidos
  desde +x. Hay un vértice en +z (al fondo) y otro en −z (al frente).
  - Normales de los laterales: 0°, 60°, 120°, 180°, 240° y 300°.
  - Desde la cámara frontal se ven los laterales de **240° y 300°**, es decir, los dos frontales
    diagonales. Los de 0° y 180° quedan de canto.

## HexCell

Celda de la rejilla en coordenadas **offset odd-r**.

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Column` | `int` | Cualquier entero |
| `Row` | `int` | Cualquier entero. Crece hacia el frente del tablero |

- **Igualdad**: por valor.
- **Centro en el mundo**:
  - `x = W · (Column + 0,5 · (Row & 1))`
  - `z = −1,5 · R · Row`
  - `y = 0`, que es la base del prisma.
- **Conversión a axial** (para vecinos): `q = Column − (Row − (Row & 1)) / 2`, `r = Row`.
- **Vecinos**: las seis direcciones axiales `(+1,0) (−1,0) (0,+1) (0,−1) (+1,−1) (−1,+1)`. Dos
  celdas vecinas están a **distancia `W` exacta** entre centros.

## TerritoryLayout

Disposición validada e inmutable.

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Cells` | lista de solo lectura de `HexCell` | Al menos 1 celda y sin repeticiones. El orden del dato se conserva |

**Creación**: `TerritoryLayout.Create(cells)` devuelve un **resultado**, no una excepción
(principio V):

| Resultado | Cuándo | Mensaje (registro de error de la vista) |
|-----------|--------|----------------------------------------|
| `Ok(layout)` | Lista no vacía y sin repetidas | — |
| `Empty` | Lista nula o vacía | «La disposición del territorio está vacía.» |
| `Duplicate(cell)` | La primera celda repetida | «La disposición repite la celda (columna C, fila F).» |

**Derivados**:
- `Bounds(metrics)`: la caja 3D que contiene todas las piezas, con sus vértices y su altura
  `h`. La usa el encuadre.
- `AdjacentPairs()`: el número de parejas de celdas vecinas. Para la disposición inicial 4 × 4
  son **33**: 12 en horizontal y 21 en diagonal (3 saltos de fila × 7).

**Disposición inicial** (`InitialTerritory.asset`): `Column ∈ {0,1,2,3}` × `Row ∈ {0,1,2,3}`,
16 celdas. Las filas 1 y 3 van desplazadas media pieza a la derecha.

## HexMetrics

| Campo | Valor | Origen |
|-------|-------|--------|
| `OuterRadius` (R) | 0,5 | Fijo (R-3) |
| `Height` (h) | 0,35 de partida | Dirección visual (contracts/visual-direction.md) |

`HexMetrics.Default` es la **única fuente** de `R` y `h` en el código. La usan la colocación, los
límites para el encuadre, el generador de la malla y la prueba que comprueba que la malla está al
día.

Funciones puras:
- `Center(cell)`: el centro de la celda en el mundo.
- `Corner(i)`: el vértice `i` de la cara superior.
- `AreNeighbours(a, b)`: si dos celdas son vecinas.

## HexPrismMesh (salida de `HexPrismMeshBuilder`)

| Parte | Vértices | Triángulos | Submalla | Coordenadas de textura |
|-------|----------|------------|----------|------------------------|
| Cara superior | 6 (normal +y) | 4 (abanico) | 0 · césped | Planar: `u = (x/W) + 0,5`, `v = (z/2R) + 0,5` |
| Laterales | 6 × 4 = 24 (normal de cada cara) | 12 | 1 · tierra | Por cara: `u` ∈ [0,1] a lo ancho, `v` ∈ [0,1] de la base (y=0) a la cara superior (y=h) |

- Sin cara inferior.
- Orden de vértices para que las caras exteriores sean las frontales en Unity (horario visto
  desde fuera).
- Total: **30 vértices y 16 triángulos**.
- La malla `Art/Territory/Meshes/HexTile.asset` MUST coincidir con esta salida para la `h`
  aprobada. Lo comprueba una prueba EditMode.

## TerritoryFraming (entrada → salida)

| Entrada | Tipo |
|---------|------|
| `boardCorners` | 8 `Point3` (las esquinas de `Bounds`) |
| `tiltDegrees` | `float` (35 de partida) |
| `screen` | ancho y alto en píxeles |
| `safeArea` | rectángulo en píxeles, contenido en la pantalla |
| `marginFraction` | `float`, 0,06 |

| Salida | Significado |
|--------|-------------|
| `OrthographicSize` | Semialtura visible en unidades de mundo |
| `CameraOffset` (`u`, `v`) | Desplazamiento de la cámara en su plano para centrar el tablero en el área segura |

**Invariante** (probado): proyectadas con la cámara resultante, todas las esquinas quedan dentro
del área segura reducida por el margen, y el tablero ocupa al menos el 70 % de la dimensión que
limita.

## TerritoryLayoutAsset (Unity, `Ecosystem.Territory.View`)

ScriptableObject con un campo serializado `cells`, una lista de `{ int column; int row; }`.
Solo guarda datos. `ToLayout()` llama a `TerritoryLayout.Create`.

## Componentes de escena (sin estado de juego)

| Componente | Estado | Ciclo de vida |
|------------|--------|---------------|
| `TerritoryView` | Referencias al asset de disposición y al prefab de pieza, más las instancias creadas | `[ExecuteAlways]`. Construye en `OnEnable`, destruye en `OnDisable` y reconstruye tras `OnValidate`. Fuera de *Play*, las instancias son `DontSave` |
| `TerritoryCamera` | La cámara, la `TerritoryView` de la que lee los límites, y los últimos ancho, alto y área segura aplicados | `[ExecuteAlways]`. Recalcula solo si cambian el tamaño de pantalla o el área segura |
| `ConnectionScreen` (modificado) | `sceneOnConnected` (ruta) y `_navigated` (bandera) | Carga la escena una vez al terminar en `Connected` |

No hay transiciones de estado de juego. La pieza no tiene estado, tipo ni comportamiento.
