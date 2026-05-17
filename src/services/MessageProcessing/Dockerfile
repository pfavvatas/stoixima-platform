# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/shared/Contracts/ src/shared/Contracts/
COPY src/shared/Messaging/  src/shared/Messaging/
COPY src/services/MessageProcessing/ src/services/MessageProcessing/

RUN dotnet restore src/services/MessageProcessing/MessageProcessing.csproj

RUN dotnet publish src/services/MessageProcessing/MessageProcessing.csproj \
    -c Release -o /app --no-restore

# Runtime stage — includes Tesseract OCR engine + English language data
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-eng \
        libtesseract-dev \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MessageProcessing.dll"]
