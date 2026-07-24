# Known Issues

- O ambiente atual não possui `dotnet`; restore, build, testes, migrations e execução da aplicação ASP.NET não foram validados localmente.
- Há páginas legadas duplicadas com rotas novas (`/Login` e `/Auth/Login`, `/Cadastro` e `/Auth/Register`, `/Dashboard` e `/Dashboard/Index`, `/Historico` e `/Documents`).
- Catálogo de serviços está como stub visual; não há CRUD ASP.NET funcional confirmado.
- Algumas tabelas do script SQL representam funcionalidades não integradas no código ASP.NET, como versionamento, follow-ups e solicitação LGPD de exclusão.
- Testes de isolamento multiusuário ainda são insuficientes para critério bloqueador de lançamento.
- Mercado Pago precisa validação sandbox real, assinatura de webhook e comportamento com integração desabilitada.
