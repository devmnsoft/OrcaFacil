# Auditoria de tipos dos testes unitários — Release Operacional 5.2

Escopo: declarações de nível superior (`class`, `record`, `struct`, `enum` e `interface`) em `tests/OrcaFacil.UnitTests`, desconsiderando `bin`, `obj` e código gerado. Todos os tipos usam o namespace `OrcaFacil.UnitTests`.

| Tipo | Arquivo | Duplicidade | Responsabilidade | Decisão |
|---|---|---:|---|---|
| `BrazilianDocumentTests` | `BrazilianDocumentTests.cs`; anteriormente também `DomainTests.cs` | corrigida | Validação, normalização e dígitos verificadores de CPF/CNPJ | Mantido exclusivamente no arquivo canônico; casos únicos foram consolidados e os equivalentes não foram repetidos. |
| `DomainTests` | `DomainTests.cs` | não | Testes de domínio ainda não extraídos | Mantido temporariamente, sem a classe de documentos brasileiros. |
| `RazorRouteUniquenessTests`, `RouteApplicationFactory` | `RazorRouteUniquenessTests.cs` | não | Endpoints Razor e infraestrutura do teste de rotas | Mantidos; nomes e responsabilidades são distintos. |
| `DatabaseConnectionOptionsTests`, `CommercialStatusTransitionTests`, `DbContextContractTests`, `PublicDocumentTokenServiceTests`, `AuditServiceTests` | arquivos homônimos | não | Contratos unitários específicos | Mantidos, um tipo de teste público por arquivo. |
| `VisualTransformationContractTests`, `ConfigurationSourceDescriptorTests`, `ClientPageTests`, `RegistrationRelationshipModelTests` | arquivos homônimos | não | Contratos de UI/configuração/cadastro | Mantidos, sem colisões. |
| `BillingProfileSchemaRepairTests`, `DatabaseDiagnosticsTests`, `CommercialPlatformTests`, `QuerySchemaTests` | arquivos homônimos | não | Persistência, diagnóstico e plataforma comercial | Mantidos, sem colisões. |

## Ocorrências de `BrazilianDocumentTests`

- `DomainTests.cs` declarava testes de CPF/CNPJ válidos e inválidos e tratava documento vazio como opcional. Os casos válidos e de dígitos incorretos já tinham equivalentes no arquivo canônico; os casos de tamanho curto foram consolidados na matriz parametrizada.
- `BrazilianDocumentTests.cs` é a definição mantida. Ela cobre CPF e CNPJ com e sem máscara, repetição, tamanhos menor/maior, caractere inválido, primeiro/segundo dígitos verificadores, valores nulo/vazio/em branco e normalização.
- A expectativa antiga de aceitar documento ausente conflitava com o contrato atual (`HasValidCheckDigits` rejeita ausência) e não foi preservada como duplicação artificial.

## Prevenção

`scripts/check-csharp-test-type-collisions.mjs` agrupa cada declaração por namespace e nome, informa arquivo e linha e falha em colisões. Tipos `partial` somente são aceitos quando o nome totalmente qualificado constar explicitamente na lista de autorização, atualmente vazia. O compilador C# permanece como validação definitiva.
