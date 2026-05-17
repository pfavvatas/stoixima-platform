-- V3: Processed tips table (denormalized)
-- Stores structured betting tips after NLP/OCR processing.
-- Fully denormalized: no JOINs needed for analytics queries.
-- Primary ordering: (timestamp, match_id, channel_id) — optimal for
-- per-day/per-match aggregation queries.

CREATE TABLE IF NOT EXISTS stoixima.processed_tips
(
    -- time & identity
    timestamp     DateTime64(3),
    tip_id        UUID,

    -- channel
    channel_id    Int64,
    channel_title String,

    -- match (denormalized — no JOIN needed)
    match_id      Int32,
    home_team     String,
    away_team     String,
    league        LowCardinality(String),
    kick_off      DateTime64(3),

    -- tip
    tip_type      LowCardinality(String), -- 'over_under' | '1x2' | 'btts' | 'handicap'
    tip_value     String,                 -- 'over_2.5' | 'home' | 'yes' | ...
    odds          Float32,
    confidence    Float32,                -- 0.0 – 1.0 from ML model

    -- metadata
    source        LowCardinality(String), -- 'telegram' | 'discord'
    raw_message_id Int64                  -- reference back to raw_messages
)
ENGINE = MergeTree
PARTITION BY toYYYYMM(timestamp)
ORDER BY (timestamp, match_id, channel_id, tip_id)
-- TTL: keep processed tips for 1 year (used for tipster performance tracking)
TTL toDate(timestamp) + INTERVAL 1 YEAR DELETE
SETTINGS index_granularity = 8192;

-- Secondary index: fast lookup by match — used by aggregation queries
ALTER TABLE stoixima.processed_tips
    ADD INDEX idx_match_id match_id TYPE minmax GRANULARITY 4;

-- Secondary index: fast lookup by channel
ALTER TABLE stoixima.processed_tips
    ADD INDEX idx_channel_id channel_id TYPE minmax GRANULARITY 4;

INSERT INTO stoixima._migrations (version) VALUES ('V3');
