-- hotfix_plan_grace_period_v76.sql
-- FECHÔ 2.0 — Carência real de 3 dias nas assinaturas pagas (idempotente e reversível).
--
-- Problema: as versões de plano eram semeadas sem grace_period_days (DEFAULT 0).
-- O motor de inadimplência (BillingStatusService) anuncia "carência de 3 dias" no
-- evento billing.overdue e mantém o status PastDue durante esse período, mas a
-- liberação de benefícios (PlanAccessService.GetEffectivePlanVersionAsync) lê
-- exclusivamente plan_versions.grace_period_days. Com 0 dias, os benefícios pagos
-- eram cortados no mesmo instante do vencimento, tornando a carência anunciada
-- fictícia — exatamente o tipo de sucesso que a evolução da jornada não pode
-- simular.
--
-- Este patch:
--   1. define 3 dias em versões PUBLICADAS de planos pagos que estejam abaixo do
--      mínimo contratado (nunca reduz um valor já configurado por operação);
--   2. deixa o plano FREE em 0 dias, pois não há cobrança a regularizar.
--
-- Evidência: tests/OrcaFacil.UnitTests/DunningCycleHomologationTests.cs trava o
-- contrato ponta a ponta (carência -> suspensão -> restauração -> benefícios).
--
-- Sem DROP/TRUNCATE. Reexecutável. Aplicar com: psql -f hotfix_plan_grace_period_v76.sql

BEGIN;

UPDATE orcafacil.plan_versions pv
SET grace_period_days = 3, updated_at = now()
FROM orcafacil.plans p
WHERE pv.plan_id = p.id
  AND p.code <> 'FREE'
  AND pv.status = 'Published'
  AND pv.grace_period_days < 3;

COMMIT;
