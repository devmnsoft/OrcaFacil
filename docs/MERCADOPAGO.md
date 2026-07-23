# Mercado Pago no OrçaFácil

Use apenas variáveis de ambiente em produção: `MercadoPago__AccessToken`, `MercadoPago__PublicKey` e `MercadoPago__WebhookSecret`. Nunca commite tokens reais.

A integração foi preparada em modo seguro: Pix e boleto recebem e-mail do pagador, tipo/número de documento CPF/CNPJ normalizado, valor, descrição, referência externa e `idempotency key` obrigatória. O QR code/base64 retornado deve ser tratado como texto em banco, sem gerar arquivo binário.

Webhook: `POST /api/webhooks/mercadopago` registra o payload bruto em `orcafacil.mercadopago_webhook_events`, deduplica por chave do evento e retorna 200 rapidamente. Em produção, habilite validação de assinatura quando `WebhookSecret` estiver configurado.

Teste em Sandbox com `MercadoPago:Environment=Sandbox`, crie cobranças Pix/boleto, envie webhooks `pending`, `approved` e `expired`, confira pagamentos/assinaturas e auditoria.
