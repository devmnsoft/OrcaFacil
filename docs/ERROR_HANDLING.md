
## Tratamento de erros amigáveis

- A camada web usa TempData Toasts (`TempData.Success`, `TempData.Error`, `TempData.Warning`, `TempData.Info`) para mensagens comuns de cadastro, login, formulários e ações de documentos.
- O middleware global registra o erro completo no servidor e retorna `ProblemDetails` com `correlationId` para suporte.
- Em produção, stack trace nunca é devolvido ao usuário; a mensagem padrão é: “Não foi possível concluir a operação. Tente novamente em instantes ou fale com o suporte MNSOFT.”
- Em desenvolvimento, o detalhe é resumido com tipo e mensagem do erro, sem despejar stack trace completo.

### PostgreSQL 28P01

`28P01` indica senha inválida para o usuário PostgreSQL. Para o usuário comum, cadastro/login exibem uma mensagem amigável. Para operação técnica, confira `ConnectionStrings__DefaultConnection` e redefina a senha:

```sql
ALTER USER orcafacil_user WITH PASSWORD '123456';
```
