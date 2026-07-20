# Migração Firebase para PostgreSQL

O Firebase/Firestore deixa de ser o backend principal. O PostgreSQL passa a ser a fonte oficial para usuários, perfis, documentos, links públicos, assinaturas, auditoria e logs.

## Equivalência inicial
- `users` -> `identity.users`
- `profiles` -> `core.issuer_profiles`
- `documents` e itens -> `core.documents` / `core.document_items`
- `publicQuotes` -> `public_access.public_quotes`
- `logs` -> `logs.system_logs` e `logs.audit_logs`

## Estratégia futura
Exportar coleções Firestore em JSON, normalizar campos, validar donos dos dados, importar por lote com idempotência e gerar relatório de divergências. Nenhum dado real é migrado nesta primeira reescrita.
