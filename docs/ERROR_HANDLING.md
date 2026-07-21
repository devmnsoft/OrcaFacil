# Error handling

- A camada web usa TempData Toasts (`TempData.Success`, `TempData.Error`, `TempData.Warning`, `TempData.Info`) para mensagens comuns.
- Em Production, erros inesperados não exibem stack trace para usuário comum.
- Respostas HTML mostram mensagem amigável e `correlationId` para suporte.
- Diagnóstico de banco é área administrativa restrita a SuperAdmin.
