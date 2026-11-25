# Feature Specification: Variantes de terreno y territorio circular

**Feature Branch**: `feat/005-terreno-circular-variantes`

**Created**: 2026-11-25

**Status**: Draft

**Input**: User description: "Ampliar el territorio de la spec 004 para que tenga un contorno
circular formado por hexágonos y añadir variantes visuales de terreno: nieve, arena y agua, además
de piezas de río. La dirección visual y los recursos se crean con Claude Design y se aprueban antes
de integrarlos. Todo se ve en la escena `Territory` existente, sin backend, y el flujo de invitado
de la spec 003 sigue funcionando. Es una ampliación visual: sin mecánicas, red ni cambios de
servidor."

> **Nivel de detalle técnico**: igual que en las specs 001–004, la pila está fijada por ADR (0003,
> 0005, 0006, 0008 y 0011) y el territorio ya existe con una arquitectura concreta (spec 004). Esta
> spec dice *qué* debe verse y comprobarse, y nombra los elementos existentes que amplía. Los
> formatos, la estructura de código y los valores visuales exactos se deciden en `/speckit-plan`
> (ver *Decisiones pendientes*).

## Clarifications

### Session 2026-11-25

- Q: ¿Desde dónde se crea la rama de la spec, si la spec 004 aún no estaba en `main`? → A: **Se
  espera al merge del PR #4** y la rama se crea desde `main` actualizada (`0918fbf`), como exige la
  constitución.
- Q: ¿Qué tamaño tiene el territorio circular y cómo se decide su contorno? → A: Un **disco de
  55 celdas**. Una celda entra si la distancia entre su centro y el de la celda central es como
  máximo **√13 anchos de pieza**. En coordenadas axiales relativas al centro, eso es
  `q² + q·r + r² ≤ 13`. Las 9 filas tienen **3‑6‑7‑8‑7‑8‑7‑6‑3** piezas. Es un disco sobre el
  suelo: con la cámara inclinada a 35° se ve como una isla redonda en escorzo, no como un círculo
  perfecto en pantalla.
- Q: ¿Nieve, arena y agua pueden ocupar cualquier celda, o tienen reglas de vecindad? → A:
  **Cualquier variante puede ir en cualquier celda, sin reglas de adyacencia**, porque no son
  reglas de juego. La escena usa una **composición fija**, guardada como datos, que se define y se
  aprueba en la propuesta de diseño.
- Q: ¿Cómo se representan los ríos y qué piezas necesita el primer ejemplo? → A: Con una **pieza de
  río propia**: base de hierba y un **cauce hundido** que une los puntos medios de dos aristas.
  - El conjunto básico tiene un **tramo recto** (aristas opuestas) y una **curva suave** (aristas
    a 120°). Ambos giran en pasos de 60°.
  - El río **nace en un manantial** y **termina en una celda de agua o en el borde** del
    territorio.
  - No hay curvas cerradas (aristas contiguas) ni bifurcaciones.

## Punto de partida

Estado de `main` en `0918fbf`, con las specs 001–004 fusionadas:

- **Escenas de la build**: `Bootstrap` (índice 0), con la pantalla de conexión de la spec 003, y
  `Territory` (índice 1). Al llegar a `Connected`, `Bootstrap` carga `Territory` una sola vez. La
  prueba `BuildScenes_AreBootstrapThenTerritory` fija esa lista.
- **Territorio** (spec 004):
  - 16 piezas idénticas de tierra con césped, en 4 filas de 4;
  - hexágonos con vértice arriba, en coordenadas offset *odd-r*: las filas impares van desplazadas
    media pieza a la derecha y la fila crece hacia el frente;
  - circunradio `R = 0,5`, altura `h = 0,35` y cámara ortográfica a 35° sin giro;
  - fondo `#8E9BAA`, sin sombras ni posprocesado.
- **Código del territorio**:
  - núcleo sin Unity `Ecosystem.Territory`: `HexCell`, `HexMetrics`, `TerritoryLayout`,
    `HexPrismMeshBuilder` y `TerritoryFraming`;
  - vista `Ecosystem.Territory.View`: `TerritoryLayoutAsset`, `TerritoryView` y
    `TerritoryCamera`.
- **Datos**: la disposición es `Data/Territory/InitialTerritory.asset`, una lista de celdas
  `{column, row}`. Su validación rechaza las disposiciones vacías y las que repiten celdas. Si la
  disposición no es válida, no se crea ninguna pieza y se registra un error `[Territorio] …`.
- **Arte** (`Art/Territory/`):
  - texturas `T_Grass_Top` y `T_Earth_Side` (512 px, ASTC 6×6; unos 0,3 MB en total);
  - materiales URP *Simple Lit* `M_Grass_Top` y `M_Earth_Side`;
  - malla generada `HexTile.asset` y prefab `HexTile.prefab`.

  Los valores aprobados están en `specs/004-territorio-hexagonal/contracts/visual-direction.md`.
- **Binarios**: sin Git LFS. ADR-0011 exige un ADR nuevo antes de confirmar un binario de más de
  5 MB, el primer audio o vídeo, o recursos que lleven el total de `client-unity/Assets/` por
  encima de 50 MB.
- **Pruebas que fijan el tablero 4 × 4**: `TerritorySceneTests`,
  `BootstrapSceneIntegrationTests`, `TerritoryLayoutTests`, `TerritoryLayoutAssetTests`,
  `TerritorySceneAssetTests` y `TerritoryTestGeometry` esperan 16 piezas.

## User Scenarios & Testing *(mandatory)*

Actores:
- **jugador**: ve el territorio tras conectarse como invitado;
- **desarrollador/a o artista**: revisa y ajusta el aspecto en el editor sin backend;
- **revisor/a**: valida la spec con la revisión manual y las pruebas.

### User Story 1 - Territorio circular en la escena existente (Priority: P1)

Una persona abre la escena `Territory` en el editor, con o sin *Play*, y ve un territorio de
contorno redondo formado por 55 piezas hexagonales contiguas, en la misma vista isométrica de la
spec 004, sin Nakama.

**Why this priority**: es la base sobre la que se colocan las variantes y el río. Por sí sola ya
sustituye el tablero cuadrado y se puede revisar, aunque todas las piezas sigan siendo de hierba.

**Independent Test**: con el entorno local detenido y sin `connection.local.json`, abrir
`Territory.unity` y reproducirla. Las pruebas EditMode comprueban la forma exacta de la
disposición, sin escenas.

**Acceptance Scenarios**:

1. **Given** el proyecto abierto y Nakama detenido, **When** se abre `Territory` y se pulsa
   *Play*, **Then** la escena se reproduce con 0 errores y 0 avisos y muestra **exactamente
   55 piezas** en 9 filas de 3‑6‑7‑8‑7‑8‑7‑6‑3.
2. **Given** la disposición inicial, **When** se calcula la distancia de cada celda a la central,
   **Then** las 55 celdas cumplen `q² + q·r + r² ≤ 13` y **ninguna** celda con ese valor ≤ 13
   falta. No hay huecos.
3. **Given** el territorio visible, **When** se mira desde la cámara del juego, **Then** el
   contorno se lee como una isla redonda en escorzo, sin picos, salientes de una sola pieza ni
   piezas sueltas.
4. **Given** la escena abierta **sin** reproducir, **When** se mira la vista de escena o de juego,
   **Then** el territorio completo también se ve, igual que en la spec 004.

---

### User Story 2 - Hierba, nieve, arena y agua distinguibles (Priority: P1)

El jugador distingue a simple vista cuatro tipos de terreno en el territorio, tanto por la cara
superior como por los laterales del borde frontal. La hierba conserva exactamente el aspecto
aprobado en la spec 004.

**Why this priority**: es el objetivo visual principal de la spec y fija cómo se ampliarán los
terrenos más adelante.

**Independent Test**: revisión visual manual (FR-027) en las proporciones de referencia, y pruebas
automáticas de que cada celda muestra los recursos de su variante.

**Acceptance Scenarios**:

1. **Given** la composición de la escena, **When** se observa la vista de juego, **Then** aparecen
   las cuatro variantes y cada una se reconoce por su cara superior: césped verde, nieve
   blanca‑azulada, arena dorada pálida y agua azul‑verdosa.
2. **Given** las celdas del borde frontal, **When** se miran sus laterales visibles, **Then** cada
   variante muestra un lateral propio, distinto de las demás:
   - hierba: tierra con borde de césped (sin cambios);
   - nieve: tierra con borde de nieve;
   - arena: capas de arena o arenisca;
   - agua: lecho con la lámina de agua arriba, más baja que el resto.
3. **Given** una celda de hierba, **When** se compara con la spec 004, **Then** sus texturas,
   colores, proporciones y materiales son los mismos.
4. **Given** una celda de agua junto a celdas de tierra, **When** se observa, **Then** la lámina de
   agua queda por debajo de la cara superior de las vecinas, y el escalón de la orilla se ve sin
   huecos ni solapamientos.
5. **Given** la escena reproduciéndose 30 s, **When** se observa, **Then** nada se mueve ni cambia.
   El agua tampoco se anima.

---

### User Story 3 - Un río continuo formado por piezas conectadas (Priority: P2)

El jugador ve un río que nace en un manantial, recorre varias piezas, gira al menos una vez y
desemboca en el agua, como un único curso continuo.

**Why this priority**: es la pieza nueva más compleja. Depende de US1 y de la variante de agua de
US2.

**Independent Test**: pruebas EditMode de las reglas de conexión, sin escenas, y revisión manual
del río de la composición de la escena.

**Acceptance Scenarios**:

1. **Given** la composición de la escena, **When** se observa el río, **Then** está formado por al
   menos **5 piezas de río**: un manantial, al menos un tramo recto y al menos una curva suave.
   Termina en una celda de agua.
2. **Given** dos piezas de río vecinas conectadas, **When** se mira la arista que comparten,
   **Then** el cauce y el agua del cauce continúan sin escalón, hueco, solapamiento ni cambio de
   anchura.
3. **Given** la última pieza de río y la celda de agua, **When** se observa la desembocadura,
   **Then** el agua del cauce se une a la lámina de la celda de agua al mismo nivel.
4. **Given** el manantial, **When** se observa, **Then** el cauce empieza en una pequeña poza o
   fuente dentro de la pieza. Se lee como un nacimiento intencionado, no como un cauce cortado.
5. **Given** una disposición en la que una salida del cauce da a una celda que no la continúa,
   **When** se carga, **Then** se rechaza con un error que dice la celda y la arista. No se muestra
   ninguna pieza (US5).

---

### User Story 4 - El territorio ampliado se ve entero y en el flujo normal (Priority: P2)

El jugador gira el móvil o usa otra proporción de pantalla y el territorio circular sigue entero,
centrado y legible. Al conectarse como invitado, ve este territorio.

**Why this priority**: conserva lo que la spec 004 ya garantiza con un tablero más grande.

**Independent Test**: pruebas de encuadre en las seis proporciones y con área segura, la prueba de
`Bootstrap` sin configuración y la prueba de integración de paso a `Connected`.

**Acceptance Scenarios**:

1. **Given** cada proporción de referencia (16:9, 19.5:9 y 4:3 en horizontal; 9:16, 9:19.5 y 3:4
   en vertical), **When** se reproduce la escena, **Then** las 55 piezas, con sus laterales
   visibles, quedan dentro del área segura, centradas y con margen.
2. **Given** un dispositivo con muesca, **When** se muestra el territorio, **Then** ninguna pieza
   queda bajo una zona no segura.
3. **Given** el servidor disponible y la configuración local válida, **When** la conexión llega a
   «conectado», **Then** en ≤ 2 s se ve el territorio circular completo con sus variantes y el
   río.
4. **Given** cualquiera de los otros cinco estados finales de la spec 003, **When** se llega a él,
   **Then** no se muestra el territorio y la pantalla de conexión no cambia.

---

### User Story 5 - Disposición, variantes y río definidos por datos y validados (Priority: P3)

Una persona desarrolladora describe en un recurso de datos qué celdas forman el territorio, qué
variante tiene cada una y qué pieza de río va en cada celda de río, con su giro. Si los datos son
incoherentes, recibe un error claro en lugar de una escena a medias.

**Why this priority**: permite cambiar la composición sin tocar la lógica y protege la escena,
pero no añade nada que vea el jugador.

**Independent Test**: pruebas EditMode de validación con disposiciones de prueba, y prueba PlayMode
de que una disposición no válida no crea piezas.

**Acceptance Scenarios**:

1. **Given** una disposición válida distinta de la inicial, **When** se carga, **Then** se colocan
   exactamente esas celdas con sus variantes y piezas de río, con la misma lógica.
2. **Given** una disposición con celdas repetidas, vacía, no conexa, con una variante desconocida,
   con datos de río incompletos o con un río mal conectado, **When** se carga, **Then** se rechaza
   con un error `[Territorio] …` que nombra la causa y la celda. Se crean **0** piezas.
3. **Given** una variante sin recursos visuales asignados, **When** se carga la escena, **Then** se
   registra un error que nombra la variante y se crean 0 piezas.

---

### Edge Cases

- **Río que sale por el borde del territorio**: es válido. El cauce se ve abierto en el lateral de
  la pieza del borde. Si ese lateral es frontal y visible, el cauce se ve cortado limpiamente en el
  lateral, sin caras que falten.
- **Dos ríos o tramos vecinos no conectados entre sí**: es válido. Cada cauce solo llega a sus
  propias aristas, y la arista común es hierba en las dos piezas.
- **Celda de agua en el borde frontal**: su lateral muestra el lecho y la lámina de agua, y no deja
  ver el fondo a través.
- **Celdas de agua vecinas**: las láminas se unen al mismo nivel, sin una línea de borde de pieza
  marcada entre ellas.
- **Agua junto a nieve o arena**: se ve el escalón de la orilla con el lateral propio de la
  variante vecina (borde de nieve o capas de arena).
- **Cauce que gira**: solo hay curvas suaves de 120°. Los datos que pidan una curva cerrada no
  pueden expresarse, porque esa pieza no existe.
- **Río cerrado sobre sí mismo o sin manantial**: cumple las reglas locales de conexión (FR-016),
  así que es válido como dato. La composición de la escena no lo usa.
- **Rotación de pantalla o *Free Aspect***: mismo comportamiento que en la spec 004.
- **Parpadeo entre caras coincidentes**: ni las orillas ni el cauce ni las láminas de agua
  parpadean (*z-fighting*) desde el ángulo de la cámara.
- **Dispositivos de gama baja**: las texturas nuevas tienen el mismo tamaño y la misma compresión
  acotados que las de la spec 004. No hay transparencias, reflejos, sombras en tiempo real ni
  posprocesado.

## Requirements *(mandatory)*

Convención (constitución): **MUST** es obligatorio; **SHOULD** es una recomendación que se puede
omitir con justificación escrita.

### Functional Requirements

#### Territorio circular

- **FR-001**: La disposición inicial MUST sustituirse por un **disco de 55 celdas**: todas las
  celdas cuya distancia axial a la celda central cumple `q² + q·r + r² ≤ 13`, y ninguna más.
  Expresado en offset *odd-r*, con la celda central en `(columna 4, fila 4)`:

  | Fila | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
  |------|---|---|---|---|---|---|---|---|---|
  | Columnas | 3–5 | 1–6 | 1–7 | 0–7 | 1–7 | 0–7 | 1–7 | 1–6 | 3–5 |
  | Piezas | 3 | 6 | 7 | 8 | 7 | 8 | 7 | 6 | 3 |

  Las propiedades derivadas, que se comprueban, son:
  - 138 parejas de celdas vecinas;
  - 24 celdas de borde, con al menos un vecino fuera;
  - 31 celdas interiores.

  Si el plan elige otra celda central u otro origen, la tabla se traslada sin cambiar la forma.
- **FR-002**: El territorio MUST seguir describiéndose como un **conjunto configurable de celdas**
  guardado como datos, igual que en la spec 004. El disco es la disposición inicial, no una regla
  del sistema: la lógica de colocación no calcula círculos ni genera celdas.
- **FR-003**: La validación de una disposición MUST rechazar, además de lo que ya rechaza la
  spec 004 (vacía y celdas repetidas), una disposición **no conexa**: la que tiene alguna celda o
  grupo de celdas sin vecinos dentro del resto. Así se evitan hexágonos aislados no intencionados.
  Los huecos interiores no se rechazan de forma general, porque pueden ser una forma deliberada en
  el futuro. La disposición inicial se comprueba sin huecos con FR-001.
- **FR-004**: La colocación de piezas, el tamaño de la pieza, la cámara (ortográfica, 35°, sin
  giro), la luz y el fondo MUST seguir siendo los de la spec 004.

#### Variantes de terreno

- **FR-005**: MUST existir cuatro **variantes de terreno**: **hierba** (la existente), **nieve**,
  **arena** y **agua**. Cada celda de la disposición MUST tener exactamente una variante o ser una
  pieza de río (FR-012). Las variantes son solo visuales: no tienen reglas de vecindad ni ningún
  efecto de juego.
- **FR-006**: **Hierba** MUST conservar **exactamente** la identidad aprobada en la spec 004. No
  cambian sus texturas, sus valores de paleta, sus proporciones ni la configuración de sus
  materiales.
- **FR-007**: **Nieve**. Es una pieza de tierra con la misma geometría que la de hierba:
  - cara superior: cubierta de nieve blanca con un matiz frío, con una textura sutil de aspecto
    pintado a mano (ondulaciones, ventisca suave);
  - laterales: la misma tierra que la hierba, con un **borde de nieve** en la parte alta en lugar
    del borde de césped, con una altura comparable;
  - la nieve MUST distinguirse del fondo gris azulado y de la arena a simple vista.
- **FR-008**: **Arena**. Es una pieza de la misma geometría:
  - cara superior: arena de tono dorado pálido, con una textura sutil (ondas de viento o grano);
  - laterales: **capas de arena o arenisca** de tonos cálidos claros, sin borde de césped,
    claramente distintas de la tierra marrón de la hierba.
- **FR-009**: **Agua**. Es una pieza cuya **lámina de agua queda por debajo** de la cara superior
  de las piezas de tierra:
  - cara superior: agua azul‑verdosa, opaca y mate, con una textura sutil (ondulación pintada),
    sin reflejos, transparencia ni animación;
  - laterales: un **lecho** (arena húmeda o tierra) con la lámina de agua arriba, sin borde de
    césped ni de nieve;
  - el nivel exacto del agua sale de la dirección visual (FR-019), y MUST ser el mismo en todas
    las celdas de agua y en el cauce de las piezas de río (FR-014);
  - entre celdas de agua vecinas no se marca borde de pieza.
- **FR-010**: Las cuatro variantes MUST mantener el estilo de la spec 004:
  - low-poly 3D estilizado, geometría limpia y acabado mate;
  - sin brillos especulares, contornos negros, saturación alta ni ruido fuerte;
  - iluminadas por la misma luz fija;
  - sin decoración ni variaciones aleatorias por pieza.
- **FR-011**: Qué recursos visuales usa cada variante MUST configurarse mediante **datos**: un
  catálogo de variante a recursos. La escena no MUST codificar variantes en su lógica. Añadir una
  variante más adelante no MUST requerir cambiar la lógica de colocación.

#### Piezas de río

- **FR-012**: MUST existir tres **piezas de río**. Todas tienen la base y los bordes de hierba y un
  **cauce hundido** con agua al nivel de FR-009:

  | Pieza | Aristas abiertas | Giros distintos |
  |-------|------------------|-----------------|
  | **Manantial** | 1. El cauce nace en una poza dentro de la pieza | 6 |
  | **Tramo recto** | 2, opuestas | 3 (6 giros, simétricos dos a dos) |
  | **Curva suave** | 2, separadas por una arista (120°) | 6 |

  Cada pieza de río se describe con su tipo y un **giro** de 0 a 5 pasos de 60°. Las aristas se
  nombran por su dirección en la rejilla: **E** y **O**; **NE** y **NO**, hacia el fondo; **SE**
  y **SO**, hacia el frente. El plan fija qué aristas abre cada pieza en el giro 0 y el sentido
  del giro. No hay curvas cerradas (aristas contiguas), bifurcaciones ni cruces.
- **FR-013**: El cauce MUST cortar la arista abierta en su **punto medio**, con la misma anchura y
  profundidad en todas las piezas y giros. Así, dos aristas abiertas enfrentadas coinciden
  exactamente.
- **FR-014**: El agua del cauce MUST estar a la **misma altura** que la lámina de las celdas de
  agua (FR-009), para que la desembocadura no tenga escalón.
- **FR-015**: Los laterales de una pieza de río MUST ser los de la hierba (tierra con borde de
  césped). En una arista abierta, el lateral MUST mostrar el corte del cauce, sin caras que falten
  ni huecos a través de los que se vea el fondo.
- **FR-016**: **Reglas de conexión**. Son locales y se comprueban sin Unity. Para cada arista
  abierta de cada pieza de río, la celda vecina por esa arista MUST ser una de estas:
  1. una pieza de río con la **arista enfrentada abierta** (conexión recíproca);
  2. una celda de **agua** (desembocadura);
  3. **fuera del territorio** (salida por el borde).

  Si la vecina es de hierba, nieve o arena, o es una pieza de río con esa arista cerrada, la
  disposición MUST rechazarse con un error que nombre la celda, la arista y la causa, por ejemplo
  «extremo de río sin continuación». Una arista **cerrada** junto a una pieza de río con la arista
  enfrentada abierta MUST rechazarse por la misma regla vista desde la otra pieza. No se validan
  flujo, sentido, pendiente ni longitud.

#### Composición de la escena

- **FR-017**: La disposición inicial MUST llevar una **composición fija** definida como datos y
  aprobada en la dirección visual (FR-019). Como mínimo:
  - las cuatro variantes con **al menos 4 celdas** cada una;
  - cada variante con al menos una celda en el **borde frontal**, es decir, con su vecino SO o SE
    fuera del territorio, para que sus laterales se vean;
  - **un río** de al menos 5 piezas: un manantial, al menos un tramo recto y al menos una curva
    suave, que termina en una celda de agua;
  - el río y el agua visibles en la captura de 16:9 de la revisión C6 (no ocultos tras el borde
    posterior).

  Dónde va cada zona lo decide la propuesta de diseño, no una regla.
- **FR-018**: La escena `Territory` existente MUST ampliarse; no se crea una segunda escena del
  territorio. MUST seguir abriéndose y reproduciéndose sin Nakama, sin configuración local y sin
  pasar por `Bootstrap`, con 0 errores y 0 avisos, y verse también sin *Play*.
  - La lista cerrada de componentes del contrato `territory-scene.md` de la spec 004 MAY ampliarse
    solo con lo imprescindible para variantes y río.
  - La lista sigue sin incluir animación, partículas, audio, posprocesado ni componentes de
    conexión.

#### Dirección visual y recursos

- **FR-019**: La **dirección visual** de las variantes y del río MUST desarrollarse con Claude
  Design (la herramienta de artefactos de Claude, intención *design*), como en la spec 004. El
  responsable la aprueba **antes** de importar las texturas definitivas. Incluye como mínimo:
  - la paleta en hex sRGB:
    - nieve: base, luz, sombra y borde de nieve del lateral;
    - arena: base, luz, sombra y capas del lateral;
    - agua: base, luz, sombra y lecho;
    - cauce y orilla del río;
  - el nivel de la lámina de agua y del cauce, como fracción de `h`;
  - la anchura y la profundidad del cauce;
  - la vista de cada variante aislada y de las tres piezas de río;
  - la **composición fija** (FR-017) y referencias del territorio completo en **16:9 horizontal**
    y **9:19.5 vertical**, con el fondo aprobado;
  - notas de estilo: qué se evita y por qué, como continuación de las de la spec 004.

  Hasta la aprobación MAY usarse valores de partida del plan, pero la entrega no se da por buena
  con ellos.
- **FR-020**: Los valores aprobados MUST registrarse en un **contrato de dirección visual** de esta
  spec (`contracts/visual-direction.md`), que amplía el de la spec 004 sin modificar sus valores de
  hierba, enlaza al artefacto de diseño y lleva la fecha de aprobación.
- **FR-021**: Los **recursos finales** que entran en Unity MUST ser, como mínimo:
  1. textura de cara superior y textura de lateral de **nieve**, **arena** y **agua**;
  2. para el cauce del río, la textura de tierra existente. Si el diseño aprobado pide una textura
     propia de cauce u orilla, se añade como recurso con su prueba de importación;
  3. materiales mate del pipeline del proyecto para cada textura nueva;
  4. la geometría de la pieza de agua (lámina baja) y de las tres piezas de río, con el cauce
     exacto de FR-013;
  5. un prefab o recurso equivalente por variante y por pieza de río;
  6. el catálogo de variantes (FR-011);
  7. la disposición inicial con la composición (FR-017).

  El contrato de FR-020 lista cada archivo con su ruta y su configuración de importación. Las
  texturas se generan a partir de la dirección aprobada y son los mismos archivos que se revisan
  en el artefacto.
- **FR-022**: Los recursos MUST guardarse en `client-unity/Assets/_Project/`, en `Art/Territory/` y
  `Data/Territory/`, con la organización por tipo de la spec 004. Cada recurso MUST tener su
  `.meta` versionado, y los `.meta` existentes MUST conservarse (los GUID no cambian). MUST NOT
  versionarse SVG, capturas, exportaciones intermedias, scripts de generación ni temporales del
  diseño.
- **FR-023**: Las texturas nuevas MUST usar el tamaño y la compresión de la spec 004 (512 px y ASTC
  6×6, o lo que fije el plan con justificación). Antes de confirmarlas MUST comprobarse el umbral
  de ADR-0011: ningún binario de más de 5 MB y menos de 50 MB en total en
  `client-unity/Assets/`. Si se cruzara, primero un ADR nuevo.

#### Integración y alcance

- **FR-024**: El flujo de `Bootstrap` MUST seguir igual que en la spec 004: el territorio se
  carga una sola vez y solo en `Connected`. No cambian la conexión, la orientación ni la
  resolución del proyecto, ni la lista de escenas de la build.
- **FR-025**: Esta spec MUST NOT añadir RPC, contratos JSON, persistencia ni cambios en
  `server-nakama/`, `shared/`, `deploy/`, PostgreSQL o la CI. Tampoco añade mecánicas,
  interacción, simulación ni animación del agua, generación procedural, editor de territorios ni
  selección de mapas.

#### Pruebas y documentación

- **FR-026**: MUST añadirse pruebas automatizadas, escritas con TDD donde haya lógica comprobable
  (principio VI):
  - **EditMode, núcleo sin Unity**:
    - la disposición inicial tiene exactamente las 55 celdas de FR-001, 138 parejas vecinas y
      24 celdas de borde;
    - ninguna celda que cumpla `≤ 13` falta (sin huecos);
    - una disposición no conexa se rechaza (FR-003), y las vacías y repetidas se siguen
      rechazando;
    - aristas abiertas de cada pieza de río en cada giro;
    - aceptación de conexiones recíprocas, desembocadura en agua y salida por el borde;
    - rechazo de: extremo sin continuación, vecina de hierba, nieve o arena, conexión no
      recíproca, giro fuera de 0–5, tipo o variante desconocidos y datos de río en una celda sin
      variante de río;
    - la composición inicial cumple los mínimos de FR-017;
    - la geometría del cauce corta cada arista abierta en su punto medio con la misma anchura,
      y el nivel del agua del cauce es igual al de la lámina de agua (con una tolerancia de
      ≤ 0,1 % de `W`);
    - los recursos generados (mallas) están al día con las medidas, como `HexTileMeshAssetTests`.
  - **EditMode, recursos**: cada variante del catálogo tiene sus recursos asignados; las texturas
    nuevas tienen el tamaño y la compresión de FR-023; los recursos de hierba no cambian.
  - **PlayMode, sin servidor**:
    - `Territory` carga con 0 errores y 0 avisos y crea 55 piezas en las posiciones de la
      disposición, cada una con los recursos de su variante o pieza de río;
    - el encuadre contiene el territorio en las seis proporciones y con área segura;
    - una disposición no válida crea 0 piezas y registra `[Territorio] …`;
    - la escena no contiene componentes fuera de la lista cerrada (FR-018).
  - **Integración**: el paso a `Connected` muestra el territorio con 55 piezas en ≤ 2 s.
- **FR-027**: MUST documentarse en `quickstart.md` una **revisión visual manual reproducible**,
  con la estructura de la de la spec 004: proporciones, simulador con muesca, una tabla de
  criterios y el registro de resultados con fecha y persona. Los criterios cubren como mínimo:
  - el contorno redondo en escorzo, sin picos ni piezas sueltas;
  - el recuento de 55 piezas;
  - las cuatro variantes reconocibles por la cara superior y por los laterales del borde
    frontal;
  - la hierba idéntica a la de la spec 004 (comparada con su referencia aprobada);
  - el agua más baja que la tierra, con orillas sin huecos;
  - el río continuo desde el manantial hasta el agua, sin escalones, huecos, cambios de anchura
    ni extremos cortados;
  - sin parpadeo en orillas ni cauce;
  - nada se mueve en 30 s;
  - el encuadre en las seis proporciones y con muesca;
  - la coincidencia con las referencias aprobadas del artefacto.
- **FR-028**: Las pruebas existentes de las specs 001–004 MUST seguir pasando. Las que fijan el
  tablero de 16 piezas MUST actualizarse a la nueva disposición de forma justificada, sin reducir
  lo que comprueban.
- **FR-029**: MUST actualizarse:
  - `client-unity/README.md`: estado, estructura, sección *Territorio* (forma, variantes, río,
    catálogo y cómo cambiar la composición) y las pruebas nuevas de cada grupo;
  - el README raíz: su línea de estado.
- **FR-030**: Todo cambio MUST entregarse en commits atómicos con Conventional Commits en la rama
  `feat/005-terreno-circular-variantes`. Ninguna verificación se declara superada sin haberse
  ejecutado.

### Recomendaciones (SHOULD)

- **R-01**: No añadir paquetes nuevos; basta con lo que usa la spec 004 (URP y el núcleo propio).
- **R-02**: Reutilizar la geometría del prisma de la spec 004 para nieve y arena, de modo que solo
  cambien los materiales.
- **R-03**: Que el catálogo de variantes sea el único sitio donde se asocian variantes y recursos.

### Decisiones pendientes (se resuelven en `/speckit-plan`)

| ID | Decisión | Qué bloquea | Cómo se resuelve | Resolución |
|----|----------|-------------|------------------|------------|
| P-01 | Cómo se amplía el dato de la disposición con la variante y la pieza de río (mismo recurso o recurso asociado) y cómo se migra `InitialTerritory.asset` | FR-002, FR-011, FR-017 | El más simple que se valide sin Unity y conserve el GUID del recurso existente | **Resuelta** (research.md R-1): mismo asset y GUID; cada celda lleva `terrain`, `river` y `rotation` con valores por defecto compatibles |
| P-02 | Si la lámina de agua y el cauce se generan por código, como `HexTile`, o se importan como modelo | FR-009, FR-013, FR-021 | Exactitud de FR-013 y FR-014 comprobable por prueba | **Resuelta** (R-2): generadas por código, guardadas como assets y comprobadas por prueba; 15 mallas de río ya giradas |
| P-03 | Qué aristas abre cada pieza en el giro 0 y el sentido del giro | FR-012, FR-016 | Convención coherente con `HexCell` y data-model de la spec 004 | **Resuelta** (R-3): aristas en el orden de `HexCell.Neighbours()` (E, NE, NO, O, SO, SE), giro antihorario visto desde arriba |
| P-04 | Cómo representa la vista la variante de cada celda (prefab por variante o prefab común con materiales del catálogo) | FR-011, FR-018 | Menor cambio sobre `TerritoryView` y sin cambiar el aspecto de la hierba | **Resuelta** (R-5): `TerrainCatalogAsset` con un prefab por variante y mallas de río; la hierba conserva `HexTile.prefab` |
| P-05 | Valores visuales de partida (paletas, nivel del agua, anchura y profundidad del cauce) | FR-019 | Valores de partida del plan; los definitivos, al aprobar el diseño | **Parcial** (R-7): valores de partida; los definitivos, al aprobar el diseño (contracts/visual-direction.md §2) |

### Key Entities

- **Disposición del territorio**: conjunto de celdas en coordenadas hexagonales. Cada celda tiene
  una variante de terreno, o una pieza de río con su giro. La inicial es el disco de 55 celdas con
  la composición aprobada. Es un dato local del cliente, no un contrato de red.
- **Variante de terreno**: hierba, nieve, arena o agua. Es solo visual: no tiene estado ni
  comportamiento.
- **Pieza de río**: manantial, tramo recto o curva suave, más un giro de 0 a 5. Determina qué
  aristas de la celda abre su cauce.
- **Catálogo de variantes**: asocia cada variante y cada pieza de río con sus recursos visuales.
- **Dirección visual 005**: paletas, niveles, medidas del cauce, composición y referencias
  aprobadas en Claude Design. Amplía la de la spec 004 sin modificarla.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: La escena `Territory` se reproduce con Nakama detenido y sin configuración local, con
  **0 errores y 0 avisos**, en **10 de 10** reproducciones. Muestra **exactamente 55** piezas, en
  la escena directa y en el flujo normal.
- **SC-002**: El **100 %** de las celdas de la disposición inicial cumplen el criterio del disco, y
  **0** celdas que lo cumplen faltan. El territorio es **un único** grupo conexo, y la revisión
  manual no encuentra picos ni piezas sueltas.
- **SC-003**: En la revisión manual, la persona revisora identifica correctamente la variante de
  **8 de 8** celdas señaladas al azar (dos por variante), por la cara superior y por el lateral
  frontal cuando está en el borde.
- **SC-004**: La hierba no cambia: **0** cambios en sus texturas, materiales, malla y valores
  aprobados, comprobado con el historial de Git y una prueba.
- **SC-005**: En el río de la escena, el **100 %** de las aristas abiertas cumplen las reglas de
  conexión, que verifica una prueba. Las discrepancias de posición, anchura o nivel en cada unión
  son de **≤ 0,1 %** del ancho de pieza. La revisión manual no encuentra escalones, huecos ni
  extremos cortados.
- **SC-006**: Cada tipo de dato no válido de US5 produce un error que nombra la causa, y **0**
  piezas en escena. Lo verifica una prueba por cada tipo de error.
- **SC-007**: En las **6 proporciones de referencia** y con muesca, las 55 piezas quedan **100 %**
  dentro del área segura, y el territorio ocupa **al menos el 70 %** del ancho o del alto
  disponible.
- **SC-008**: Con el servidor disponible, el territorio aparece en **≤ 2 s** desde «conectado» y
  **nunca** aparece en los otros cinco estados finales.
- **SC-009**: La revisión visual manual se completa en **≤ 15 minutos** siguiendo solo
  `quickstart.md`, y el responsable aprueba **todos** sus criterios.
- **SC-010**: El cambio deja **0** archivos modificados en `server-nakama/`, `shared/`, `deploy/`,
  en la CI y en la configuración de orientación, resolución y escenas de la build. Hay **0**
  recursos sin `.meta`, **0** temporales o fuentes del diseño en Git, **0** GUID cambiados, y los
  binarios quedan por debajo de los umbrales de ADR-0011.

## Assumptions

- Las specs 001–004 están fusionadas y funcionan como quedaron documentadas (`main` en `0918fbf`).
- La skill `design` del pedido se entiende como **Claude Design**: la herramienta de artefactos de
  Claude con intención *design*, la misma que se usó en la spec 004. No hay ninguna otra skill con
  ese nombre en el entorno.
- «Circular» significa un disco en el plano del suelo visto en escorzo, no una forma estirada para
  que salga redonda en pantalla (Clarifications).
- Las piezas de río tienen siempre base de hierba. Los ríos sobre nieve o arena quedan para una
  spec futura.
- Un territorio de 55 piezas cabe en las proporciones verticales con piezas más pequeñas que las
  del tablero de 16. Se considera legible si pasa la revisión manual (FR-027).
- La verificación se hace en el editor de Unity, como en la spec 004. Las builds de Android e iOS
  deben seguir compilando, pero no se exige revisarlas en un dispositivo físico.
- El volumen de binarios nuevo es de unas 7 texturas de 512 px, del orden de 1–2 MB en total, muy
  por debajo de los umbrales de ADR-0011.

## Out of Scope

- Mecánicas de juego, construcción, combate, recursos, ocupación o efectos de clima.
- Movimiento de unidades o personajes, y cualquier interacción con las piezas.
- Flujo, sentido o caudal del río; navegación; puentes; simulación o animación del agua; sonido.
- Curvas cerradas, bifurcaciones, cruces o ríos sobre nieve o arena.
- Reglas de vecindad entre terrenos, transiciones suaves entre variantes o decoración.
- Generación procedural, editor de territorios o selección de mapas por el jugador.
- Cambios en Nakama, PostgreSQL, contratos cliente-servidor, CI u orientación de pantalla.
- Publicación en tiendas.

## Revisión visual

Revisión manual de [quickstart.md → C](quickstart.md#c-revisión-visual-manual-fr-027) (FR-027), en el
editor de Unity con las 6 proporciones y la vista *Simulator* con muesca. La hace el responsable; un ❌
bloquea la entrega. Incluye los pasos B2 y B3 de quickstart.md, que necesitan el editor con interfaz.

**Estado**: **aprobada** por el responsable el 2026-11-25 (T057 y T064): todos los criterios ✅.

Comprobado antes con capturas en *Play* (registro de tasks.md, T020, T037 y T053), sin sustituir la
revisión del responsable:
- C1: el contorno es redondo, sin huecos ni piezas sueltas. Las filas 3 y 5 sobresalen media pieza
  respecto a la 4 a izquierda y derecha, lo que deja una pequeña muesca propia del disco acordado
  (K = 13).
- C2–C5, C8 y C13: las variantes y el agua coinciden con las referencias aprobadas.
- C6 y C7: el cauce es continuo de una pieza a otra, sin escalones ni huecos, y no hay parpadeo.

| Fecha | Persona | Criterios | Resultado | Tiempo |
|-------|---------|-----------|-----------|--------|
| 2026-11-25 | Responsable del proyecto | C1–C13 en las 6 proporciones y en *Simulator* con muesca, B2 y B3 | Todo ✅ | — (no indicado) |
