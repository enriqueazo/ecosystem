# 0003 · Pipeline de render del cliente: URP

- **Estado**: aceptada
- **Fecha**: 2026-11-25

## Contexto

El proyecto Unity debe crearse con un pipeline de render, y cambiarlo más tarde obliga a migrar
materiales y shaders de todo el arte existente. El juego es para Android e iOS, con unidades y
edificios animados, y necesita calidad escalable entre dispositivos.

## Opciones consideradas

- **Universal Render Pipeline (URP)**: es el pipeline principal de Unity para móvil, con calidad
  escalable. Añade el paquete URP y sus assets de configuración.
- **Built-in Render Pipeline**: arranque más sencillo, pero Unity ya no lo impulsa y migrar
  después, con arte ya hecho, es caro.
- **Decidir más tarde**: bloquea la creación del proyecto Unity.

## Decisión

Se usa **URP**. El proyecto se crea desde la plantilla *Universal 3D* de Unity Hub. Se eliminan
los recursos de muestra de la plantilla y solo se conservan los assets de URP (pipeline y
renderer) que necesita la escena inicial, en `client-unity/Assets/_Project/Settings/`.

## Consecuencias

- `client-unity/Packages/manifest.json` incluye el paquete URP; su versión la fija el lockfile de
  la versión de Unity elegida (D-02).
- Las pruebas EditMode comprueban que el pipeline activo es un asset de URP.
- El arte, los shaders y los materiales futuros deben ser compatibles con URP.
