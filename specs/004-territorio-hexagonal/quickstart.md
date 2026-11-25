# Quickstart: validar el territorio hexagonal

Guía para comprobar la spec 004 de principio a fin. La parte **A** no necesita backend, la **B**
necesita el Nakama local y la **C** es la revisión visual manual (FR-024).

Requisitos: Unity **6000.3.29f1** con el proyecto `client-unity/` abierto
([client-unity/README.md](../../client-unity/README.md)).

```sh
UNITY="/Applications/Unity/Hub/Editor/6000.3.29f1/Unity.app/Contents/MacOS/Unity"
```

## A. Sin backend

Comprueba antes que Nakama está detenido (`docker compose ps` en `deploy/local/` no muestra
servicios) y que **no** existe `client-unity/LocalConfig/connection.local.json`, o renómbralo
temporalmente.

| # | Paso | Resultado esperado |
|---|------|--------------------|
| A1 | EditMode en modo batch: `"$UNITY" -batchmode -nographics -projectPath client-unity -runTests -testPlatform EditMode -assemblyNames Ecosystem.Tests.EditMode -testResults editmode.xml -logFile -` | Código de salida 0. Incluye las pruebas de disposición, colocación, vecinos (33 parejas), validación, encuadre (6 proporciones y área segura), malla al día y la lista de escenas de la build |
| A2 | PlayMode en modo batch: `"$UNITY" -batchmode -projectPath client-unity -runTests -testPlatform PlayMode -assemblyNames Ecosystem.Tests.PlayMode -testResults playmode.xml -logFile -` | Código de salida 0. Incluye `TerritorySceneTests` y que `Bootstrap` sin configuración no pasa al territorio |
| A3 | En el editor, abrir `Assets/_Project/Scenes/Territory.unity` **sin** pulsar *Play* | El tablero de 16 piezas se ve en las vistas *Scene* y *Game*, y la escena **no** aparece como modificada (sin `*`) |
| A4 | Pulsar *Play* 10 veces seguidas (SC-001), con la consola limpia y *Error Pause* activado | 0 errores y 0 avisos en las 10 |
| A5 | En local, **sin confirmar**: quitar una celda de `Data/Territory/InitialTerritory.asset` en el inspector | La escena muestra 15 piezas. Después, duplicar una celda: consola con `[Territorio] La disposición repite la celda …` y ninguna pieza. Deshacer los cambios (`git checkout -- client-unity/Assets/_Project/Data`) |

## B. Con backend (flujo normal)

Requiere el entorno local arrancado con el módulo (`tools/check-backend.sh --module` → 0) y
`connection.local.json` configurado ([client-unity/README.md → Configuración local](../../client-unity/README.md#configuración-local)).

| # | Paso | Resultado esperado |
|---|------|--------------------|
| B1 | Integración en modo batch: `"$UNITY" -batchmode -projectPath client-unity -runTests -testPlatform PlayMode -assemblyNames Ecosystem.Tests.Integration -testResults integration.xml -logFile -` | Código de salida 0. Incluye `Connected_LoadsTerritoryOnce` |
| B2 | Abrir `Bootstrap` y pulsar *Play* | «Conectado» y, en ≤ 2 s, el territorio completo (SC-006). Sin errores ni avisos |
| B3 | Detener el entorno (`docker compose stop` en `deploy/local/`), pulsar *Play* en `Bootstrap` y esperar «Sin conexión» | No aparece el territorio. Arrancar de nuevo (`docker compose start`), pulsar «Reintentar»: «Conectado» y territorio, **una sola vez** |

Para comprobar los otros estados que no llevan al territorio, usa los pasos de
[spec 003 → quickstart](../003-sesion-invitado-cliente/quickstart.md). En ninguno debe aparecer el
territorio.

## C. Revisión visual manual (FR-024)

Escena `Territory.unity` en *Play*. En la vista *Game*, elige cada proporción del desplegable de
aspecto. Añade las que falten con «+» → *Aspect Ratio*.

Proporciones: **16:9, 19.5:9 y 4:3** (horizontal) y **9:16, 9:19.5 y 3:4** (vertical). Para el
área segura, usa la vista *Simulator* con un iPhone con muesca, en vertical y en horizontal.

| # | Criterio (aprueba si…) | 16:9 | 19.5:9 | 4:3 | 9:16 | 9:19.5 | 3:4 | Simulador |
|---|------------------------|------|--------|-----|------|--------|-----|-----------|
| C1 | Hay 16 piezas hexagonales con el vértice arriba, en 4 filas horizontales de 4, con las filas 2 y 4 desplazadas media pieza | | | | | | | |
| C2 | Las piezas vecinas comparten arista. Con el zoom de la vista *Game* a 2×, no se ven huecos, líneas de fondo, solapamientos ni parpadeo | | | | | | | |
| C3 | Las caras superiores se leen como césped verde con textura sutil, y cada pieza se distingue por su borde | | | | | | | |
| C4 | Los laterales frontales se leen como tierra, con textura terrosa y borde de césped arriba, claramente distintos del césped | | | | | | | |
| C5 | La vista es inclinada desde el frente, sin giro (filas horizontales) y sin perspectiva (las piezas del fondo y del frente miden lo mismo) | | | | | | | |
| C6 | El fondo es un gris azulado liso y mate: sin degradado, textura, brillo ni viñeta | | | | | | | |
| C7 | Las 16 piezas se ven enteras, centradas, con margen y **fuera de la muesca** o las esquinas redondeadas. El tablero ocupa al menos el 70 % del ancho o del alto | | | | | | | |
| C8 | Tras 30 s quieto, nada se ha movido ni cambiado (cámara, luz, fondo y piezas) | | | | | | | |
| C9 | Las texturas no se ven pixeladas ni borrosas | | | | | | | |
| C10 | Al cambiar de proporción **durante** *Play*, el encuadre se ajusta al momento | — | — | — | — | — | — | — |
| C11 | El aspecto coincide con las referencias aprobadas del artefacto de diseño ([visual-direction.md](contracts/visual-direction.md)) | | | | | | | |

Marca ✅ o ❌ en cada celda y registra el resultado en la spec con la fecha y la persona que
revisa. Un ❌ bloquea la entrega.

La revisión completa debe llevar ≤ 10 minutos (SC-005).

## D. Higiene del cambio (SC-007)

```sh
git diff --stat main...HEAD -- server-nakama shared deploy        # vacío
git diff main...HEAD -- client-unity/ProjectSettings/ProjectSettings.asset | grep -iE 'orient|autorotat|defaultScreen'   # vacío
tools/check-repo.sh                                                 # 0
git ls-files client-unity/Assets/_Project | grep -vE '\.meta$' | while read f; do [ -f "$f.meta" ] || echo "sin .meta: $f"; done   # vacío
```
