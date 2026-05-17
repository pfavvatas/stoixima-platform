# Service: Aggregation Service

## Ρόλος

Συλλέγει όλα τα processed tips για κάθε αγώνα και φτιάχνει ένα aggregated feed object που είναι έτοιμο για κατανάλωση από το frontend.

---

## Tech Stack

- **.NET 9 Worker Service**
- **Confluent.Kafka** (consumer + producer)
- **Npgsql** (PostgreSQL reads)
- **StackExchange.Redis** (cache aggregated feed)

---

## Βασική Λειτουργία

```
Trigger A — Kafka Event:
  Consume ProcessedTipEvent από: telegram.messages.processed
  → Βρες τον αγώνα (match_id)
  → Ανακτά όλα τα tips για αυτό το match από PostgreSQL/Redis
  → Υπολόγισε aggregation
  → Cache στο Redis: feed:match:{match_id}
  → Publish AggregatedMatchEvent στο: feed.matches.aggregated

Trigger B — Scheduled (κάθε λεπτό):
  → Για κάθε αγώνα με κοντινό kick-off (< 3 ώρες)
  → Ανακτά όλα τα tips
  → Re-calculate aggregation (νέα tips μπορεί να έχουν έρθει)
  → Publish updated event
```

---

## Aggregation Logic

Για κάθε `match_id`:

```
Συλλογή: όλα τα tips από channels για αυτό το match

Ομαδοποίηση ανά tip_type:
  over_under:
    over_2.5: 4 channels
    under_2.5: 1 channel
    → "Over 2.5: 80%, Under 2.5: 20%"

  1x2:
    home: 3 channels
    draw: 2 channels
    away: 1 channel
    → "Home: 50%, Draw: 33%, Away: 17%"

  btts:
    yes: 5 channels
    no: 0 channels
    → "BTTS Yes: 100%"
```

**Consensus Score**: weighted average αν channel έχει "confidence rating" (μελλοντική βελτίωση).

---

## Output Event

Topic: `feed.matches.aggregated`

```json
{
  "matchId": 42,
  "homeTeam": "Arsenal",
  "awayTeam": "Chelsea",
  "league": "Premier League",
  "kickOff": "2026-05-17T15:00:00Z",
  "totalTippers": 6,
  "consensus": {
    "over_under": {
      "over_2.5": 0.80,
      "under_2.5": 0.20
    },
    "btts": {
      "yes": 1.00
    },
    "1x2": {
      "home": 0.50,
      "draw": 0.33,
      "away": 0.17
    }
  },
  "tips": [
    {
      "channelId": -1001234567890,
      "channelTitle": "Football Tips VIP",
      "tipType": "btts",
      "tipValue": "yes",
      "odds": 1.85
    }
  ],
  "updatedAt": "2026-05-17T11:00:00Z"
}
```

---

## Redis Cache

```
feed:match:42 → AggregatedMatchEvent JSON
  TTL: 5 minutes (ανανεώνεται σε κάθε νέο tip)
```

API Gateway διαβάζει κατευθείαν από Redis — δεν χρειάζεται να κάνει query στη βάση για κάθε request.

---

## Metrics

- `aggregation_matches_active` (gauge — αγώνες με τουλάχιστον 1 tip)
- `aggregation_tips_per_match` (histogram)
- `aggregation_events_published_total`

---

## NuGet Packages

```
Confluent.Kafka
Npgsql
StackExchange.Redis
OpenTelemetry.Extensions.Hosting
```
