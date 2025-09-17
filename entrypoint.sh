#!/usr/bin/env sh
set -e

export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-5000}"
echo "Starting API on $ASPNETCORE_URLS"

exec dotnet MercadinhoSaoGeraldo.Api.dll
