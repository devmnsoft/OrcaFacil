# Arquitetura OrçaFácil

O MVP usa Clean Architecture/DDD com `Domain` sem dependências de camadas superiores, `Shared` com contratos comuns, `Application` com casos de uso e validações, `Persistence` com EF Core/PostgreSQL e Dapper, `Infrastructure` com serviços transversais, `Web` Razor Pages e `Api` Controllers.

Schemas esperados no PostgreSQL: `identity`, `core`, `billing`, `admin`, `logs` e `public_access`.

## Diagnóstico e persistência PostgreSQL

A persistência usa EF Core com `HasDefaultSchema("orcafacil")` e conversão global de propriedades para colunas em `snake_case`, alinhando o modelo ao script `database/script_completop.sql`. Consultas Dapper usam nomes qualificados (`orcafacil.documents`, `orcafacil.users`, `orcafacil.user_usage`) e parâmetros.

O contrato `IDatabaseDiagnosticsService` fica na camada Application e a implementação PostgreSQL em Persistence. O Web expõe `/health/db`, a página pública `/diagnostico` e a página SuperAdmin `/Admin/Settings/Database` sem revelar senha ou connection string completa.
