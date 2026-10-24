# PR — Fechô 2.0: branch de evolução completa (84 commits)

**Base:** `rebranding/fecho` · **Head:** `fecho/evolution-2.0` @ `df2713b` · **Data:** 24/10/2026

> Descrição pronta para colar na criação do PR no GitHub
> (URL de criação: `<repo>/compare/rebranding/fecho...fecho/evolution-2.0?expand=1`).

## O que é

Toda a evolução do Fechô 2.0 construída sobre o rebranding, em quatro frentes:

1. **Monetização** — consolidação v74/v75/v76, carência real de 3 dias, ciclo de inadimplência ponta a ponta, gates de plano em todo o app, gates reais (clientes/serviços/PDF) e instrumento de homologação sandbox R5 (Mercado Pago).
2. **Jornada comercial** — estados de OS alcançáveis (espera/vencimento), assinatura recorrente do cliente, cobrança com lembrete e data civil na zona de negócio da agenda.
3. **Integrações** — NFS-e fiscal manual, exportação para o contador, WhatsApp oficial (Cloud API), e-mail transacional na outbox e API pública somente-leitura.
4. **Backlog de evolução (15 itens)** — todos concluídos e registrados em `NEXT-RELEASE.md`, fechados com o escopo padrão "correções + configuração + testes + validador + docs" (dois commits por item).

## Backlog de evolução — 15/15 concluídos

| # | Item | Código | Validador |
|---|---|---|---|
| 1 | Convites de equipe por e-mail | `26cf4e6` | registro no roadmap (item 8) |
| 2 | PWA instalável | `89e3573` | `check:pwa` |
| 3 | E-mail transacional de cobrança | `3a34da5` | `check:email-outbox` / `check:outbox-flow` |
| 4 | WhatsApp oficial (Cloud API) | `862fa3e` | `check:whatsapp-api` |
| 5 | Ordem de serviço (espera + vencimento) | `0aa3a3d` | `check:work-orders` |
| 6 | Assinatura recorrente do cliente | `9386fee` | `check:recurring-billing` |
| 7 | Cobrança do cliente (lembrete) | `0db02bb` | `check:receivable-reminder` |
| 8 | Agenda (zona de negócio + conflitos) | `348a170` | `check:schedule-flow` |
| 9 | Integração fiscal (NFS-e manual) | `c429624` | `check:fiscal-flow` |
| 10 | Aplicativo móvel (PWA app-grade) | `b3a5839` | `check:mobile-app` |
| 11 | API pública somente-leitura | `723d29e` | `check:public-api` |
| 12 | Integração com contadores (export) | `3739bd1` | `check:accounting-export` |
| 13 | Backup exportável (self-service) | `6bac1ea` | `check:data-export` |
| 14 | Relatórios de conversão | `c02eaaa` | `check:conversion-report` |
| 15 | Usuários adicionais pagos no Negócio | `c31abb7` | `check:additional-seats` |

Cada item traz **dois commits**: código (validador novo ou regressão + testes unitários) e docs (marcação no `NEXT-RELEASE.md` com o hash + item detalhado no `FECHO-EVOLUTION-ROADMAP.md`).

## Monetização e operações (trabalho pré-backlog nesta branch)

- **Consolidação monetização v74/v75/v76** (`75dd978`) — base nova sai integral, guards Postgres verdes.
- **Carência real de 3 dias nos planos pagos + ciclo de inadimplência provado ponta a ponta** (`d17f8db`, R5).
- **Gates de plano em todo o app** — recibos/relatórios/equipe/aprovação pública (`8e8fefb`), ordens de serviço (`cf32e63`), auditoria da conta (`6a150ca`), templates/branding/prefixo/suporte/métricas (`22fa3ed`).
- **Gates reais de monetização** — clientes, serviços e cota de PDF (Free: 10/mês) (`531c75b`, `check:feature-gates`).
- **R5: instrumento de homologação sandbox Mercado Pago (CLI) + runbook com matriz de aceites** (`59ad161`).
- **E-mail transacional de cobrança** (`3a34da5`) — atraso, carência, suspensão e restauração idempotentes na outbox.
- **WhatsApp oficial pela Cloud API do Meta** (`862fa3e`) — envio real, webhook assinado, configuração auditada.
- **PWA instalável** (`89e3573`) — ícones PNG 192/512 e apple-touch-icon rasterizados do SVG.
- **Higiene** — `node_modules`/`.vs` desrastreados do git (`e0f4568`); npm audit fix brace-expansion high (`d0f4e4b`).

## Evidência (gate final de integração)

- **Varredura completa: 623/623 validadores `check:*` verdes** — a única falha encontrada na varredura foi dívida desta branch (3 inputs sem rótulo na página fiscal do item 9), corrigida em `15cfc09`.
- **Suíte C#: 920 aprovados / 0 falhas / 19 ignorados.**
- **Build: 0 erros.**
- Registro: `FECHO-EVOLUTION-ROADMAP.md`, item 22.

## Plano de teste sugerido (manual)

1. `dotnet build OrcaFacil.sln` + migrações (base nova ou existente).
2. **Fiscal:** recebível pago → solicitar nota → registrar emissão (nº/chave/justificativa) → cancelar com motivo → exportação contábil.
3. **Assentos:** conta Negócio → página Assinatura → contratar usuário adicional → convidar membro além do limite original.
4. **Relatórios:** relatório de conversão com período/filtros → CSV → tile na central de inteligência.
5. **API pública:** leitura de recursos com token; verificar escopos e isolamento por conta.
6. **Móvel:** instalar a PWA; navegação e telas essenciais no modo instalado.
7. **Inadimplência:** simular atraso → e-mails via outbox → suspensão/restauração.
8. **WhatsApp:** envio com token sandbox configurado + webhook assinado.

## Decisões abertas (não bloqueiam o merge)

- **R5 — provedor de pagamento para cobrança efetiva:** emissão NFS-e real, cobrança/repasse do cliente e assentos adicionais ficam registrados com provedor manual; o instrumento sandbox + runbook já estão prontos (`59ad161`) para a troca quando as credenciais por tenant forem definidas.

## Documentação de referência

- `docs/NEXT-RELEASE.md` — fila de evolução com itens, hashes e validadores.
- `docs/FECHO-EVOLUTION-ROADMAP.md` — registro completo da evolução (itens 1 a 22).
