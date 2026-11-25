# Contrato: dirección visual y recursos gráficos

Qué tiene que entregar el diseño con Claude Design (FR-011), cómo entra en Unity (FR-012 a
FR-014) y dónde quedan registrados los valores aprobados.

> **Estado**: **aprobado por el responsable el 2026-11-25** (T019), sin cambios sobre la propuesta.
> Artefacto de diseño: <https://claude.ai/artifact/HDLt4z5rwdcuXTd84hh96V> (privado: solo lo abre su
> propietario hasta que se comparta).

## 1. Artefacto de diseño (Claude Design)

Se crea con la herramienta de artefactos de Claude, intención *design*. Contiene:

| Sección | Contenido obligatorio |
|---------|-----------------------|
| Paleta | Césped (base, luz y sombra), tierra (base y estratos), borde de pieza y fondo. Valores hex en sRGB |
| Pieza | Vista de la pieza aislada con su altura `h` relativa al ancho, el borde de césped en los laterales y la banda de borde de la cara superior |
| Cámara y luz | Ángulo de inclinación `θ`, rotación de la luz direccional e intensidad. Color ambiente |
| Referencias | Tablero 4 × 4 completo en **16:9 horizontal** y en **9:19.5 vertical**, con el fondo aprobado |
| Texturas | `T_Grass_Top` y `T_Earth_Side` dibujadas en el artefacto, cada una con un botón «Descargar PNG» de **512 × 512** |
| Notas de estilo | Qué se evita y por qué: brillos, contornos negros, saturación alta, ruido fuerte (R-03 de la spec) |

**Requisitos de las texturas**:
- **`T_Grass_Top`**:
  - el hexágono con vértice arriba, inscrito según el mapeo planar de data-model.md, ocupa el
    cuadrado y lo que queda fuera no se ve;
  - dentro, césped con variación suave;
  - banda de borde hexagonal sutil para distinguir cada pieza.
- **`T_Earth_Side`**:
  - tierra con estratos o terrones;
  - el 20 % superior (o el valor aprobado) es el borde de césped que cae sobre la tierra;
  - **se repite sin costura en horizontal**, para que no se note la unión en las esquinas del
    prisma.
- Ninguna lleva luces ni sombras pintadas que contradigan la luz de la escena: solo una oclusión
  muy suave.

**Cómo se hicieron las texturas** (registro de T018): las dos texturas se pintaron con un script
determinista (Python, sin dependencias, fuera del repositorio) a partir de la paleta de §2. Se
subieron al artefacto y son **los mismos archivos** que se importan en Unity al aprobar. Desviaciones
respecto a §1:
- no hay botón «Descargar PNG», porque el visor de artefactos bloquea las descargas que inicia la
  página;
- no se dibujaron en SVG dentro del artefacto, porque en el equipo no hay rasterizador de SVG para
  pasarlas a PNG.

Las referencias 16:9 y 9:19.5 son renders reales de la escena en *Play*, hechos con una importación
temporal de las texturas que se revirtió sin confirmar.

## 2. Valores aprobados

| Parámetro | Valor de partida (research.md R-5) | Aprobado |
|-----------|-------------------------------------|----------|
| Césped base | `#6FA34A` | `#6FA34A` |
| Tierra base | `#8A5A3B` | `#8A5A3B` |
| Fondo | `#8E9BAA` | `#8E9BAA` |
| Color ambiente | `#8A929C` (propuesto en el diseño) | `#8A929C` |
| Altura `h` (con `R = 0,5`) | 0,35 | 0,35 |
| Borde de césped en el lateral | 20 % | 20 % |
| Inclinación de la cámara `θ` | 35° | 35° |
| Luz direccional (rotación, intensidad, color) | (50°, −30°, 0°), 1, blanco cálido | (50°, −30°, 0°), 1, `#FFF4E5` |
| Paleta de la textura de césped | luz `#86B858`, sombra `#5B8C3C`, borde de pieza `#4E7A34` (propuesta) | Igual |
| Paleta de la textura de tierra | estrato claro `#9E6C48`, estrato oscuro `#6E452B`, guijarro `#A88A6E` (propuesta) | Igual |
| Banda de borde de la cara superior | 0,045 u (propuesta) | 0,045 u |
| Enlace al artefacto y fecha de aprobación | <https://claude.ai/artifact/HDLt4z5rwdcuXTd84hh96V> | 2026-11-25 |

El **fondo** MUST ser gris azulado (con el azul como canal dominante, por poco), de luminosidad
media o clara y saturación baja. El revisor comprueba en la revisión manual que no se lee como
gris neutro ni como azul vivo.

## 3. Recursos en Unity

Todos van con `.meta` versionado. **No** se versionan SVG, capturas, exportaciones intermedias ni
archivos temporales del diseño (FR-013).

```text
client-unity/Assets/_Project/
├── Art/Territory/
│   ├── Textures/T_Grass_Top.png       512×512 · sRGB · mipmaps · trilineal · Clamp · ASTC 6×6 (Android/iOS) · máx. 512
│   ├── Textures/T_Earth_Side.png      512×512 · sRGB · mipmaps · trilineal · Repeat U / Clamp V · ASTC 6×6 · máx. 512
│   ├── Materials/M_Grass_Top.mat      URP/Simple Lit · Specular Highlights off · Receive Shadows off
│   ├── Materials/M_Earth_Side.mat     Ídem, con T_Earth_Side
│   ├── Meshes/HexTile.asset           Generada (Ecosystem → Territorio → Regenerar malla de pieza)
│   └── Prefabs/HexTile.prefab         MeshFilter(HexTile) + MeshRenderer[M_Grass_Top, M_Earth_Side] · sin sombras
├── Data/Territory/InitialTerritory.asset   TerritoryLayoutAsset con 16 celdas
└── Scenes/Territory.unity
```

**Cambiar un valor aprobado** sigue este orden:
1. actualizar el artefacto;
2. actualizar este documento;
3. actualizar el recurso en Unity (con `h` se regenera la malla).

Si se cambia `h` y no se regenera la malla, la prueba EditMode `HexTileMeshAssetTests` falla.
