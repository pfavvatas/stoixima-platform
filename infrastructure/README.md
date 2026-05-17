# Infrastructure

## Local Development

```bash
# Copy secrets template
cp docker-compose.override.yml.example docker-compose.override.yml
# Edit docker-compose.override.yml with your Telegram API credentials

# Start all infrastructure services
docker compose up -d

# Check status
docker compose ps
```

## Services & Ports

| Service | Port | URL |
|---------|------|-----|
| Kafka | 9092 | — |
| Kafka UI | 8080 | http://localhost:8080 |
| PostgreSQL | 5432 | localhost:5432 |
| ClickHouse HTTP | 8123 | http://localhost:8123 |
| ClickHouse Native | 9000 | localhost:9000 |
| Redis | 6379 | localhost:6379 |
| Prometheus | 9090 | http://localhost:9090 |
| Grafana | 3001 | http://localhost:3001 |

## Useful Commands

```bash
# View Kafka topics
docker exec -it kafka kafka-topics.sh --list --bootstrap-server localhost:9092

# PostgreSQL shell
docker exec -it postgres psql -U postgres -d stoixima

# ClickHouse shell
docker exec -it clickhouse clickhouse-client

# Redis CLI
docker exec -it redis redis-cli
```

Full setup guide: [../docs/infrastructure/local-dev.md](../docs/infrastructure/local-dev.md)
