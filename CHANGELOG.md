# Changelog

## [1.0.0-final] - 2026-08-19

### Módulos finalizados
- Release ASP.NET congelada com site público, autenticação/onboarding, comercial, proposta pública, operação, financeiro, configurações, suporte e SuperAdmin protegido.

### Correções críticas e segurança
- Corrigido o contrato Razor do filtro do pipeline e criado o gate final obrigatório da release.
- Ampliada a validação das tabelas físicas dos fluxos financeiro, recorrente, comercial e suporte.
- Mantidos gates para isolamento de conta, Admin, proposta pública, antiforgery, segredos, logs, auditoria e diagnóstico sanitizado.

### Banco, scripts e operação
- Restore passou a recomendar backup validado antes da confirmação destrutiva do destino.
- Publicados runbooks de go-live, homologação, regressões, rollback e operação assistida.
- O schema continua aditivo/idempotente; nenhum `DROP`/`TRUNCATE` de tabela foi introduzido.

### Design e pendências conhecidas
- Gates de consistência, acessibilidade e responsividade integram a aprovação final.
- Integrações opcionais ainda dependem da configuração e homologação do ambiente de cada instalação.
- Build/test/publish, PostgreSQL e navegador devem ser executados num agente de release que disponha do SDK .NET, PostgreSQL e navegador antes da autorização humana de produção.

## [1.0.0] - 2026-06-21

### Adicionado
- MVP do OrçaFácil.
- Esteira CI/CD com validação, build, security check, artifacts e deploy manual.

### Segurança
- Build de produção minificado/ofuscado.
- Deploy de produção manual com GitHub Secrets.

### Corrigido
- Ajustes de inicialização em IIS/static.
