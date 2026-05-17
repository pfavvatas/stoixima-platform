# Service: Storage Service

## Ρόλος

Καταναλώνει raw messages από Kafka και τα αποθηκεύει στις βάσεις δεδομένων. Ρόλος αποκλειστικά persistence — καμία επεξεργασία.

---

## Tech Stack

- **.NET 9 Worker Service**
- **Confluent.Kafka** (consumer)
- **Npgsql** (PostgreSQL)
- **ClickHouse.Client** (ClickHouse bulk insert)
- **StackExchange.Redis** (deduplication)

---

## Βασική Λειτουργία

```
Consumer Loop:
  1. Consume batch από Kafka topic: telegram.messages.raw
  2. Έλεγχος deduplication στο Redis (key: msg:dedup:{messageId})
  3. Αποθήκευση στο ClickHouse (raw_messages table) — bulk insert
  4. Upsert channel metadata στο PostgreSQL
  5. Commit Kafka offset
```

---

## Batching Strategy

Το ClickHouse είναι βελτιστοποιημένο για bulk inserts. Αποφύγαμε row-by-row inserts.

```
Buffer: 500 messages ή max 5 seconds
→ flush to ClickHouse as one INSERT
```

Αυτό δίνει ~10x καλύτερο throughput από single-row inserts.

---

## Deduplication

Πριν κάθε insert, έλεγχος:
```
REDIS: GET msg:dedup:{messageId}
  → αν exists: skip (ήδη αποθηκευμένο)
  → αν δεν exists: SET msg:dedup:{messageId} 1 EX 86400
```

Αν το Kafka message replay γίνει (π.χ. after crash), δεν θα υπάρχουν duplicates στη βάση.

---

## PostgreSQL Writes

Αποθηκεύει μόνο channel metadata (όχι κάθε μήνυμα):

```sql
INSERT INTO channels (id, title, source)
VALUES (@id, @title, @source)
ON CONFLICT (id) DO UPDATE SET title = EXCLUDED.title;
```

---

## Configuration

```json
{
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "TopicRaw": "telegram.messages.raw",
    "ConsumerGroup": "storage-service-group"
  },
  "ClickHouse": {
    "ConnectionString": "Host=clickhouse;Port=8123;Database=stoixima"
  },
  "PostgreSQL": {
    "ConnectionString": "Host=postgres;Database=stoixima..."
  },
  "Redis": {
    "ConnectionString": "redis:6379"
  }
}
```

---

## Scaling

Kafka consumer group: `storage-service-group`
- Περισσότερα partitions = περισσότερα parallel consumers
- Κάθε instance παίρνει ένα subset των partitions αυτόματα

---

## Metrics (Prometheus)

- `storage_messages_persisted_total`
- `storage_duplicates_skipped_total`
- `storage_clickhouse_insert_duration_ms`
- `storage_batch_size` (histogram)

---

## NuGet Packages

```
Confluent.Kafka
Npgsql
ClickHouse.Client
StackExchange.Redis
OpenTelemetry.Extensions.Hosting
```
