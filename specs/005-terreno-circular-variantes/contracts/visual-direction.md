# Contrato: dirección visual de las variantes y del río

Qué tiene que entregar el diseño con Claude Design (spec FR-019), cómo entra en Unity (FR-021 a
FR-023) y dónde quedan los valores aprobados (FR-020). Amplía el
[contrato visual de la spec 004](../../004-territorio-hexagonal/contracts/visual-direction.md), cuyos
valores de hierba, fondo, cámara y luz **no cambian**.

> **Estado**: variantes (nieve, arena y agua) y nivel del agua `hw` **aprobados por el responsable el
> 2026-11-25** (T033), sin cambios sobre la propuesta. El río (`w`, `rp`) y la composición con el río
> están **pendientes** (T050).
> Artefacto de diseño: <https://claude.ai/artifact/98VB8yMnzYHcMT252Mm8wp> (privado: solo lo abre su
> propietario hasta que se comparta).

## 1. Artefacto de diseño (Claude Design)

Se crea con la herramienta de artefactos de Claude, con intención *design*, siguiendo el proceso
de research.md R-7. Contiene:

| Sección | Contenido obligatorio |
|---------|-----------------------|
| Paletas | Nieve (base, luz, sombra y borde del lateral), arena (base, luz, sombra y capas), agua (base, luz, sombra, lecho y lámina), en hex sRGB. Se recuerdan los colores de la hierba y del fondo, sin cambios |
| Variantes | Vista aislada de cada pieza (hierba, nieve, arena y agua), con su cara superior y sus laterales frontales |
| Río | Vista de las tres piezas (manantial, recto y curva), con el nivel `hw`, la anchura `w` y el radio de la poza `rp`. Además, un detalle de una unión entre dos piezas y de la desembocadura en el agua |
| Composición | Mapa de las 55 celdas con la variante de cada una y el trazado del río (data-model.md → *Composición de partida*, o la ajustada) |
| Referencias | El territorio completo en **16:9 horizontal** y **9:19.5 vertical**: renders reales de la escena |
| Texturas | Las 6 texturas de §3, a 512 × 512. Son los mismos archivos que se importan |
| Notas de estilo | Continuación de las de la spec 004. Qué se evita y por qué: brillos, reflejos o transparencia en el agua, contornos negros, saturación alta, ruido fuerte y transiciones entre variantes |

## 2. Valores

| Parámetro | Valor de partida (research.md R-7) | Aprobado |
|-----------|-------------------------------------|----------|
| Nieve: base, luz y sombra | `#E6EDF2`, `#F7FAFC` y `#C3D1DD` | Igual |
| Nieve: borde de nieve del lateral (altura y color) | 20 %, `#DCE5EC` | Igual |
| Nieve: banda de borde de la cara superior | `#AFC0CF` (propuesta), 0,045 u | Igual |
| Arena: base, luz y sombra | `#D8BF8A`, `#E8D4A4` y `#BC9E68` | Igual |
| Arena: banda de borde de la cara superior | `#A98C5A` (propuesta), 0,045 u | Igual |
| Arena: capas del lateral | `#C9AA74`, `#B08E5A` y `#DCC596` | Igual |
| Agua: base, luz y sombra | `#4E8E9F`, `#6DA8B5` y `#3D7585` | Igual |
| Agua: lecho y lámina del lateral | lecho `#7A6A55` y `#5E5040`; lámina en el 12 % superior | Igual |
| Nivel del agua `hw` (con `h = 0,35`) | 0,25 | 0,25 |
| Anchura del cauce `w` (con `W ≈ 0,866`) | 0,26 | |
| Radio de la poza `rp` | 0,17 | |
| Composición de las variantes | data-model.md → *Composición de partida* | Igual (nieve 8, arena 6, agua 6) |
| Composición del río | data-model.md → *Composición de partida* | |
| Enlace al artefacto y fecha de aprobación | <https://claude.ai/artifact/98VB8yMnzYHcMT252Mm8wp> | Variantes: 2026-11-25. Río: pendiente |

**Cómo se hicieron las texturas** (registro de T032): con un script determinista (Python, sin
dependencias, fuera del repositorio) que reutiliza el ruido y el borde hexagonal del script de la spec
004, a partir de la paleta de esta tabla. Se subieron al artefacto y son **los mismos archivos** que se
importan en Unity. Las referencias 16:9 y 9:19.5 son renders reales de la escena en *Play*. Medido en la
aprobación: contraste de la nieve frente al fondo de 30 L* (93,4 frente a 63,4) y `T_Water_Top`
idéntica a sí misma desplazada (½, ½).

**Condiciones que el revisor comprueba en la revisión manual**:
- la nieve no se confunde con el fondo `#8E9BAA` ni con la arena;
- el agua es azul‑verdosa, opaca y mate, y se distingue del fondo;
- la arena no se confunde con la tierra de los laterales de la hierba.

**Cambiar un valor aprobado** sigue este orden:
1. actualizar el artefacto;
2. actualizar este documento;
3. actualizar `RiverMetrics.Default` si cambia `hw`, `w` o `rp`;
4. regenerar las mallas (*Ecosystem → Territorio → Regenerar mallas del territorio*);
5. reemplazar las texturas.

Si no se regeneran las mallas, `TerritoryMeshAssetTests` falla.

## 3. Recursos en Unity

Todos llevan `.meta` versionado. Los recursos de la spec 004 **no se modifican**, y sus `.meta`
conservan el GUID:
- `T_Grass_Top`, `T_Earth_Side`;
- `M_Grass_Top`, `M_Earth_Side`;
- `HexTile.asset`, `HexTile.prefab`.

**No** se versionan SVG, capturas, el script que pinta las texturas, exportaciones intermedias ni
temporales del diseño.

```text
client-unity/Assets/_Project/
├── Art/Territory/
│   ├── Textures/
│   │   ├── T_Snow_Top.png      512 · sRGB · mipmaps · trilineal · Clamp · ASTC 6×6 · máx. 512
│   │   ├── T_Snow_Side.png     Ídem · Repeat U / Clamp V
│   │   ├── T_Sand_Top.png      Ídem · Clamp
│   │   ├── T_Sand_Side.png     Ídem · Repeat U / Clamp V
│   │   ├── T_Water_Top.png     Ídem · Repeat U y V · periódica en (½, ½) (research.md R-4)
│   │   └── T_Water_Side.png    Ídem · Repeat U / Clamp V
│   ├── Materials/
│   │   └── M_Snow_Top, M_Snow_Side, M_Sand_Top, M_Sand_Side, M_Water_Top, M_Water_Side (.mat)
│   │                           URP/Simple Lit · Specular Highlights off · Receive Shadows off
│   ├── Meshes/
│   │   ├── WaterTile.asset     Generada (HexPrismMeshBuilder, altura hw, mapeo en red)
│   │   └── River/River_{Spring_0..5, Straight_0..2, Curve_0..5}.asset   15 mallas generadas
│   └── Prefabs/
│       ├── SnowTile.prefab     MeshFilter(HexTile) + MeshRenderer[M_Snow_Top, M_Snow_Side] · sin sombras
│       ├── SandTile.prefab     MeshFilter(HexTile) + MeshRenderer[M_Sand_Top, M_Sand_Side] · sin sombras
│       ├── WaterTile.prefab    MeshFilter(WaterTile) + MeshRenderer[M_Water_Top, M_Water_Side] · sin sombras
│       └── RiverTile.prefab    MeshFilter(ninguna) + MeshRenderer[M_Grass_Top, M_Earth_Side, M_Water_Top] · sin sombras
└── Data/Territory/
    ├── InitialTerritory.asset  55 celdas con variante y río (mismo GUID)
    └── TerrainCatalog.asset    Catálogo de variantes y mallas de río
```

Antes de confirmar las texturas, se comprueba el umbral de ADR-0011 (quickstart D).
