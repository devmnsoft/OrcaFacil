# Inventário de textos da experiência

Vocabulário público aprovado: **Grátis**, **Profissional**, **Negócio**, **Conta**, **Usuário**, **Cliente cadastrado**, **Meu plano**, **Próximo vencimento**, **Recursos** e **Benefícios**.

| Área | Direção de voz | Estado |
|---|---|---|
| Landing | benefício direto e começo gratuito | Parcial |
| Cadastro/login | instruções curtas, segurança e recuperação clara | Parcial |
| Onboarding | cinco passos reais, sem bloquear o sistema | Parcial |
| Dashboard | “Olá, {primeiroNome}. O que vamos preparar hoje?” | Pendente |
| Clientes | “Guarde os dados para usar novamente em orçamentos e recibos.” | Pendente |
| Serviços | “Cadastre o que você oferece e reutilize valores quando precisar.” | Pendente |
| Orçamentos/recibos/PDF | ação concreta, estados vazios e erros amigáveis | Parcial |
| Planos | “Escolha os recursos que combinam com a sua rotina.” | Pendente |
| Notificações/suporte/Admin | situação, impacto e próxima ação; justificativa em ações críticas | Parcial |
| Erros | correlation id sem stack trace ou dados sensíveis | Parcial |

## Termos proibidos na interface

Não expor ao usuário: `SaaS`, `tenant`, `workspace`, `entitlement`, `billing`, `subscription`, `gateway`, `payload`, `upgrade`, `downgrade`, `MRR` ou `churn`. Nomes técnicos podem permanecer no código, logs protegidos e documentação de engenharia.

## Critério de conclusão

A revisão somente estará concluída após varredura automatizada de `.cshtml`/JavaScript, inspeção de todas as rotas e validação dos estados vazio, erro, sucesso, tolerância, suspensão e liberação temporária.
