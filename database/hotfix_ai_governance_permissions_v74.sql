-- V7.4: correção da deriva de seed de RBAC para permissões de IA e de margem.
-- Aditivo e idempotente. Não altera schema, não apaga dados de negócio e não usa DROP/TRUNCATE.
--
-- Motivo: Ai.Suggest, Ai.UseCopilot, Ai.UseRag, Ai.UseSemanticSearch, Ai.AnalyzeDocuments e
-- Margins.View são exigidos pelo código (AiGovernance / orquestrador de IA) mas nunca foram
-- inseridos por nenhuma migração. Sem eles, copiloto, busca semântica e análise de documentos
-- falham para todos os papéis.
--
-- Papéis (espelham o catálogo já existente):
--   Ai.*         -> Owner, Administrator, Manager, Commercial (mesmos papéis de Ai.View)
--   Margins.View -> Owner, Administrator (mesmos papéis de Finance.View)
--
-- Os escopos receipts.read, contracts.read, webhooks.read, analytics.read e files.read NÃO entram
-- aqui: são escopos de chave de API (ApiKeyService.AllowedScopes), não permissões de RBAC — os
-- demais escopos (ex.: clients.read) também não têm linha em orcafacil.permissions.
CREATE SCHEMA IF NOT EXISTS orcafacil;

INSERT INTO orcafacil.permissions (id, code, display_name, is_platform_permission, created_at, is_deleted)
SELECT gen_random_uuid(), v.code, v.display_name, false, now(), false
FROM (VALUES
    ('Ai.Suggest', 'Sugerir melhorias com IA'),
    ('Ai.UseCopilot', 'Usar o copiloto de IA'),
    ('Ai.UseRag', 'Usar respostas com fontes de conhecimento'),
    ('Ai.UseSemanticSearch', 'Usar busca semântica'),
    ('Ai.AnalyzeDocuments', 'Analisar documentos com IA'),
    ('Margins.View', 'Ver custos e margens')
) AS v(code, display_name)
ON CONFLICT (code) DO UPDATE
    SET display_name = EXCLUDED.display_name,
        is_deleted = false,
        updated_at = now();

INSERT INTO orcafacil.role_permissions (id, role_id, permission_id, created_at, is_deleted)
SELECT gen_random_uuid(), r.id, p.id, now(), false
FROM orcafacil.roles r
JOIN orcafacil.permissions p
    ON p.is_deleted = false
   AND p.is_platform_permission = false
   AND (
        (p.code IN ('Ai.Suggest', 'Ai.UseCopilot', 'Ai.UseRag', 'Ai.UseSemanticSearch', 'Ai.AnalyzeDocuments')
            AND r.code IN ('Owner', 'Administrator', 'Manager', 'Commercial'))
     OR (p.code = 'Margins.View' AND r.code IN ('Owner', 'Administrator'))
   )
WHERE r.is_deleted = false
ON CONFLICT (role_id, permission_id) DO UPDATE
    SET is_deleted = false,
        updated_at = now();

-- Verificação rápida (deve retornar 6 linhas e os papéis esperados).
-- SELECT p.code, string_agg(r.code, ', ' ORDER BY r.code) AS roles
--   FROM orcafacil.role_permissions rp
--   JOIN orcafacil.roles r ON r.id = rp.role_id
--   JOIN orcafacil.permissions p ON p.id = rp.permission_id
--  WHERE rp.is_deleted = false
--    AND p.code IN ('Ai.Suggest','Ai.UseCopilot','Ai.UseRag','Ai.UseSemanticSearch','Ai.AnalyzeDocuments','Margins.View')
--  GROUP BY p.code ORDER BY p.code;
