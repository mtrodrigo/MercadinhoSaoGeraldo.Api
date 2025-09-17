# ===== build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

# Copiamos tudo para dentro do container (simples e robusto)
COPY . .

# Descobre o primeiro .csproj encontrado e publica
RUN set -eux; \
    PROJECT=$(find . -name "*.csproj" -print -quit); \
    echo "Usando projeto: $PROJECT"; \
    dotnet restore "$PROJECT"; \
    dotnet publish "$PROJECT" -c Release -o /app/out

# ===== runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

# Instala curl para o healthcheck
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

# Copia app publicado
COPY --from=build /app/out ./

# Script de entrada (usa $PORT do Render)
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Healthcheck bate no /ping
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fsS "http://127.0.0.1:${PORT:-5000}/ping" || exit 1

# Usuário não-root
RUN useradd -m appuser
USER appuser

CMD ["/entrypoint.sh"]
