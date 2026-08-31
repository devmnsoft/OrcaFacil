# OrçaFácil V5.0 — Auditoria de regras, métodos e design

Data: 2026-08-31. Escopo: exclusivamente a solução ASP.NET atual.

| Arquivo / linha aproximada | Tipo | Módulo | Severidade | Risco | Correção realizada | Pendência real |
|---|---|---|---|---|---|---|
| `Application/Quality/BusinessRefinementServices.cs:10` | status distribuídos | Transversal | P1 | transições incompatíveis entre fluxos | catálogo e mapa determinístico centralizados | integrar gradualmente todos os comandos persistentes ao lifecycle |
| `Application/Quality/BusinessRefinementServices.cs:43` | autorização/auditoria | Transversal | P1 | transição crítica sem autorização ou trilha | regra de permissão, motivo e auditoria obrigatória | persistência concreta do audit deve ser ligada ao compor o fluxo |
| `Application/Quality/BusinessRefinementServices.cs:81` | dinheiro | Financeiro | P1 | desconto/retenção negativos e arredondamento implícito | cálculo decimal, limites e arredondamento explícito | migrar calculadoras legadas incrementalmente |
| `Application/Quality/BusinessRefinementServices.cs:116` | datas | Financeiro | P2 | vencimento anterior à emissão | política server-side por `DateOnly` | adotar relógio de negócio nos comandos legados |
| `Application/Quality/BusinessRefinementServices.cs:124` | isolamento | Portais | P0 | acesso cruzado entre conta/cliente/parceiro | guard composto por conta e identidade do portal | revisar downloads legados individualmente |
| `Application/Quality/BusinessRefinementServices.cs:148` | score | Qualidade | P2 | score arbitrário/não reproduzível | score derivado apenas de checks reais | persistir snapshots quando a Central for integrada ao banco |
| `scripts/check-sprint49-refinement.mjs:1` | prevenção de regressão | Transversal | P2 | padrões inseguros voltarem | check determinístico registrado no npm | ampliar allowlist contextual sem reduzir cobertura |

## Mapas cobertos

Proposta, OS, fatura, pagamento, contrato, ativo, tarefa e documento fiscal possuem transições explícitas. Transições críticas exigem permissão; reabertura/cancelamento/estorno críticos exigem motivo. Pagamento pendente é impedido de gerar recibo.

## Limites desta entrega

A auditoria não declara telas ou integrações não exercitadas como aprovadas. Validação visual completa requer banco, credenciais e runtime .NET disponíveis no ambiente; nenhuma evidência foi fabricada.
