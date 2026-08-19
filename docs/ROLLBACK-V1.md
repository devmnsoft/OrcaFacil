# Rollback operacional V1

## Pré-condições

Interrompa novas gravações, registre incidente/horário/commit e identifique o último artefato aprovado. Confirme checksum do artefato anterior, backup atual, destino PostgreSQL e persistência das chaves de Data Protection. Nunca execute restore sem janela e autorização.

## Voltar o artefato

1. Coloque a aplicação em manutenção e pare o App Pool/serviço.
2. Preserve logs e o artefato atual fora da pasta publicada.
3. Reponha atomicamente a pasta do artefato anterior aprovado, sem alterar arquivos de segredo do servidor.
4. Reaplique permissões NTFS, confirme variáveis de ambiente e inicie o App Pool.
5. Se a versão anterior for compatível com o schema aditivo atual, não restaure o banco.

## Restaurar o banco quando indispensável

1. Faça outro backup do estado incidente com `scripts/windows/backup-db.ps1`.
2. Restaure primeiro em banco isolado e valide o arquivo.
3. Com aprovação explícita, execute `scripts/windows/restore-db.ps1 -Backup <arquivo> -ConfirmRestore` e confirme o `ShouldProcess`.
4. Não use `--clean`, `DROP`, `TRUNCATE` ou recriação de usuários. Se uma alteração de schema não for retrocompatível, use somente patch compensatório revisado e testado.

## Saúde após rollback

- Verifique `/health`, inicialização, logs e diagnóstico de schema sem exposição da connection string.
- Faça login com usuário autorizado, logout e novo login; confirme isolamento de conta e bloqueio de `/Admin` para usuário comum.
- Abra em janela anônima uma proposta pública previamente válida e confirme que custo, margem e token bruto não aparecem.
- Consulte tabelas críticas, contagens e registros-sentinela; compare com o backup e execute `npm run check:database-schema`.
- Valide cliente → orçamento → proposta, aprovação → OS e pagamento → recibo sem criar duplicatas.
- Monitore erros, EmailOutbox, auditoria e métricas por pelo menos uma janela operacional antes de encerrar o incidente.
