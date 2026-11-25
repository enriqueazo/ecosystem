#!/usr/bin/env bash
# Pruebas de los scripts de tools/.
#
# Cada caso trabaja en un repositorio Git temporal al que se copia el script bajo prueba
# (los scripts calculan la raíz del repo a partir de su propia ubicación). Las dependencias
# externas (curl, docker) se sustituyen por stubs colocados al frente de PATH, así que las
# pruebas no necesitan red ni el demonio de Docker.
#
# Uso: tools/tests/run.sh
# Salida: 0 si todas las pruebas pasan; 1 si alguna falla.

set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TMP_ROOT="$(mktemp -d)"
trap 'rm -rf "$TMP_ROOT"' EXIT

PASSED=0
FAILED=0
SKIPPED=0
CASE_DIR=""
OUT=""
ERR=""
CODE=0

# --- utilidades -----------------------------------------------------------------------------

pass() { PASSED=$((PASSED + 1)); printf '  ✔ %s\n' "$1"; }
fail() { FAILED=$((FAILED + 1)); printf '  ✘ %s\n      %s\n' "$1" "$2"; }
skip() { SKIPPED=$((SKIPPED + 1)); printf '  ○ OMITIDO %s (%s)\n' "$1" "$2"; }

# Crea un repositorio temporal con una copia de los archivos indicados (rutas relativas a la
# raíz del repo) y deja su ruta en CASE_DIR.
new_repo() {
  CASE_DIR="$(mktemp -d "$TMP_ROOT/case.XXXXXX")"
  git -C "$CASE_DIR" init -q
  git -C "$CASE_DIR" config user.email "tests@example.invalid"
  git -C "$CASE_DIR" config user.name "tools-tests"
  local rel
  for rel in "$@"; do
    mkdir -p "$CASE_DIR/$(dirname "$rel")"
    cp "$REPO_ROOT/$rel" "$CASE_DIR/$rel"
  done
  mkdir -p "$CASE_DIR/.stubs"
}

# Escribe un stub ejecutable en el directorio de stubs del caso actual.
stub() {
  local name="$1" body="$2"
  printf '#!/usr/bin/env bash\n%s\n' "$body" >"$CASE_DIR/.stubs/$name"
  chmod +x "$CASE_DIR/.stubs/$name"
}

# Ejecuta un script del caso actual con los stubs al frente de PATH.
# Variables de entorno adicionales: pasar como "VAR=valor" antes de "--".
run_in_case() {
  local env_args=()
  while [[ $# -gt 0 && "$1" != "--" ]]; do env_args+=("$1"); shift; done
  shift
  local script="$1"; shift
  OUT="$CASE_DIR/.out"; ERR="$CASE_DIR/.err"
  (
    cd "$CASE_DIR" || exit 99
    env -u NAKAMA_API_PORT PATH="$CASE_DIR/.stubs:$PATH" ${env_args[@]+"${env_args[@]}"} \
      bash "$CASE_DIR/$script" "$@" >"$OUT" 2>"$ERR"
  )
  CODE=$?
}

assert_exit() {
  local name="$1" expected="$2"
  if [[ "$CODE" -eq "$expected" ]]; then return 0; fi
  fail "$name" "código de salida $CODE, esperado $expected. stderr: $(tr '\n' ' ' <"$ERR")"
  return 1
}

assert_file_contains() {
  local name="$1" file="$2" text="$3"
  if grep -qF -- "$text" "$file"; then return 0; fi
  fail "$name" "no contiene «$text»: $(tr '\n' ' ' <"$file")"
  return 1
}

# --- check-backend.sh -----------------------------------------------------------------------

CB="tools/check-backend.sh"

# curl simulado: escribe sus argumentos en .curl-args e imita el comportamiento de
# `curl -s -o /dev/null -w '%{http_code}'` según CURL_MODE (ok | http500 | refused).
curl_stub() {
  stub curl 'printf "%s\n" "$@" >"'"$CASE_DIR"'/.curl-args"
case "${CURL_MODE:-ok}" in
  ok) printf 200; exit 0 ;;
  http500) printf 500; exit 0 ;;
  refused) printf 000; exit 7 ;;
esac'
}

# docker simulado: `docker info` responde según DOCKER_MODE (up | down).
docker_stub() {
  stub docker '[[ "${DOCKER_MODE:-up}" == up ]] && exit 0 || exit 1'
}

test_check_backend() {
  echo "check-backend.sh"
  local n

  n="responde 200 → 0"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case CURL_MODE=ok -- "$CB"
  assert_exit "$n" 0 && assert_file_contains "$n" "$OUT" "Backend disponible en 127.0.0.1:7350" && pass "$n"

  n="sin respuesta y Docker activo → 1"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case CURL_MODE=refused DOCKER_MODE=up -- "$CB"
  assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" "backend no disponible" \
    && assert_file_contains "$n" "$ERR" "docker compose up -d --wait" && pass "$n"

  n="HTTP 500 → 2"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case CURL_MODE=http500 -- "$CB"
  assert_exit "$n" 2 && assert_file_contains "$n" "$ERR" "500" && pass "$n"

  n="sin respuesta y Docker detenido → 3"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case CURL_MODE=refused DOCKER_MODE=down -- "$CB"
  assert_exit "$n" 3 && assert_file_contains "$n" "$ERR" "Docker no está en ejecución" && pass "$n"

  n="argumento desconocido → 64"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case -- "$CB" --desconocido
  assert_exit "$n" 64 && assert_file_contains "$n" "$ERR" "Uso:" && pass "$n"

  n="--help → 0 con la ayuda"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case -- "$CB" --help
  assert_exit "$n" 0 && assert_file_contains "$n" "$OUT" "Uso:" && pass "$n"

  n="usa NAKAMA_API_PORT del entorno"
  new_repo "$CB"; curl_stub; docker_stub
  mkdir -p "$CASE_DIR/deploy/local"; printf 'NAKAMA_API_PORT=18000\n' >"$CASE_DIR/deploy/local/.env"
  run_in_case NAKAMA_API_PORT=17350 -- "$CB"
  assert_exit "$n" 0 && assert_file_contains "$n" "$CASE_DIR/.curl-args" "127.0.0.1:17350" && pass "$n"

  n="sin variable de entorno usa el puerto de deploy/local/.env"
  new_repo "$CB"; curl_stub; docker_stub
  mkdir -p "$CASE_DIR/deploy/local"
  printf '# comentario\nNAKAMA_API_PORT="18000"\n' >"$CASE_DIR/deploy/local/.env"
  run_in_case -- "$CB"
  assert_exit "$n" 0 && assert_file_contains "$n" "$CASE_DIR/.curl-args" "127.0.0.1:18000" && pass "$n"

  n="puerto no numérico → 64"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case NAKAMA_API_PORT='7350;rm' -- "$CB"
  assert_exit "$n" 64 && assert_file_contains "$n" "$ERR" "NAKAMA_API_PORT" && pass "$n"
}

# --- ejecución ------------------------------------------------------------------------------

test_check_backend

echo
printf 'Resultado: %d correctas, %d fallidas, %d omitidas\n' "$PASSED" "$FAILED" "$SKIPPED"
[[ "$FAILED" -eq 0 ]]
