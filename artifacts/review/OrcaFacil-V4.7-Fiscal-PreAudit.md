# Pré-auditoria fiscal V4.7

Escopo revisado: solução ASP.NET atual, scripts e banco. A busca de bloqueadores não identificou fallback silencioso `Database=unavailable` ou `127.0.0.1:1` no código operacional. O módulo fiscal anterior era somente uma proteção do financeiro avançado e não autorizava NFS-e.

## Decisões de controle

- Emissão online permanece bloqueada sem provedor e certificado reais.
- Registro manual possui estado próprio, justificativa e trilha de auditoria; não equivale a autorização municipal.
- Protocolo e identificador do provedor são obrigatórios para o estado `Authorized`.
- Conta e cliente compõem todas as consultas e arquivos fiscais devem referenciar um `FileAsset` protegido.
- Segredos persistidos usam campos protegidos e nunca retornam em modelos de leitura.

## Pendências operacionais

Homologar um adaptador municipal real, validar certificado A1 com a cadeia ICP-Brasil e executar os testes manuais contra uma base PostgreSQL local antes de habilitar produção.
