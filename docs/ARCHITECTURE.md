# Arquitetura OrçaFácil ASP.NET Core

A solução segue Clean Architecture com `Domain`, `Shared`, `Application`, `Persistence`, `Infrastructure`, `Api`, `Web` e `UnitTests`.

## Regras principais

- `Domain` contém entidades, enums e value objects sem dependência de infraestrutura.
- `Application` contém abstrações, DTOs, comandos, validadores e serviços de caso de uso.
- `Persistence` contém EF Core, Dapper, repositórios, auditoria persistida e configurações de mapeamento.
- `Infrastructure` contém serviços técnicos, autenticação auxiliar, middleware e PDF QuestPDF.
- `Api` e `Web` compõem DI, autenticação por cookie, Serilog, health checks e endpoints.

## Pendências

- Completar CRUDs de edição/exclusão/duplicação.
- Evoluir admin para consultas Dapper reais.
- Revisar migration após instalação do SDK .NET 10.
