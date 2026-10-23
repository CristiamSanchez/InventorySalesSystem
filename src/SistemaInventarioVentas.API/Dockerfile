# ==============================================================================
# Multi-stage Dockerfile for SistemaInventarioVentas.API (.NET 10 Minimal APIs)
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Build & Publish
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for dependency caching
COPY SistemaInventarioVentas.slnx ./
COPY src/SistemaInventarioVentas.Domain/SistemaInventarioVentas.Domain.csproj src/SistemaInventarioVentas.Domain/
COPY src/SistemaInventarioVentas.Application/SistemaInventarioVentas.Application.csproj src/SistemaInventarioVentas.Application/
COPY src/SistemaInventarioVentas.Infrastructure/SistemaInventarioVentas.Infrastructure.csproj src/SistemaInventarioVentas.Infrastructure/
COPY src/SistemaInventarioVentas.API/SistemaInventarioVentas.API.csproj src/SistemaInventarioVentas.API/

# Restore dependencies
RUN dotnet restore src/SistemaInventarioVentas.API/SistemaInventarioVentas.API.csproj

# Copy application source code
COPY src/ ./src/

# Build and publish release binaries without executable host wrapper
WORKDIR /src/src/SistemaInventarioVentas.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ------------------------------------------------------------------------------
# Stage 2: Minimal Runtime
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as non-root user (app user, UID 1654 in official .NET images)
USER $APP_UID

# Copy compiled binaries from build stage
COPY --from=build /app/publish .

# Configure container networking on port 8080 (standard non-root port in .NET 8/9/10)
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "SistemaInventarioVentas.API.dll"]
