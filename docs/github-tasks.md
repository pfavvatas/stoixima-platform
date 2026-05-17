# GitHub Issues — Λίστα Εργασιών σε Σειρά Υλοποίησης

Κάθε issue έχει: τίτλο, milestone, labels, περιγραφή, acceptance criteria.

---

## Phase 0 — Foundation (Ξεκίνα εδώ)

> **Σκοπός**: Το monorepo και το local development environment να είναι έτοιμα.
> Τίποτα δεν μπορεί να υλοποιηθεί χωρίς αυτό.

---

### Issue #1 — [INFRA] Δημιουργία monorepo structure
**Labels**: `infrastructure`, `priority:critical`
**Milestone**: Phase 0 — Foundation

**Περιγραφή**:
Δημιουργία του GitHub repository `stoixima-platform` με τη βασική δομή φακέλων.

**Tasks**:
- [ ] Δημιουργία repo `stoixima-platform` στο GitHub
- [ ] Δημιουργία φακελοδομής (βλ. `docs/repo-structure.md`)
- [ ] Δημιουργία `.gitignore` για .NET + Node.js + secrets
- [ ] Δημιουργία `stoixima-platform.sln` (.NET solution)
- [ ] Δημιουργία `README.md` με overview και setup instructions
- [ ] Δημιουργία `.github/PULL_REQUEST_TEMPLATE.md`
- [ ] Δημιουργία `.github/ISSUE_TEMPLATE/` (feature + bug templates)

**Acceptance Criteria**:
- Το repo είναι accessible στο GitHub
- `git clone` + `ls` δείχνει τη σωστή δομή
- `.gitignore` εξαιρεί `*.session`, `docker-compose.override.yml`, `.env`

---

### Issue #2 — [INFRA] Docker Compose για local development
**Labels**: `infrastructure`, `priority:critical`
**Milestone**: Phase 0 — Foundation
**Depends on**: #1

**Περιγραφή**:
Docker Compose που εκκινεί όλες τις εξαρτήσεις τοπικά: Kafka, PostgreSQL, ClickHouse, Redis, monitoring.

**Tasks**:
- [ ] `infrastructure/docker-compose.yml` με services: Kafka, Zookeeper, Kafka UI, PostgreSQL, ClickHouse, Redis, Prometheus, Grafana
- [ ] `infrastructure/docker-compose.override.yml.example` (template για secrets)
- [ ] Προσθήκη `docker-compose.override.yml` στο `.gitignore`
- [ ] Health checks για κάθε service
- [ ] `infrastructure/README.md` με εντολές εκκίνησης

**Acceptance Criteria**:
- `docker compose up -d` εκκινεί όλα τα services χωρίς errors
- Kafka UI accessible στο http://localhost:8080
- PostgreSQL accessible στο localhost:5432

---

### Issue #3 — [INFRA] Kafka topics και shared Contracts library
**Labels**: `infrastructure`, `priority:critical`
**Milestone**: Phase 0 — Foundation
**Depends on**: #1

**Περιγραφή**:
Δημιουργία shared .NET class library με τα Kafka event contracts και βασικοί Kafka helper classes.

**Tasks**:
- [ ] Δημιουργία project `src/shared/Contracts/Contracts.csproj`
- [ ] `RawMessageEvent.cs` (βλ. schema στο `docs/services/01-telegram-gateway.md`)
- [ ] `ProcessedTipEvent.cs` (βλ. schema στο `docs/services/03-message-processing.md`)
- [ ] `AggregatedMatchEvent.cs` (βλ. schema στο `docs/services/05-aggregation-service.md`)
- [ ] Δημιουργία project `src/shared/Messaging/Messaging.csproj`
- [ ] `KafkaProducerFactory.cs` — helper για δημιουργία typed producers
- [ ] `KafkaConsumerFactory.cs` — helper για δημιουργία typed consumers
- [ ] Προσθήκη στο solution file

**Acceptance Criteria**:
- `dotnet build src/shared/Contracts` επιτυχές
- `dotnet build src/shared/Messaging` επιτυχές

---

### Issue #4 — [INFRA] PostgreSQL schema και migrations
**Labels**: `infrastructure`, `priority:critical`
**Milestone**: Phase 0 — Foundation
**Depends on**: #2

**Περιγραφή**:
Δημιουργία initial PostgreSQL schema με όλους τους πίνακες.

**Tasks**:
- [ ] Migration script `V1__init.sql` με πίνακες: users, channels, telegram_accounts, account_channels
- [ ] Migration script `V2__teams_matches.sql` με πίνακες: teams, matches, api_team_mappings
- [ ] Migration script `V3__tips.sql` με πίνακα: tips
- [ ] Script εκτέλεσης migrations (`infrastructure/scripts/run-migrations.sh`)
- [ ] Seed script για top-5 league ομάδες (`infrastructure/seeds/teams.sql`)

**Acceptance Criteria**:
- Migrations τρέχουν χωρίς errors σε fresh PostgreSQL instance
- Seed script εισάγει τουλάχιστον 20 ομάδες με aliases

---

### Issue #5 — [INFRA] ClickHouse schema
**Labels**: `infrastructure`, `priority:high`
**Milestone**: Phase 0 — Foundation
**Depends on**: #2

**Περιγραφή**:
Δημιουργία ClickHouse tables.

**Tasks**:
- [ ] SQL script για `raw_messages` table (βλ. `docs/database-design.md`)
- [ ] SQL script για `processed_tips` table
- [ ] Script εκτέλεσης (`infrastructure/scripts/init-clickhouse.sh`)

**Acceptance Criteria**:
- Tables δημιουργούνται χωρίς errors
- Test INSERT + SELECT λειτουργεί

---

## Phase 1 — Data Ingestion

> **Σκοπός**: Telegram messages να φτάνουν στη βάση δεδομένων.

---

### Issue #6 — [GATEWAY] Telegram Gateway Service — project setup
**Labels**: `service:telegram-gateway`, `priority:critical`
**Milestone**: Phase 1 — Data Ingestion
**Depends on**: #3

**Περιγραφή**:
Δημιουργία .NET 9 Worker Service project για το Telegram Gateway.

**Tasks**:
- [ ] Δημιουργία project `src/services/TelegramGateway/TelegramGateway.csproj`
- [ ] NuGet packages: `WTelegramClient`, `Confluent.Kafka`, `Npgsql`, `OpenTelemetry.Extensions.Hosting`
- [ ] `appsettings.json` template (χωρίς secrets)
- [ ] Reference στο `src/shared/Contracts` και `src/shared/Messaging`
- [ ] Προσθήκη στο `.sln`
- [ ] Dockerfile (βλ. `docs/services/01-telegram-gateway.md`)
- [ ] Health check endpoint

**Acceptance Criteria**:
- `dotnet build` επιτυχές
- `docker build` επιτυχές

---

### Issue #7 — [GATEWAY] WTelegramClient integration
**Labels**: `service:telegram-gateway`, `priority:critical`
**Milestone**: Phase 1 — Data Ingestion
**Depends on**: #6, #2

**Περιγραφή**:
Σύνδεση στο Telegram και λήψη μηνυμάτων από channels.

**Tasks**:
- [ ] `TelegramClientService.cs` — wrapper γύρω από WTelegramClient
- [ ] Session management: load από file ή ENV, save on update
- [ ] Channel list loading από PostgreSQL `account_channels` table
- [ ] `UpdatesHandler.cs` — event handler για νέα μηνύματα
- [ ] Interactive first-run auth mode (flag `--setup`)
- [ ] FloodWaitException handling με sleep
- [ ] Reconnect με exponential backoff

**Acceptance Criteria**:
- Service συνδέεται επιτυχώς σε Telegram account (real test)
- Νέα μηνύματα σε παρακολουθούμενο channel εμφανίζονται στο log

---

### Issue #8 — [GATEWAY] Kafka publishing
**Labels**: `service:telegram-gateway`, `priority:critical`
**Milestone**: Phase 1 — Data Ingestion
**Depends on**: #7, #3

**Περιγραφή**:
Publish κάθε Telegram message ως `RawMessageEvent` στο Kafka.

**Tasks**:
- [ ] `MessagePublisher.cs` — serialize + produce σε Kafka
- [ ] Partition key = `chat_id.ToString()`
- [ ] Async publish με error handling
- [ ] Prometheus counter: `telegram_messages_published_total`
- [ ] Dead letter handling αν Kafka unavailable

**Acceptance Criteria**:
- Μηνύματα εμφανίζονται στο Kafka UI (`telegram.messages.raw` topic)
- Partition key είναι το `chat_id`

---

### Issue #9 — [STORAGE] Storage Service — project setup + Kafka consumer
**Labels**: `service:storage`, `priority:critical`
**Milestone**: Phase 1 — Data Ingestion
**Depends on**: #3, #4, #5

**Περιγραφή**:
Storage Service που καταναλώνει raw messages και τα αποθηκεύει.

**Tasks**:
- [ ] Δημιουργία project `src/services/StorageService/`
- [ ] NuGet: `Confluent.Kafka`, `Npgsql`, `ClickHouse.Client`, `StackExchange.Redis`
- [ ] Kafka consumer group: `storage-service-group`
- [ ] Batching: buffer 500 messages ή 5 seconds flush
- [ ] ClickHouse bulk insert για `raw_messages`
- [ ] PostgreSQL upsert για `channels`
- [ ] Redis deduplication check (βλ. `docs/services/02-storage-service.md`)
- [ ] Dockerfile

**Acceptance Criteria**:
- Messages που έρχονται στο Kafka εμφανίζονται στο ClickHouse `raw_messages`
- Duplicate messages δεν αποθηκεύονται (dedup test)

---

## Phase 2 — Data Processing

> **Σκοπός**: Εξαγωγή structured tips από raw messages.

---

### Issue #10 — [PROCESSING] Message Processing Service — project setup
**Labels**: `service:message-processing`, `priority:critical`
**Milestone**: Phase 2 — Data Processing
**Depends on**: #3

**Tasks**:
- [ ] Δημιουργία project `src/services/MessageProcessing/`
- [ ] NuGet: `Confluent.Kafka`, `Npgsql`, `ClickHouse.Client`, `Tesseract`, `FuzzySharp`
- [ ] Dockerfile
- [ ] Kafka consumer: `message-processing-group`

---

### Issue #11 — [PROCESSING] Team database και team recognition
**Labels**: `service:message-processing`, `priority:critical`
**Milestone**: Phase 2 — Data Processing
**Depends on**: #10, #4

**Περιγραφή**:
In-memory team cache και αναγνώριση ομάδων σε κείμενο.

**Tasks**:
- [ ] `TeamCache.cs` — loads teams + aliases από PostgreSQL στη startup
- [ ] `TeamRecognizer.cs` — exact match + fuzzy match (FuzzySharp, threshold 80%)
- [ ] Unit tests για `TeamRecognizer` με παραδείγματα
- [ ] Refresh cache κάθε ώρα

**Acceptance Criteria**:
- "Arsenal", "Arsenal FC", "The Gunners" → team_id=X
- "Man Utd", "Manchester United", "MUFC" → team_id=Y
- "Arsnal" (typo) → αναγνώριση με fuzzy match

---

### Issue #12 — [PROCESSING] Tip extraction με regex patterns
**Labels**: `service:message-processing`, `priority:critical`
**Milestone**: Phase 2 — Data Processing
**Depends on**: #10

**Περιγραφή**:
Regex-based εξαγωγή tip type, tip value, odds από κείμενο.

**Tasks**:
- [ ] `TipExtractor.cs` με patterns για: over/under, btts/gg, 1x2, handicap
- [ ] Odds extraction pattern (`@1.85`, `odds: 1.85`, `1.85`)
- [ ] `TipExtractionResult` με confidence score
- [ ] Unit tests με 20+ παραδείγματα πραγματικών μηνυμάτων

**Acceptance Criteria**:
- "Arsenal vs Chelsea BTTS Yes @1.85" → `{type: btts, value: yes, odds: 1.85, confidence: 0.95}`
- "Over 2.5 goals tonight" → `{type: over_under, value: over_2.5}`
- Unit tests pass

---

### Issue #13 — [PROCESSING] OCR για image messages
**Labels**: `service:message-processing`, `priority:high`
**Milestone**: Phase 2 — Data Processing
**Depends on**: #10

**Περιγραφή**:
Εξαγωγή κειμένου από εικόνες που περιέχουν betting tips.

**Tasks**:
- [ ] `OcrService.cs` — Tesseract wrapper
- [ ] Download media από Telegram (χρήση WTelegramClient media download)
- [ ] Pre-processing εικόνας (grayscale, contrast) για καλύτερη ακρίβεια
- [ ] Append OCR text στο message text πριν processing
- [ ] Metric: `processing_ocr_calls_total`, `processing_ocr_duration_ms`
- [ ] Tesseract language data (`eng`) στο Docker image

**Acceptance Criteria**:
- Image με printed text "Over 2.5 @ 1.75" → OCR εξάγει σωστό κείμενο
- Αν OCR αποτύχει: log warning, συνεχίζει χωρίς crash

---

### Issue #14 — [PROCESSING] Full processing pipeline + PostgreSQL/ClickHouse writes
**Labels**: `service:message-processing`, `priority:critical`
**Milestone**: Phase 2 — Data Processing
**Depends on**: #11, #12, #13

**Περιγραφή**:
Ενοποίηση OCR + team recognition + tip extraction. Αποθήκευση + Kafka publish.

**Tasks**:
- [ ] `MessageProcessor.cs` — orchestrates full pipeline
- [ ] Match correlation (βλ. `docs/services/03-message-processing.md` Step 3)
- [ ] INSERT στο PostgreSQL `tips` table
- [ ] INSERT στο ClickHouse `processed_tips`
- [ ] Publish `ProcessedTipEvent` στο `telegram.messages.processed`
- [ ] Metric: `processing_tips_extracted_total`, `processing_unmatched_messages_total`

**Acceptance Criteria**:
- End-to-end: Telegram message → tip record στη βάση + Kafka event

---

### Issue #15 — [MATCH-DATA] Match Data Service
**Labels**: `service:match-data`, `priority:critical`
**Milestone**: Phase 2 — Data Processing
**Depends on**: #4

**Περιγραφή**:
Scheduled service που φέρνει σημερινά ματς από football API.

**Tasks**:
- [ ] Δημιουργία project `src/services/MatchDataService/`
- [ ] NuGet: `Npgsql`, `StackExchange.Redis`, `Polly`, `Cronos`
- [ ] `FootballApiClient.cs` — HTTP client για football-data.org
- [ ] Response mapping → `Match` entities
- [ ] Upsert στο PostgreSQL `matches` table
- [ ] Redis cache: `matches:today:{date}`
- [ ] Cron schedule: 06:00 daily + hourly retry
- [ ] `api_team_mappings` table lookup για normalization
- [ ] Dockerfile

**Acceptance Criteria**:
- Cron τρέχει στις 06:00
- Matches για σήμερα υπάρχουν στο PostgreSQL μετά τη fetch
- Redis key `matches:today:{date}` populated

---

## Phase 3 — Aggregation & API

> **Σκοπός**: Έτοιμο feed για consumption από το frontend.

---

### Issue #16 — [AGGREGATION] Aggregation Service
**Labels**: `service:aggregation`, `priority:critical`
**Milestone**: Phase 3 — Aggregation & API
**Depends on**: #14, #15

**Περιγραφή**:
Συλλογή tips ανά match και υπολογισμός consensus.

**Tasks**:
- [ ] Δημιουργία project `src/services/AggregationService/`
- [ ] Kafka consumer: `telegram.messages.processed`
- [ ] `AggregationCalculator.cs` — consensus logic (βλ. `docs/services/05-aggregation-service.md`)
- [ ] Redis write: `feed:match:{matchId}` με TTL 5min
- [ ] Kafka publish: `AggregatedMatchEvent` στο `feed.matches.aggregated`
- [ ] Scheduled re-aggregation (κάθε λεπτό για αγώνες < 3ώρες)
- [ ] Dockerfile

**Acceptance Criteria**:
- Μετά από 3 tips για Arsenal vs Chelsea: Redis key `feed:match:42` υπάρχει
- Consensus percentages αθροίζουν 100% ανά tip_type

---

### Issue #17 — [API] API Gateway — REST endpoints
**Labels**: `service:api-gateway`, `priority:critical`
**Milestone**: Phase 3 — Aggregation & API
**Depends on**: #16

**Περιγραφή**:
ASP.NET Core Web API με endpoints για το frontend.

**Tasks**:
- [ ] Δημιουργία project `src/services/ApiGateway/`
- [ ] NuGet: `Npgsql`, `StackExchange.Redis`, `Microsoft.AspNetCore.SignalR`
- [ ] `GET /api/feed` με query params (date, league, minTippers)
- [ ] `GET /api/feed/match/{matchId}` με full tip details
- [ ] `GET /api/channels` — active channels list
- [ ] `GET /api/matches` — matches by date
- [ ] `GET /health`
- [ ] Swagger/OpenAPI documentation
- [ ] API Key authentication middleware
- [ ] CORS configuration
- [ ] Dockerfile

**Acceptance Criteria**:
- `GET /api/feed` επιστρέφει matches με tips
- Swagger UI accessible
- Request χωρίς API Key επιστρέφει 401

---

### Issue #18 — [API] SignalR real-time feed
**Labels**: `service:api-gateway`, `priority:high`
**Milestone**: Phase 3 — Aggregation & API
**Depends on**: #17, #16

**Περιγραφή**:
WebSocket hub για real-time updates στο frontend.

**Tasks**:
- [ ] `FeedHub.cs` — SignalR hub
- [ ] Kafka consumer (στο API Gateway) για `feed.matches.aggregated`
- [ ] Broadcast `MatchUpdated` event σε connected clients
- [ ] Reconnect handling στον client side

**Acceptance Criteria**:
- Browser WebSocket connection επιτυχής
- Νέο tip → frontend λαμβάνει event χωρίς refresh

---

## Phase 4 — Frontend

> **Σκοπός**: Το UI που βλέπει ο χρήστης.

---

### Issue #19 — [FRONTEND] React project setup
**Labels**: `frontend`, `priority:critical`
**Milestone**: Phase 4 — Frontend
**Depends on**: #17

**Tasks**:
- [ ] Vite + React + TypeScript setup
- [ ] TailwindCSS
- [ ] `@microsoft/signalr` package
- [ ] React Query (TanStack Query)
- [ ] API types (βλ. `docs/frontend/frontend-spec.md`)
- [ ] `.env.example` με `VITE_API_URL`
- [ ] Nginx Dockerfile για production

---

### Issue #20 — [FRONTEND] Feed page και MatchCard component
**Labels**: `frontend`, `priority:critical`
**Milestone**: Phase 4 — Frontend
**Depends on**: #19

**Tasks**:
- [ ] `FeedPage.tsx` — main page
- [ ] `MatchCard.tsx` — match display component
- [ ] `ConsensusBars.tsx` — progress bars για percentages
- [ ] Date picker για ημερομηνία
- [ ] League filter dropdown
- [ ] Loading skeleton
- [ ] Empty state (καμία πρόβλεψη σήμερα)

**Acceptance Criteria**:
- Feed εμφανίζεται σωστά με mock data
- Φιλτράρισμα ανά league λειτουργεί

---

### Issue #21 — [FRONTEND] Match detail και real-time WebSocket
**Labels**: `frontend`, `priority:high`
**Milestone**: Phase 4 — Frontend
**Depends on**: #20, #18

**Tasks**:
- [ ] `TipperList.tsx` — ανά channel tips
- [ ] `useFeedWebSocket.ts` hook (βλ. `docs/frontend/frontend-spec.md`)
- [ ] Match card animation όταν νέο tip έρχεται
- [ ] "LIVE" badge για ongoing matches

**Acceptance Criteria**:
- Νέο Telegram tip → match card ανανεώνεται χωρίς page refresh
- Match detail modal εμφανίζει per-tipster tips

---

## Phase 5 — DevOps & Monitoring

---

### Issue #22 — [DEVOPS] GitHub Actions CI pipeline
**Labels**: `infrastructure`, `priority:high`
**Milestone**: Phase 5 — DevOps
**Depends on**: #9, #14, #16, #17, #21

**Tasks**:
- [ ] Workflow per service με path filters
- [ ] Build + test για κάθε .NET service
- [ ] Build + test για frontend
- [ ] Docker build check (χωρίς push)

---

### Issue #23 — [DEVOPS] Docker image builds + registry
**Labels**: `infrastructure`, `priority:high`
**Milestone**: Phase 5 — DevOps
**Depends on**: #22

**Tasks**:
- [ ] Push Docker images στο GitHub Container Registry (ghcr.io)
- [ ] Semantic versioning tags
- [ ] Multi-stage Dockerfiles (build + runtime stages)

---

### Issue #24 — [DEVOPS] Kubernetes manifests
**Labels**: `infrastructure`, `priority:medium`
**Milestone**: Phase 5 — DevOps
**Depends on**: #23

**Tasks**:
- [ ] Namespace `stoixima-prod` + `stoixima-dev`
- [ ] Deployment + Service για κάθε microservice
- [ ] ConfigMaps για non-secret config
- [ ] Secrets για Telegram API keys, DB passwords
- [ ] HPA (autoscaling) για Message Processing και API Gateway
- [ ] Ingress με TLS

---

### Issue #25 — [DEVOPS] Prometheus metrics + Grafana dashboard
**Labels**: `infrastructure`, `priority:medium`
**Milestone**: Phase 5 — DevOps
**Depends on**: #22

**Tasks**:
- [ ] Prometheus scrape config για όλα τα services
- [ ] Grafana dashboard: messages/sec, active channels, tip extraction rate, API latency
- [ ] Alert rules: Kafka lag > 1000, connection errors

---

## Summary — Σειρά Υλοποίησης

```
Phase 0:  #1 → #2 → (#3, #4, #5 parallel)
Phase 1:  #6 → #7 → #8 → #9
Phase 2:  (#10 → #11 → #12 → #13 → #14) parallel με #15
Phase 3:  #16 → #17 → #18
Phase 4:  #19 → #20 → #21
Phase 5:  #22 → #23 → #24, #25 parallel
```

**Minimum Viable Product** (δείτε κάτι να λειτουργεί): Issues #1-#9, #15, #16, #17, #19, #20
