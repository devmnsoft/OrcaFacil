-- V7.0: clients.version precisa devolver um xid no INSERT.
-- O modelo usa a coluna como rowversion. Sem default, o RETURNING vinha nulo
-- e a gravação do cliente falhava. Aditivo: não apaga linhas.
UPDATE orcafacil.clients SET version = '0' WHERE version IS NULL;
ALTER TABLE orcafacil.clients ALTER COLUMN version SET DEFAULT '0';
ALTER TABLE orcafacil.clients ALTER COLUMN version SET NOT NULL;

ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS unit varchar(40) NOT NULL DEFAULT 'serviço';
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS notes varchar(1000);
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS sort_order integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS service_catalog_item_id uuid;
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS estimated_cost_snapshot numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS category_snapshot varchar(80);
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS duration_minutes_snapshot integer;
