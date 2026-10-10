-- Incremental indexes for quote workspace financial balance lookups.
-- They support deterministic contract-origin selection and batched payment aggregation.
BEGIN;
CREATE INDEX IF NOT EXISTS ix_work_orders_account_source_document_created
    ON orcafacil.work_orders(account_id, source_document_id, created_at)
    WHERE source_document_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_manual_payments_account_document_paid_at
    ON orcafacil.manual_payments(account_id, document_id, paid_at)
    WHERE document_id IS NOT NULL;
COMMIT;
