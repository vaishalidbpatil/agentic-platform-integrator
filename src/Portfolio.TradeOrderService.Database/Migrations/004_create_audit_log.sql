CREATE TABLE audit_log (
    id          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    caller      VARCHAR(128)    NOT NULL,
    tool        VARCHAR(128)    NOT NULL,
    arguments   JSONB,
    result      JSONB,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT now()
);

CREATE INDEX idx_audit_log_caller      ON audit_log (caller);
CREATE INDEX idx_audit_log_created_at  ON audit_log (created_at DESC);
