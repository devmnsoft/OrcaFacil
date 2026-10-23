-- OrçaFácil V7.9: cadência de lembrete de cobrança em orcafacil.financial_entries.
-- Adiciona o controle de quando o cliente foi lembrado pela última vez e quantas vezes,
-- para o lembrete automático ser limitado e nunca repetir em excesso. Aditivo e idempotente.
BEGIN;

ALTER TABLE orcafacil.financial_entries
    ADD COLUMN IF NOT EXISTS last_reminder_at date,
    ADD COLUMN IF NOT EXISTS reminder_count integer NOT NULL DEFAULT 0;

COMMIT;
