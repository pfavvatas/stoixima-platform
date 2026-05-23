FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY src/shared/Contracts/Contracts.csproj shared/Contracts/
COPY src/shared/Messaging/Messaging.csproj shared/Messaging/
COPY src/services/ApiGateway/ApiGateway.csproj services/ApiGateway/
RUN dotnet restore services/ApiGateway/ApiGateway.csproj

COPY src/shared/ shared/
COPY src/services/ApiGateway/ services/ApiGateway/
RUN dotnet publish services/ApiGateway/ApiGateway.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

RUN adduser -D -h /app appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 5000
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -qO- http://localhost:5000/health || exit 1

ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
