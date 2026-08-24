# Autenticação e escopos

Crie a chave em **Configurações > API Keys**. O valor completo é exibido uma única vez; o banco conserva SHA-256 e o prefixo identificador. Envie `Authorization: Bearer of_live_xxx`. Chaves expiradas ou revogadas recebem `401` imediatamente.

Escopos atuais: `clients.read`, `clients.write`, `services.read`, `quotes.read`, `quotes.write`, `work_orders.read`, `receipts.read`, `contracts.read`, `webhooks.read`, `webhooks.manage`, `files.read` e `analytics.read`. A ausência do escopo exigido recebe `403 scope_required`.
