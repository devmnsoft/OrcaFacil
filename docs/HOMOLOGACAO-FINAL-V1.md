# Homologação final V1

**Versão:** 1.0.0 — **Data:** 19/08/2026

## Preparação e evidências

Use o publish Release e banco de staging restaurável, nunca dados falsos apresentados como aprovação. Para cada caso registre data, executor, perfil, conta, navegador, viewport, resultado, evidência e incidente. Segredos e tokens não podem aparecer nas evidências.

## Matriz por perfil

- **Visitante:** Home, Preços, Ajuda, Suporte, Cadastro, Login, CTAs, menu desktop/mobile e footer.
- **Novo usuário:** cadastro, login, criação de conta, onboarding, configuração inicial e Dashboard.
- **Comercial:** clientes, serviços, orçamento, proposta pública, WhatsApp, pipeline, ações, templates, alertas sem duplicação e relatórios.
- **Cliente externo (anônimo):** visualizar, aprovar, recusar, pedir alteração, imprimir e tratar links inválido/expirado; não revelar custo, margem ou token.
- **Operacional:** conversão idempotente em OS, agenda, início, conclusão, cancelamento com motivo, timeline e checklist disponível.
- **Financeiro:** recebíveis, pagamento positivo, reversão, recibo único com aviso fiscal, contrato ativo/suspenso, cobrança e fluxo de caixa real.
- **Administrador da conta:** configurações, usuários, permissões, plano, identidade, dados comerciais e de pagamento.
- **SuperAdmin:** Dashboard, contas, usuários, planos, assinaturas, logs, auditoria, diagnóstico, EmailOutbox e suporte; acesso comum deve retornar bloqueio.

## Banco e segurança

- [ ] Instalação limpa e atualização preservam registros-sentinela reais de homologação.
- [ ] Consultas autenticadas respeitam `AccountId`; tentativa cruzada é negada.
- [ ] POSTs rejeitam antiforgery ausente; validação server-side rejeita entradas inválidas.
- [ ] Produção usa HTTPS/HSTS/cookie seguro e erro sem detalhe técnico.
- [ ] Logs, auditoria e diagnóstico não exibem senha, token ou connection string completa.

## Navegador, Console e responsividade

Execute a matriz em 320, 360, 390, 430, 768, 1024, 1366 e 1440 px. Não aprove com scroll horizontal, menu/topbar sobrepostos, botão inacessível, modal fora da viewport, tabela sem contenção, card cortado ou formulário impraticável. Em cada fluxo confirme Console sem erro e requisições sem 4xx/5xx inesperado.

## Resultado

O checklist só pode ser assinado quando todos os P0/P1 estiverem corrigidos, o rollback tiver sido ensaiado e os resultados reais estiverem registrados. Credencial ilustrativa: `usuario.homologacao@example.invalid`, sempre com senha fornecida fora do repositório e revogada após o ensaio.
