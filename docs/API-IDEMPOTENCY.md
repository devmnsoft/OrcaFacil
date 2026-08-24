# Idempotência

`POST /api/v1/clients` exige `Idempotency-Key` de 8 a 200 caracteres. Durante 24 horas, conta e API key recebem a mesma resposta para chave e conteúdo iguais. Reutilizar a chave com conteúdo diferente recebe `409 idempotency_conflict`. Somente hashes da chave e do conteúdo são persistidos.
