# Repository Structure & GitHub Setup

## Repo Strategy: Monorepo

Όλα τα services σε ένα GitHub repository: **`stoixima-platform`**

### Γιατί Monorepo (για αυτό το project)
- Ένα PR μπορεί να αλλάζει contracts + service μαζί
- Shared code (Contracts, Messaging helpers) χωρίς npm/NuGet packages
- Ευκολότερο onboarding για νέο developer
- CI/CD με path filters: κάθε service build μόνο όταν αλλάζει

---

## Δομή Φακέλων

```
stoixima-platform/
│
├── src/
│   ├── services/
│   │   ├── TelegramGateway/          # .NET 9 Worker Service
│   │   │   ├── TelegramGateway.csproj
│   │   │   ├── Worker.cs
│   │   │   ├── Dockerfile
│   │   │   └── ...
│   │   │
│   │   ├── StorageService/           # .NET 9 Worker Service
│   │   │   ├── StorageService.csproj
│   │   │   ├── Dockerfile
│   │   │   └── ...
│   │   │
│   │   ├── MessageProcessing/        # .NET 9 Worker Service
│   │   │   ├── MessageProcessing.csproj
│   │   │   ├── Dockerfile
│   │   │   └── ...
│   │   │
│   │   ├── MatchDataService/         # .NET 9 Worker Service
│   │   │   ├── MatchDataService.csproj
│   │   │   ├── Dockerfile
│   │   │   └── ...
│   │   │
│   │   ├── AggregationService/       # .NET 9 Worker Service
│   │   │   ├── AggregationService.csproj
│   │   │   ├── Dockerfile
│   │   │   └── ...
│   │   │
│   │   └── ApiGateway/               # ASP.NET Core Web API
│   │       ├── ApiGateway.csproj
│   │       ├── Dockerfile
│   │       └── ...
│   │
│   ├── shared/
│   │   ├── Contracts/                # Kafka event schemas (shared DTOs)
│   │   │   ├── Events/
│   │   │   │   ├── RawMessageEvent.cs
│   │   │   │   ├── ProcessedTipEvent.cs
│   │   │   │   └── AggregatedMatchEvent.cs
│   │   │   └── Contracts.csproj
│   │   │
│   │   ├── Messaging/                # Kafka producer/consumer helpers
│   │   │   ├── KafkaProducer.cs
│   │   │   ├── KafkaConsumer.cs
│   │   │   └── Messaging.csproj
│   │   │
│   │   └── Observability/            # OpenTelemetry setup
│   │       ├── TracingExtensions.cs
│   │       └── Observability.csproj
│   │
│   └── frontend/                     # React + TypeScript
│       ├── src/
│       │   ├── components/
│       │   │   ├── MatchCard/
│       │   │   ├── TipFeed/
│       │   │   └── TipperDetail/
│       │   ├── hooks/
│       │   │   └── useFeedWebSocket.ts
│       │   └── App.tsx
│       ├── package.json
│       └── ...
│
├── infrastructure/
│   ├── docker-compose.yml            # Local development
│   ├── docker-compose.override.yml   # Local secrets
│   ├── kubernetes/
│   │   ├── namespaces.yaml
│   │   ├── telegram-gateway/
│   │   ├── storage-service/
│   │   ├── message-processing/
│   │   ├── aggregation-service/
│   │   └── api-gateway/
│   └── monitoring/
│       ├── prometheus.yml
│       └── grafana/
│           └── dashboards/
│
├── .github/
│   ├── workflows/
│   │   ├── ci-telegram-gateway.yml   # Build/test on path change
│   │   ├── ci-storage-service.yml
│   │   ├── ci-message-processing.yml
│   │   ├── ci-match-data.yml
│   │   ├── ci-aggregation.yml
│   │   ├── ci-api-gateway.yml
│   │   └── ci-frontend.yml
│   ├── ISSUE_TEMPLATE/
│   │   ├── feature.md
│   │   └── bug.md
│   └── PULL_REQUEST_TEMPLATE.md
│
├── docs/                             # (copy of /documentation)
├── stoixima-platform.sln             # .NET Solution file
└── README.md
```

---

## GitHub Issues — Labels

Δημιούργησε τα παρακάτω labels πριν τα issues:

### Service Labels
| Label | Χρώμα | Περιγραφή |
|-------|-------|-----------|
| `service:telegram-gateway` | `#0075ca` | Telegram Gateway MS |
| `service:storage` | `#e4e669` | Storage MS |
| `service:message-processing` | `#d93f0b` | Message Processing MS |
| `service:match-data` | `#0e8a16` | Match Data MS |
| `service:aggregation` | `#5319e7` | Aggregation MS |
| `service:api-gateway` | `#1d76db` | API Gateway |
| `frontend` | `#f9d0c4` | React Frontend |
| `infrastructure` | `#bfdadc` | Docker/K8s/CI |
| `documentation` | `#cfd3d7` | Docs |

### Priority Labels
| Label | Χρώμα |
|-------|-------|
| `priority:critical` | `#b60205` |
| `priority:high` | `#e99695` |
| `priority:medium` | `#f9d0c4` |
| `priority:low` | `#fef2c0` |

---

## GitHub Milestones

| Milestone | Περιγραφή |
|-----------|-----------|
| **Phase 0 — Foundation** | Monorepo, Docker Compose, Kafka setup, shared contracts |
| **Phase 1 — Data Ingestion** | Telegram Gateway + Storage Service |
| **Phase 2 — Data Processing** | Message Processing + Match Data Service |
| **Phase 3 — Aggregation & API** | Aggregation Service + API Gateway |
| **Phase 4 — Frontend** | React real-time feed |
| **Phase 5 — DevOps** | CI/CD, Kubernetes, Monitoring |

---

## CI/CD Path Filter Example

Κάθε workflow τρέχει μόνο όταν αλλάζει το αντίστοιχο service:

```yaml
# .github/workflows/ci-telegram-gateway.yml
on:
  push:
    paths:
      - 'src/services/TelegramGateway/**'
      - 'src/shared/**'
```
