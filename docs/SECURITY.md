# Segurança e LGPD

- Autenticação por cookie HTTP-only com claims `sub`, `email`, `name`, `role` e `plan`.
- Endpoints administrativos exigem policy `SuperAdmin`.
- `ICurrentUserService` impede fallback para `Guid.Empty` em operações protegidas.
- Logs e auditoria registram eventos sem senha ou token sensível em mensagens.
- Middleware global retorna `ProblemDetails` com `correlationId` e `traceId`.

## Pendências

- Configurar secrets reais em produção por variáveis de ambiente.
- Revisar retenção de logs/auditoria conforme LGPD.
