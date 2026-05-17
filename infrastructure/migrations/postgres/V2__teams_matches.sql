-- V2: Football domain — teams, matches, API name mappings

-- ─── Teams ────────────────────────────────────────────────────────────────────

CREATE TABLE teams (
    id      SERIAL PRIMARY KEY,
    name    TEXT   NOT NULL,
    aliases TEXT[] NOT NULL DEFAULT '{}',  -- alternative names for fuzzy matching
    country TEXT,
    league  TEXT,
    UNIQUE (name, league)
);

CREATE INDEX idx_teams_name   ON teams USING gin(to_tsvector('simple', name));
CREATE INDEX idx_teams_league ON teams(league);

-- ─── API team name mappings ───────────────────────────────────────────────────
-- Maps external API team names to internal team IDs.
-- Needed because football-data.org uses "Manchester United FC"
-- while our canonical name is "Manchester United".

CREATE TABLE api_team_mappings (
    api_name   TEXT NOT NULL,
    api_source TEXT NOT NULL,  -- 'football-data' | 'api-football'
    team_id    INT  NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    PRIMARY KEY (api_name, api_source)
);

-- ─── Matches ─────────────────────────────────────────────────────────────────

CREATE TABLE matches (
    id           SERIAL      PRIMARY KEY,
    external_id  TEXT,                          -- ID from football API
    home_team_id INT         NOT NULL REFERENCES teams(id),
    away_team_id INT         NOT NULL REFERENCES teams(id),
    kick_off     TIMESTAMPTZ NOT NULL,
    league       TEXT        NOT NULL,
    season       TEXT,
    status       TEXT        NOT NULL DEFAULT 'scheduled', -- scheduled | live | finished | postponed
    home_score   INT,
    away_score   INT,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_matches_kickoff      ON matches(kick_off);
CREATE INDEX idx_matches_status       ON matches(status);
CREATE INDEX idx_matches_kickoff_date ON matches(DATE(kick_off));
CREATE UNIQUE INDEX idx_matches_external ON matches(external_id, league) WHERE external_id IS NOT NULL;

INSERT INTO _migrations (version) VALUES ('V2');
