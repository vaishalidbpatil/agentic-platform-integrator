CREATE TABLE IF NOT EXISTS orders (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    idempotency_key VARCHAR(128)    NOT NULL UNIQUE,
    account_id      VARCHAR(64)     NOT NULL,
    symbol          VARCHAR(16)     NOT NULL,
    side            VARCHAR(4)      NOT NULL CHECK (side IN ('BUY', 'SELL')),
    quantity        NUMERIC(18, 8)  NOT NULL CHECK (quantity > 0),
    price           NUMERIC(18, 8)  NOT NULL CHECK (price > 0),
    status          VARCHAR(16)     NOT NULL DEFAULT 'PENDING'
                                    CHECK (status IN ('PENDING', 'FILLED', 'REJECTED', 'DEAD_LETTERED')),
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_orders_account_id  ON orders (account_id);
CREATE INDEX IF NOT EXISTS idx_orders_symbol      ON orders (symbol);
CREATE INDEX IF NOT EXISTS idx_orders_status      ON orders (status);
CREATE INDEX IF NOT EXISTS idx_orders_created_at  ON orders (created_at DESC);
