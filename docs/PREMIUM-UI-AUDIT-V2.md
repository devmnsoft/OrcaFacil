# Auditoria de experiência premium V2

Esta auditoria orienta a evolução visual posterior à estabilização operacional. Em todas as telas: foco visível, contraste AA, zoom a 200%, regiões de `aria-live`, mensagens associadas aos campos, navegação por teclado e respeito a `prefers-reduced-motion` são requisitos de aceite.

| Tela | Objetivo e ações | Hierarquia / problema atual | Proposta e mobile | Estados vazio, erro e carregamento |
|---|---|---|---|---|
| Landing | explicar valor; criar conta / ver exemplo | benefício precisa anteceder recursos | hero com workspace real, prova e CTA; fluxo linear sem overflow | skeleton do mockup; fallback textual |
| Preços | escolher plano; comparar / tirar dúvida | comparação deve evitar jargão | tabela responsiva com recursos e custo total | plano indisponível explicado |
| Suporte | resolver dúvida; buscar / contatar | conteúdo disperso | busca, categorias e contato contextual | nenhum resultado e falha de contato |
| Login | entrar; recuperar / cadastrar | reduzir distrações | formulário curto, autocomplete e teclado correto | credenciais genéricas; progresso no botão |
| Cadastro | criar espaço; entrar / consultar termos | formulário longo | etapas PF/PJ e resumo; uma coluna no celular | validação inline e preservação segura |
| Recuperação | recuperar acesso; voltar ao login | não revelar existência do e-mail | confirmação genérica e instrução clara | token inválido/expirado acionável |
| Onboarding | chegar ao primeiro PDF; continuar depois | progresso não pode ser fictício | etapas derivadas do banco e próxima ação | retomada e falha recuperável |
| Dashboard | decidir próximo trabalho; novo orçamento / cadastros | evitar grade de cards iguais | saudação, próxima ação, indicadores reais e recentes | ilustração de primeiro sucesso; skeleton |
| Clientes | localizar/cadastrar; filtrar / editar | densidade e filtros | tabela no desktop, lista no mobile | vazio específico, limite e retry |
| Serviços | reutilizar catálogo; cadastrar / filtrar | unidade/preço precisam leitura rápida | busca e filtros persistentes | vazio específico, limite e retry |
| Documentos | acompanhar jornada; criar / filtrar | status e total são prioritários | linha temporal e ações contextuais | primeiro orçamento e erro de paginação |
| Modelos | escolher aparência; usar / visualizar | miniaturas precisam ser fiéis | galeria derivada do render real | bloqueio de plano explicado |
| Perfil | manter emitente; salvar / cancelar | agrupar identidade e contato | seções curtas e ação fixa segura | conflito, validação e salvamento |
| Plano | entender uso; evoluir / faturamento | uso real antes do upsell | medidores acessíveis e comparação | cobrança indisponível sem perda de dados |
| Notificações | identificar pendências; abrir / marcar lida | prioridade e data devem dominar | lista agrupada com ações por item | caixa vazia positiva e retry |
| Administração | diagnosticar operação; corrigir / auditar | separar saúde de gestão | alertas sanitizados, inclusive seed seguro | indisponibilidade parcial explícita |
| PDF / prévia | conferir e entregar; gerar / editar | prévia e PDF devem compartilhar modelo | A4 com zoom, toolbar móvel e sem consumir cota | renderização, bloqueio de plano e retry |

## Princípios de composição

1. Uma ação primária inequívoca por contexto; ações destrutivas nunca competem visualmente.
2. Dados reais isolados por `AccountId`; nenhum número demonstrativo no espaço autenticado.
3. Estados de carregamento preservam a geometria para evitar deslocamento de layout.
4. Ícones complementam rótulos e nunca são a única indicação de função.
5. Mobile prioriza criação, documentos e clientes, respeitando safe areas e retorno de foco.
