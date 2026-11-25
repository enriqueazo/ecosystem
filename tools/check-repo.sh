#!/usr/bin/env bash
# Comprobaciones estáticas del repositorio. No necesitan el editor de Unity ni el demonio de
# Docker, pero sí el CLI de Docker para validar la configuración de Compose.
#
# Contrato: specs/001-base-proyecto-entorno/contracts/tooling-cli.md
#
# Comprobaciones:
#   compose  La configuración de deploy/local/compose.yaml es válida con .env.example.
#   secrets  No hay archivos .env (salvo .env.example), datos de PostgreSQL ni la configuración
#            local del cliente (connection.local.json) en el índice de Git.
#   ignored  Ningún archivo imprescindible para reconstruir el proyecto está ignorado.
#
# Códigos de salida: 0 todas pasan; 1 alguna falla; 64 uso incorrecto.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly REPO_ROOT
readonly COMPOSE_DIR="deploy/local"
readonly ALL_CHECKS=(compose secrets ignored)

usage() {
  cat <<'EOF'
Uso: tools/check-repo.sh [--only compose|secrets|ignored] [--help]

Sin opciones ejecuta todas las comprobaciones.
EOF
}

report_fail() { echo "✘ [$1] $2" >&2; }
report_ok() { echo "✔ [$1] $2"; }

check_compose() {
  local output rc=0
  if ! command -v docker >/dev/null 2>&1; then
    report_fail compose "CLI de Docker no disponible: instala Docker para validar $COMPOSE_DIR/compose.yaml."
    return 1
  fi
  output="$(cd "$REPO_ROOT/$COMPOSE_DIR" &&
    docker compose -f compose.yaml --env-file .env.example config --quiet 2>&1)" || rc=$?
  if [[ $rc -eq 127 ]]; then
    report_fail compose "CLI de Docker no disponible: instala Docker para validar $COMPOSE_DIR/compose.yaml."
    return 1
  fi
  if [[ $rc -ne 0 ]]; then
    report_fail compose "configuración de $COMPOSE_DIR/compose.yaml no válida con .env.example: $output"
    return 1
  fi
  report_ok compose "$COMPOSE_DIR/compose.yaml válido con .env.example"
}

check_secrets() {
  local offenders
  offenders="$(git -C "$REPO_ROOT" ls-files --cached |
    grep -E '(^|/)\.env(\.[^/]*)?$|(^|/)PG_VERSION$|(^|/)postgres-data/|^client-unity/LocalConfig/connection\.local\.json$' |
    grep -vE '(^|/)\.env\.example$' || true)"
  if [[ -n "$offenders" ]]; then
    local file
    while IFS= read -r file; do
      report_fail secrets "archivo de secretos o datos locales en Git: $file (quítalo con git rm --cached)"
    done <<<"$offenders"
    return 1
  fi
  report_ok secrets "sin archivos .env ni datos locales en Git"
}

check_ignored() {
  local required=("$COMPOSE_DIR/.env.example")
  if [[ -d "$REPO_ROOT/client-unity/ProjectSettings" ]]; then
    required+=(
      "client-unity/Packages/manifest.json"
      "client-unity/Packages/packages-lock.json"
      "client-unity/ProjectSettings/ProjectVersion.txt"
      "client-unity/Assets/_Project.meta"
    )
  fi
  if [[ -f "$REPO_ROOT/server-nakama/package.json" ]]; then
    required+=(
      "server-nakama/package-lock.json"
      "server-nakama/.node-version"
    )
  fi
  local path failed=0
  for path in "${required[@]}"; do
    if git -C "$REPO_ROOT" check-ignore -q --no-index -- "$path"; then
      report_fail ignored "archivo imprescindible ignorado por .gitignore: $path"
      failed=1
    fi
  done
  [[ $failed -eq 0 ]] || return 1
  report_ok ignored "ningún archivo imprescindible está ignorado (${#required[@]} comprobados)"
}

main() {
  local checks=("${ALL_CHECKS[@]}")
  case "${1:-}" in
    "") ;;
    -h | --help) usage; exit 0 ;;
    --only)
      case "${2:-}" in
        compose | secrets | ignored) checks=("$2") ;;
        *) usage >&2; exit 64 ;;
      esac
      [[ $# -eq 2 ]] || { usage >&2; exit 64; }
      ;;
    *) usage >&2; exit 64 ;;
  esac

  local check failed=0
  for check in "${checks[@]}"; do
    "check_$check" || failed=1
  done
  exit "$failed"
}

main "$@"
