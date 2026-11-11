CREATE TABLE IF NOT EXISTS outbox (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id        UUID            NOT NULL REFERENCES orders(id),
    event_type      VARCHAR(64)     NOT NULL,
    payload         JSONB           NOT NULL,
    status          VARCHAR(16)     NOT NULL DEFAULT 'PENDING'
                                    CHECK (status IN ('PENDING', 'SENT', 'FAILED')),
    attempts        INT             NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),
    sent_at         TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_outbox_status      ON outbox (status);
CREATE INDEX IF NOT EXISTS idx_outbox_created_at  ON outbox (created_at ASC) WHERE status = 'PENDING';
