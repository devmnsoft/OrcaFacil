# Erros da API

Erros usam `{ "error": { "code", "message", "correlationId", "details" } }`. Os códigos públicos não incluem stack trace, SQL ou configurações: `unauthorized`, `forbidden`, `not_found`, `validation_error`, `rate_limited`, `scope_required`, `idempotency_conflict`, `resource_conflict`, `internal_error` e `integration_error`.
