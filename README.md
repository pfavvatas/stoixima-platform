# Stoixima Platform

Real-time betting tips aggregator from Telegram channels — microservices architecture.

## What it does

Reads messages from Telegram tipster channels, extracts structured betting predictions using OCR + NLP, correlates them with today's football matches, and presents a real-time consensus feed on a web UI.

```
Telegram Channels → Gateway → Kafka → Processing → Aggregation → API → React UI
```

## Services

| Service | Description | Docs |
|---------|-------------|------|
| `TelegramGateway` | Reads Telegram, publishes raw messages to Kafka | [docs](docs/services/01-telegram-gateway.md) |
| `StorageService` | Persists raw messages to ClickHouse + PostgreSQL | [docs](docs/services/02-storage-service.md) |
| `MessageProcessing` | OCR + team recognition + tip extraction | [docs](docs/services/03-message-processing.md) |
| `MatchDataService` | Fetches today's fixtures from football API | [docs](docs/services/04-match-data-service.md) |
| `AggregationService` | Calculates consensus per match | [docs](docs/services/05-aggregation-service.md) |
| `ApiGateway` | REST + WebSocket for the frontend | [docs](docs/services/06-api-gateway.md) |
| `frontend` | React real-time feed UI | [docs](docs/frontend/frontend-spec.md) |

## Stack

- **Backend**: .NET 9 (Worker Services + ASP.NET Core)
- **Broker**: Apache Kafka
- **Databases**: PostgreSQL · ClickHouse · Redis
- **Frontend**: React 18 + TypeScript + TailwindCSS
- **Infra**: Docker · Kubernetes · GitHub Actions
- **Monitoring**: Prometheus + Grafana + Loki

## Quick Start (local)

```bash
# 1. Start infrastructure
cd infrastructure
docker compose up -d

# 2. Run migrations
psql -U postgres -d stoixima -f infrastructure/seeds/teams.sql

# 3. First-time Telegram auth (interactive OTP)
dotnet run --project src/services/TelegramGateway -- --setup

# 4. Start all services
dotnet run --project src/services/TelegramGateway
dotnet run --project src/services/StorageService
dotnet run --project src/services/MessageProcessing
dotnet run --project src/services/MatchDataService
dotnet run --project src/services/AggregationService
dotnet run --project src/services/ApiGateway

# 5. Frontend
cd src/frontend && npm install && npm run dev
```

Full setup guide: [docs/infrastructure/local-dev.md](docs/infrastructure/local-dev.md)

## Documentation

Full architecture and per-service specs: [docs/](docs/)

## GitHub Issues

Implementation tasks in order: [github.com/pfavvatas/stoixima-platform/issues](https://github.com/pfavvatas/stoixima-platform/issues)

Implementation order:
```
Phase 0 (Foundation):  #1 → #2 → #3, #4, #5
Phase 1 (Ingestion):   #6 → #7 → #8 → #9
Phase 2 (Processing):  #10-#14 + #15
Phase 3 (API):         #16 → #17 → #18
Phase 4 (Frontend):    #19 → #20 → #21
Phase 5 (DevOps):      #22 → #23 → #24, #25
```
