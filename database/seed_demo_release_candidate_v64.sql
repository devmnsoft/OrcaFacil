-- Seed Demo V6.4 opcional e idempotente. Requer usuário criado via fluxo normal de registro.
-- Uso: PGOPTIONS="-c orcafacil.demo_seed_enabled=true -c orcafacil.demo_email=demo@local.invalid" psql ... -f este-arquivo
\set ON_ERROR_STOP on
BEGIN;
DO $demo$
DECLARE demo_user uuid; demo_account uuid;
BEGIN
  IF current_setting('orcafacil.demo_seed_enabled', true) IS DISTINCT FROM 'true' THEN
    RAISE EXCEPTION 'Demo seed desabilitado. Defina DEMO_SEED_ENABLED somente em Development.';
  END IF;
  SELECT u.id INTO demo_user FROM orcafacil.users u
   WHERE lower(u.email)=lower(current_setting('orcafacil.demo_email', true)) AND u.is_active AND NOT u.is_deleted;
  IF demo_user IS NULL THEN
    RAISE EXCEPTION 'Usuário Demo inexistente. Registre-o pela aplicação para gerar a senha com segurança.';
  END IF;
  SELECT am.account_id INTO demo_account FROM orcafacil.account_members am
   WHERE am.user_id=demo_user AND am.is_active AND NOT am.is_deleted ORDER BY am.created_at LIMIT 1;
  IF demo_account IS NULL THEN RAISE EXCEPTION 'Usuário Demo não possui conta ativa.'; END IF;

  INSERT INTO orcafacil.budget_templates(id,account_id,user_id,profession,title,description,is_system_template,is_active,created_at,is_deleted)
  VALUES ('64000000-0000-4000-8000-000000000001',demo_account,demo_user,'Demonstração','[Demo] Proposta comercial','Template exclusivo da conta Demo V6.4',false,true,now(),false)
  ON CONFLICT (id) DO NOTHING;
  INSERT INTO orcafacil.budget_template_items(id,budget_template_id,description,quantity,unit_price,unit,sort_order,created_at,is_deleted)
  VALUES ('64000000-0000-4000-8000-000000000002','64000000-0000-4000-8000-000000000001','[Demo] Diagnóstico e implantação',1,1500,'serviço',1,now(),false)
  ON CONFLICT (id) DO NOTHING;
END $demo$;
COMMIT;
