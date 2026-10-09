-- V7.2: campos comerciais nos modelos de orçamento (condições, garantia, forma de pagamento, descontos)
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS discount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.budget_template_items ADD COLUMN IF NOT EXISTS discount numeric(18,2) NOT NULL DEFAULT 0;
