# Release Candidate Checklist

| Item | Status | Evidência/Pendência |
|---|---|---|
| Build | Não validado | SDK .NET ausente no ambiente. |
| Testes | Não validado | `dotnet test` bloqueado por ausência do SDK. |
| Banco | Parcial | Matriz em `docs/DATABASE_ALIGNMENT.md`. |
| Migrations | Não validado | Requer SDK .NET. |
| Script SQL | Parcial | Auditado estaticamente; execução não realizada. |
| Cadastro | Parcial | Fluxo existe, falta E2E. |
| Login | Parcial | Fluxo existe, falta E2E. |
| Clientes | Parcial | CRUD existe, faltam testes de isolamento. |
| Orçamento | Parcial | Serviço/páginas existem, falta E2E. |
| Recibo | Parcial | Serviço/páginas existem, falta E2E. |
| PDF | Parcial | Endpoint/QuestPDF existem, falta validação runtime. |
| Aprovação | Parcial | Estrutura existe, falta teste de tokens. |
| Free | Parcial | Serviços existem, falta contrato único. |
| Pro | Parcial | Serviços existem, falta contrato único. |
| SuperAdmin | Parcial | Área existe, falta validação completa. |
| Mercado Pago desabilitado | Não validado | Precisa teste com config vazia. |
| Mercado Pago Sandbox | Não validado | Precisa credenciais sandbox via ambiente. |
| Logs | Parcial | Serilog/middlewares existem. |
| LGPD | Parcial | Docs e parte do banco; fluxos incompletos. |
| Acessibilidade | Parcial | Script criado; revisão manual pendente. |
| Mobile | Não validado | Requer execução visual. |
| IIS | Não validado | Docs existem; publicação não executada. |
| Backup | Parcial | Documentado em arquivos existentes; revisar procedimento real. |
| Rollback | Parcial | Documentar scripts e restauração de backup antes de RC final. |
