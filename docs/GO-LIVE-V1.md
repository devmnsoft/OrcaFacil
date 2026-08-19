# OrçaFácil ASP.NET — Go-live controlado V1

**Versão:** 1.0.0
**Data de congelamento:** 19/08/2026
**Escopo:** exclusivamente a solução ASP.NET `OrcaFacil.sln`.

## Escopo congelado

Incluídos: site público, autenticação, cadastro, onboarding, Dashboard, clientes, serviços, orçamentos e proposta pública, pipeline e ações comerciais, ordens de serviço e agenda, contas a receber, pagamentos manuais, recibos, contratos, fluxo de caixa, relatórios, alertas, configurações da conta, plano, suporte e área SuperAdmin protegida (contas, usuários, planos, assinaturas, logs, auditoria, diagnóstico e EmailOutbox).

Ficam ocultas rotas sem fluxo operacional completo. Integrações SMTP e de pagamento somente são habilitadas quando a instalação fornece configuração válida; nenhuma credencial acompanha a release.

## Pendências e riscos conhecidos

- **Bloqueantes:** devem permanecer vazios antes da autorização de produção; qualquer P0/P1 reabre a release.
- **Não bloqueantes:** homologação de integrações opcionais específicas do cliente e acompanhamento assistido após o deploy.
- O ensaio final depende de PostgreSQL, HTTPS, SMTP e permissões do host de destino.
- Restore sobrescreve o banco de destino; exige janela, confirmação explícita e backup prévio.
- A pasta persistente de Data Protection deve acompanhar o backup para preservar cookies e dados protegidos.

## Checklist de deploy

- [ ] Registrar commit, responsável, janela e artefato 1.0.0 aprovado.
- [ ] Executar todos os comandos de `RELEASE_CHECKLIST.md`.
- [ ] Configurar variáveis de ambiente sem gravar segredos em arquivos versionados.
- [ ] Confirmar HTTPS, HSTS, cookie seguro, `PublicBaseUrl` e diretório de Data Protection.
- [ ] Instalar o artefato, iniciar a aplicação e validar `/health`.

## Banco, backup e rollback

- [ ] Fazer backup datado e testar sua leitura antes da atualização.
- [ ] Aplicar `database/script_completop.sql` ou `scripts/windows/update-database.ps1` sem limpeza destrutiva.
- [ ] Verificar schema e tabelas críticas com `npm run check:database-schema`.
- [ ] Executar smoke test de login, proposta pública, OS, pagamento e recibo.
- [ ] Manter artefato anterior e seguir `docs/ROLLBACK-V1.md` em caso de falha.

## Homologação

- [ ] Validar todos os perfis e resoluções descritos em `docs/HOMOLOGACAO-FINAL-V1.md`.
- [ ] Confirmar isolamento por conta, antiforgery e bloqueio de `/Admin` para usuário comum.
- [ ] Confirmar que proposta pública não revela custo, margem ou token bruto.
- [ ] Confirmar Console e Network sem erros e ausência de scroll horizontal.

## Primeiro acesso

Crie o SuperAdmin com `scripts/windows/seed-superadmin.ps1`, fornecendo e-mail e senha por parâmetro seguro ou prompt. Exemplo de identificação: `admin.homologacao@example.invalid` (não é uma conta existente e nenhuma senha é fornecida). No primeiro acesso, altere a senha temporária, confirme dados da conta e conclua o onboarding. Nunca reutilize credenciais de homologação em produção.
