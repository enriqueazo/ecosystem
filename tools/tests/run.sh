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
    env -u NAKAMA_API_PORT -u NAKAMA_RUNTIME_HTTP_KEY PATH="$CASE_DIR/.stubs:$PATH" ${env_args[@]+"${env_args[@]}"} \
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
  fail "$name" "no contiene «${text}»: $(tr '\n' ' ' <"$file")"
  return 1
}

# --- check-backend.sh -----------------------------------------------------------------------

CB="tools/check-backend.sh"

# curl simulado: escribe sus argumentos en .curl-args e imita el comportamiento de
# `curl -s -o /dev/null -w '%{http_code}'` según CURL_MODE (ok | http500 | refused).
# Para la comprobación de salud imita `curl -s -o /dev/null -w '%{http_code}'` según CURL_MODE
# (ok | http500 | refused). Para el RPC (--module) imita `curl -s -w '\n%{http_code}'` según
# RPC_MODE: loaded (403 SESSION_REQUIRED), missing (404, función no registrada) u odd (500).
curl_stub() {
  stub curl 'printf "%s\n" "$@" >>"'"$CASE_DIR"'/.curl-args"
case "$*" in
  */v2/rpc/*)
    case "${RPC_MODE:-loaded}" in
      loaded) printf "%s\n403" "{\"code\":7,\"message\":\"{\\\"code\\\":\\\"SESSION_REQUIRED\\\",\\\"message\\\":\\\"x\\\"}\"}" ;;
      missing) printf "%s\n404" "{\"code\":5,\"message\":\"RPC function not found\"}" ;;
      odd) printf "%s\n500" "{}" ;;
    esac
    exit 0 ;;
esac
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

  n="sin respuesta y sin server-nakama/build/index.js → 1 con la indicación de construir"
  new_repo "$CB"; curl_stub; docker_stub
  mkdir -p "$CASE_DIR/server-nakama"; printf '{}\n' >"$CASE_DIR/server-nakama/package.json"
  run_in_case CURL_MODE=refused DOCKER_MODE=up -- "$CB"
  assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" "server-nakama/build/index.js" \
    && assert_file_contains "$n" "$ERR" "npm run build" && pass "$n"

  n="sin respuesta con el módulo construido → 1 sin la indicación de construir"
  new_repo "$CB"; curl_stub; docker_stub
  mkdir -p "$CASE_DIR/server-nakama/build"; printf '{}\n' >"$CASE_DIR/server-nakama/package.json"
  printf 'function InitModule() {}\n' >"$CASE_DIR/server-nakama/build/index.js"
  run_in_case CURL_MODE=refused DOCKER_MODE=up -- "$CB"
  if assert_exit "$n" 1; then
    if grep -q "npm run build" "$ERR"; then fail "$n" "sugiere construir aunque el módulo existe"; else pass "$n"; fi
  fi

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

  n="--module con el módulo cargado (403 SESSION_REQUIRED) → 0"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case NAKAMA_RUNTIME_HTTP_KEY=clave-de-prueba-123 RPC_MODE=loaded -- "$CB" --module
  assert_exit "$n" 0 && assert_file_contains "$n" "$OUT" "Módulo cargado" \
    && assert_file_contains "$n" "$CASE_DIR/.curl-args" "/v2/rpc/protocol_version" && pass "$n"

  n="--module sin el módulo (404) → 4 con la indicación de construir"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case NAKAMA_RUNTIME_HTTP_KEY=clave-de-prueba-123 RPC_MODE=missing -- "$CB" --module
  assert_exit "$n" 4 && assert_file_contains "$n" "$ERR" "módulo no cargado" \
    && assert_file_contains "$n" "$ERR" "npm run build" && pass "$n"

  n="--module con una respuesta inesperada (500) → 4"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case NAKAMA_RUNTIME_HTTP_KEY=clave-de-prueba-123 RPC_MODE=odd -- "$CB" --module
  assert_exit "$n" 4 && pass "$n"

  n="--module con la salud fallida → 1 (sin cambios)"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case NAKAMA_RUNTIME_HTTP_KEY=clave-de-prueba-123 CURL_MODE=refused -- "$CB" --module
  assert_exit "$n" 1 && pass "$n"

  n="--module sin NAKAMA_RUNTIME_HTTP_KEY → 64"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case -- "$CB" --module
  assert_exit "$n" 64 && assert_file_contains "$n" "$ERR" "NAKAMA_RUNTIME_HTTP_KEY" && pass "$n"

  n="--module lee la clave de deploy/local/.env"
  new_repo "$CB"; curl_stub; docker_stub
  mkdir -p "$CASE_DIR/deploy/local"; printf 'NAKAMA_RUNTIME_HTTP_KEY=clave-del-env-456\n' >"$CASE_DIR/deploy/local/.env"
  run_in_case RPC_MODE=loaded -- "$CB" --module
  assert_exit "$n" 0 && assert_file_contains "$n" "$CASE_DIR/.curl-args" "clave-del-env-456" && pass "$n"

  n="--module no imprime la clave"
  new_repo "$CB"; curl_stub; docker_stub
  run_in_case NAKAMA_RUNTIME_HTTP_KEY=clave-de-prueba-123 RPC_MODE=missing -- "$CB" --module
  if grep -q "clave-de-prueba-123" "$OUT" "$ERR"; then
    fail "$n" "la clave aparece en la salida"
  else
    pass "$n"
  fi
}

# --- check-repo.sh --------------------------------------------------------------------------

CR="tools/check-repo.sh"
CR_FILES=("$CR" ".gitignore" "deploy/local/compose.yaml" "deploy/local/.env.example")

# Repo temporal con los archivos reales necesarios, todos confirmados en Git.
new_repo_for_check_repo() {
  new_repo "${CR_FILES[@]}"
  git -C "$CASE_DIR" add -A -- . ':!.stubs'
  git -C "$CASE_DIR" commit -q -m "base"
}

# Simula un proyecto Unity mínimo para que se comprueben las rutas del cliente.
fake_unity_project() {
  mkdir -p "$CASE_DIR/client-unity/ProjectSettings" "$CASE_DIR/client-unity/Packages"
}

# Simula el proyecto del servidor para que se comprueben su lockfile y .node-version.
fake_server_project() {
  mkdir -p "$CASE_DIR/server-nakama"
  printf '{"name":"x","version":"0.0.0"}\n' >"$CASE_DIR/server-nakama/package.json"
}

have_docker_cli() { command -v docker >/dev/null 2>&1; }

test_check_repo() {
  echo "check-repo.sh"
  local n

  n="repo válido → 0"
  if have_docker_cli; then
    new_repo_for_check_repo
    run_in_case -- "$CR"
    assert_exit "$n" 0 && pass "$n"
  else
    skip "$n" "CLI de Docker no disponible"
  fi

  n="--only secrets con deploy/local/.env en el índice → 1"
  new_repo_for_check_repo
  printf 'POSTGRES_PASSWORD=real\n' >"$CASE_DIR/deploy/local/.env"
  git -C "$CASE_DIR" add -f deploy/local/.env
  run_in_case -- "$CR" --only secrets
  assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" "deploy/local/.env" && pass "$n"

  n="--only secrets con .env.local en el índice → 1"
  new_repo_for_check_repo
  printf 'X=1\n' >"$CASE_DIR/.env.local"
  git -C "$CASE_DIR" add -f .env.local
  run_in_case -- "$CR" --only secrets
  assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" ".env.local" && pass "$n"

  n="--only secrets con datos de PostgreSQL en el índice → 1"
  new_repo_for_check_repo
  mkdir -p "$CASE_DIR/deploy/local/pgdata"; printf '16\n' >"$CASE_DIR/deploy/local/pgdata/PG_VERSION"
  git -C "$CASE_DIR" add -f deploy/local/pgdata/PG_VERSION
  run_in_case -- "$CR" --only secrets
  assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" "PG_VERSION" && pass "$n"

  n="--only secrets con la configuración local del cliente en el índice → 1"
  new_repo_for_check_repo
  mkdir -p "$CASE_DIR/client-unity/LocalConfig"
  printf '{"serverKey":"real"}\n' >"$CASE_DIR/client-unity/LocalConfig/connection.local.json"
  git -C "$CASE_DIR" add -f client-unity/LocalConfig/connection.local.json
  run_in_case -- "$CR" --only secrets
  assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" "client-unity/LocalConfig/connection.local.json" && pass "$n"

  n="--only secrets permite connection.example.json → 0"
  new_repo_for_check_repo
  mkdir -p "$CASE_DIR/client-unity/LocalConfig"
  printf '{}\n' >"$CASE_DIR/client-unity/LocalConfig/connection.example.json"
  git -C "$CASE_DIR" add client-unity/LocalConfig/connection.example.json
  run_in_case -- "$CR" --only secrets
  assert_exit "$n" 0 && pass "$n"

  n="--only secrets permite .env.example → 0"
  new_repo_for_check_repo
  run_in_case -- "$CR" --only secrets
  assert_exit "$n" 0 && pass "$n"

  n="--only ignored con *.meta ignorado y proyecto Unity → 1"
  new_repo_for_check_repo; fake_unity_project
  printf '*.meta\n' >>"$CASE_DIR/.gitignore"
  run_in_case -- "$CR" --only ignored
  assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" "client-unity/Assets/_Project.meta" && pass "$n"

  n="--only ignored con .env.example ignorado → 1"
  new_repo_for_check_repo
  printf '.env.example\n' >>"$CASE_DIR/.gitignore"
  run_in_case -- "$CR" --only ignored
  assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" "deploy/local/.env.example" && pass "$n"

  n="--only ignored sin proyecto Unity omite las rutas del cliente → 0"
  new_repo_for_check_repo
  printf '*.meta\n' >>"$CASE_DIR/.gitignore"
  run_in_case -- "$CR" --only ignored
  assert_exit "$n" 0 && pass "$n"

  n="--only compose con compose.yaml inválido → 1"
  if have_docker_cli; then
    new_repo_for_check_repo
    printf 'services:\n  roto: [\n' >"$CASE_DIR/deploy/local/compose.yaml"
    run_in_case -- "$CR" --only compose
    assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" "compose" && pass "$n"
  else
    skip "$n" "CLI de Docker no disponible"
  fi

  n="--only compose sin CLI de Docker → 1 con mensaje explícito"
  new_repo_for_check_repo
  stub docker 'echo "docker: command not found" >&2; exit 127'
  run_in_case -- "$CR" --only compose
  assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" "CLI de Docker no disponible" && pass "$n"

  n="--only con comprobación desconocida → 64"
  new_repo_for_check_repo
  run_in_case -- "$CR" --only otra
  assert_exit "$n" 64 && assert_file_contains "$n" "$ERR" "Uso:" && pass "$n"

  n="--only ignored con el lockfile del servidor ignorado → 1"
  new_repo_for_check_repo; fake_server_project
  printf 'server-nakama/package-lock.json\n' >>"$CASE_DIR/.gitignore"
  run_in_case -- "$CR" --only ignored
  assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" "server-nakama/package-lock.json" && pass "$n"

  n="--only ignored con .node-version del servidor ignorado → 1"
  new_repo_for_check_repo; fake_server_project
  printf '.node-version\n' >>"$CASE_DIR/.gitignore"
  run_in_case -- "$CR" --only ignored
  assert_exit "$n" 1 && assert_file_contains "$n" "$ERR" "server-nakama/.node-version" && pass "$n"

  n="--only ignored sin server-nakama/package.json no comprueba el servidor → 0"
  new_repo_for_check_repo
  printf 'server-nakama/package-lock.json\n' >>"$CASE_DIR/.gitignore"
  run_in_case -- "$CR" --only ignored
  assert_exit "$n" 0 && pass "$n"

  n="argumento desconocido → 64"
  new_repo_for_check_repo
  run_in_case -- "$CR" --desconocido
  assert_exit "$n" 64 && pass "$n"
}

# --- ejecución ------------------------------------------------------------------------------

test_check_backend
test_check_repo

echo
printf 'Resultado: %d correctas, %d fallidas, %d omitidas\n' "$PASSED" "$FAILED" "$SKIPPED"
[[ "$FAILED" -eq 0 ]]
