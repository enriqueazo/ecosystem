# Research: Territorio hexagonal isométrico

Decisiones de la fase 0. Resuelven las decisiones pendientes P-01 a P-06 de la spec y las dudas
técnicas del plan. Formato: decisión, motivo y alternativas descartadas.

## R-1 · Git LFS (D-08, P-01)

**Decisión**: **no usar Git LFS** por ahora. Los binarios de arte se versionan en Git normal con
`.gitattributes` `binary`, que ya cubre `*.png`. Se registra en **ADR-0011**, y D-08 pasa a
*Resueltas* en el mismo commit.

**Motivo**:
- Esta spec añade dos texturas PNG de 512 × 512, menos de 1 MB entre las dos. El resto del arte
  es texto YAML de Unity: materiales, prefab, malla y escena.
- LFS obliga a que todo clon y toda CI tenga `git-lfs` instalado y configurado. Además, consume la
  cuota de almacenamiento y transferencia del alojamiento, y un clon sin LFS recibe punteros en
  lugar de imágenes, lo que rompe Unity de forma confusa. Es un coste que hoy no compensa.
- Migrar más adelante es posible (`git lfs migrate`), aunque reescribe historia. Por eso el ADR
  fija un **umbral de revisión** antes de llegar a ese punto.

**Umbral de revisión** (en ADR-0011): se vuelve a decidir antes de añadir:
- el primer archivo binario de más de **5 MB**;
- el primer audio o vídeo;
- recursos que lleven el total de binarios de `client-unity/Assets/` por encima de **50 MB**.

**Alternativas descartadas**:
- **LFS desde ya**: el coste de herramientas y cuota es desproporcionado para menos de 1 MB.
- **Dejar D-08 abierta**: la constitución y `pendientes.md` exigen resolverla antes del primer
  recurso de arte (FR-015).

## R-2 · Datos de la disposición y coordenadas (P-02)

**Decisión**:
- **Recurso**: un **ScriptableObject** `TerritoryLayoutAsset`, serializado como texto YAML
  (`ForceText`), con una lista de celdas `{column, row}`.
  - La disposición inicial vive en `Assets/_Project/Data/Territory/InitialTerritory.asset`.
  - El asset solo guarda datos. Al leerlo, se convierte en un `TerritoryLayout` del núcleo, que
    lo valida.
- **Coordenadas**: **offset «odd-r»** para hexágonos con vértice arriba. `row` crece hacia el
  frente del tablero (abajo en pantalla) y las filas impares se desplazan media pieza a la derecha.
  - Las 16 celdas iniciales son `column ∈ {0..3}` × `row ∈ {0..3}`.
  - Para calcular vecinos, el núcleo convierte a coordenadas axiales.

**Motivo**:
- El asset se edita y se revisa como texto, y no necesita parser.
- No se confunde con los contratos de red de `shared/` (FR-001).
- El inspector por defecto de Unity basta para cambiar posiciones a mano. No se construye ningún
  editor de territorios.
- Con *odd-r*, «4 columnas × 4 filas» se expresa literalmente, sin traducir desde un rombo axial.

**Alternativas descartadas**:
- **JSON local** en `Resources/` o `StreamingAssets/`: exige parser y carga asíncrona en móvil, y
  se parece a un contrato sin serlo.
- **Coordenadas axiales o cúbicas en el dato**: una rejilla rectangular deja de leerse
  literalmente (las filas se «tuercen»).
- **Posiciones en el mundo**: mezclan datos y geometría e impiden validar la contigüidad.

## R-3 · Malla del prisma (P-03)

**Decisión**:
- **Generación**: la geometría se **genera por código** en el núcleo (`HexPrismMeshBuilder`, sin
  Unity) con las mismas fórmulas que la colocación. Un comando del editor la **guarda como asset**,
  `Art/Territory/Meshes/HexTile.asset`, en texto YAML.
- **Comprobación**: una prueba EditMode verifica que el asset guardado coincide con la salida del
  generador. Si no coincide, falla. Es la misma idea que el C# generado de la spec 003.
- **Forma**:
  - prisma con vértice arriba, circunradio `R = 0,5` unidades (ancho `√3·R ≈ 0,866`), altura `h`
    según la dirección visual (R-5);
  - **sombreado plano** (vértices duplicados por cara, normal de cara);
  - dos submallas: `0` = cara superior (césped) y `1` = seis laterales (tierra);
  - **sin cara inferior**, porque nunca se ve.
- **Coordenadas de textura**:
  - cara superior: planar en XZ sobre el cuadrado que la contiene;
  - laterales: `u ∈ [0,1]` por cara y `v ∈ [0,1]` de abajo arriba. El borde de césped está en la
    banda alta de la textura de tierra.

**Motivo**:
- Las aristas compartidas son exactas por construcción (FR-004) y se prueban con la misma
  matemática.
- Las proporciones de la dirección visual se cambian con un parámetro, sin herramienta 3D externa
  ni binario FBX.
- El prefab referencia una malla real, así que se ve en el inspector y en la escena (FR-012).

**Parpadeo (z-fighting)**: las caras laterales que coinciden entre dos vecinas tienen normales
opuestas. El *backface culling* elimina una, y la otra queda dentro del prisma vecino, oculta por
su cara superior. Las caras superiores están a la misma altura y no se solapan.

**Alternativas descartadas**:
- **Modelo FBX** hecho en una herramienta 3D: añade un binario y una herramienta externa, y la
  exactitud depende del modelado.
- **Generar la malla en tiempo de ejecución sin asset**: el prefab no sería autosuficiente
  (FR-012.5).
- **Biseles o chaflanes en la geometría**: más vértices y casos para una mejora que la textura ya
  da (R-5, borde de pieza).

## R-4 · Paso de `Bootstrap` al territorio (P-04)

**Decisión**:
- **Cuándo**: al terminar `ConnectionFlow.RunAsync` en `Connected`, `ConnectionScreen` carga la
  escena del territorio con `SceneManager.LoadSceneAsync(..., LoadSceneMode.Single)`.
- **Qué escena**: la indica un campo serializado `sceneOnConnected`, con la ruta de la escena,
  asignado en `Bootstrap`. `Presentation` **no referencia** los ensamblados del territorio, solo
  conoce una ruta de escena de la build.
- **Una sola vez**: una bandera impide cargar dos veces, también si «Reintentar» lleva después a
  `Connected`.
- **Build**: `Territory.unity` entra en la build en el **índice 1**. `Bootstrap` sigue en el 0.
  `ProjectConfigurationTests.BuildScenes_ContainOnlyBootstrapSceneAtIndexZero` pasa a
  `BuildScenes_AreBootstrapThenTerritory`, con la lista exacta y justificado en FR-021.

**Motivo**:
- En `Connected`, `RunAsync` ya ha terminado y no queda ninguna operación de red pendiente (spec
  003 R-6/FR-017). El cambio de escena destruye `ConnectionScreen` y su
  `destroyCancellationToken` cubre cualquier carrera.
- No hay socket ni sesión en directo que mantener viva, así que no hace falta una carga aditiva
  (*Out of Scope*: vigilar la conexión).

**Alternativas descartadas**:
- **Carga aditiva con `Bootstrap` persistente**: deja la conexión viva sin uso y mezcla
  cámaras y luces de dos escenas.
- **Territorio dentro de `Bootstrap`**: incumple FR-016, que pide revisar la escena sin conexión.
- **Un servicio de navegación global**: no tiene más consumidores; es una abstracción sin valor
  (principio IV).

## R-5 · Dirección visual con Claude Design y valores (P-05)

**Decisión**: en la implementación, la dirección visual se crea como **artefacto de diseño de
Claude** (la herramienta de artefactos con intención *design*, que en esta sesión sustituye a
`/design`). Debe contener lo que fija [contracts/visual-direction.md](contracts/visual-direction.md):
- paleta con valores hexadecimales en sRGB;
- proporciones de la pieza;
- ángulo de cámara y dirección de la luz;
- vistas de referencia del tablero 4 × 4 en 16:9 y 9:19.5;
- **las dos texturas**, dibujadas en el propio artefacto, con un botón «Descargar PNG» a
  512 × 512 que las exporta.

El responsable aprueba el artefacto antes de importar nada. Su enlace y los valores aprobados se
copian en `contracts/visual-direction.md`.

**Valores de partida** (punto de arranque para el diseño, no definitivos):

| Parámetro | Valor inicial |
|-----------|---------------|
| Césped (base) | `#6FA34A` |
| Tierra (base) | `#8A5A3B` |
| Fondo | `#8E9BAA`, gris azulado suave |
| Altura del prisma `h` | `0,35` (≈ 40 % del ancho) |
| Borde de césped en los laterales | 20 % superior de la textura de tierra |
| Inclinación de la cámara | 35° hacia abajo, giro 0° |
| Luz direccional | Rotación (50°, −30°, 0°), blanca cálida, intensidad 1, **sin sombras** |
| Luz ambiente | Color plano (no cielo) |

**Legibilidad de cada pieza**: si los 16 céspedes fueran idénticos y contiguos, se leerían como
una sola superficie. Para evitarlo, la textura de césped lleva una **banda de borde hexagonal**
sutil (un poco más oscura o más clara) alineada con la proyección planar de R-3, de modo que
cada hexágono se distinga. Su intensidad la fija el diseño.

**Importación en Unity** (texturas):
- tamaño máximo 512, sRGB, *mipmaps* activos y filtro trilineal;
- compresión **ASTC 6×6** en Android e iOS (formato por defecto de Unity 6 en las dos
  plataformas; se comprueba en el inspector);
- césped: *wrap* `Clamp`; tierra: `Repeat` en U y `Clamp` en V, así que tiene que repetirse sin
  costura en horizontal.

**Materiales**:
- shader `Universal Render Pipeline/Simple Lit`, con *Specular Highlights* desactivado
  (acabado mate);
- *Receive Shadows* desactivado;
- un material por textura.

**Escena**:
- cámara ortográfica, `Clear Flags = Solid Color` con el fondo aprobado y *Post Processing*
  desactivado;
- `RenderSettings`: sin *skybox*, sin niebla y ambiente en color plano;
- sin datos de iluminación horneada.

Así no se aplica *bloom* ni ningún efecto del `DefaultVolumeProfile` (FR-010 y FR-014).

**Alternativas descartadas**:
- **Texturas generadas por código** (ruido): no responden a la petición de crear el arte con
  Claude Design.
- **SVG como fuente versionada**: FR-013 prohíbe versionar archivos fuente de herramientas
  externas. La fuente es el artefacto, y en el repositorio solo entran los PNG.
- **Shader `Lit`** con *smoothness* 0: válido, pero `Simple Lit` es más barato en móvil y basta
  sin especular.

## R-6 · Encuadre en cualquier proporción y área segura

**Decisión**:
- **Cálculo**: lo hace una función pura del núcleo, `TerritoryFraming`. Recibe:
  - las esquinas 3D de la caja que contiene el tablero (las 16 piezas con su altura);
  - la inclinación de la cámara;
  - el tamaño de la pantalla en píxeles y su área segura;
  - un margen del **6 % por lado**.

  Devuelve el `orthographicSize` y el desplazamiento de la cámara que centra el tablero en el
  área segura.
- **Aplicación**: el componente `TerritoryCamera` (`[ExecuteAlways]`) recalcula **solo cuando
  cambian** `Screen.width`, `Screen.height` o `Screen.safeArea`. Lo comprueba en `LateUpdate`
  comparando valores, sin asignaciones de memoria.

**Matemática** (cámara sin giro, inclinada `θ` hacia abajo):
- un punto `p` se proyecta en `u = p.x` y `v = p.y·cos θ + p.z·sin θ` (salvo el signo del eje de
  profundidad, que fija la convención de R-2);
- la semialtura necesaria es `max(Δv/2, Δu/(2·aspect_seguro)) / (1 − 2·margen)`, escalada por
  `alto_pantalla / alto_área_segura`.

Con el margen del 6 %, el tablero ocupa el **88 %** de la dimensión que limita (SC-004 pide
≥ 70 %).

**Motivo**: se prueba en EditMode con las seis proporciones y con áreas seguras ficticias, sin
escena. En PlayMode se comprueba con `Camera.WorldToViewportPoint`, fijando `camera.aspect`.

**Revisión manual del área segura**: con el **Device Simulator**, integrado en el editor de
Unity 6 (vista *Simulator*, sin paquete extra) y un dispositivo con muesca.

**Alternativas descartadas**:
- **Encuadre fijo** con un tamaño ortográfico constante: falla en vertical.
- **Recalcular en cada fotograma sin comparar**: trabajo innecesario.
- **Escuchar eventos de orientación de la plataforma**: no cubren el redimensionado de la vista
  de juego del editor.

## R-7 · Visibilidad en el editor sin *Play* (US1, escenario 3)

**Decisión**:
- **Construcción de piezas**: `TerritoryView` es `[ExecuteAlways]`. En `OnEnable` instancia el
  prefab por cada celda de la disposición, en la posición que da el núcleo.
  - **Fuera de *Play***, las instancias llevan `HideFlags.DontSave`: se ven, pero no se guardan
    en la escena ni la marcan como modificada.
  - **En *Play***, son objetos normales.
  - `OnDisable` las destruye.
- **Cambios en la disposición**: `OnValidate` marca la vista como pendiente y la reconstrucción
  ocurre en el siguiente `Update` del editor, porque Unity no permite instanciar dentro de
  `OnValidate`.
- **Disposición no válida**: la vista registra un **error** con la causa (`Empty` o
  `Duplicate(c, r)`) y no instancia nada (FR-003).

**Motivo**: la escena guardada no repite los datos (16 piezas serializadas que podrían
desfasarse de la disposición) y se ve igual en edición que en *Play*.

**Alternativas descartadas**:
- **Hornear las 16 piezas en la escena** con un comando: duplica datos y puede desfasarse.
- **Construir solo en *Play***: incumple US1, escenario 3.

## R-8 · Ensamblados y pruebas

**Decisión**:
- **Núcleo**: `Ecosystem.Territory`, con `noEngineReferences: true` y sin dependencias. Contiene
  `HexCell`, `TerritoryLayout` (con validación), `HexMetrics` (centros, vecinos y esquinas),
  `HexPrismMeshBuilder` y `TerritoryFraming`.
  - Usa **structs propios mínimos** (`Point2`, `Point3`) en lugar de `System.Numerics`, para no
    depender del perfil de la API ni hacer conversiones ambiguas.
- **Vista**: `Ecosystem.Territory.View` contiene `TerritoryLayoutAsset`, `TerritoryView` y
  `TerritoryCamera`. Referencia a `Ecosystem.Territory`.
- **Editor**: `Ecosystem.Territory.Editor`, solo editor. Contiene el comando *Ecosystem →
  Territorio → Regenerar malla de pieza*.
- **Pruebas EditMode**: añaden referencias a `Ecosystem.Territory` y `Ecosystem.Territory.View`.
- **Pruebas PlayMode**: añaden referencias a `Ecosystem.Territory` y `Ecosystem.Territory.View`.
- **Pruebas de integración**: se amplían para comprobar el paso a la escena del territorio al
  llegar a `Connected` (decisión de FR-023: **integración real**, no un doble, porque
  `ConnectionScreen` monta sus propias dependencias y cambiarlo solo para la prueba sería un
  cambio de diseño fuera de alcance).
- **Sin paquetes nuevos** (R-01 de la spec).

**Dirección de dependencias**:

```text
Territory.Editor → Territory.View → Territory
Presentation → Connection.Nakama → Connection → Contracts      (sin cambios; Presentation solo conoce una ruta de escena)
```

**Alternativas descartadas**:
- **Un solo ensamblado con Unity**: la lógica de disposición no podría probarse sin el motor
  (FR-005).
- **Que `Presentation` referencie `Territory.View`**: acopla conexión y territorio sin necesidad
  (FR-017).
