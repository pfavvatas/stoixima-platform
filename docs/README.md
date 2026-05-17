# Stoixima Platform — Documentation

Πλατφόρμα real-time aggregation στοιχηματικών προβλέψεων από Telegram channels.

## Τι κάνει

Διαβάζει μηνύματα από Telegram tipster channels, εξάγει προβλέψεις για αγώνες, τις αντιστοιχεί με σημερινά ματς και παρουσιάζει ένα real-time feed με consensus ανά αγώνα (π.χ. 60% Over 2.5, 30% 1X2 Home, 10% Draw).

## Δομή Documentation

| Αρχείο | Περιεχόμενο |
|--------|-------------|
| [architecture.md](architecture.md) | High-level αρχιτεκτονική, διάγραμμα, data flow |
| [repo-structure.md](repo-structure.md) | GitHub monorepo layout, CI/CD |
| [database-design.md](database-design.md) | Σχεδιασμός βάσεων (PostgreSQL, ClickHouse, Redis) |
| [services/01-telegram-gateway.md](services/01-telegram-gateway.md) | Telegram Gateway Service |
| [services/02-storage-service.md](services/02-storage-service.md) | Storage Service |
| [services/03-message-processing.md](services/03-message-processing.md) | Message Processing Service |
| [services/04-match-data-service.md](services/04-match-data-service.md) | Match Data Service |
| [services/05-aggregation-service.md](services/05-aggregation-service.md) | Aggregation Service |
| [services/06-api-gateway.md](services/06-api-gateway.md) | API Gateway |
| [frontend/frontend-spec.md](frontend/frontend-spec.md) | Frontend (React) spec |
| [infrastructure/local-dev.md](infrastructure/local-dev.md) | Local dev setup (Docker Compose) |
| [github-tasks.md](github-tasks.md) | Λίστα GitHub Issues σε σειρά υλοποίησης |

## Quick Overview

```
Telegram Channels
      │
      ▼
Telegram Gateway (WTelegramClient)
      │  Kafka: telegram.messages.raw
      ▼
Storage Service ──────────────► PostgreSQL (metadata) + ClickHouse (messages)
      │
      │  Kafka: telegram.messages.raw
      ▼
Message Processing Service ──► Εξαγωγή tip data, OCR, team recognition
      │  Kafka: telegram.messages.processed
      ▼
Aggregation Service ◄──────── Match Data Service (football API)
      │  Kafka: feed.matches.aggregated
      ▼
API Gateway (REST + WebSocket)
      │
      ▼
React Frontend (real-time feed)
```

## Stack Summary

- **Backend**: .NET 9 (Worker Services + ASP.NET Core)
- **Message Broker**: Apache Kafka
- **Databases**: PostgreSQL, ClickHouse, Redis
- **Frontend**: React + TypeScript
- **Infrastructure**: Docker, Kubernetes, GitHub Actions
- **Monitoring**: Prometheus + Grafana + Loki
