#!/usr/bin/env bash
# Comprueba que la API de cliente de Nakama del entorno local responde en el anfitrión.
#
# Contrato: specs/001-base-proyecto-entorno/contracts/tooling-cli.md
#
# Puerto: NAKAMA_API_PORT del entorno; si no está definida, la de deploy/local/.env; si
# tampoco, 7350. Con --module, la clave NAKAMA_RUNTIME_HTTP_KEY se toma igual (entorno o .env).
#
# Códigos de salida:
#   0  Nakama responde a su comprobación de salud (y, con --module, el módulo está cargado).
#   1  No hay respuesta en el puerto (backend no disponible).
#   2  Respuesta con un código HTTP inesperado en la comprobación de salud.
#   3  No hay respuesta y Docker no está en ejecución o no está instalado.
#   4  Con --module: salud correcta, pero el módulo de servidor no está cargado.
#   64 Uso incorrecto, puerto no válido o, con --module, falta NAKAMA_RUNTIME_HTTP_KEY.

set -euo pipefail

readonly DEFAULT_PORT=7350
readonly TIMEOUT_SECONDS=5

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly REPO_ROOT
readonly ENV_FILE="$REPO_ROOT/deploy/local/.env"

usage() {
  cat <<'EOF'
Uso: tools/check-backend.sh [--module] [--help]

Comprueba que la API de cliente de Nakama responde en 127.0.0.1.
El puerto se toma de NAKAMA_API_PORT (entorno), de deploy/local/.env o, si no, 7350.

  --module  Comprueba además que el módulo de servidor está cargado (RPC protocol_version),
            con la clave NAKAMA_RUNTIME_HTTP_KEY del entorno o de deploy/local/.env.
EOF
}

# Lee una variable de deploy/local/.env sin ejecutar el archivo (no se usa `source`).
value_from_env_file() {
  local name="$1"
  [[ -f "$ENV_FILE" ]] || return 0
  local line value
  line="$(grep -E "^[[:space:]]*${name}[[:space:]]*=" "$ENV_FILE" | tail -n 1 || true)"
  [[ -n "$line" ]] || return 0
  value="${line#*=}"
  value="${value%%#*}"
  value="$(printf '%s' "$value" | tr -d '[:space:]"'"'")"
  printf '%s' "$value"
}

resolve_port() {
  local port="${NAKAMA_API_PORT:-}"
  [[ -n "$port" ]] || port="$(value_from_env_file NAKAMA_API_PORT)"
  [[ -n "$port" ]] || port="$DEFAULT_PORT"
  if ! [[ "$port" =~ ^[0-9]+$ ]] || ((port < 1 || port > 65535)); then
    echo "Error: NAKAMA_API_PORT no es un puerto válido: '$port'" >&2
    exit 64
  fi
  printf '%s' "$port"
}

docker_running() {
  command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1
}

# El módulo responde al RPC con la clave HTTP de servidor y sin sesión de usuario rechazándolo
# con SESSION_REQUIRED (403): eso prueba que está cargado sin crear cuentas. Si Nakama no conoce
# el RPC, responde 404. La clave no se imprime nunca.
check_module() {
  local port="$1" key response http_code body
  key="${NAKAMA_RUNTIME_HTTP_KEY:-}"
  [[ -n "$key" ]] || key="$(value_from_env_file NAKAMA_RUNTIME_HTTP_KEY)"
  if [[ -z "$key" ]]; then
    echo "Error: --module necesita NAKAMA_RUNTIME_HTTP_KEY (entorno o deploy/local/.env)." >&2
    exit 64
  fi
  response="$(curl -s -w '\n%{http_code}' --max-time "$TIMEOUT_SECONDS" -X POST \
    -H 'Content-Type: application/json' --data '{}' \
    "http://127.0.0.1:${port}/v2/rpc/protocol_version?unwrap&http_key=${key}" || true)"
  http_code="${response##*$'\n'}"
  body="${response%$'\n'*}"
  if [[ "$http_code" == "403" && "$body" == *SESSION_REQUIRED* ]]; then
    echo "Módulo cargado: RPC protocol_version disponible"
    return 0
  fi
  echo "Error: módulo no cargado (RPC protocol_version respondió HTTP ${http_code:-sin respuesta})." >&2
  echo "Ejecuta npm run build en server-nakama y reinicia el entorno (docker compose up -d --wait en deploy/local)." >&2
  exit 4
}

main() {
  local check_module_too=false
  case "${1:-}" in
    "") ;;
    --module) check_module_too=true ;;
    -h | --help) usage; exit 0 ;;
    *) usage >&2; exit 64 ;;
  esac
  [[ $# -le 1 ]] || { usage >&2; exit 64; }

  local port url http_code
  port="$(resolve_port)"
  url="http://127.0.0.1:${port}/healthcheck"

  if http_code="$(curl -s -o /dev/null -w '%{http_code}' --max-time "$TIMEOUT_SECONDS" "$url")"; then
    if [[ "$http_code" == "200" ]]; then
      echo "Backend disponible en 127.0.0.1:${port}"
      if [[ "$check_module_too" == true ]]; then
        check_module "$port"
      fi
      exit 0
    fi
    echo "Error: Nakama respondió en 127.0.0.1:${port} con un código HTTP inesperado: ${http_code}" >&2
    exit 2
  fi

  if ! docker_running; then
    echo "Error: backend no disponible en 127.0.0.1:${port}: Docker no está en ejecución (o no está instalado)." >&2
    echo "Arranca Docker y después el entorno (docker compose up -d --wait en deploy/local)." >&2
    exit 3
  fi

  echo "Error: backend no disponible en 127.0.0.1:${port}." >&2
  echo "¿Has arrancado el entorno? (docker compose up -d --wait en deploy/local)" >&2
  # Nakama no arranca si falta el módulo (js_entrypoint): es la causa más probable en ese caso.
  if [[ -f "$REPO_ROOT/server-nakama/package.json" && ! -f "$REPO_ROOT/server-nakama/build/index.js" ]]; then
    echo "Falta server-nakama/build/index.js y Nakama no puede arrancar sin él:" >&2
    echo "ejecuta npm ci && npm run build en server-nakama y vuelve a arrancar el entorno." >&2
  fi
  exit 1
}

main "$@"
