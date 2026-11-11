CREATE TABLE IF NOT EXISTS ingestion_jobs (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    source          VARCHAR(256)    NOT NULL,
    source_hash     VARCHAR(64)     NOT NULL,
    status          VARCHAR(16)     NOT NULL DEFAULT 'RUNNING'
                                    CHECK (status IN ('RUNNING', 'COMPLETED', 'FAILED')),
    records_read    INT             NOT NULL DEFAULT 0,
    records_loaded  INT             NOT NULL DEFAULT 0,
    records_rejected INT            NOT NULL DEFAULT 0,
    started_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),
    finished_at     TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_ingestion_jobs_status      ON ingestion_jobs (status);
CREATE INDEX IF NOT EXISTS idx_ingestion_jobs_source_hash ON ingestion_jobs (source_hash);
