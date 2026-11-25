# Contrato: cambios en los scripts de `tools/`

Amplía el contrato de la spec 001 (`specs/001-base-proyecto-entorno/contracts/tooling-cli.md`)
sin romperlo. Al implementarlo, se actualiza ese documento para que siga siendo la referencia
única.

## `tools/check-backend.sh`

Uso: `tools/check-backend.sh [--module] [--help]`

- **Sin `--module`**: el comportamiento y los códigos 0/1/2/3/64 no cambian.
- **Con `--module`**: después de la comprobación de salud, comprueba que el módulo de servidor
  está cargado:
  - llama a `POST /v2/rpc/protocol_version?unwrap&http_key=<clave>` en `127.0.0.1:<puerto>`;
  - la clave es `NAKAMA_RUNTIME_HTTP_KEY`, tomada del entorno o, si no, de
    `deploy/local/.env` (leído sin ejecutarlo).

| Código | Significado |
|--------|-------------|
| 0 | Salud correcta y, con `--module`, el RPC responde 403 con `SESSION_REQUIRED` (módulo cargado). |
| 1 / 2 / 3 / 64 | Sin cambios respecto a la spec 001. |
| 4 | Salud correcta, pero el módulo no está cargado (HTTP 404 «function not found» u otra respuesta inesperada). Mensaje: «módulo no cargado: ejecuta npm run build en server-nakama y reinicia el entorno (docker compose up -d --wait)». |
| 64 | También si falta `NAKAMA_RUNTIME_HTTP_KEY` con `--module`. |

La clave nunca se imprime.

## `tools/check-repo.sh`

`ignored` añade a los archivos imprescindibles, cuando existe `server-nakama/package.json`:
`server-nakama/package-lock.json` y `server-nakama/.node-version`. Las demás comprobaciones no
cambian.
