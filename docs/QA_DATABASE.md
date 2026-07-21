# QA do Banco de Dados — OrçaFácil

Use este checklist antes de avançar para funcionalidades maiores.

## Infraestrutura

- [ ] PostgreSQL 15+ ou 17 instalado/localizado.
- [ ] Database `orcafacil` criada.
- [ ] Usuário `orcafacil_user` criado com senha segura.
- [ ] Permissões concedidas ao usuário na database.
- [ ] `database/script_completop.sql` executado sem erro.

## Estrutura

- [ ] Schemas criados: `identity`, `core`, `billing`, `admin`, `logs`, `public_access`.
- [ ] Tabelas criadas conforme `docs/DATABASE.md`.
- [ ] Constraints únicas e checks validados.
- [ ] Índices obrigatórios existentes.
- [ ] Seeds de `admin.admin_settings` criados.

## Aplicação

- [ ] `ConnectionStrings:DefaultConnection` configurada em appsettings/user-secrets/ambiente.
- [ ] `dotnet restore OrcaFacil.sln` executa sem erro.
- [ ] `dotnet build OrcaFacil.sln` executa sem erro.
- [ ] `dotnet test OrcaFacil.sln` executa sem erro.
- [ ] `/health` retorna saudável para PostgreSQL.
- [ ] Dashboard abre.
- [ ] Cria usuário.
- [ ] Cria perfil do emitente.
- [ ] Cria orçamento.
- [ ] Cria recibo.
- [ ] Gera PDF.
- [ ] Logs/auditoria gravam registros.
