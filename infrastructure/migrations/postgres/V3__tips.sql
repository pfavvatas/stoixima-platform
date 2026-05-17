-- V3: Betting tips — structured predictions extracted from messages

CREATE TABLE tips (
    id             UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    channel_id     BIGINT      NOT NULL REFERENCES channels(id),
    match_id       INT         NOT NULL REFERENCES matches(id),
    raw_message_id BIGINT,                -- Telegram message_id (reference to ClickHouse)
    tip_type       TEXT        NOT NULL,  -- 'over_under' | '1x2' | 'btts' | 'handicap'
    tip_value      TEXT        NOT NULL,  -- 'over_2.5' | 'home' | 'yes' | 'draw' | ...
    odds           DECIMAL(6,2),
    confidence     DECIMAL(4,3) CHECK (confidence BETWEEN 0 AND 1),
    source         TEXT        NOT NULL DEFAULT 'telegram',
    extracted_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_valid       BOOLEAN     NOT NULL DEFAULT true
);

CREATE INDEX idx_tips_match      ON tips(match_id);
CREATE INDEX idx_tips_channel    ON tips(channel_id);
CREATE INDEX idx_tips_extracted  ON tips(extracted_at DESC);
CREATE INDEX idx_tips_type_value ON tips(tip_type, tip_value);

-- Prevent the same channel from submitting the same tip for the same match twice
CREATE UNIQUE INDEX idx_tips_dedup
    ON tips(channel_id, match_id, tip_type, tip_value)
    WHERE is_valid = true;

INSERT INTO _migrations (version) VALUES ('V3');
