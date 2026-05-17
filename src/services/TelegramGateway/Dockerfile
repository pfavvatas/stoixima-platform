# Build stage — uses full SDK image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared libraries first (better layer caching — changes rarely)
COPY src/shared/Contracts/ src/shared/Contracts/
COPY src/shared/Messaging/  src/shared/Messaging/

# Copy service
COPY src/services/TelegramGateway/ src/services/TelegramGateway/

# Restore (uses layer cache if nothing changed in .csproj files)
RUN dotnet restore src/services/TelegramGateway/TelegramGateway.csproj

# Publish
RUN dotnet publish src/services/TelegramGateway/TelegramGateway.csproj \
    -c Release -o /app --no-restore

# Runtime stage — slim ASP.NET image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Directory for Telegram session persistence (mount a PersistentVolume here in K8s)
RUN mkdir -p /data

COPY --from=build /app .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TelegramGateway.dll"]
