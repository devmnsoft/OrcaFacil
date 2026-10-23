-- OrçaFácil V7.8: estados de espera e vencimento da ordem de serviço em orcafacil.work_orders.
-- WaitingCustomer / WaitingMaterial / Overdue existiam na tabela de transições, mas nada no sistema
-- conseguia gravá-las: o KPI de "serviços atrasados" do dashboard ficava sempre zero.
-- Aditivo e idempotente. Não remove nem reescreve dados.
BEGIN;

ALTER TABLE orcafacil.work_orders
    ADD COLUMN IF NOT EXISTS wait_reason varchar(40) NOT NULL DEFAULT 'None',
    ADD COLUMN IF NOT EXISTS waiting_since timestamptz,
    ADD COLUMN IF NOT EXISTS wait_note varchar(1000),
    ADD COLUMN IF NOT EXISTS overdue_marked_at timestamptz;

COMMIT;
