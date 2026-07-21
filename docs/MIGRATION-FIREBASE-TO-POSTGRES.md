# Migração Firebase para PostgreSQL

O Firebase/Firestore deixa de ser o backend principal. O PostgreSQL passa a ser a fonte oficial para usuários, perfis, documentos, links públicos, assinaturas, auditoria e orcafacil.

## Equivalência inicial
- `users` -> `orcafacil.users`
- `profiles` -> `orcafacil.issuer_profiles`
- `documents` e itens -> `orcafacil.documents` / `orcafacil.document_items`
- `publicQuotes` -> `orcafacil.public_quotes`
- `logs` -> `orcafacil.system_logs` e `orcafacil.audit_logs`

## Estratégia futura
Exportar coleções Firestore em JSON, normalizar campos, validar donos dos dados, importar por lote com idempotência e gerar relatório de divergências. Nenhum dado real é migrado nesta primeira reescrita.
