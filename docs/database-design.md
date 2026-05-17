# Database Design

## Overview — Γιατί 3 Databases

| Database | Ρόλος | Γιατί |
|----------|-------|-------|
| **PostgreSQL** | Transactional data, config, metadata | ACID, relational integrity, familiar |
| **ClickHouse** | Raw + processed messages (time-series analytics) | Billions of rows, ultra-fast aggregations |
| **Redis** | Cache, deduplication, distributed locks | Sub-millisecond reads, TTL support |

---

## PostgreSQL Schema

### Channels Table
```sql
CREATE TABLE channels (
    id          BIGINT PRIMARY KEY,      -- Telegram chat_id
    title       TEXT NOT NULL,
    username    TEXT,                    -- @username αν υπάρχει
    source      TEXT DEFAULT 'telegram', -- telegram / discord / rss
    active      BOOLEAN DEFAULT true,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);
```

### Telegram Accounts Table
```sql
CREATE TABLE telegram_accounts (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    phone       TEXT NOT NULL UNIQUE,
    session_str TEXT,                    -- WTelegramClient session
    active      BOOLEAN DEFAULT true,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);
```

### Account-Channel Mapping
```sql
CREATE TABLE account_channels (
    account_id  UUID REFERENCES telegram_accounts(id),
    channel_id  BIGINT REFERENCES channels(id),
    PRIMARY KEY (account_id, channel_id)
);
```

### Football Teams Table
```sql
CREATE TABLE teams (
    id          SERIAL PRIMARY KEY,
    name        TEXT NOT NULL,
    aliases     TEXT[],                  -- ["Man Utd", "Manchester United", "MUFC"]
    country     TEXT,
    league      TEXT
);
```

### Matches Table
```sql
CREATE TABLE matches (
    id          SERIAL PRIMARY KEY,
    external_id TEXT,                    -- ID από football API
    home_team_id INT REFERENCES teams(id),
    away_team_id INT REFERENCES teams(id),
    kick_off    TIMESTAMPTZ NOT NULL,
    league      TEXT,
    season      TEXT,
    status      TEXT DEFAULT 'scheduled', -- scheduled / live / finished
    home_score  INT,
    away_score  INT,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_matches_kickoff ON matches(kick_off);
```

### Tips Table (structured tips από processing)
```sql
CREATE TABLE tips (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    channel_id      BIGINT REFERENCES channels(id),
    match_id        INT REFERENCES matches(id),
    raw_message_id  TEXT,               -- ClickHouse message reference
    tip_type        TEXT,               -- 'over_under', '1x2', 'btts', 'handicap', etc.
    tip_value       TEXT,               -- 'over 2.5', 'home', 'yes', etc.
    odds            DECIMAL(6,2),
    confidence      DECIMAL(3,2),       -- 0.0 - 1.0 από ML model
    extracted_at    TIMESTAMPTZ DEFAULT NOW(),
    is_valid        BOOLEAN DEFAULT true
);

CREATE INDEX idx_tips_match ON tips(match_id);
CREATE INDEX idx_tips_channel ON tips(channel_id);
```

### Users Table (για API authentication)
```sql
CREATE TABLE users (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email       TEXT UNIQUE NOT NULL,
    role        TEXT DEFAULT 'viewer',  -- viewer / admin
    api_key     TEXT UNIQUE,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);
```

---

## ClickHouse Schema

### Raw Messages Table
```sql
CREATE TABLE raw_messages (
    timestamp       DateTime,
    message_id      Int64,
    chat_id         Int64,
    sender_id       Int64,
    message_text    String,
    has_media       UInt8,
    media_type      LowCardinality(String),  -- 'photo', 'document', ''
    ocr_text        String,                  -- εξαγόμενο κείμενο από εικόνα
    source          LowCardinality(String)   -- 'telegram', 'discord'
)
ENGINE = MergeTree
PARTITION BY toYYYYMM(timestamp)
ORDER BY (chat_id, timestamp)
SETTINGS index_granularity = 8192;
```

### Processed Tips Table (denormalized για analytics)
```sql
CREATE TABLE processed_tips (
    timestamp       DateTime,
    channel_id      Int64,
    channel_title   String,
    match_id        Int32,
    home_team       String,
    away_team       String,
    league          String,
    tip_type        LowCardinality(String),
    tip_value       String,
    odds            Float32,
    source          LowCardinality(String)
)
ENGINE = MergeTree
PARTITION BY toYYYYMM(timestamp)
ORDER BY (timestamp, match_id, channel_id);
```

**Γιατί denormalized στο ClickHouse;** Joins είναι ακριβά. Για analytics queries θέλουμε όλα σε ένα table.

---

## Redis Key Patterns

| Key Pattern | TTL | Περιεχόμενο |
|-------------|-----|-------------|
| `msg:dedup:{message_id}` | 24h | `1` (deduplication flag) |
| `matches:today:{date}` | 4h | JSON list of today's matches |
| `feed:match:{match_id}` | 5min | Aggregated tips για αγώνα |
| `channel:info:{channel_id}` | 1h | Channel metadata |
| `lock:processing:{message_id}` | 30s | Distributed lock |

---

## Migration Strategy

- PostgreSQL: **Flyway** ή **EF Core Migrations**
- ClickHouse: SQL migration scripts (versioned)
- Αρχεία: `infrastructure/migrations/postgres/V1__init.sql`, `V2__teams.sql` κλπ.

---

## Data Retention

| Store | Retention | Λόγος |
|-------|-----------|-------|
| ClickHouse raw_messages | 2 years | Analytics history |
| PostgreSQL tips | 1 year | Tipster performance tracking |
| Redis cache | Per TTL | Operational only |
