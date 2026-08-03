# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files for layer caching
COPY src/FraudDetection.Domain/*.csproj src/FraudDetection.Domain/
COPY src/FraudDetection.Application/*.csproj src/FraudDetection.Application/
COPY src/FraudDetection.Infrastructure/*.csproj src/FraudDetection.Infrastructure/
COPY src/FraudDetection.Api/*.csproj src/FraudDetection.Api/

# Restore
RUN dotnet restore src/FraudDetection.Api/FraudDetection.Api.csproj

# Copy source
COPY src/ src/

# Publish
WORKDIR /src/src/FraudDetection.Api
RUN dotnet publish FraudDetection.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
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

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "FraudDetection.Api.dll"]
