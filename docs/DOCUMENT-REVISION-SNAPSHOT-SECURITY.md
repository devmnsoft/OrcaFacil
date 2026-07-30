# Segurança do snapshot de revisão

## Decisão atual

`DocumentRevision.ProtectedSnapshot` preserva, por compatibilidade com a migration já aplicada, o JSON canônico usado para renderizar uma versão imutável do orçamento. Apesar do nome histórico, o conteúdo **não é criptografado pela aplicação**. O banco e seus backups devem, portanto, usar criptografia em repouso e acesso de menor privilégio.

A Release Operacional 8.1 não altera silenciosamente a semântica dessa coluna nem reescreve migrations aplicadas. Uma evolução futura deve usar migration aditiva para renomeá-la para `SnapshotJson` ou introduzir uma nova coluna protegida. Caso se adote ASP.NET Data Protection, o purpose obrigatório será `OrcaFacil.DocumentRevision.Snapshot.v1`, com rotação e persistência segura das chaves antes da migração dos dados.

O `SnapshotHash` continua sendo apenas o identificador de integridade e reutilização da revisão; ele não substitui proteção do conteúdo.
