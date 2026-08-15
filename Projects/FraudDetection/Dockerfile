# Multi-stage Dockerfile building BOTH runnable artifacts from one build stage:
#   - target final-api    (default) — the FraudDetection API
#   - target final-worker — the anti-fraud Kafka worker
# docker-compose selects the target per service via `build.target`.

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files for layer caching
COPY src/FraudDetection.Domain/*.csproj src/FraudDetection.Domain/
COPY src/FraudDetection.Application/*.csproj src/FraudDetection.Application/
COPY src/FraudDetection.Infrastructure/*.csproj src/FraudDetection.Infrastructure/
COPY src/FraudDetection.Api/*.csproj src/FraudDetection.Api/
COPY src/FraudDetection.Worker/*.csproj src/FraudDetection.Worker/

# Restore both publishable projects
RUN dotnet restore src/FraudDetection.Api/FraudDetection.Api.csproj \
    && dotnet restore src/FraudDetection.Worker/FraudDetection.Worker.csproj

# Copy source
COPY src/ src/

# Publish API.
# NOTE (ADR-059): the publish deliberately does NOT pass
# -p:SourceRevisionId=$(git rev-parse HEAD) — the .git folder lives in the
# parent repository (above this build context), so git metadata is
# inaccessible here; the SDK image would also find no repo to query. The API
# exposes the commit via GET /api/v1/version only when the assembly carries
# a SourceRevisionId, so a build with access to git can add the flag later
# without any code change.
WORKDIR /src/src/FraudDetection.Api
RUN dotnet publish FraudDetection.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Publish Worker
WORKDIR /src/src/FraudDetection.Worker
RUN dotnet publish FraudDetection.Worker.csproj -c Release -o /app/worker/publish /p:UseAppHost=false

# ---------------------------------------------------------------- API stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final-api
WORKDIR /app

# Install curl for the healthcheck — not present in the base aspnet image.
# Installed before switching to the non-root user (apt-get requires root).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Non-root user for security ($APP_UID is defined in .NET 8 images)
USER $APP_UID

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

# Liveness probe only (/health/live — no dependencies, always 200 while the
# process serves): a Kafka/SQL Server outage must not cascade-restart the API
# (see ADR-059). Readiness with per-dependency detail lives at /health/ready
# for manual/curl inspection.
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "FraudDetection.Api.dll"]

# ------------------------------------------------------------- Worker stage
# The worker is a console host without an HTTP surface, so it has no
# HEALTHCHECK — docker-compose orders startup via the api/kafka healthchecks
# (worker depends_on api healthy + kafka healthy).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final-worker
WORKDIR /app

# Non-root user for security ($APP_UID is defined in .NET 8 images)
USER $APP_UID

COPY --from=build /app/worker/publish .

ENTRYPOINT ["dotnet", "FraudDetection.Worker.dll"]