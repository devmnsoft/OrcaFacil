import { readFileSync, existsSync } from 'node:fs';
const root = new URL('../', import.meta.url);
const read = p => readFileSync(new URL(p, root), 'utf8');
const required = [
  'src/OrcaFacil.Application/Ai/AiGovernance.cs',
  'database/sprint30_governed_ai.sql',
  'tests/OrcaFacil.UnitTests/AiGovernanceTests.cs'
];
for (const file of required) if (!existsSync(new URL(file, root))) throw new Error(`Ausente: ${file}`);
const ai = read(required[0]); const sql = read(required[1]);
for (const symbol of ['IAiProvider','IAiChatClient','IAiEmbeddingClient','IAiDocumentAnalysisClient','RulesOnlyAiProvider','NoopAiProvider','AiGovernanceService','AiRedactionService','AiRagService','AiDocumentAnalysisService','AiDraftService','AiActionDraftService','AiQuotaService','AiPromptInjectionGuard'])
  if (!ai.includes(symbol)) throw new Error(`Contrato ausente: ${symbol}`);
for (const table of ['ai_provider_settings','ai_usage_logs','ai_knowledge_bases','ai_rag_query_sources','ai_action_drafts','semantic_search_results'])
  if (!sql.includes(`CREATE TABLE IF NOT EXISTS orcafacil.${table}`)) throw new Error(`Tabela idempotente ausente: ${table}`);
if (!ai.includes('CanExecuteAutomatically(string action) => false')) throw new Error('A execução automática precisa permanecer bloqueada.');
if (ai.includes('Math.random')) throw new Error('Sugestão aleatória encontrada.');
console.log('OK: fundação de IA governada, sanitizada e sem execução crítica automática.');
