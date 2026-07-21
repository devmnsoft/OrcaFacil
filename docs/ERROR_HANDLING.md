# Error handling

- A camada web usa TempData Toasts (`TempData.Success`, `TempData.Error`, `TempData.Warning`, `TempData.Info`) para mensagens comuns.
- Em Production, erros inesperados não exibem stack trace para usuário comum.
- Respostas HTML mostram mensagem amigável e `correlationId` para suporte.
- Diagnóstico de banco é área administrativa restrita a SuperAdmin.

## Tratamento de PostgreSQL 28P01 em cadastro/login

Usuários comuns devem receber mensagem amigável quando o banco estiver indisponível ou a autenticação do PostgreSQL falhar, sem detalhes de senha do banco ou stack trace.

Mensagem de cadastro: "Não foi possível concluir seu cadastro agora. Tente novamente em instantes ou fale com a MNSOFT."

Em Development ou em telas administrativas, `28P01` deve ser explicado como falha de autenticação no PostgreSQL para `orcafacil_user`, com orientação para verificar `ConnectionStrings:DefaultConnection` ou `ConnectionStrings__DefaultConnection`.

Logs técnicos devem registrar `SqlState`, usuário do banco sem senha, correlationId, operação e stack trace, mas nunca a senha.
