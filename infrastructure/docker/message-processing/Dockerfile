FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY src/shared/Contracts/Contracts.csproj shared/Contracts/
COPY src/shared/Messaging/Messaging.csproj shared/Messaging/
COPY src/services/MessageProcessing/MessageProcessing.csproj services/MessageProcessing/
RUN dotnet restore services/MessageProcessing/MessageProcessing.csproj

COPY src/shared/ shared/
COPY src/services/MessageProcessing/ services/MessageProcessing/
RUN dotnet publish services/MessageProcessing/MessageProcessing.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Tesseract language data
RUN apk add --no-cache tesseract-ocr tesseract-ocr-data-eng

RUN adduser -D -h /app appuser
USER appuser

COPY --from=build /app/publish .

ENV Ocr__TessDataPath=/usr/share/tessdata

EXPOSE 8081
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -qO- http://localhost:8081/health || exit 1

ENTRYPOINT ["dotnet", "MessageProcessing.dll"]
