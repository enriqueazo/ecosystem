# Feature Specification: Territorio hexagonal isométrico

**Feature Branch**: `feat/004-territorio-hexagonal`

**Created**: 2026-11-25

**Status**: Draft

**Input**: User description: "Crear el primer elemento visual del juego que se pueda ver y probar en Unity: un territorio de 16 piezas hexagonales de tierra con césped (disposición 4 × 4), en vista isométrica sobre un fondo liso gris azulado y mate. La escena se revisa en el editor sin Nakama y, en el flujo normal, se muestra tras la conexión de invitado de la spec 003. La disposición se describe por datos. La dirección visual y los recursos gráficos se crean con Claude Design (`/design`). Sin mecánicas, interacción, red ni cambios de servidor."

> **Nivel de detalle técnico**: igual que en las specs 001–003, la pila (Unity 6000.3.29f1 con URP,
> Nakama y contratos JSON) está fijada por ADR (0003, 0005, 0006 y 0008). Esta spec dice *qué* debe
> verse y comprobarse. Los formatos, la estructura de código y los valores exactos se deciden en
> `/speckit-plan` (ver *Decisiones pendientes*).

## Clarifications

### Session 2026-11-25

- Q: ¿Con qué orientación se dibujan los hexágonos? → A: **Vértice arriba** (*pointy-top*). Las
  filas son horizontales y cada fila alterna se desplaza media pieza.
- Q: ¿Qué vista isométrica usa la cámara? → A: **Inclinada frontal**: proyección ortográfica,
  inclinada hacia abajo y sin giro horizontal. El tablero se ve como un bloque casi rectangular en
  escorzo, con las caras laterales delanteras visibles.
- Q: ¿Qué estilo artístico tienen las piezas? → A: **Low-poly 3D estilizado**: prismas
  hexagonales 3D de formas limpias, iluminados por el pipeline del proyecto, con texturas sencillas
  de aspecto pintado a mano (césped arriba, tierra en los lados).
- Q: ¿Cómo se trata la orientación de pantalla, que hoy es rotación automática en las cuatro
  orientaciones (valor por defecto de Unity, no decidido en ninguna spec)? → A: **Se mantiene la
  rotación automática** sin tocar la configuración del proyecto. El territorio se reencuadra y se ve
  entero en vertical y en horizontal.

## Punto de partida

Estado de `main` en `64b2a13`, con las specs 001–003 fusionadas:

- **Cliente Unity** (6000.3.29f1, URP, plantilla 3D):
  - una sola escena en la build, `Bootstrap`, con una cámara en perspectiva y la pantalla de
    conexión provisional (IMGUI);
  - la prueba `BuildScenes_ContainOnlyBootstrapSceneAtIndexZero` exige que sea la única escena
    de la build.
- **Conexión**: el núcleo `Ecosystem.Connection` termina en uno de estos estados:
  - `Connected`, el único compatible para continuar;
  - `Offline`, `UpdateRequired`, `ServerIncompatible`, `AccountError` o `ConfigurationError`.

  Dependencias del cliente: `Presentation → Connection.Nakama → Connection → Contracts`.
- **Pantalla**: rotación automática con las cuatro orientaciones permitidas. Resolución por defecto
  1024 × 768 y espacio de color lineal.
- **Arte**: no hay ningún recurso binario de arte ni audio. La decisión D-08 (Git LFS) sigue abierta
  y su límite es «antes de añadir el primer recurso de arte o audio».
- **Juego**: no hay mecánicas. El juego será en tiempo real, no por turnos, con posibles eventos
  asíncronos aún sin definir.

## User Scenarios & Testing *(mandatory)*

Actores:
- **jugador**: ve el territorio tras conectarse como invitado;
- **desarrollador/a o artista**: revisa y ajusta el aspecto en el editor sin backend;
- **revisor/a**: valida la spec con la revisión manual y las pruebas.

### User Story 1 - Revisar el territorio en el editor sin backend (Priority: P1)

Una persona abre la escena del territorio en el editor de Unity, pulsa *Play* y ve el tablero
completo de 16 piezas hexagonales en vista isométrica, sin arrancar Nakama ni tener configuración
local de conexión.

**Why this priority**: es el primer resultado visible del juego y la base sobre la que se
revisará todo el arte futuro. Sin depender del backend, cualquiera puede iterar el aspecto.

**Independent Test**: en un clon limpio, sin `connection.local.json` y con el entorno local
detenido, abrir la escena del territorio y reproducirla.

**Acceptance Scenarios**:

1. **Given** el proyecto abierto y Nakama detenido, **When** se abre la escena del territorio y se
   pulsa *Play*, **Then** la escena se reproduce sin errores ni avisos en la consola.
2. **Given** la escena en reproducción, **When** se observa la vista de juego, **Then** se ven
   exactamente 16 piezas hexagonales con el vértice hacia arriba, en 4 filas de 4, con cada fila
   alterna desplazada media pieza.
3. **Given** la escena abierta **sin** reproducir, **When** se mira la vista de escena o de juego,
   **Then** el territorio también se ve. Así se revisa el aspecto sin entrar en *Play*.
4. **Given** el tablero visible, **When** se observan las uniones entre piezas vecinas, **Then**
   comparten arista sin huecos, sin solapamientos y sin parpadeo de caras coincidentes.

---

### User Story 2 - Terreno de tierra con césped legible y sereno (Priority: P1)

El jugador reconoce cada pieza como un bloque de tierra cubierto de césped, visto desde arriba en
escorzo, sobre un fondo tranquilo que no compite con el terreno.

**Why this priority**: es el objetivo visual de la spec y fija la dirección artística que heredarán
las siguientes.

**Independent Test**: seguir la revisión visual manual (FR-024) en la vista de juego del editor
con las proporciones de referencia.

**Acceptance Scenarios**:

1. **Given** el territorio visible, **When** se mira la cara superior de cualquier pieza, **Then**
   es verde, con una textura sutil que se lee como césped y no como un color plano.
2. **Given** el territorio visible, **When** se miran las caras laterales que se ven (las
   delanteras de cada pieza y, sobre todo, las del borde frontal del tablero), **Then** muestran
   tierra con textura terrosa, claramente distinta del césped, con un borde de césped reconocible
   en la parte alta.
3. **Given** el territorio visible, **When** se observa el fondo, **Then** es un color liso gris
   azulado, suave y mate, sin degradado, textura, brillo, niebla ni animación.
4. **Given** la cámara del territorio, **When** se inspecciona, **Then** es ortográfica (sin
   distorsión de perspectiva), inclinada hacia abajo y sin giro horizontal. Las filas de
   hexágonos se ven horizontales en pantalla.
5. **Given** la escena reproduciéndose durante 30 s, **When** se observa, **Then** nada se mueve
   ni cambia: ni cámara, ni luz, ni fondo, ni piezas.

---

### User Story 3 - El territorio se ve entero en cualquier orientación y proporción (Priority: P2)

El jugador gira el móvil o usa un dispositivo con otra proporción de pantalla, y el territorio
sigue entero, centrado y legible.

**Why this priority**: el proyecto mantiene la rotación automática (Clarifications). Un tablero
cortado en vertical sería un defecto visible desde el primer día.

**Independent Test**: en la vista de juego del editor, recorrer las seis proporciones de
referencia (SC-004) en reproducción y comprobar el encuadre. La parte automatizable se cubre con
una prueba PlayMode (FR-023).

**Acceptance Scenarios**:

1. **Given** cada proporción de referencia en horizontal (16:9, 19.5:9 y 4:3) y en vertical (9:16,
   9:19.5 y 3:4), **When** se reproduce la escena, **Then** las 16 piezas, con sus caras laterales
   visibles, quedan dentro del encuadre con margen y centradas.
2. **Given** la escena reproduciéndose, **When** cambia la proporción o la orientación, **Then**
   el encuadre se recalcula en ese momento, sin reiniciar la escena.
3. **Given** un dispositivo con muesca o esquinas redondeadas, **When** se muestra el territorio,
   **Then** ninguna pieza queda bajo una zona no segura de la pantalla.

---

### User Story 4 - Ver el territorio tras conectarse como invitado (Priority: P2)

El jugador abre el juego, se conecta como invitado (spec 003) y, al quedar conectado, pasa a ver el
territorio. Si la conexión termina en cualquier otro estado, sigue en la pantalla de conexión.

**Why this priority**: integra el primer elemento visual en el flujo real del cliente. Depende de
US1, pero no cambia la conexión.

**Independent Test**: con el entorno local arrancado y la configuración local, reproducir
`Bootstrap` y comprobar el paso al territorio. Con el entorno detenido, comprobar que no se pasa.

**Acceptance Scenarios**:

1. **Given** el servidor disponible y la configuración local válida, **When** el jugador abre el
   juego y la conexión llega a «conectado», **Then** en ≤ 2 s se muestra el territorio completo.
2. **Given** una conexión que termina en «sin conexión», «actualiza el juego», «servidor
   incompatible», «error de cuenta» o «error de configuración», **When** se llega a ese estado,
   **Then** no se muestra el territorio y la pantalla de conexión sigue igual que en la spec 003,
   «Reintentar» incluido.
3. **Given** el estado «sin conexión», **When** el jugador pulsa «Reintentar» y la conexión llega a
   «conectado», **Then** se muestra el territorio.
4. **Given** el territorio mostrado tras conectar, **When** se revisa la consola, **Then** no hay
   errores ni avisos, y las operaciones de conexión pendientes se cerraron como define la spec 003.

---

### User Story 5 - Disposición del territorio definida por datos (Priority: P3)

Una persona desarrolladora encuentra la disposición del territorio descrita como un conjunto de
posiciones de piezas en un recurso de datos. Una spec futura puede definir otra forma sin cambiar
la lógica que coloca las piezas.

**Why this priority**: lo pide el alcance para no rehacer la base más adelante, pero no cambia lo
que ve el jugador en esta spec.

**Independent Test**: pruebas automatizadas EditMode sobre la disposición inicial y sobre la
colocación (FR-022). Además, en local y sin confirmar el cambio, editar los datos para quitar una
posición y comprobar que la escena muestra 15 piezas.

**Acceptance Scenarios**:

1. **Given** la disposición inicial, **When** se lee, **Then** contiene exactamente 16 posiciones
   distintas que forman 4 filas de 4 columnas.
2. **Given** una disposición con otras posiciones válidas, **When** se carga la escena, **Then** se
   colocan exactamente esas piezas, contiguas, con la misma lógica.
3. **Given** una disposición vacía o con posiciones repetidas, **When** se carga, **Then** se
   rechaza con un error definido que dice qué falla. No se muestra en silencio un tablero vacío o
   con piezas superpuestas.

---

### Edge Cases

- **Rotación durante la reproducción**: el encuadre se recalcula y nunca queda una pieza
  recortada, ni siquiera un fotograma después de que la rotación termine (US3).
- **Proporciones extremas** (móviles muy alargados como 19.5:9 o 9:19.5, tabletas 4:3): el
  tablero cabe entero. En vertical se verá más pequeño, pero sigue siendo legible (SC-004).
- **Vista de juego del editor en *Free Aspect* o redimensionada**: mismo comportamiento que en un
  dispositivo.
- **Escena del territorio abierta directamente con *Play***: no intenta conectar, no lee la
  configuración local y no muestra la pantalla de conexión.
- **Conexión que llega a «conectado» después de reintentos**: el paso al territorio ocurre una
  sola vez.
- **Salir del juego durante el paso al territorio**: no deja errores ni avisos, como en la spec
  003 (FR-017).
- **Pérdida de conexión con el territorio ya visible**: la spec 003 no vigila la conexión mientras
  se usa, porque no hay socket ni llamadas posteriores, y esta spec no lo añade. El territorio
  sigue visible. Vigilarla será cosa de la spec que traiga partidas o eventos en directo.
- **Caras coincidentes entre piezas vecinas**: no producen parpadeo (*z-fighting*) desde el ángulo
  de la cámara.
- **Dispositivos de gama baja**: las texturas tienen un tamaño acotado (FR-014). No se usan
  sombras en tiempo real ni posprocesado que no aporten a esta vista.

## Requirements *(mandatory)*

Convención (constitución): **MUST** es obligatorio; **SHOULD** es una recomendación que se puede
omitir con justificación escrita.

### Functional Requirements

#### Disposición del territorio

- **FR-001**: La disposición del territorio MUST describirse como un conjunto de posiciones de
  piezas en coordenadas de rejilla hexagonal, guardado como **datos** separados del código que las
  coloca. Es un recurso local del cliente, **no** un contrato cliente-servidor: no viaja por red ni
  se valida en el servidor.
- **FR-002**: La disposición inicial MUST tener exactamente **16 posiciones distintas**, en
  **4 filas horizontales de 4 piezas**. Los hexágonos van con el vértice hacia arriba y cada fila
  alterna se desplaza media pieza en horizontal, siempre hacia el mismo lado (rejilla rectangular
  desplazada). El sistema de coordenadas concreto se fija en el plan.
- **FR-003**: Una disposición vacía o con posiciones repetidas MUST rechazarse con un error
  definido que indique la causa. MUST NOT mostrarse en silencio un tablero vacío o con piezas
  superpuestas.
- **FR-004**: La colocación MUST calcular la posición de cada pieza a partir de su coordenada, de
  modo que dos piezas vecinas compartan exactamente una arista, sin huecos ni solapamientos, y dos
  piezas no vecinas no se toquen.
- **FR-005**: La lógica de disposición y colocación MUST estar separada de escenas y componentes,
  para probarla sin cargar escenas, como el núcleo de conexión de la spec 003 (principios IV y
  VI). Su alcance MUST limitarse a lo que necesita esta spec:
  - leer la disposición;
  - validarla;
  - calcular las posiciones y los límites del tablero para el encuadre.

  No incluye vecindad para jugabilidad, búsqueda de caminos, tipos de terreno, alturas variables
  ni generación procedural.

#### Aspecto visual

- **FR-006**: Cada pieza MUST ser un **prisma hexagonal 3D** con el vértice hacia arriba: una cara
  superior y caras laterales de altura visible desde la cámara. Todas las piezas son idénticas en
  esta spec.
- **FR-007**: La cara superior MUST mostrar una cubierta verde de césped con una textura sutil de
  aspecto pintado a mano. Las caras laterales MUST mostrar tierra con textura terrosa (por
  ejemplo, estratos o terrones), con un borde de césped reconocible en la parte alta. Las dos
  superficies MUST distinguirse a simple vista.
- **FR-008**: El estilo MUST ser **low-poly 3D estilizado**:
  - geometría limpia y acabado mate, sin reflejos especulares ni brillos;
  - iluminado con el pipeline del proyecto mediante una luz fija que diferencie la cara superior
    de los laterales.

  No se añaden vegetación, rocas, decoración ni variaciones por pieza.
- **FR-009**: La cámara del territorio MUST ser **ortográfica**, **inclinada hacia abajo** y **sin
  giro horizontal**, de modo que las filas de hexágonos se vean horizontales y las caras laterales
  frontales sean visibles. El ángulo exacto sale de la dirección visual (FR-011) y queda
  documentado. La cámara está fija: sin desplazamiento, zoom ni rotación por el jugador.
- **FR-010**: El fondo MUST ser un **color liso gris azulado, suave y mate**. Su valor exacto sale
  de la dirección visual y queda documentado. MUST NOT tener:
  - cielo (*skybox*), degradado, textura, niebla o viñeta;
  - brillo (*bloom*), animación o cualquier efecto que cambie con el tiempo.
- **FR-011**: La **dirección visual** MUST definirse con Claude Design (`/design`) y aprobarla el
  responsable **antes** de importar las texturas y de fijar los valores visuales definitivos en
  Unity. Hasta esa aprobación, la escena, la malla, los materiales y el prefab MAY usar los
  valores de partida del plan (research.md R-5), y se ajustan a los aprobados al integrarlos. La
  entrega no se da por buena con los valores de partida. Incluye como mínimo:
  - una paleta con los valores exactos del verde del césped, los tonos de la tierra y el gris
    azulado del fondo;
  - las proporciones de la pieza (relación entre ancho y altura del prisma, y altura del borde de
    césped);
  - el ángulo de inclinación de la cámara y la dirección de la luz;
  - una imagen de referencia del tablero 4 × 4 completo en horizontal y en vertical.

  La dirección visual aprobada MUST enlazarse desde la documentación de la spec y resumirse en
  ella: valores de color, proporciones y ángulos.
- **FR-012**: Los **recursos gráficos** de esta spec MUST ser, como mínimo:
  1. textura de césped para la cara superior;
  2. textura de tierra para las caras laterales;
  3. materiales mate del pipeline del proyecto que las usan;
  4. la malla del prisma hexagonal con vértice arriba, con coordenadas de textura que separen la
     cara superior de los laterales;
  5. un prefab de pieza que combine malla y materiales;
  6. la configuración de cámara, luz y color de fondo de la escena del territorio.

  Las texturas se crean a partir de la dirección visual de Claude Design, se exportan a un formato
  de imagen que Unity importe directamente y se ajustan para móvil (FR-014). El plan decide si la
  malla se genera por código o se importa como modelo (P-03).
- **FR-013**: Los recursos MUST guardarse dentro de `client-unity/Assets/_Project/`, en carpetas
  por tipo coherentes con las existentes (`Scenes/`, `Scripts/`, `Tests/`, y una carpeta de arte
  del territorio que fija el plan). Cada recurso MUST tener su `.meta` versionado. MUST NOT
  versionarse cachés, exportaciones intermedias, archivos fuente de herramientas externas ni
  temporales de Claude Design.
- **FR-014**: Las texturas MUST tener un tamaño y una compresión acotados para Android e iOS.
  Los valores los fija el plan, y la imagen no debe verse pixelada ni borrosa en la revisión
  manual. La escena MUST NOT usar sombras en tiempo real ni posprocesado que no aporten a esta
  vista.
- **FR-015**: Antes de añadir el primer recurso binario de arte, MUST resolverse D-08 (Git LFS) con
  su ADR, como exige `docs/decisions/pendientes.md`.

#### Escena del territorio

- **FR-016**: MUST existir una **escena del territorio** propia que se pueda abrir y reproducir
  directamente en el editor:
  - sin Nakama;
  - sin configuración local de conexión;
  - sin pasar por `Bootstrap`;
  - con 0 errores y 0 avisos en la consola.

  El territorio MUST verse también en la vista de escena sin reproducir (US1, escenario 3).
- **FR-017**: La escena del territorio MUST NOT depender de la conexión: no la inicia, no lee su
  configuración y no conoce sus estados. La conexión tampoco depende de los detalles del
  territorio. Las dependencias siguen en una sola dirección y sin ciclos (principio IV), y ninguna
  escena o singleton hace de dependencia global implícita.
- **FR-018**: El encuadre MUST mantener dentro del área segura de la pantalla las 16 piezas
  completas, con sus caras laterales visibles, centradas y con margen, en las seis proporciones
  de referencia (SC-004). MUST recalcularse cuando cambien la proporción o la orientación.
- **FR-019**: La configuración de orientación y resolución del proyecto MUST NOT cambiar: se
  mantiene la rotación automática en las cuatro orientaciones (Clarifications).

#### Integración en el flujo del cliente

- **FR-020**: En el flujo normal, el cliente MUST mostrar el territorio **solo** cuando la conexión
  de la spec 003 llega a `Connected`. En los demás estados, la pantalla de conexión MUST seguir
  comportándose exactamente igual que en la spec 003. El paso ocurre una sola vez por apertura.
- **FR-021**: Al pasar al territorio, las operaciones de conexión pendientes MUST cerrarse como
  define la spec 003 (FR-017 de 003), sin errores ni avisos. `Bootstrap` MUST seguir siendo la
  primera escena de la build. Si la escena del territorio entra en la build, la prueba
  `BuildScenes_ContainOnlyBootstrapSceneAtIndexZero` MUST actualizarse de forma justificada, tal
  como prevé la spec 003 (FR-027), para exigir `Bootstrap` en el índice 0 y la lista exacta de
  escenas.
- **FR-022**: Esta spec MUST NOT añadir RPC, contratos JSON, persistencia ni cambios en
  `server-nakama/`, `shared/`, `deploy/` o PostgreSQL. Tampoco introduce turnos, bucle de
  simulación ni eventos de juego. La vista se construye a partir de la disposición y no asume que
  el juego avance por turnos.

#### Pruebas y documentación

- **FR-023**: MUST añadirse pruebas automatizadas, escritas con TDD donde haya lógica comprobable
  (principio VI):
  - **EditMode** (sin escenas):
    - la disposición inicial tiene 16 posiciones distintas en 4 filas de 4;
    - la colocación hace que cada pareja de vecinas esté a la distancia exacta de arista
      compartida y que ninguna pareja esté más cerca (sin solapamientos);
    - cada pieza interior tiene sus vecinas esperadas (sin huecos);
    - una disposición vacía o con repetidas se rechaza;
    - los límites del tablero contienen todas las piezas.
  - **PlayMode** (sin servidor):
    - la escena del territorio carga sin errores ni avisos y contiene exactamente 16 piezas;
    - la cámara es ortográfica, sin giro horizontal y con fondo de color liso;
    - en cada proporción de referencia, los límites de las 16 piezas quedan dentro del encuadre;
    - con `ECOSYSTEM_NAKAMA_CONFIG=none`, `Bootstrap` **no** pasa al territorio.
  - **Paso a `Connected`**: se prueba en el grupo de integración contra el Nakama local o con un
    doble del servidor. El plan decide cuál.
- **FR-024**: MUST documentarse en `quickstart.md` una **revisión visual manual reproducible** con
  pasos concretos:
  - qué escena abrir;
  - qué proporciones elegir en la vista de juego;
  - qué comprobar en cada una.

  Los criterios de aprobado o fallo cubren:
  - el recuento y el orden de las piezas;
  - las uniones sin huecos ni parpadeo;
  - el césped arriba y la tierra en los laterales;
  - la cámara inclinada sin giro;
  - el fondo liso, mate y sin cambios en 30 s;
  - el encuadre y la legibilidad en vertical y en horizontal;
  - el paso desde `Bootstrap` tras conectar.

  Los resultados de la revisión se registran en la spec con fecha.
- **FR-025**: Las pruebas existentes de las specs 001–003 MUST seguir pasando, salvo el cambio
  justificado de FR-021.
- **FR-026**: `client-unity/README.md` MUST actualizarse:
  - estructura de carpetas;
  - escena del territorio y cómo revisarla sin backend;
  - nuevas pruebas en cada grupo;
  - paquetes, si se añadiera alguno (con su necesidad, principio IV).

  El README raíz MUST actualizar su línea de estado.
- **FR-027**: Todo cambio MUST entregarse en commits atómicos con Conventional Commits en la rama
  `feat/004-territorio-hexagonal`. Ninguna verificación se declara superada sin haberse ejecutado.

### Recomendaciones (SHOULD)

- **R-01**: No añadir paquetes nuevos. La malla, los materiales y la cámara se resuelven con lo que
  ya trae el proyecto (URP).
- **R-02**: Que el prefab de pieza y la paleta permitan cambiar más adelante el aspecto de todas
  las piezas desde un único sitio, sin introducir todavía tipos de terreno.
- **R-03**: Mantener en la dirección visual de Claude Design una nota breve con las decisiones de
  estilo (qué se evita y por qué) para que las specs de arte posteriores la sigan.

### Decisiones pendientes (se resuelven en `/speckit-plan`)

| ID | Decisión | Qué bloquea | Cómo se resuelve | Resolución |
|----|----------|-------------|------------------|------------|
| P-01 | D-08: usar Git LFS o no para los binarios de arte | FR-012, FR-015 | Volumen previsto de texturas y límites del alojamiento; ADR y entrada en *Resueltas* | **Resuelta** (research.md R-1): sin LFS, con umbral de revisión. Se registrará en ADR-0011 (T002) |
| P-02 | Formato y ubicación del recurso de datos de la disposición (recurso de Unity frente a archivo JSON local), y sistema de coordenadas hexagonales | FR-001, FR-002 | El más simple que se pruebe sin escenas y no se confunda con los contratos de `shared/` | **Resuelta** (R-2): ScriptableObject en texto, coordenadas offset *odd-r* |
| P-03 | Malla del prisma: generada por código o modelo importado | FR-012 | Comparar la exactitud de las aristas compartidas (FR-004) y la facilidad de ajustar las proporciones de FR-011 | **Resuelta** (R-3): generada por código, guardada como asset y comprobada por una prueba |
| P-04 | Mecanismo de paso de `Bootstrap` al territorio (cambio de escena o carga aditiva) y lugar de la escena en la build | FR-020, FR-021 | Respetar FR-017 y la cancelación de la spec 003 | **Resuelta** (R-4): carga `Single` una vez en `Connected`; `Territory.unity` en el índice 1 |
| P-05 | Tamaño y compresión de las texturas, y valores de la dirección visual (colores, proporciones, ángulo de cámara y luz) | FR-010, FR-011, FR-014 | Aprobación de la dirección visual (FR-011) | **Parcial** (R-5): 512 px y ASTC 6×6, y valores de partida. Los definitivos, al aprobar el diseño (contracts/visual-direction.md §2) |
| P-06 | Carpeta de arte del territorio dentro de `Assets/_Project/` | FR-013 | Coherencia con la estructura existente del README del cliente | **Resuelta** (plan.md): `Art/Territory/` y `Data/Territory/` |

### Key Entities

- **Disposición del territorio**: conjunto de posiciones de piezas en coordenadas hexagonales. Es
  un dato local del cliente. La inicial tiene 16 posiciones en 4 filas de 4.
- **Pieza de terreno**: prisma hexagonal con el vértice arriba, de tierra con césped. No tiene
  estado, tipo ni comportamiento en esta spec.
- **Dirección visual**: paleta, proporciones, ángulos e imágenes de referencia aprobados en
  Claude Design. Es la fuente de los valores visuales y de las texturas.
- **Encuadre**: parámetros de cámara que garantizan que el tablero completo cabe en el área segura
  de la pantalla actual.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: La escena del territorio se abre y se reproduce con Nakama detenido y sin
  configuración local, con **0 errores y 0 avisos** en la consola, en **10 de 10** reproducciones.
- **SC-002**: Se muestran **exactamente 16** piezas en **4 filas de 4**, en la escena directa y en
  el flujo normal.
- **SC-003**: **100 %** de las parejas de piezas vecinas comparten arista con una diferencia de
  ≤ 0,1 % del ancho de pieza, y **0** parejas se solapan (prueba automatizada). La revisión manual
  no encuentra huecos ni parpadeo visibles.
- **SC-004**: En las **6 proporciones de referencia** (16:9, 19.5:9 y 4:3 en horizontal; 9:16,
  9:19.5 y 3:4 en vertical), las 16 piezas quedan **100 %** dentro del área segura, y el tablero
  ocupa **al menos el 70 %** del ancho o del alto disponible, lo que limite.
- **SC-005**: La revisión visual manual (FR-024) se completa en **≤ 10 minutos** siguiendo solo
  `quickstart.md`, y el responsable aprueba **todos** sus criterios.
- **SC-006**: Con el servidor disponible, el territorio aparece en **≤ 2 s** desde que la conexión
  llega a «conectado». En los otros cinco estados finales de la spec 003, **nunca** aparece.
- **SC-007**: El cambio deja **0** archivos modificados en `server-nakama/`, `shared/`, `deploy/` y
  en la configuración de orientación y resolución del proyecto. Además, **0** cachés, temporales o
  recursos sin `.meta` en Git.

## Assumptions

- Las specs 001–003 están fusionadas y funcionan como quedaron documentadas (`main` en `64b2a13`).
- «Isométrica» se entiende como proyección ortográfica inclinada frontal, no como isometría
  matemática estricta (Clarifications).
- «4 columnas × 4 filas» se entiende como una rejilla rectangular desplazada de hexágonos con el
  vértice arriba, no como un rombo de coordenadas axiales.
- La verificación se hace en el editor de Unity, como en la spec 003. Las builds de Android e iOS
  deben seguir compilando con la nueva escena, pero no se exige revisarlas en un dispositivo
  físico.
- La pantalla de conexión de la spec 003 conserva su aspecto provisional. Darle arte final no
  forma parte de esta spec.
- Claude Design se usa para definir la dirección visual y crear las texturas. El ajuste final de
  materiales y escena se hace en Unity.
- El juego será en tiempo real, pero esta spec solo presenta un territorio estático. Ni impide ni
  anticipa la sincronización o los eventos futuros.

## Out of Scope

- Personajes, enemigos, edificios, vegetación decorativa u otros objetos sobre el terreno.
- Movimiento, selección, toque, cámara controlable, combate, construcción, recursos, progreso o
  cualquier interacción con las piezas.
- Partidas multijugador, sincronización en tiempo real, eventos asíncronos o vigilancia de la
  conexión con el territorio visible.
- Nuevos RPC, contratos JSON, persistencia o cambios en Nakama, PostgreSQL o la CI.
- Variantes de territorio, biomas, tipos de pieza, alturas variables, editor visual o generación
  procedural.
- Arte de la pantalla de conexión, interfaz, sonido o música.
- Cambiar la orientación o la resolución de pantalla del proyecto.
