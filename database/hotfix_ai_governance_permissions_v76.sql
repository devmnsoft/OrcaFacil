-- hotfix_ai_governance_permissions_v76.sql
-- V7.6: correcao da cobertura de Margins.View (deriva remanescente).
--
-- O patch V7.4 declarava "Margins.View -> mesmos papeis de Finance.View", mas concedia
-- apenas Owner e Administrator. Finance.View, na pratica, esta concedido a 5 papeis
-- (Administrator, Financial, Manager, Owner, ReadOnly). Sem Financial e ReadOnly, a
-- visualizacao de custos/margens ficava inconsistente com a governanca financeira.
--
-- Este patch concede Margins.View a TODOS os papeis que ja possuem Finance.View, usando
-- subconsulta, de forma idempotente (ON CONFLICT ... DO UPDATE). Nao altera schema, nao
-- apaga dados e nao usa DROP/TRUNCATE. Reexecutavel.

BEGIN;

INSERT INTO orcafacil.permissions (id, code, display_name, is_platform_permission, created_at, updated_at, is_deleted)
VALUES (gen_random_uuid(), 'Margins.View', 'Ver custos e margens', false, now(), now(), false)
ON CONFLICT (code) DO UPDATE
    SET display_name = EXCLUDED.display_name,
        is_deleted = false,
        updated_at = now();

INSERT INTO orcafacil.role_permissions (id, role_id, permission_id, created_at, updated_at, is_deleted)
SELECT gen_random_uuid(), r.id, p.id, now(), now(), false
  FROM orcafacil.roles r
  JOIN orcafacil.permissions p ON p.code = 'Margins.View'
 WHERE r.is_deleted = false
   AND EXISTS (
        SELECT 1
          FROM orcafacil.role_permissions rp2
          JOIN orcafacil.permissions p2 ON p2.id = rp2.permission_id
         WHERE rp2.role_id = r.id
           AND rp2.is_deleted = false
           AND p2.code = 'Finance.View'
           AND p2.is_deleted = false
   )
ON CONFLICT (role_id, permission_id) DO UPDATE
    SET is_deleted = false,
        updated_at = now();

COMMIT;
