-- V1: Core tables — users, channels, telegram accounts

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ─── Migration tracking ───────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS _migrations (
    version     TEXT PRIMARY KEY,
    applied_at  TIMESTAMPTZ DEFAULT NOW()
);

-- ─── Users ────────────────────────────────────────────────────────────────────

CREATE TABLE users (
    id         UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    email      TEXT        NOT NULL UNIQUE,
    role       TEXT        NOT NULL DEFAULT 'viewer', -- viewer | admin
    api_key    TEXT        UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_api_key ON users(api_key) WHERE api_key IS NOT NULL;

-- ─── Telegram accounts ────────────────────────────────────────────────────────

CREATE TABLE telegram_accounts (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    phone       TEXT        NOT NULL UNIQUE,
    session_str TEXT,                            -- WTelegramClient session (encrypted at rest)
    active      BOOLEAN     NOT NULL DEFAULT true,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─── Channels ────────────────────────────────────────────────────────────────

CREATE TABLE channels (
    id         BIGINT      PRIMARY KEY,          -- Telegram chat_id (negative for groups/channels)
    title      TEXT        NOT NULL,
    username   TEXT,                             -- @username if public
    source     TEXT        NOT NULL DEFAULT 'telegram', -- telegram | discord | rss
    active     BOOLEAN     NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_channels_source ON channels(source);
CREATE INDEX idx_channels_active ON channels(active) WHERE active = true;

-- ─── Account ↔ Channel mapping ────────────────────────────────────────────────

CREATE TABLE account_channels (
    account_id UUID   NOT NULL REFERENCES telegram_accounts(id) ON DELETE CASCADE,
    channel_id BIGINT NOT NULL REFERENCES channels(id)          ON DELETE CASCADE,
    PRIMARY KEY (account_id, channel_id)
);

INSERT INTO _migrations (version) VALUES ('V1');
