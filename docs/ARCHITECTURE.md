# Arquitetura OrçaFácil

O MVP usa Clean Architecture/DDD com `Domain` sem dependências de camadas superiores, `Shared` com contratos comuns, `Application` com casos de uso e validações, `Persistence` com EF Core/PostgreSQL e Dapper, `Infrastructure` com serviços transversais, `Web` Razor Pages e `Api` Controllers.

Schemas esperados no PostgreSQL: `identity`, `core`, `billing`, `admin`, `logs` e `public_access`.
