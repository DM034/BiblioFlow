#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RUN_DIR="$ROOT_DIR/.run"

BACK_DIR="$ROOT_DIR/Biblio.BackOffice"
FRONT_DIR="$ROOT_DIR/Biblio.FrontOffice"

SQL_CONTAINER="sql1biblio"
SQL_IMAGE="mcr.microsoft.com/mssql/server:2022-latest"
SQL_PASSWORD="Root12345678"

BACK_PORT=5161
FRONT_PORT=5193

BACK_PID_FILE="$RUN_DIR/backoffice.pid"
FRONT_PID_FILE="$RUN_DIR/frontoffice.pid"

BACK_LOG="$RUN_DIR/backoffice.log"
FRONT_LOG="$RUN_DIR/frontoffice.log"

mkdir -p "$RUN_DIR"

print_help() {
  cat <<'EOF'
Usage: ./scripts/dev.sh <command>

Commands:
  up        Start Docker/SQL, run migrations, then start BackOffice + FrontOffice
  down      Stop BackOffice + FrontOffice
  down-all  Stop BackOffice + FrontOffice + SQL container
  status    Show current runtime status
  logs      Tail app logs (BackOffice + FrontOffice)
EOF
}

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Error: required command '$1' not found." >&2
    exit 1
  fi
}

is_port_listening() {
  local port="$1"
  lsof -nP -iTCP:"$port" -sTCP:LISTEN -t >/dev/null 2>&1
}

wait_http() {
  local url="$1"
  local timeout="${2:-120}"
  local elapsed=0

  while [ "$elapsed" -lt "$timeout" ]; do
    if curl -s -o /dev/null "$url"; then
      return 0
    fi
    sleep 1
    elapsed=$((elapsed + 1))
  done

  return 1
}

wait_sql_ready() {
  local elapsed=0
  local timeout=240

  while [ "$elapsed" -lt "$timeout" ]; do
    if docker logs "$SQL_CONTAINER" 2>&1 | grep -q "SQL Server is now ready for client connections"; then
      return 0
    fi
    sleep 2
    elapsed=$((elapsed + 2))
  done

  return 1
}

ensure_docker_engine() {
  require_cmd docker

  if docker info >/dev/null 2>&1; then
    return 0
  fi

  if command -v colima >/dev/null 2>&1; then
    echo "Docker daemon unavailable, starting Colima..."
    colima start >/dev/null
  fi

  if ! docker info >/dev/null 2>&1; then
    echo "Error: Docker daemon is not reachable." >&2
    echo "Start Docker Desktop or run: colima start" >&2
    exit 1
  fi
}

ensure_sql_container() {
  if docker ps --format '{{.Names}}' | grep -qx "$SQL_CONTAINER"; then
    echo "SQL container '$SQL_CONTAINER' is already running."
    return
  fi

  if docker ps -a --format '{{.Names}}' | grep -qx "$SQL_CONTAINER"; then
    echo "Starting existing SQL container '$SQL_CONTAINER'..."
    docker start "$SQL_CONTAINER" >/dev/null
  else
    echo "Creating SQL container '$SQL_CONTAINER'..."
    docker run -d \
      --name "$SQL_CONTAINER" \
      -e "ACCEPT_EULA=Y" \
      -e "MSSQL_SA_PASSWORD=$SQL_PASSWORD" \
      -p 1433:1433 \
      "$SQL_IMAGE" >/dev/null
  fi

  echo "Waiting for SQL Server readiness..."
  if wait_sql_ready; then
    echo "SQL Server is ready."
  else
    echo "Warning: SQL readiness probe timed out, continuing anyway." >&2
  fi
}

ensure_dotnet_ef() {
  local ef_tool="$HOME/.dotnet/tools/dotnet-ef"

  if [ ! -x "$ef_tool" ]; then
    echo "Installing dotnet-ef global tool..."
    dotnet tool install -g dotnet-ef >/dev/null 2>&1 || dotnet tool update -g dotnet-ef >/dev/null 2>&1
  fi

  if [ ! -x "$ef_tool" ]; then
    echo "Error: dotnet-ef installation failed." >&2
    exit 1
  fi
}

restore_projects() {
  echo "Restoring BackOffice..."
  (cd "$BACK_DIR" && dotnet restore >/dev/null)

  echo "Restoring FrontOffice..."
  (cd "$FRONT_DIR" && dotnet restore >/dev/null)
}

apply_migrations() {
  local ef_tool="$HOME/.dotnet/tools/dotnet-ef"
  echo "Applying EF migrations..."
  (cd "$BACK_DIR" && "$ef_tool" database update >/dev/null)
}

start_backoffice() {
  if is_port_listening "$BACK_PORT"; then
    echo "BackOffice already running on http://localhost:$BACK_PORT"
    return
  fi

  echo "Starting BackOffice..."
  (
    cd "$BACK_DIR"
    nohup env ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --urls "http://localhost:$BACK_PORT" >"$BACK_LOG" 2>&1 &
    echo $! >"$BACK_PID_FILE"
  )

  if ! wait_http "http://localhost:$BACK_PORT/Login" 120; then
    echo "Error: BackOffice failed to start. See log: $BACK_LOG" >&2
    tail -n 60 "$BACK_LOG" || true
    exit 1
  fi

  echo "BackOffice is up: http://localhost:$BACK_PORT/Login"
}

start_frontoffice() {
  if is_port_listening "$FRONT_PORT"; then
    echo "FrontOffice already running on http://localhost:$FRONT_PORT"
    return
  fi

  echo "Starting FrontOffice..."
  (
    cd "$FRONT_DIR"
    nohup env ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --urls "http://localhost:$FRONT_PORT" >"$FRONT_LOG" 2>&1 &
    echo $! >"$FRONT_PID_FILE"
  )

  if ! wait_http "http://localhost:$FRONT_PORT/Books" 120; then
    echo "Error: FrontOffice failed to start. See log: $FRONT_LOG" >&2
    tail -n 60 "$FRONT_LOG" || true
    exit 1
  fi

  echo "FrontOffice is up: http://localhost:$FRONT_PORT/Books"
}

stop_pid_file() {
  local pid_file="$1"
  local label="$2"

  if [ ! -f "$pid_file" ]; then
    return
  fi

  local pid
  pid="$(cat "$pid_file" 2>/dev/null || true)"

  if [ -n "$pid" ] && kill -0 "$pid" >/dev/null 2>&1; then
    echo "Stopping $label (pid $pid)..."
    kill "$pid" >/dev/null 2>&1 || true
    sleep 1
    if kill -0 "$pid" >/dev/null 2>&1; then
      kill -9 "$pid" >/dev/null 2>&1 || true
    fi
  fi

  rm -f "$pid_file"
}

stop_by_port() {
  local port="$1"
  local label="$2"

  local pids
  pids="$(lsof -nP -iTCP:"$port" -sTCP:LISTEN -t 2>/dev/null || true)"

  if [ -n "$pids" ]; then
    echo "Stopping $label on port $port..."
    echo "$pids" | xargs -n1 kill >/dev/null 2>&1 || true
  fi
}

stop_apps() {
  stop_pid_file "$BACK_PID_FILE" "BackOffice"
  stop_pid_file "$FRONT_PID_FILE" "FrontOffice"

  stop_by_port "$BACK_PORT" "BackOffice"
  stop_by_port "$FRONT_PORT" "FrontOffice"

  echo "Apps stopped."
}

stop_sql_container() {
  if ! docker info >/dev/null 2>&1; then
    echo "Docker daemon not reachable, skipping SQL container shutdown."
    return
  fi

  if docker ps --format '{{.Names}}' | grep -qx "$SQL_CONTAINER"; then
    echo "Stopping SQL container '$SQL_CONTAINER'..."
    docker stop "$SQL_CONTAINER" >/dev/null
  else
    echo "SQL container '$SQL_CONTAINER' is already stopped."
  fi
}

show_status() {
  local docker_status="down"
  local sql_status="unknown"
  local back_status="down"
  local front_status="down"

  if docker info >/dev/null 2>&1; then
    docker_status="up"
    if docker ps --format '{{.Names}}' | grep -qx "$SQL_CONTAINER"; then
      sql_status="running"
    elif docker ps -a --format '{{.Names}}' | grep -qx "$SQL_CONTAINER"; then
      sql_status="stopped"
    else
      sql_status="missing"
    fi
  else
    sql_status="docker-down"
  fi

  if is_port_listening "$BACK_PORT"; then
    back_status="running"
  fi

  if is_port_listening "$FRONT_PORT"; then
    front_status="running"
  fi

  echo "Docker daemon : $docker_status"
  echo "SQL container : $sql_status"
  echo "BackOffice    : $back_status (http://localhost:$BACK_PORT/Login)"
  echo "FrontOffice   : $front_status (http://localhost:$FRONT_PORT/Books)"
}

tail_logs() {
  touch "$BACK_LOG" "$FRONT_LOG"
  tail -n 80 -f "$BACK_LOG" "$FRONT_LOG"
}

main() {
  local command="${1:-}"

  case "$command" in
    up)
      require_cmd dotnet
      require_cmd curl
      require_cmd lsof
      ensure_docker_engine
      ensure_sql_container
      ensure_dotnet_ef
      restore_projects
      apply_migrations
      start_backoffice
      start_frontoffice
      echo
      show_status
      ;;
    down)
      stop_apps
      ;;
    down-all)
      ensure_docker_engine
      stop_apps
      stop_sql_container
      ;;
    status)
      show_status
      ;;
    logs)
      tail_logs
      ;;
    *)
      print_help
      exit 1
      ;;
  esac
}

main "$@"