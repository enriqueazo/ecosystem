# Contrato: dirección visual y recursos gráficos

Qué tiene que entregar el diseño con Claude Design (FR-011), cómo entra en Unity (FR-012 a
FR-014) y dónde quedan registrados los valores aprobados.

> **Estado**: pendiente de diseño. Las celdas «Aprobado» se rellenan en la implementación, con el
> enlace al artefacto y la fecha de aprobación del responsable.

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

## 2. Valores aprobados

| Parámetro | Valor de partida (research.md R-5) | Aprobado |
|-----------|-------------------------------------|----------|
| Césped base | `#6FA34A` | — |
| Tierra base | `#8A5A3B` | — |
| Fondo | `#8E9BAA` | — |
| Color ambiente | por definir en el diseño | — |
| Altura `h` (con `R = 0,5`) | 0,35 | — |
| Borde de césped en el lateral | 20 % | — |
| Inclinación de la cámara `θ` | 35° | — |
| Luz direccional (rotación, intensidad, color) | (50°, −30°, 0°), 1, blanco cálido | — |
| Enlace al artefacto y fecha de aprobación | — | — |

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
