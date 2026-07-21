# Checklist QA UI — OrçaFácil

## Landing
- [ ] Usuário entende em segundos o que é o OrçaFácil.
- [ ] Hero tem CTA Começar grátis, Entrar e Ver como funciona.
- [ ] A seção “problema resolvido” deixa claro por que usar.
- [ ] Planos Free e Pro estão simples.

## Autenticação
- [ ] Login tem labels, mensagens claras e link para suporte.
- [ ] Cadastro tem aceite de termos e privacidade.
- [ ] Mobile mostra formulário com bom espaçamento.

## Onboarding e dashboard
- [ ] Onboarding mostra progresso do primeiro uso.
- [ ] Dashboard mostra “Seu próximo passo”.
- [ ] Usuário sem emitente é orientado a cadastrar dados.
- [ ] Usuário sem documentos é orientado a criar orçamento.

## Documentos
- [ ] Novo orçamento explica cliente, itens, valores e validade.
- [ ] Novo recibo informa que não substitui nota fiscal quando obrigatória.
- [ ] Histórico tem filtros, empty state e ação principal clara.
- [ ] Detalhes mostra total, itens, observações e ações.

## Público e suporte
- [ ] Página pública explica aprovação e exibe aviso de aceite simples.
- [ ] Suporte mostra WhatsApp, e-mail, FAQ e dados MNSOFT.
- [ ] Assinatura explica diferença entre Free e Pro.

## Admin
- [ ] Dashboard admin usa cards e badges.
- [ ] Banco mostra conexão, schema esperado, tabelas e /health/db.

## Mobile e acessibilidade
- [ ] Menu mobile abre e fecha por teclado.
- [ ] Botões são grandes no celular.
- [ ] Foco visível aparece nos elementos interativos.
- [ ] Textos têm contraste adequado.
- [ ] Ícones têm texto ou aria-label quando necessário.

## Checklist visual desta etapa

- Validar que `/img/branding/mnsoft-logo.png` ausente exibe fallback textual MNSOFT.
- Validar landing, Como Funciona, Onboarding, Dashboard, Histórico e Suporte em mobile.
- Validar que os SVGs em `wwwroot/img/illustrations` são texto puro e não contêm base64.
