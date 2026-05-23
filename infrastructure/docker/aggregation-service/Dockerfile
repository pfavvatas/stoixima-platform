FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY src/shared/Contracts/Contracts.csproj shared/Contracts/
COPY src/shared/Messaging/Messaging.csproj shared/Messaging/
COPY src/services/AggregationService/AggregationService.csproj services/AggregationService/
RUN dotnet restore services/AggregationService/AggregationService.csproj

COPY src/shared/ shared/
COPY src/services/AggregationService/ services/AggregationService/
RUN dotnet publish services/AggregationService/AggregationService.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

RUN adduser -D -h /app appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8081
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -qO- http://localhost:8081/health || exit 1

ENTRYPOINT ["dotnet", "AggregationService.dll"]
