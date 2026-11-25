# 0011 · Binarios de arte sin Git LFS

- **Estado**: aceptada
- **Fecha**: 2026-11-25
- **Resuelve**: D-08 de [pendientes.md](pendientes.md)

## Contexto

La spec 004 (territorio hexagonal) añade los primeros recursos de arte del cliente. D-08 exigía
decidir sobre Git LFS «antes de añadir el primer recurso de arte o audio».

Lo que esta spec versiona:
- dos texturas PNG de 512 × 512, en total menos de 1 MB;
- el resto del arte en texto YAML de Unity (`ForceText`): materiales, malla, prefab y escena.

`.gitattributes` ya marca `*.png`, `*.fbx`, `*.psd`, `*.wav` y otros como `binary`.

## Opciones consideradas

- **Git LFS desde ya**:
  - Ventajas: el historial no crece con los binarios, y está preparado para arte y audio
    voluminosos.
  - Costes:
    - todo clon, entorno y CI necesita `git-lfs` instalado y configurado;
    - un clon sin LFS recibe punteros en lugar de imágenes y Unity falla de forma confusa;
    - consume cuota de almacenamiento y transferencia del alojamiento;
    - añade otra pieza a la configuración documentada.
- **Sin Git LFS, con un umbral de revisión**:
  - Ventajas: ninguna herramienta ni cuota nueva, y los clones y la CI no cambian.
  - Costes: cada binario queda en el historial para siempre. Si se pasa a LFS más tarde, migrar
    los binarios ya confirmados (`git lfs migrate`) reescribe la historia compartida, cosa que la
    constitución solo permite con petición explícita.

## Decisión

**Sin Git LFS.** Los binarios se versionan en Git normal con el atributo `binary`. El volumen
actual (menos de 1 MB) no justifica el coste de LFS.

**Umbral de revisión**: la decisión se vuelve a tomar en un ADR nuevo **antes** de confirmar
cualquiera de estos:
- un archivo binario de más de **5 MB**;
- el primer recurso de **audio o vídeo**;
- recursos que lleven el total de binarios de `client-unity/Assets/` por encima de **50 MB**.

Revisarla antes de cruzar el umbral permite, si se adopta LFS, aplicarlo solo a los archivos
nuevos sin reescribir la historia.

## Consecuencias

- No cambian los requisitos previos, los clones ni la CI.
- Quien añada arte comprueba el umbral en la revisión del PR. Para medir el total:
  `git ls-files -z client-unity/Assets | xargs -0 du -ch -- 2>/dev/null | tail -1`.
- Las texturas se mantienen pequeñas, con el tamaño y la compresión que fija cada spec (la 004:
  512 px y ASTC).
- Si se adopta LFS más adelante, el ADR que sustituya a este debe indicar qué patrones se mueven y
  cómo se configuran los clones y la CI.
