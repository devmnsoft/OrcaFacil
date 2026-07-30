# Segurança do snapshot de revisão

`DocumentRevision.ProtectedSnapshot` preserva atualmente o JSON canônico usado para renderizar a revisão. Apesar do nome histórico, o valor **não é cifrado**. A coluna permanece inalterada nesta entrega para evitar mudar silenciosamente sua finalidade e invalidar revisões existentes.

O conteúdo é isolado por conta nas consultas da aplicação e deve receber a mesma proteção operacional aplicada aos demais dados comerciais (TLS, controle de acesso, backups protegidos e criptografia do volume do PostgreSQL). Uma futura adoção de ASP.NET Data Protection deverá ser feita por migration aditiva e usar o purpose versionado `OrcaFacil.DocumentRevision.Snapshot.v1`, incluindo uma estratégia explícita de rotação e leitura dos registros legados.

Hashes técnicos de IP e user-agent são independentes desse snapshot e usam HMAC-SHA256 com `Security:TechnicalFingerprintPepper`. O segredo não deve ser registrado nem armazenado no banco.
