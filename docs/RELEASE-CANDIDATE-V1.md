# OrçaFácil — escopo congelado da Release Candidate V1

## Módulos incluídos

Home, preços, autenticação, cadastro, onboarding, Dashboard, clientes, catálogo de serviços, orçamentos e proposta pública, ordens de serviço, agenda, pagamentos manuais, recibos, relatórios, alertas, configurações, plano, suporte e administração autorizada. Diagnóstico, logs e orientação de backup são recursos operacionais protegidos.

## Parcialmente incluídos

- SMTP/outbox: operacional quando as credenciais forem fornecidas; sem credenciais permanece desabilitado e diagnosticável.
- Backup: criação, listagem operacional e restore por scripts; não há restore iniciado pelo navegador.
- Integração de pagamento: depende das credenciais e homologação do gateway no ambiente do cliente.

## Ocultados do menu / não incluídos

Contratos recorrentes, importação de dados, webhooks administrativos e arquivos avançados não compõem a navegação comercial da V1. O código e o schema aditivo são preservados para evolução, sem botões simulados. Não estão incluídos novos módulos, restore pelo navegador ou o frontend JavaScript legado.

## Pendências reais e riscos conhecidos

- Cada instalação deve homologar PostgreSQL, SMTP, HTTPS, permissões NTFS e Hosting Bundle próprios.
- O fluxo completo requer banco real e conta de homologação; checks estáticos não substituem o ensaio no navegador.
- Restauração pode sobrescrever dados e exige janela, backup atual e confirmação forte no script.
- Chaves de Data Protection perdidas invalidam cookies e payloads protegidos; a pasta deve entrar no backup operacional.

## Instalar e atualizar banco

No PowerShell, copie `.env.example` apenas como referência, configure variáveis de ambiente e execute `scripts/windows/setup-local.ps1`. Para IIS, siga `DEPLOY-IIS.md`. Atualize sem apagar dados com `scripts/windows/update-database.ps1`; um patch alternativo deve estar dentro de `database/`.

## Validar login e fluxo principal

1. Abra a URL HTTPS, faça login com uma conta real e confirme redirecionamento para onboarding ou Dashboard.
2. Saia, entre novamente e reinicie o App Pool para confirmar persistência do cookie/Data Protection.
3. Crie empresa, cliente e serviço; gere orçamento e link público em janela anônima; aprove a proposta.
4. Gere a OS, registre pagamento, emita recibo e confira o relatório.
5. Confirme isolamento entre contas, bloqueio de `/Admin` para usuário comum, ausência de custo/margem na proposta e ausência de erros no Console/Network.
6. Repita em 320, 360, 390, 430, 768, 1024, 1366 e 1440 px e registre evidências da homologação.
