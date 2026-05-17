# Local Development Setup

## Prerequisites

- Docker Desktop
- .NET 9 SDK
- Node.js 20+
- GitHub CLI (`gh`)

---

## Docker Compose Services

Το `infrastructure/docker-compose.yml` εκκινεί:

| Service | Port | Περιγραφή |
|---------|------|-----------|
| Kafka | 9092 | Message broker |
| Zookeeper | 2181 | Kafka dependency |
| Kafka UI | 8080 | Web UI για Kafka topics |
| PostgreSQL | 5432 | Transactional DB |
| ClickHouse | 8123, 9000 | Analytics DB |
| Redis | 6379 | Cache |
| Prometheus | 9090 | Metrics |
| Grafana | 3001 | Dashboards |

---

## Εκκίνηση

```bash
# 1. Clone
git clone https://github.com/pfavvatas/stoixima-platform.git
cd stoixima-platform

# 2. Εκκίνηση infrastructure
cd infrastructure
docker compose up -d

# 3. Run migrations (PostgreSQL)
cd src/services/StorageService
dotnet ef database update

# 4. Seed teams database
psql -U postgres -d stoixima -f infrastructure/seeds/teams.sql

# 5. Τρέξε services τοπικά (κάθε σε ξεχωριστό terminal)
dotnet run --project src/services/TelegramGateway
dotnet run --project src/services/StorageService
dotnet run --project src/services/MessageProcessing
dotnet run --project src/services/MatchDataService
dotnet run --project src/services/AggregationService
dotnet run --project src/services/ApiGateway

# 6. Frontend
cd src/frontend
npm install
npm run dev
```

---

## Kafka Topics — Manual Creation

Τα topics δημιουργούνται αυτόματα, αλλά για manual setup:

```bash
# Kafka UI: http://localhost:8080
# Ή CLI:
docker exec -it kafka kafka-topics.sh --create \
  --bootstrap-server localhost:9092 \
  --replication-factor 1 \
  --partitions 6 \
  --topic telegram.messages.raw

docker exec -it kafka kafka-topics.sh --create \
  --bootstrap-server localhost:9092 \
  --replication-factor 1 \
  --partitions 6 \
  --topic telegram.messages.processed

docker exec -it kafka kafka-topics.sh --create \
  --bootstrap-server localhost:9092 \
  --replication-factor 1 \
  --partitions 3 \
  --topic feed.matches.aggregated
```

---

## Telegram Gateway — First Run Auth

Η πρώτη φορά χρειάζεται interactive OTP:

```bash
# Τρέξε με flag για interactive auth
dotnet run --project src/services/TelegramGateway -- --setup

# Θα ζητήσει OTP code στο terminal
# Μετά το session αποθηκεύεται τοπικά
```

---

## Environment Variables (local)

Δημιούργησε `infrastructure/docker-compose.override.yml`:

```yaml
# ΜΗΝ κάνεις commit αυτό το αρχείο — προστατεύεται από .gitignore
services:
  telegram-gateway:
    environment:
      - TELEGRAM__APIID=12345
      - TELEGRAM__APIHASH=your_hash
      - TELEGRAM__ACCOUNTPHONE=+306xxxxxxxxx
```

---

## Useful URLs (local)

| URL | Service |
|-----|---------|
| http://localhost:3000 | React Frontend |
| http://localhost:5000 | API Gateway |
| http://localhost:5000/swagger | API Docs |
| http://localhost:8080 | Kafka UI |
| http://localhost:3001 | Grafana |
| http://localhost:9090 | Prometheus |
