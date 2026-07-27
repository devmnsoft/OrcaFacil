# Migração para contas

1. Faça backup e aplique `database/script_completop.sql` em homologação.
2. Para cada usuário legado, crie uma `business_account` e um `account_member` Owner em uma transação idempotente.
3. Preencha `account_id` de clientes, documentos, assinaturas, pagamentos, notificações e auditoria a partir do proprietário.
4. Valide contagens e isolamento antes de criar chaves estrangeiras e tornar colunas obrigatórias.
5. Mantenha `user_id` durante a janela de compatibilidade; não altere IDs nem apague registros.
