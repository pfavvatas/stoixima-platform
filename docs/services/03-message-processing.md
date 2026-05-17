# Service: Message Processing Service

## Ρόλος

Το πιο πολύπλοκο service. Παίρνει raw messages και εξάγει structured betting tips. Αναλαμβάνει:

1. **OCR**: εξαγωγή κειμένου από εικόνες
2. **Team Recognition**: αναγνώριση ομάδων/αγώνων σε κείμενο
3. **Tip Extraction**: εξαγωγή της πρόβλεψης (tip type + value + odds)
4. **Publishing**: αποθήκευση structured tip και δημοσίευση στο Kafka

---

## Γιατί Ξεχωριστό Service

- Είναι computationally intensive (OCR, ML)
- Μπορεί να scale ανεξάρτητα
- Αν ο αλγόριθμος βελτιωθεί, γίνεται redeploy χωρίς να αγγίξουμε τα άλλα services
- Μελλοντικά: αντικατάσταση με AI-based extraction χωρίς να αλλάξουμε τίποτα άλλο

---

## Tech Stack

- **.NET 9 Worker Service**
- **Tesseract OCR** (.NET bindings) — local, offline OCR
- ή **Azure Computer Vision / Google Vision API** — για καλύτερη ακρίβεια
- **Regex + Pattern Matching** — για structured tip extraction
- **ML.NET** ή **Python sidecar** — για NLP team recognition (optional enhancement)

---

## Processing Pipeline

```
Input: RawMessageEvent από Kafka (telegram.messages.raw)

Step 1 — Media Check:
  IF hasMedia == true AND mediaType == "photo":
    → Download media file
    → Run OCR → extract text
    → Append to messageText

Step 2 — Team Recognition:
  Input: full text (original + OCR)
  → Lowercase + normalize
  → Scan για γνωστές ομάδες από βάση (teams table)
  → Aliases matching: "Man Utd" → team_id=5, "Arsenal" → team_id=12
  → Output: List<TeamMatch> { teamId, position (home/away) }

Step 3 — Match Correlation:
  → Αν βρέθηκαν 2 ομάδες: lookup στο matches table (αγώνες σήμερα/αύριο)
  → Match by home_team_id + away_team_id
  → Output: match_id ή null

Step 4 — Tip Extraction:
  → Regex patterns για γνωστά formats:
    - "Over 2.5" / "o2.5" / "over2.5" → tip_type: over_under, tip_value: over_2.5
    - "BTTS Yes" / "GG" / "Both teams score" → tip_type: btts, tip_value: yes
    - "1" / "Home Win" / "1X2" → tip_type: 1x2, tip_value: home
    - "@1.85" / "odds 1.85" → odds: 1.85
  → Confidence score (0.0-1.0) βάσει pattern quality

Step 5 — Store & Publish:
  IF match_id found AND tip extracted:
    → INSERT into PostgreSQL tips table
    → INSERT into ClickHouse processed_tips
    → PUBLISH ProcessedTipEvent to kafka: telegram.messages.processed
  ELSE:
    → Log as "unmatched message" (metrics)
    → Skip (δεν δημοσιεύουμε garbage)
```

---

## Kafka Output Event

Topic: `telegram.messages.processed`

```json
{
  "tipId": "uuid",
  "channelId": -1001234567890,
  "channelTitle": "Football Tips VIP",
  "matchId": 42,
  "homeTeam": "Arsenal",
  "awayTeam": "Chelsea",
  "league": "Premier League",
  "kickOff": "2026-05-17T15:00:00Z",
  "tipType": "btts",
  "tipValue": "yes",
  "odds": 1.85,
  "confidence": 0.92,
  "rawMessageId": 123456,
  "source": "telegram",
  "timestamp": "2026-05-17T10:30:00Z"
}
```

---

## Team Recognition Strategy

### Phase 1 (απλό, υλοποιείται πρώτο)
- PostgreSQL `teams` table με `aliases TEXT[]` column
- Load all teams + aliases σε memory cache at startup
- String matching (case-insensitive) σε κείμενο

### Phase 2 (βελτίωση)
- Fuzzy matching για typos: "Arsnal" → "Arsenal"
- Χρήση βιβλιοθήκης όπως FuzzySharp ή Lucene.NET
- ML.NET Named Entity Recognition για πιο robust αναγνώριση

### Team Database Seeding
- Αρχικό seed: top 5 ευρωπαϊκά leagues (Premier League, La Liga, Serie A, Bundesliga, Ligue 1)
- Data source: football-data.org API ή manual CSV
- Script: `infrastructure/seeds/teams.sql`

---

## OCR Implementation Notes

### Option A: Tesseract (recommended για privacy)
- Τοπικό, offline, χωρίς κόστος
- Ακρίβεια: καλή για τυπωμένο κείμενο, μέτρια για stylized fonts
- NuGet: `Tesseract`
- Γλώσσα: `eng` + `ell` (αν υπάρχουν ελληνικά)

### Option B: Azure Computer Vision
- Σχεδόν τέλεια ακρίβεια
- Κόστος: ~$1/1000 calls
- Κατάλληλο αν τα πρωτότυπα images είναι stylized/complex

Προτείνεται: ξεκίνα με Tesseract, αναβάθμισε αν χρειαστεί.

---

## Tip Pattern Examples

```csharp
// Regex patterns (παραδείγματα)
@"over\s*2\.5"          → over_under: over_2.5
@"under\s*2\.5"         → over_under: under_2.5
@"\bbtts\b|\bgg\b"      → btts: yes
@"\bng\b|no goal"       → btts: no
@"\b1x2\b.*\bhome\b"    → 1x2: home
@"@\s*(\d+\.\d+)"       → odds extraction
```

---

## Metrics

- `processing_messages_consumed_total`
- `processing_ocr_calls_total`
- `processing_tips_extracted_total`
- `processing_unmatched_messages_total` (βαρόμετρο ποιότητας)
- `processing_ocr_duration_ms`

---

## NuGet Packages

```
Confluent.Kafka
Npgsql
ClickHouse.Client
Tesseract (ή Azure.AI.Vision.ImageAnalysis)
FuzzySharp (για fuzzy team matching)
OpenTelemetry.Extensions.Hosting
```
