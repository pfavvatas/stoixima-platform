FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY src/shared/Contracts/Contracts.csproj shared/Contracts/
COPY src/shared/Messaging/Messaging.csproj shared/Messaging/
COPY src/services/StorageService/StorageService.csproj services/StorageService/
RUN dotnet restore services/StorageService/StorageService.csproj

COPY src/shared/ shared/
COPY src/services/StorageService/ services/StorageService/
RUN dotnet publish services/StorageService/StorageService.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

RUN adduser -D -h /app appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8081
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -qO- http://localhost:8081/health || exit 1

ENTRYPOINT ["dotnet", "StorageService.dll"]
