# Arquitetura do OrçaFácil

## Plataforma atual

O OrçaFácil é um SaaS multi-tenant em **.NET 10**, com interface autenticada em **ASP.NET Core Razor Pages** e APIs HTTP no projeto `OrcaFacil.Api`. A aplicação principal é renderizada no servidor e usa JavaScript progressivo apenas para interações locais. PostgreSQL é a fonte de verdade; a implementação legada estática/Firebase permanece no repositório somente para compatibilidade durante a migração e não descreve a arquitetura principal.

## Camadas e dependências

- `OrcaFacil.Domain`: entidades, enums, value objects e invariantes sem dependência da Web.
- `OrcaFacil.Application`: casos de uso, contratos de serviços, comandos, validação e read models. Define as fronteiras consumidas pela Web.
- `OrcaFacil.Persistence`: EF Core/Npgsql, consultas Dapper, repositórios e serviços transacionais que implementam as fronteiras da Application.
- `OrcaFacil.Infrastructure`: integrações externas, e-mail, pagamentos, logging e geração de PDF com QuestPDF.
- `OrcaFacil.Web`: Razor Pages, composição de ViewModels, autenticação por cookie e assets do design system.
- `OrcaFacil.Api`: endpoints para integrações e fluxos públicos.
- `OrcaFacil.Shared`: contratos técnicos compartilhados estritamente necessários.

O fluxo esperado é `Razor Page → Application service/query → Persistence/Infrastructure → PostgreSQL`. PageModels não devem implementar regras comerciais nem consultar dados sem o escopo da conta.

## Isolamento multi-tenant e segurança

`ICurrentAccountService` resolve a conta ativa e valida a associação do usuário. Consultas comerciais filtram simultaneamente pelo identificador da entidade e por `AccountId`; GUIDs isolados nunca são tratados como autorização. Soft delete também é respeitado. Links públicos usam tokens aleatórios cujo valor bruto é devolvido somente na criação; apenas o hash é persistido. Decisões preservam a revisão, evidências técnicas e idempotência sem expor hashes ou segredos nos read models.

## Jornada comercial

O agregado de leitura comercial conecta documento, itens, revisões, acesso público, visualizações, decisão do cliente, eventos, ordem de serviço, agenda, pagamentos e recibos. `ICommercialWorkspaceQueryService` prepara o workspace e o pipeline sem espalhar consultas pela camada Web.

As mutações passam por `ICommercialJourneyService`: criação/reuso de revisão, link seguro, decisão pública, conversão idempotente para ordem de serviço, agendamento, início, conclusão, pagamento manual e recibo. Operações críticas usam transações e checagem da conta. `ActivityEvent` fornece a timeline verificável; a UI nunca fabrica acontecimentos.

## Documentos e PDF

Orçamentos e recibos mantêm itens e totais no domínio. Revisões guardam snapshots imutáveis e hash de integridade. A geração de PDFs ocorre no servidor por meio de `IPdfService`, implementado com QuestPDF, mantendo layout e dados consistentes com a versão registrada.

## Design system e experiência Web

O design system nativo usa tokens e componentes `of-*` em `tokens.css`, `components.css`, `forms.css`, `feedback.css` e folhas de domínio. A UI autenticada não depende de Bootstrap nem de CDN visual. Razor preserva HTML semântico, foco visível, contraste AA, estados vazios e responsividade mobile-first. Scripts em `wwwroot/js` não contêm autorização ou regra comercial.

## Persistência e evolução de esquema

EF Core 10 com Npgsql mapeia o schema PostgreSQL `orcafacil`. Alterações persistentes exigem migration compatível, atualização do script integral em `database`, preservação dos dados e validação de índices e constraints. Dapper é reservado a consultas de leitura bem delimitadas.

## CI e qualidade

O workflow instala .NET 10 e Node, restaura, compila em Release e executa os testes. Gates adicionais validam sintaxe e módulos JavaScript, dependências legadas na UI autenticada, contraste, Razor, SQL e colisões de contratos/test types. Auditoria de pacotes (`dotnet list package --vulnerable`) faz parte da preparação de release. Produção deve ser promovida somente com todos os gates verdes.
