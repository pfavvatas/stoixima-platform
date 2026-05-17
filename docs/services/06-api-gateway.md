# Service: API Gateway

## Ρόλος

Η μοναδική πύλη πρόσβασης για το frontend. Εκθέτει REST API και WebSocket για real-time feed. Δεν επεξεργάζεται δεδομένα — μόνο διαβάζει από Redis/PostgreSQL και προωθεί.

---

## Tech Stack

- **ASP.NET Core 9 Web API**
- **SignalR** (WebSocket για real-time)
- **StackExchange.Redis** (read aggregated feed)
- **Npgsql** (read match/channel metadata)
- **JWT Authentication**

---

## REST Endpoints

### GET /api/feed
Επιστρέφει το aggregated feed για σημερινές αγώνες.

**Query params:**
- `date` (optional, default: today) — `?date=2026-05-17`
- `league` (optional) — `?league=Premier League`
- `minTippers` (optional) — `?minTippers=3`

**Response:**
```json
{
  "date": "2026-05-17",
  "matches": [
    {
      "matchId": 42,
      "homeTeam": "Arsenal",
      "awayTeam": "Chelsea",
      "league": "Premier League",
      "kickOff": "2026-05-17T15:00:00Z",
      "totalTippers": 6,
      "consensus": { ... }
    }
  ]
}
```

---

### GET /api/feed/match/{matchId}
Λεπτομέρειες για συγκεκριμένο αγώνα: consensus + όλα τα individual tips ανά channel.

---

### GET /api/channels
Λίστα active Telegram channels που παρακολουθούνται.

**Response:**
```json
[
  {
    "id": -1001234567890,
    "title": "Football Tips VIP",
    "source": "telegram",
    "messageCount": 1523
  }
]
```

---

### GET /api/matches
Αγώνες ανά ημερομηνία (από Match Data Service / PostgreSQL).

---

### GET /health
Kubernetes health check endpoint.

---

## WebSocket — SignalR Hub

Hub endpoint: `/hubs/feed`

Το frontend συνδέεται και λαμβάνει real-time updates όταν νέο tip έρχεται για οποιοδήποτε match.

```typescript
// Frontend
const connection = new HubConnectionBuilder()
  .withUrl("/hubs/feed")
  .build();

connection.on("MatchUpdated", (aggregatedMatch) => {
  updateMatchCard(aggregatedMatch);
});
```

**Server-side**: SignalR Hub καταναλώνει `feed.matches.aggregated` Kafka topic (ή διαβάζει από Redis pub/sub) και κάνει broadcast σε συνδεδεμένους clients.

---

## Authentication

Αρχική υλοποίηση: API Key header
```
X-Api-Key: your-api-key
```

Keys αποθηκευμένα στο PostgreSQL `users` table.

Μελλοντική αναβάθμιση: JWT + Keycloak.

---

## CORS Configuration

Επιτρέπει requests από frontend origin:
```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "https://stoixima.yourdomain.com"]
  }
}
```

---

## Caching Strategy

- `GET /api/feed`: διαβάζει από Redis `feed:match:*` keys — cache-first, fallback PostgreSQL
- Cache-Control headers: `max-age=30` (30 seconds, frontend refresh)

---

## Metrics

- `api_requests_total` (counter per endpoint)
- `api_request_duration_ms` (histogram)
- `websocket_connections_active` (gauge)

---

## NuGet Packages

```
Npgsql
StackExchange.Redis
Confluent.Kafka (consumer για SignalR broadcast)
Microsoft.AspNetCore.SignalR
OpenTelemetry.AspNetCore
```
