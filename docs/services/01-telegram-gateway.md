# Service: Telegram Gateway

## Ρόλος

Το μοναδικό σημείο επαφής με το Telegram network. Συνδέεται σε N Telegram accounts, παρακολουθεί channels, και δημοσιεύει κάθε νέο μήνυμα στο Kafka.

**Δεν κάνει processing.** Μόνο ingest και publish.

---

## Γιατί Πολλαπλά Accounts

Το Telegram επιβάλλει rate limits ανά account. Αν έχουμε 50+ channels, μοιράζουμε τα channels σε 2-3 accounts. Κάθε instance του service τρέχει με ένα account.

---

## Tech Stack

- **.NET 9 Worker Service** (background service, όχι HTTP)
- **WTelegramClient** — MTProto library για .NET, user-level access (όχι bot)
- **Confluent.Kafka** — Kafka producer
- **Npgsql** — διαβάζει τα channels/accounts από PostgreSQL

---

## Βασική Λειτουργία

```
Startup:
  1. Φόρτωση config (account phone, session) από PostgreSQL / ENV
  2. Σύνδεση στο Telegram μέσω WTelegramClient
  3. Fetch λίστα channels που παρακολουθεί αυτό το account
  4. Εγγραφή στα UpdatesEvent

On Message Received:
  1. Serialize σε RawMessageEvent
  2. Publish στο Kafka topic: telegram.messages.raw
     - Partition key: chat_id (εγγυάται ordering ανά channel)
  3. Log + metrics
```

---

## Kafka Event Schema

Topic: `telegram.messages.raw`

```json
{
  "messageId": 123456,
  "chatId": -1001234567890,
  "chatTitle": "Football Tips VIP",
  "senderId": 111222333,
  "senderUsername": "tipster_example",
  "messageText": "Arsenal vs Chelsea BTTS Yes @ 1.85",
  "hasMedia": false,
  "mediaType": "",
  "mediaFileId": "",
  "timestamp": "2026-05-17T10:30:00Z",
  "source": "telegram",
  "accountId": "uuid-of-account"
}
```

Αν το μήνυμα έχει εικόνα, τα πεδία `hasMedia: true`, `mediaType: "photo"`, `mediaFileId: "..."` — το OCR γίνεται αλλού (Message Processing Service).

---

## Configuration (appsettings.json)

```json
{
  "Telegram": {
    "ApiId": 12345,
    "ApiHash": "abc123...",
    "AccountPhone": "+30691...",
    "SessionPath": "/data/session.session"
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "TopicRaw": "telegram.messages.raw"
  },
  "Database": {
    "ConnectionString": "Host=postgres;Database=stoixima..."
  }
}
```

Secrets (ApiId, ApiHash, phone) φέρνονται από environment variables / Kubernetes secrets — ποτέ hardcoded.

---

## Reconnection & Health

- Αν η σύνδεση Telegram χαθεί: exponential backoff retry (1s, 2s, 4s... max 60s)
- Health check endpoint (HTTP /health) για Kubernetes liveness probe
- Metrics (Prometheus):
  - `telegram_messages_received_total` (counter per chat_id)
  - `telegram_connection_status` (gauge: 1=connected, 0=disconnected)
  - `kafka_publish_errors_total`

---

## Multi-Instance Scaling

Κάθε instance = 1 Telegram account. Scale out = deploy περισσότερα instances με διαφορετικά accounts.

```yaml
# Kubernetes: 3 instances, 3 διαφορετικά accounts
- name: TELEGRAM_ACCOUNT_PHONE
  valueFrom:
    secretKeyRef:
      name: telegram-account-1  # / account-2 / account-3
      key: phone
```

---

## Dependencies

| Εξωτερικό | Τρόπος |
|-----------|--------|
| Telegram MTProto | WTelegramClient |
| Kafka | Confluent.Kafka producer |
| PostgreSQL | Read-only: channels list |

---

## NuGet Packages

```
WTelegramClient
Confluent.Kafka
Npgsql
OpenTelemetry.Extensions.Hosting
Microsoft.Extensions.Hosting
```

---

## Σημειώσεις Υλοποίησης

1. **Session persistence**: το WTelegramClient session αρχείο πρέπει να είναι persistent (Kubernetes PersistentVolume ή external storage). Αν χαθεί, χρειάζεται re-auth.
2. **First run auth**: η πρώτη φορά ζητά OTP code — χρειάζεται interactive setup εκτός Kubernetes, save session, μετά deploy.
3. **Flood wait**: αν το Telegram επιστρέψει `FloodWaitException`, το service πρέπει να κάνει sleep για το ζητούμενο διάστημα.
