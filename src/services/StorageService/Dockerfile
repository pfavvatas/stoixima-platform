# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/shared/Contracts/ src/shared/Contracts/
COPY src/shared/Messaging/  src/shared/Messaging/
COPY src/services/StorageService/ src/services/StorageService/

RUN dotnet restore src/services/StorageService/StorageService.csproj

RUN dotnet publish src/services/StorageService/StorageService.csproj \
    -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "StorageService.dll"]
