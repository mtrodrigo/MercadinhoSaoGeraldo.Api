# ===== build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

# Copia csproj e restaura (melhor cache)
COPY *.sln ./
COPY MercadinhoSaoGeraldo.Api/*.csproj ./MercadinhoSaoGeraldo.Api/
RUN dotnet restore

# Copia o restante e publica
COPY . .
RUN dotnet publish MercadinhoSaoGeraldo.Api/MercadinhoSaoGeraldo.Api.csproj -c Release -o /app/out

# ===== runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

# Variáveis úteis no container
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

# Copia app publicado
COPY --from=build /app/out ./

# Script de entrada para bind na porta do Render
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Healthcheck usa seu endpoint /ping
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- http://127.0.0.1:${PORT:-5000}/ping || exit 1

# usuário sem root
RUN useradd -m appuser
USER appuser

# Render injeta $PORT. O script exporta ASPNETCORE_URLS dinamicamente.
CMD ["/entrypoint.sh"]
