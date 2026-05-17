-- V1: Create database and migration tracking table

CREATE DATABASE IF NOT EXISTS stoixima;

-- Migration tracking (ReplacingMergeTree so re-inserts of same version are idempotent)
CREATE TABLE IF NOT EXISTS stoixima._migrations
(
    version    String,
    applied_at DateTime DEFAULT now()
)
ENGINE = ReplacingMergeTree(applied_at)
ORDER BY version;

INSERT INTO stoixima._migrations (version) VALUES ('V1');
