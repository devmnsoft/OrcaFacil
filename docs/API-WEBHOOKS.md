# Webhooks

Consulte eventos em `GET /api/v1/webhooks/events` com `webhooks.read`. Endpoints são administrados em `/Settings/Webhooks`; em produção a URL deve usar HTTPS. O segredo completo só aparece na criação e as entregas usam histórico e idempotência. O receptor deve validar os cabeçalhos `X-OrcaFacil-Event`, `X-OrcaFacil-Delivery`, `X-OrcaFacil-Timestamp` e `X-OrcaFacil-Signature` antes de processar o corpo exato recebido.
