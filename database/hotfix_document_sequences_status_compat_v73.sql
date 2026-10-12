-- V7.3: compatibilidade incremental da associação de documentos legados por status textual/numerico.
-- Não substitui o V7.1; torna o reparo reaplicavel em bases que ja receberam schemas com enums convertidos para texto.

DO $$
DECLARE
  member_status_type text;
  account_status_type text;
BEGIN
  SELECT data_type INTO member_status_type
    FROM information_schema.columns
   WHERE table_schema = 'orcafacil'
     AND table_name = 'account_members'
     AND column_name = 'status';

  SELECT data_type INTO account_status_type
    FROM information_schema.columns
   WHERE table_schema = 'orcafacil'
     AND table_name = 'business_accounts'
     AND column_name = 'status';

  IF member_status_type IS NULL OR account_status_type IS NULL THEN
    RAISE EXCEPTION 'Nao foi possivel validar status de account_members/business_accounts para associacao legada.';
  END IF;

  UPDATE orcafacil.documents d
     SET account_id = eligible.account_id,
         updated_at = now()
    FROM (
      SELECT m.user_id, (array_agg(m.account_id ORDER BY m.created_at ASC, m.id ASC))[1] AS account_id
        FROM orcafacil.account_members m
        INNER JOIN orcafacil.business_accounts a ON a.id = m.account_id
       WHERE m.is_deleted = false
         AND lower(m.status::text) IN ('1', 'active')
         AND a.is_deleted = false
         AND lower(a.status::text) IN ('1', 'active')
       GROUP BY m.user_id
      HAVING count(DISTINCT m.account_id) = 1
    ) eligible
   WHERE d.account_id IS NULL
     AND d.user_id = eligible.user_id;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_document_sequences_account_type
  ON orcafacil.document_sequences(account_id, document_type);

CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_type_number
  ON orcafacil.documents(account_id, type, number)
  WHERE account_id IS NOT NULL AND is_deleted = false;
