FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY src/services/MatchDataService/MatchDataService.csproj services/MatchDataService/
RUN dotnet restore services/MatchDataService/MatchDataService.csproj

COPY src/services/MatchDataService/ services/MatchDataService/
RUN dotnet publish services/MatchDataService/MatchDataService.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

RUN adduser -D -h /app appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8081
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -qO- http://localhost:8081/health || exit 1

ENTRYPOINT ["dotnet", "MatchDataService.dll"]
