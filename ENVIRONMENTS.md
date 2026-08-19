# Ambientes do OrçaFácil ASP.NET

O ASP.NET Core carrega `appsettings.json` e o arquivo do ambiente selecionado por
`ASPNETCORE_ENVIRONMENT`. Segredos devem ser fornecidos por variáveis com `__`
como separador; `.env.example` é apenas um inventário e não é carregado pela
aplicação.

## Development

Use `appsettings.Development.json` e, para valores privados, Secret Manager ou
variáveis de ambiente. Inicie sem Visual Studio com
`scripts/windows/start-local.ps1`; encerre com `stop-local.ps1`.

## Staging e Production

Defina no host, no mínimo:

- `ConnectionStrings__DefaultConnection`;
- `Application__PublicBaseUrl` com HTTPS e domínio público real;
- `Security__TechnicalFingerprintPepper`, `Security__PasswordResetPepper` e
  `Security__SecurityEventPepper` com valores aleatórios independentes;
- `DataProtection__KeysPath` ou `ORCAFACIL_DATAPROTECTION_PATH` apontando para
  uma pasta persistente fora do diretório publicado;
- `Uploads__Path` e `SystemHealth__LogsPath` fora de `wwwroot`;
- `Email__Host`, `Email__UserName` e `Email__Password` quando SMTP for usado.

Nunca coloque senhas, peppers, tokens ou connection strings reais nos arquivos
versionados. Em Production, HSTS, cookies seguros e erros sanitizados são
ativados pela aplicação.

## Manutenção

`ORCAFACIL_MAINTENANCE_MODE=true` habilita a resposta de manutenção com HTTP
503. `/health` e os ativos essenciais continuam acessíveis. SuperAdmin pode
acessar `/Admin/SystemHealth`; usuários comuns não alcançam fluxos da aplicação.
Desabilite com `ORCAFACIL_MAINTENANCE_MODE=false` após a janela operacional.

## Variáveis equivalentes

Variáveis ASP.NET convencionais continuam aceitas. Por exemplo,
`DataProtection__KeysPath` equivale ao alias
`ORCAFACIL_DATAPROTECTION_PATH`, e `MaintenanceMode__Enabled` equivale ao alias
`ORCAFACIL_MAINTENANCE_MODE`.
