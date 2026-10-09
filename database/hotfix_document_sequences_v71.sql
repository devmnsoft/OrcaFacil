-- V7.1: Sequências atômicas de documentos por conta, proteção de histórico,
-- compatibilização verificável de legados e resolução de schema drift.

-- 1. Criação da tabela de sequências de documentos por conta e tipo
CREATE TABLE IF NOT EXISTS orcafacil.document_sequences (
  id uuid PRIMARY KEY,
  account_id uuid NOT NULL,
  document_type varchar(30) NOT NULL,
  current_number bigint NOT NULL DEFAULT 0,
  prefix varchar(12) NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz,
  is_deleted boolean NOT NULL DEFAULT false
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_document_sequences_account_type
  ON orcafacil.document_sequences(account_id, document_type);

-- 2. Compatibilização verificável para documentos legados sem account_id
-- Ocorre apenas quando o autor do documento pertence a exatamente uma conta ativa.
UPDATE orcafacil.documents d
   SET account_id = m.account_id,
       updated_at = now()
  FROM (
    SELECT user_id, min(account_id) AS account_id
      FROM orcafacil.account_members
     WHERE is_deleted = false
     GROUP BY user_id
    HAVING count(DISTINCT account_id) = 1
  ) m
 WHERE d.account_id IS NULL
   AND d.user_id = m.user_id;

-- 3. Reconstituição da sequência a partir de TODOS os documentos históricos emitidos na conta
-- Considera documentos cancelados e excluídos para JAMAIS reutilizar número emitido historicamente.
INSERT INTO orcafacil.document_sequences(id, account_id, document_type, current_number, prefix, created_at, is_deleted)
SELECT gen_random_uuid(),
       account_id,
       type,
       COALESCE(MAX(CASE
         WHEN number ~ ('^' || CASE WHEN type = 'Receipt' THEN 'REC' ELSE 'ORC' END || '-[0-9]+$')
         THEN substring(number from length(CASE WHEN type = 'Receipt' THEN 'REC' ELSE 'ORC' END) + 2)::bigint
         ELSE 0
       END), 0),
       CASE WHEN type = 'Receipt' THEN 'REC' ELSE 'ORC' END,
       now(),
       false
  FROM orcafacil.documents
 WHERE account_id IS NOT NULL AND number IS NOT NULL
 GROUP BY account_id, type
ON CONFLICT (account_id, document_type) DO UPDATE
  SET current_number = GREATEST(orcafacil.document_sequences.current_number, EXCLUDED.current_number),
      updated_at = now();

-- 4. Índice de unicidade de numeração comercial para documentos ativos por conta e tipo
CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_type_number
  ON orcafacil.documents(account_id, type, number)
  WHERE account_id IS NOT NULL AND is_deleted = false;

-- 5. Trigger para garantir que clients.version seja incrementado a cada UPDATE
CREATE OR REPLACE FUNCTION orcafacil.fn_bump_client_version()
RETURNS TRIGGER AS $$
BEGIN
    NEW.version := (COALESCE(OLD.version::bigint, 0) + 1)::text::xid;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_bump_client_version ON orcafacil.clients;
CREATE TRIGGER trg_bump_client_version
BEFORE UPDATE ON orcafacil.clients
FOR EACH ROW
EXECUTE FUNCTION orcafacil.fn_bump_client_version();

-- 6. Resolução de schema drift: whatsapp vs whats_app
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'orcafacil' AND table_name = 'partner_contacts') THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_contacts' AND column_name = 'whats_app') THEN
      IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_contacts' AND column_name = 'whatsapp') THEN
        ALTER TABLE orcafacil.partner_contacts RENAME COLUMN whatsapp TO whats_app;
      ELSE
        ALTER TABLE orcafacil.partner_contacts ADD COLUMN whats_app varchar(40);
      END IF;
    END IF;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'orcafacil' AND table_name = 'partner_profiles') THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_profiles' AND column_name = 'whats_app') THEN
      IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_profiles' AND column_name = 'whatsapp') THEN
        ALTER TABLE orcafacil.partner_profiles RENAME COLUMN whatsapp TO whats_app;
      ELSE
        ALTER TABLE orcafacil.partner_profiles ADD COLUMN whats_app varchar(40);
      END IF;
    END IF;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'orcafacil' AND table_name = 'suppliers') THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'suppliers' AND column_name = 'whats_app') THEN
      IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'suppliers' AND column_name = 'whatsapp') THEN
        ALTER TABLE orcafacil.suppliers RENAME COLUMN whatsapp TO whats_app;
      ELSE
        ALTER TABLE orcafacil.suppliers ADD COLUMN whats_app varchar(40);
      END IF;
    END IF;
  END IF;
END $$;
