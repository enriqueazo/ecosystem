#!/usr/bin/env bash
# Comprueba que la API de cliente de Nakama del entorno local responde en el anfitrión.
#
# Contrato: specs/001-base-proyecto-entorno/contracts/tooling-cli.md
#
# Puerto: NAKAMA_API_PORT del entorno; si no está definida, la de deploy/local/.env; si
# tampoco, 7350.
#
# Códigos de salida:
#   0  Nakama responde a su comprobación de salud.
#   1  No hay respuesta en el puerto (backend no disponible).
#   2  Respuesta con un código HTTP inesperado.
#   3  No hay respuesta y Docker no está en ejecución o no está instalado.
#   64 Uso incorrecto o puerto no válido.

set -euo pipefail

readonly DEFAULT_PORT=7350
readonly TIMEOUT_SECONDS=5

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly REPO_ROOT
readonly ENV_FILE="$REPO_ROOT/deploy/local/.env"

usage() {
  cat <<'EOF'
Uso: tools/check-backend.sh [--help]

Comprueba que la API de cliente de Nakama responde en 127.0.0.1.
El puerto se toma de NAKAMA_API_PORT (entorno), de deploy/local/.env o, si no, 7350.
EOF
}

# Lee NAKAMA_API_PORT de deploy/local/.env sin ejecutar el archivo (no se usa `source`).
port_from_env_file() {
  [[ -f "$ENV_FILE" ]] || return 0
  local line value
  line="$(grep -E '^[[:space:]]*NAKAMA_API_PORT[[:space:]]*=' "$ENV_FILE" | tail -n 1 || true)"
  [[ -n "$line" ]] || return 0
  value="${line#*=}"
  value="${value%%#*}"
  value="$(printf '%s' "$value" | tr -d '[:space:]"'"'")"
  printf '%s' "$value"
}

resolve_port() {
  local port="${NAKAMA_API_PORT:-}"
  [[ -n "$port" ]] || port="$(port_from_env_file)"
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

main() {
  case "${1:-}" in
    "") ;;
    -h | --help) usage; exit 0 ;;
    *) usage >&2; exit 64 ;;
  esac

  local port url http_code
  port="$(resolve_port)"
  url="http://127.0.0.1:${port}/healthcheck"

  if http_code="$(curl -s -o /dev/null -w '%{http_code}' --max-time "$TIMEOUT_SECONDS" "$url")"; then
    if [[ "$http_code" == "200" ]]; then
      echo "Backend disponible en 127.0.0.1:${port}"
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
  exit 1
}

main "$@"
