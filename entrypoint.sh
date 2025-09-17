#!/usr/bin/env sh
set -e

export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-5000}"
echo "Starting API on $ASPNETCORE_URLS"

# Permite forçar o nome do DLL via APP_DLL, senão pega o primeiro *.dll
APP_DLL="${APP_DLL:-}"
if [ -z "$APP_DLL" ]; then
  APP_DLL="$(ls -1 *.dll | head -n 1)"
fi
echo "Launching: dotnet $APP_DLL"
exec dotnet "$APP_DLL"
