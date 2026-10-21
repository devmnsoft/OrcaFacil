# R5 — Homologação do gateway Mercado Pago em sandbox

**Pré-requisito:** credenciais de teste fora do repositório.
**Instrumento:** `scripts/sandbox/mercadopago-sandbox.mjs` (Node ≥20, sem dependências).
**Estado atual (2026-10-20):** ciclo de inadimplência/regularização provado em
`DunningCycleHomologationTests` e em PostgreSQL real; este runbook adiciona a prova
**com eventos reais do gateway**.

## 0. Configuração da sessão (nunca commitar)

```powershell
$env:MERCADOPAGO_ACCESS_TOKEN  = "<token TEST- do sandbox>"
$env:MERCADOPAGO_WEBHOOK_SECRET = "<segredo do webhook>"
$env:MERCADOPAGO_WEBHOOK_URL    = "https://<host-homologacao>/api/webhooks/mercadopago"
```

App de homologação precisa estar publicada com `MercadoPago:Environment=Sandbox`
(`ASPNETCORE_ENVIRONMENT` de homologação) e banco com backup validado anterior.

## 1. Criação de cobrança sandbox

```powershell
node scripts/sandbox/mercadopago-sandbox.mjs pix  --amount 24.90 --reference "inv:<uuid da fatura>"
node scripts/sandbox/mercadopago-sandbox.mjs boleto --amount 249 --reference "inv:<uuid>"
```

Esperado: HTTP 200 com `payment_id` e QR/ticket (`pix`) ou URL do boleto sem http(s)
inválido. `idempotency_key` única por execução (header `X-Idempotency-Key`).

## 2. Consulta de estado

```powershell
node scripts/sandbox/mercadopago-sandbox.mjs status --id <payment_id>
```

Esperado: `status=pending` (cron de atraso do MP ajusta o estado do recurso criado
no modo de tela do sandbox; Pix/boleto reais disponíveis em homologação/produção).

## 3. Webhooks assinados (verificações de proteção)

Assinatura oficial ativada de verdade (`ts/v1` com o manifesto `id;request-id;ts;`):

```powershell
node scripts/sandbox/mercadopago-sandbox.mjs webhook --id <payment_id> --action updated
```

Proteções obrigatórias (recusar sem efeito financeiro):

```powershell
# a) assinatura com segredo errado => HTTP 401  (o script falha se não for 401)
node scripts/sandbox/mercadopago-sandbox.mjs webhook --id <payment_id> --tamper
# b) mesmo evento duas vezes => 2ª resposta com "duplicate=true"
# c) payload sem data.id / sem signature => 401
```

Conferência offline do HMAC (não envia; útil quando não há app publicada):

```powershell
node scripts/sandbox/mercadopago-sandbox.mjs webhook --id <payment_id> --echo
```

A prova de interoperabilidade da assinatura é travada por
`CommercialCandidateHomologationTests.MercadoPago_HandleWebhook_*` (manifesto
`ts/v1` válido aceito, assinatura adulterada e ausente recusadas) e o `--echo`
permite recomputar o HMAC com um verificador independente.

## 4. Liquidação aprovada (restituição da assinatura inadimplente)

Prepare o cenário inadimplente antes do evento `approved`:

1. No banco de homologação, recue a cobertura: `UPDATE orcafacil.subscriptions SET paid_through_at = now() - interval '7 days', next_due_at = same, status = 'Active' WHERE id = '<sub>';`
2. Aguarde o `BillingBackgroundWorker` (ou use o botão "Recalcular inadimplência" do SuperAdmin) — estado esperado: `Suspended` (7 dias de atraso excedem 3 de carência + 5 do prazo) e evento `billing.suspended`.
3. Envie o webhook do pagamento aprovado do recurso do item 2/1:
   `node scripts/sandbox/mercadopago-sandbox.mjs webhook --id <payment_id> --action updated`
4. Prova no banco (nenhum `DROP`; só consultas):
   ```sql
   SELECT status, paid_through_at > now() AS coberto, past_due_since, suspended_at
     FROM orcafacil.subscriptions WHERE id = '<sub>';
   SELECT event_type, created_at FROM orcafacil.subscription_events
    WHERE subscription_id = '<sub>' ORDER BY created_at DESC LIMIT 8;
   SELECT event_key, processed, count(*) FROM orcafacil.mercadopago_webhook_events
    WHERE external_payment_id = '<payment_id>' GROUP BY 1,2;
   ```
   Esperado: `Active` com cobertura futura implícita; trilha
   `billing.suspended -> billing.payment_approved` (a derrubada da
   inadimplência acontece imediatamente por fatura quitada); **um único** webhook
   marcado processado (dedup por `EventKey`); **uma única** linha em
   `billing_coverage_applications` para `(external_payment_id, effect='extended')`.

## 5. Matriz de resultados esperados

| Etapa | Entrada | Resultado exigido |
|---|---|---|
| 1a | Pix sandbox | 200 + `payment_id` + QR base64 (texto) |
| 1b | Boleto sandbox | 200 + documento URL de `http(s)` válido |
| 2 | status | `pending` até ativar o fluxo em approved |
| 3a | assinatura errada | **401** e nenhum row novo |
| 3b | re-envio | `duplicate=true`; nenhuma segunda cobertura |
| 4 | approved | assinatura reativada + cobertura única (idempotente) |
| reversão | refund/chargeback | cobertura recua um ciclo; `PastDue` sem regressar quitado |

## 6. Critério de fechamento do R5

Tabela acima 100% verde, capturas de tela e SQL printadas na evidência de
homologação; revisão humana; nenhum `DROP/TRUNCATE`; segredos apenas via
variáveis de ambiente.
