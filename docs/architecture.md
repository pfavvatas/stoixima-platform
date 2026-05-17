# System Architecture

## High-Level Diagram

```
┌─────────────────────────────────────────────────────┐
│                 EXTERNAL SOURCES                     │
│  Telegram Accounts   │  Discord (future)  │  ...    │
└───────────┬──────────┴──────────┬──────────┴────────┘
            │                     │
            ▼                     ▼
┌───────────────────┐   ┌──────────────────────┐
│ Telegram Gateway  │   │  Future Source MS    │
│ (WTelegramClient) │   │  (Discord, RSS, etc) │
└─────────┬─────────┘   └──────────┬───────────┘
          │                         │
          └───────────┬─────────────┘
                      │
                      ▼
         ┌────────────────────────┐
         │     Apache Kafka       │
         │ topic: messages.raw    │
         └──────────┬─────────────┘
                    │
        ┌───────────┼───────────┐
        ▼           ▼           ▼
┌──────────────┐  ┌──────────────────────┐
│ Storage MS   │  │ Message Processing MS│
│              │  │ (OCR, NLP, teams)    │
└──────┬───────┘  └──────────┬───────────┘
       │                     │
       ▼                     │ Kafka: messages.processed
┌──────────────┐             ▼
│ PostgreSQL   │   ┌──────────────────────┐
│ ClickHouse   │   │  Aggregation MS      │◄── Match Data MS
│ Redis        │   │  (consensus/feed)    │    (football API)
└──────────────┘   └──────────┬───────────┘
                               │ Kafka: feed.aggregated
                               ▼
                    ┌──────────────────────┐
                    │    API Gateway       │
                    │ (REST + WebSocket)   │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │   React Frontend     │
                    │  (real-time feed)    │
                    └──────────────────────┘
```

## Data Flow (End-to-End)

### 1. Ingestion
1. Telegram Gateway συνδέεται σε N Telegram accounts
2. Κάθε account παρακολουθεί M channels
3. Κάθε νέο μήνυμα (text ή media) δημοσιεύεται στο Kafka topic `telegram.messages.raw`

### 2. Storage
- Storage Service καταναλώνει `telegram.messages.raw`
- Raw messages → ClickHouse (πλήρης ιστορικό)
- Channel metadata, sender info → PostgreSQL

### 3. Processing
- Message Processing Service καταναλώνει `telegram.messages.raw`
- Αν το μήνυμα έχει εικόνα → OCR εξαγωγή κειμένου
- NLP/pattern matching για αναγνώριση: ομάδες, αγώνας, πρόβλεψη (tip), odds
- Αποτέλεσμα δημοσιεύεται στο `telegram.messages.processed`
- Δεδομένα αποθηκεύονται επίσης στη βάση (structured tips table)

### 4. Match Correlation
- Match Data Service φέρνει σημερινά ματς από football API
- Κάνει cache στο Redis (ανανέωση κάθε πρωί)
- Aggregation Service συνδυάζει processed tips με match data

### 5. Aggregation
- Για κάθε αγώνα: συλλέγει όλα τα tips από όλα τα channels
- Υπολογίζει consensus: "60% Over 2.5, 25% Home Win, 15% Other"
- Δημοσιεύει aggregated feed event

### 6. API & Frontend
- API Gateway εκθέτει REST endpoint `GET /api/feed?date=today`
- WebSocket για real-time ενημερώσεις όταν έρχεται νέο tip
- React app εμφανίζει match cards με tips ανά tipster

## Kafka Topics

| Topic | Producer | Consumers | Περιγραφή |
|-------|----------|-----------|-----------|
| `telegram.messages.raw` | Telegram Gateway | Storage MS, Message Processing MS | Ακατέργαστα μηνύματα |
| `telegram.messages.processed` | Message Processing MS | Aggregation MS | Εξαγμένα tips |
| `feed.matches.aggregated` | Aggregation MS | API Gateway | Έτοιμο feed ανά αγώνα |
| `telegram.deadletter` | όλα | Monitoring | Failed messages |

## Partition Strategy
- Partition key: `chat_id` — εγγυάται ordering ανά channel
- Replication factor: 3 (production)

## Γιατί Microservices

- **Ανεξάρτητο scaling**: το Processing MS χρειάζεται περισσότερους πόρους από το Gateway
- **Πολλαπλές πηγές**: μελλοντικά Discord/RSS microservices θα τροφοδοτούν το ίδιο Kafka topic, ο processing μηχανισμός παραμένει ίδιος
- **Fault isolation**: αν πέσει το Processing, τα raw messages συσσωρεύονται στο Kafka και επεξεργάζονται μόλις ξαναέρθει online
- **Ανεξάρτητη ανάπτυξη και deploy** ανά service

## Γιατί ClickHouse για Messages

- MergeTree engine: ταχύτατα range queries και aggregations
- Δισεκατομμύρια rows με queries σε milliseconds
- `LIKE '%bitcoin%'` σε 100M rows: ~50ms
- PostgreSQL για το ίδιο: 30+ seconds
