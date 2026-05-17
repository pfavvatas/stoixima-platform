# Service: Match Data Service

## Ρόλος

Φέρνει και κάνει cache τα αποτελέσματα/πρόγραμμα αγώνων από εξωτερικό football API. Παρέχει στο σύστημα τη γνώση "ποιοι αγώνες παίζονται σήμερα/αύριο".

---

## Γιατί Ξεχωριστό Service

- Ανεξάρτητη εξωτερική εξάρτηση (football API)
- Μπορεί να αντικατασταθεί χωρίς να επηρεαστεί η υπόλοιπη αρχιτεκτονική
- Cache strategy ελέγχεται σε ένα σημείο
- Μελλοντικά: προσθήκη live scores, odds κλπ.

---

## Tech Stack

- **.NET 9 Worker Service** (scheduled background task)
- **HttpClient** με Polly retry policy
- **Npgsql** (PostgreSQL writes)
- **StackExchange.Redis** (cache)

---

## Εξωτερικές Πηγές Δεδομένων

### Option A: football-data.org (προτείνεται για αρχή)
- Free tier: 10 req/min, top 12 leagues
- API Key απαιτείται (δωρεάν εγγραφή)
- Endpoint: `GET /v4/matches?dateFrom=2026-05-17&dateTo=2026-05-17`

### Option B: API-Football (rapidapi.com)
- Πιο πλήρες (1000+ leagues)
- Free: 100 req/day, paid tiers διαθέσιμα
- Endpoint: `GET /fixtures?date=2026-05-17`

### Option C: OpenFootball (GitHub, free)
- Στατικά JSON αρχεία για historical data
- Δεν είναι real-time — χρήσιμο μόνο για seeding

---

## Βασική Λειτουργία

```
Schedule: Κάθε πρωί στις 06:00 (και retry ανά ώρα αν αποτύχει)

1. Fetch matches για σήμερα + αύριο από football API
2. Normalize: map API team names → internal team IDs (teams table)
3. Upsert στο PostgreSQL matches table
4. Update Redis cache: SET matches:today:{date} <json> EX 14400 (4 ώρες)

Ξεχωριστό schedule: Κάθε 5 λεπτά κατά τη διάρκεια αγώνων:
5. Fetch live scores
6. Update match status (live/finished) + scores στο PostgreSQL
```

---

## Team Name Normalization

Πρόβλημα: football API επιστρέφει "Manchester United FC", αλλά βάση έχει "Manchester United".

Λύση:
1. Exact match πρώτα
2. Αν αποτύχει: fuzzy match με threshold 80%
3. Manual mapping table: `api_team_name_mappings`

```sql
CREATE TABLE api_team_mappings (
    api_name    TEXT NOT NULL,
    api_source  TEXT NOT NULL,  -- 'football-data', 'api-football'
    team_id     INT REFERENCES teams(id),
    PRIMARY KEY (api_name, api_source)
);
```

---

## Redis Cache Structure

```
matches:today:2026-05-17 → JSON array of MatchDto
  TTL: 4 hours (ανανεώνεται κάθε πρωί)

match:live:42 → JSON MatchDto with live score
  TTL: 5 minutes (ανανεώνεται συχνά κατά τη διάρκεια αγώνα)
```

---

## Configuration

```json
{
  "FootballApi": {
    "Provider": "football-data",
    "ApiKey": "your-key-here",
    "BaseUrl": "https://api.football-data.org/v4",
    "Leagues": ["PL", "PD", "SA", "BL1", "FL1"]
  },
  "Schedule": {
    "MorningFetchCron": "0 6 * * *",
    "LiveScoreCron": "*/5 * * * *"
  }
}
```

---

## Metrics

- `matchdata_fetch_calls_total`
- `matchdata_matches_fetched_today` (gauge)
- `matchdata_team_mapping_failures_total` (ομάδες που δεν αναγνωρίστηκαν)
- `matchdata_api_errors_total`

---

## NuGet Packages

```
Npgsql
StackExchange.Redis
Polly
Microsoft.Extensions.Http.Polly
OpenTelemetry.Extensions.Hosting
Cronos (για cron scheduling)
```
