#!/usr/bin/env bash
#
# Builds and runs SCAIS - the Smart Course Advising Information System.
#
#   ./run.sh              check the prerequisites, build, then start the application
#   ./run.sh --no-build   start the application that has already been built
#   ./run.sh --check      check the prerequisites and exit without building or starting
#   ./run.sh --help       show this text
#
# SCAIS is Windows Forms on .NET Framework 4.8 with SQL Server LocalDB, so it runs on
# Windows only. This script works from Git Bash, MSYS2 or Cygwin on Windows. The script
# waits while the application is open, so it exits when the last window is closed.

set -uo pipefail

build=1
check_only=0

for argument in "$@"; do
    case "$argument" in
        --no-build) build=0 ;;
        --check)    check_only=1 ;;
        -h|--help)
            sed -n '2,13p' "$0" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
        *)
            echo "run.sh: unknown option '$argument' (try --help)" >&2
            exit 2
            ;;
    esac
done

# The paths below are relative to the repository root, so run from wherever the script is.
cd "$(dirname "$0")" || exit 1

case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*) ;;
    *)
        cat >&2 <<'EOF'
run.sh: SCAIS runs on Windows only.

  The application is Windows Forms on .NET Framework 4.8 and it stores its data in SQL
  Server LocalDB; neither exists on Linux or macOS. From another operating system the
  design work still stands on its own - see docs/ for the diagrams, the report and the
  database scripts - and the application is built and run on Windows by following
  docs/08-windows-first-run-and-test.md.
EOF
        exit 1
        ;;
esac

solution="src/SCAIS/SCAIS.sln"
executable="src/SCAIS/SCAIS/bin/Debug/net48/SCAIS.exe"

# ------------------------------------------------------------------ prerequisites

find_dotnet() {
    if command -v dotnet >/dev/null 2>&1; then
        command -v dotnet
    elif [ -x "$HOME/.dotnet/dotnet.exe" ]; then
        echo "$HOME/.dotnet/dotnet.exe"
    elif [ -x "$HOME/.dotnet/dotnet" ]; then
        echo "$HOME/.dotnet/dotnet"
    elif [ -x "/c/Program Files/dotnet/dotnet.exe" ]; then
        echo "/c/Program Files/dotnet/dotnet.exe"
    else
        echo ""
    fi
}

find_sqllocaldb() {
    if command -v SqlLocalDB.exe >/dev/null 2>&1; then
        command -v SqlLocalDB.exe
        return
    fi

    local candidate

    for candidate in "/c/Program Files/Microsoft SQL Server"/*/Tools/Binn/SqlLocalDB.exe; do
        if [ -x "$candidate" ]; then
            echo "$candidate"
            return
        fi
    done

    echo ""
}

dotnet_path="$(find_dotnet)"
sqllocaldb_path="$(find_sqllocaldb)"
problems=0

if [ -n "$dotnet_path" ]; then
    echo "dotnet      : $dotnet_path"
    echo "              version $("$dotnet_path" --version 2>/dev/null || echo 'unknown')"
else
    echo "dotnet      : not found, so the solution cannot be built" >&2
    echo "              install the .NET SDK, or open the solution in Visual Studio" >&2
    problems=1
fi

if [ -n "$sqllocaldb_path" ]; then
    echo "SqlLocalDB  : $sqllocaldb_path"

    if "$sqllocaldb_path" info MSSQLLOCALDB >/dev/null 2>&1; then
        echo "instance    : MSSQLLOCALDB is ready"
    else
        echo "instance    : MSSQLLOCALDB is missing, creating it"
        "$sqllocaldb_path" create MSSQLLOCALDB >/dev/null 2>&1 || {
            echo "instance    : could not be created" >&2
            problems=1
        }
    fi
else
    echo "SqlLocalDB  : not found, so the application cannot create its database" >&2
    echo "              add it from the Visual Studio Installer; see docs/08 section 2" >&2
    problems=1
fi

if [ -f "$executable" ]; then
    echo "application : $executable (built)"
else
    echo "application : $executable (not built yet)"
fi

if [ "$check_only" = 1 ]; then
    exit "$problems"
fi

if [ "$problems" != 0 ]; then
    echo "" >&2
    echo "run.sh: resolve the items above first." >&2
    echo "        docs/08-windows-first-run-and-test.md section 2 says where each one comes from." >&2
    exit 1
fi

# ------------------------------------------------------------------ build

if [ "$build" = 1 ]; then
    echo ""
    echo "Building $solution ..."

    if ! "$dotnet_path" build "$solution" -c Debug -v minimal; then
        echo "" >&2
        echo "run.sh: the build failed, so there is nothing to start." >&2
        exit 1
    fi
fi

if [ ! -f "$executable" ]; then
    echo "" >&2
    echo "run.sh: $executable does not exist. Run without --no-build to build it first." >&2
    exit 1
fi

# ------------------------------------------------------------------ run

cat <<'EOF'

Starting SCAIS. On a first run the application creates the SCAIS database from the schema
and seed data embedded in the assembly, so nothing has to be attached or configured.

    administrator   admin     / Admin@123
    adviser         adviser02 / Adviser@123
    student         student02 / Student@123

Sign out from the status bar of any dashboard to return to the login screen. Closing the
window ends the application and this script.
EOF

echo ""
"$executable"
