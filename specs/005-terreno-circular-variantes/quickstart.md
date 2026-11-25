# Quickstart: validar el territorio circular, las variantes y el río

Guía para comprobar la spec 005 de principio a fin:
- **A**: sin backend;
- **B**: con el Nakama local;
- **C**: revisión visual manual (FR-027);
- **D**: higiene del cambio.

Requisitos: Unity **6000.3.29f1** con el proyecto `client-unity/` abierto
([client-unity/README.md](../../client-unity/README.md)).

```sh
UNITY="/Applications/Unity/Hub/Editor/6000.3.29f1/Unity.app/Contents/MacOS/Unity"
```

## A. Sin backend

Antes, comprueba que Nakama está detenido y que **no** existe
`client-unity/LocalConfig/connection.local.json`, o renómbralo temporalmente.

| # | Paso | Resultado esperado |
|---|------|--------------------|
| A1 | EditMode en modo batch: `"$UNITY" -batchmode -nographics -projectPath client-unity -runTests -testPlatform EditMode -assemblyNames Ecosystem.Tests.EditMode -testResults editmode.xml -logFile -` | Código 0. Incluye: disco de 55 celdas (138 parejas, 24 de borde, sin huecos), validación (no conexa, variante desconocida, datos de río, río roto), aristas y conexiones, mallas de río (cortes, nivel, áreas), mallas al día, catálogo completo, texturas nuevas y hierba sin cambios |
| A2 | PlayMode en modo batch: `"$UNITY" -batchmode -projectPath client-unity -runTests -testPlatform PlayMode -assemblyNames Ecosystem.Tests.PlayMode -testResults playmode.xml -logFile -` | Código 0. Incluye `TerritorySceneTests`: 55 piezas con sus recursos, encuadre en 6 proporciones y datos no válidos con 0 piezas |
| A3 | En el editor, abrir `Assets/_Project/Scenes/Territory.unity` **sin** *Play* | El territorio circular completo se ve en *Scene* y *Game*, y la escena no aparece como modificada |
| A4 | Pulsar *Play* 10 veces (SC-001), con la consola limpia y *Error Pause* activado | 0 errores y 0 avisos en las 10 |
| A5 | En local, **sin confirmar**, en `Data/Territory/InitialTerritory.asset`: cambiar la celda (4,6) a `terrain` = nieve, `river` = ninguna y `rotation` = 0 | Consola: `[Territorio] El río de la celda (columna 4, fila 5) no continúa por la arista SO: la vecina es de nieve.` y 0 piezas. Deshacer con `git checkout -- client-unity/Assets/_Project/Data` |
| A6 | Ídem: quitar del catálogo la entrada de nieve | `[Territorio] El catálogo no tiene recursos para variante nieve.` y 0 piezas. Deshacer igual |

## B. Con backend (flujo normal)

Requiere el entorno local con el módulo (`tools/check-backend.sh --module` → 0) y
`connection.local.json` configurado.

| # | Paso | Resultado esperado |
|---|------|--------------------|
| B1 | Integración: `"$UNITY" -batchmode -projectPath client-unity -runTests -testPlatform PlayMode -assemblyNames Ecosystem.Tests.Integration -testResults integration.xml -logFile -` | Código 0. Incluye `Connected_LoadsTerritoryOnce` con 55 piezas |
| B2 | Abrir `Bootstrap` y pulsar *Play* | «Conectado» y, en ≤ 2 s, el territorio circular con variantes y río (SC-008) |
| B3 | Detener el entorno, *Play* en `Bootstrap` y esperar «Sin conexión». Arrancarlo y pulsar «Reintentar» | Sin territorio hasta «Conectado»; después aparece **una sola vez** |

## C. Revisión visual manual (FR-027)

Escena `Territory.unity` en *Play*. En la vista *Game*, elige cada proporción: **16:9, 19.5:9 y
4:3** en horizontal, y **9:16, 9:19.5 y 3:4** en vertical. Para el área segura, usa la vista
*Simulator* con un iPhone con muesca. Ten abiertas las referencias aprobadas
([contracts/visual-direction.md](contracts/visual-direction.md)) y la de la spec 004.

| # | Criterio (aprueba si…) | 16:9 | 19.5:9 | 4:3 | 9:16 | 9:19.5 | 3:4 | Simulador |
|---|------------------------|------|--------|-----|------|--------|-----|-----------|
| C1 | El contorno se lee como una isla redonda vista en escorzo: sin picos, salientes de una pieza ni piezas sueltas. Hay 55 piezas en 9 filas (3‑6‑7‑8‑7‑8‑7‑6‑3) | | | | | | | |
| C2 | Las cuatro variantes se reconocen por la cara superior. Prueba SC-003: otra persona señala dos celdas de cada variante y aciertas las 8 | | | | | | | |
| C3 | En el borde frontal, los laterales se distinguen: tierra con césped, tierra con nieve, capas de arena y lecho con lámina de agua | | | | | | | |
| C4 | La hierba es igual que en la referencia de la spec 004 (color, textura, borde de pieza y laterales) | | | | | | | |
| C5 | El agua queda por debajo de la tierra. Las orillas tienen escalón sin huecos y entre celdas de agua no se marca borde de pieza ni costura | | | | | | | |
| C6 | El río va del manantial al agua sin escalones, huecos, cambios de anchura ni extremos cortados. La poza del manantial se lee como un nacimiento | | | | | | | |
| C7 | Con el zoom de *Game* a 2×, no hay parpadeo ni líneas de fondo en las uniones, las orillas ni el cauce | | | | | | | |
| C8 | El estilo es el de la spec 004: mate, sin brillos, reflejos, transparencia ni contornos | | | | | | | |
| C9 | Las 55 piezas se ven enteras, centradas, con margen y fuera de la muesca. El territorio ocupa al menos el 70 % del ancho o del alto, y las piezas siguen siendo legibles en vertical | | | | | | | |
| C10 | Tras 30 s quieto, nada se ha movido ni cambiado; el agua tampoco | | | | | | | |
| C11 | Las texturas no se ven pixeladas ni borrosas | | | | | | | |
| C12 | Al cambiar de proporción durante *Play*, el encuadre se ajusta al momento | — | — | — | — | — | — | — |
| C13 | El aspecto coincide con las referencias aprobadas del artefacto de diseño | | | | | | | |

Marca ✅ o ❌ en cada celda y registra el resultado en la spec con la fecha y la persona que
revisa. Un ❌ bloquea la entrega. La revisión completa debe llevar ≤ 15 minutos (SC-009).

## D. Higiene del cambio (SC-010)

```sh
git diff --stat main...HEAD -- server-nakama shared deploy .github                   # vacío
git diff --stat main...HEAD -- client-unity/ProjectSettings                          # vacío
# Hierba sin cambios (SC-004):
git diff --stat main...HEAD -- client-unity/Assets/_Project/Art/Territory/Textures/T_Grass_Top.png* \
  client-unity/Assets/_Project/Art/Territory/Textures/T_Earth_Side.png* \
  client-unity/Assets/_Project/Art/Territory/Materials/M_Grass_Top.mat* \
  client-unity/Assets/_Project/Art/Territory/Materials/M_Earth_Side.mat* \
  client-unity/Assets/_Project/Art/Territory/Meshes/HexTile.asset* \
  client-unity/Assets/_Project/Art/Territory/Prefabs/HexTile.prefab*                 # vacío
# Ningún .meta existente cambia de GUID (solo se añaden .meta):
git diff main...HEAD --diff-filter=M -- '*.meta' | grep '^[-+]guid'                  # vacío
tools/check-repo.sh                                                                  # 0
git ls-files client-unity/Assets/_Project | grep -vE '\.meta$' | while read f; do [ -f "$f.meta" ] || echo "sin .meta: $f"; done   # vacío
# ADR-0011: ningún binario de más de 5 MB y total por debajo de 50 MB
git ls-files -z client-unity/Assets | xargs -0 du -ch -- 2>/dev/null | tail -1
git ls-files -z client-unity/Assets | xargs -0 du -k -- | awk '$1 > 5120'             # vacío
```
