-- V2: Raw messages table
-- Stores every incoming message from all sources (Telegram, Discord, ...).
-- Queried for analytics and replayed for reprocessing.
-- Primary ordering: (chat_id, timestamp) — optimal for per-channel range scans.

CREATE TABLE IF NOT EXISTS stoixima.raw_messages
(
    -- time & identity
    timestamp       DateTime64(3),          -- millisecond precision
    message_id      Int64,
    chat_id         Int64,
    sender_id       Int64,

    -- content
    message_text    String,
    has_media       UInt8,                  -- 0 | 1
    media_type      LowCardinality(String), -- 'photo' | 'document' | ''
    media_file_id   String,                 -- Telegram file_id for lazy download
    ocr_text        String,                 -- text extracted from image via OCR

    -- metadata
    source          LowCardinality(String), -- 'telegram' | 'discord'
    account_id      UUID
)
ENGINE = MergeTree
PARTITION BY toYYYYMM(timestamp)
ORDER BY (chat_id, timestamp, message_id)
-- TTL: keep raw messages for 2 years then delete
TTL toDate(timestamp) + INTERVAL 2 YEAR DELETE
SETTINGS index_granularity = 8192;

-- Secondary index: fast point lookups by message_id
-- e.g. "does this message already exist?" (fallback dedup beyond Redis)
ALTER TABLE stoixima.raw_messages
    ADD INDEX idx_message_id message_id TYPE minmax GRANULARITY 4;

-- Secondary index: bloom filter on message_text for LIKE-style searches
-- e.g. WHERE message_text LIKE '%Arsenal%'
ALTER TABLE stoixima.raw_messages
    ADD INDEX idx_message_text message_text TYPE tokenbf_v1(32768, 3, 0) GRANULARITY 4;

INSERT INTO stoixima._migrations (version) VALUES ('V2');
