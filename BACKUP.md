# Backup antes de release

- Firestore: exporte manualmente pelo Google Cloud/Firebase. Quando aplicável: `gcloud firestore export gs://BUCKET/backups/orcafacil-YYYY-MM-DD`.
- Admin Geral: exporte usuários, logs, erros e auditoria em CSV antes da release.
- Pagamentos: exporte registros administrativos e concilie com Mercado Pago; não exporte tokens.
- Artifact: salve o artifact `orcafacil-dist` gerado no CI ou compacte a pasta `dist` validada.
- Versão publicada: registre versão, commit SHA, data, responsável e notas.
- Restauração: restaure o export Firestore com `gcloud firestore import`, recoloque a `dist` anterior no IIS ou faça rollback do Firebase Hosting.
