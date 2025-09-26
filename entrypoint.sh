#!/usr/bin/env sh
set -e

export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-5000}"
echo "Starting API on $ASPNETCORE_URLS"

# Permite forçar o nome do DLL via APP_DLL, senão tenta descobrir a partir do *.runtimeconfig.json
APP_DLL="${APP_DLL:-}"
if [ -z "$APP_DLL" ]; then
  if runtimeconfig="$(ls -1 *.runtimeconfig.json 2>/dev/null | head -n 1)" && [ -n "$runtimeconfig" ]; then
    APP_DLL="${runtimeconfig%.runtimeconfig.json}.dll"
  else
    APP_DLL="$(ls -1 *.dll | head -n 1)"
  fi
fi
echo "Launching: dotnet $APP_DLL"
exec dotnet "$APP_DLL"
