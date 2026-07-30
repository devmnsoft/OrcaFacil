# Auditoria visual — Release Operacional 7.2

## Método e evidências

A auditoria compara a base posterior ao PR #75 com a Release 7.2 em três viewports (375×812, 768×1024 e 1440×900). As capturas são geradas somente em `test-results` pelo Playwright e não são versionadas. Os arquivos `*-after.png` constituem os artefatos posteriores; a execução inicial do mesmo roteiro sobre o commit-base constitui o conjunto anterior.

## Diagnóstico transversal

| Dimensão | Antes | Solução implementada |
|---|---|---|
| Visual | Sidebar escura contínua, superfícies brancas equivalentes e baixa diferenciação. | Shell com identidade, conta, ação dominante, grupos, hierarquia tonal e composições visuais. |
| Navegação | Rótulos orientados pela estrutura do sistema e breadcrumb textual. | Grupos por tarefa, breadcrumb semântico, navegação móvel e command palette. |
| Conteúdo | Explicações extensas e repetidas. | Texto progressivo: eyebrow, título, benefício, ação e ajuda sob demanda. |
| Hierarquia | Cards com o mesmo peso. | Hero visual, prioridade em lista, ação rápida contrastante, métricas leves e continuidade. |
| Acessibilidade | Diálogos sem contenção completa de foco e poucos nomes visíveis. | Skip link, landmarks, nomes acessíveis, foco devolvido, Escape, reduced motion e alvos de 40–44 px. |

## Auditoria por tela do cliente

| Tela | Problema visual / navegação / conteúdo / hierarquia | Ação principal | Ação secundária | Removido | Ajuda / demonstração | Solução |
|---|---|---|---|---|---|---|
| Dashboard | Hero genérico, métricas sem contexto, cards iguais. | Novo orçamento | Continuar trabalho | Blocos redundantes | Guia e centro de comando | Composição visual, próximo passo, prioridades e sinais comerciais. |
| Clientes | Lista pouco escaneável e cadastro desconectado. | Novo cliente | Como usar | Texto repetido | Gestão visual | Shell, cabeçalho consistente e estado vazio específico. |
| Novo cliente | Campos em massa. | Salvar e criar orçamento | Salvar | Divisões arbitrárias | Passos de cadastro | Hierarquia do shell e ajuda lateral. |
| Serviços | Catálogo sem contexto de uso. | Novo serviço | Filtrar | Cards equivalentes | Catálogo visual | Navegação organizada e ilustração própria. |
| Novo serviço | Exemplos ausentes. | Salvar serviço | Cancelar | Ruído técnico | Exemplos comerciais | Contexto e ajuda sob demanda. |
| Orçamentos | Documentos sem distinção de jornada. | Novo orçamento | Ver lista | Tabela Bootstrap | Pipeline demonstrável | Ação dominante e continuidade. |
| Novo orçamento | Fluxo longo e pouco orientado. | Avançar | Salvar rascunho | Massa de campos | Builder visual | Command palette, mockup e estrutura preparada para etapas. |
| Recibos | Valor percebido baixo. | Novo recibo | Ver documentos | Explicação repetida | Exemplo visual | Ação descrita na folha móvel. |
| Modelos | Nomes sem prévia convincente. | Usar modelo | Visualizar | Badge isolado | Modelos profissionais | Mockup visual específico no Discover. |
| Pipeline | Recurso abstrato. | Abrir oportunidade | Ver lista | Modal genérico | Demo interativa própria | Colunas e próxima ação representadas no SVG. |
| Aprovações | Funcionamento pouco evidente. | Compartilhar | Ver atividade | Texto genérico | Demo de aprovação | Tela pública e fluxo de três etapas. |
| Ordens | Ligação com proposta não evidente. | Criar ordem | Ver agenda | Placeholder | Fluxo visual | Ilustração específica de execução. |
| Agenda | Falta de contexto do compromisso. | Agendar visita | Ver ordens | Placeholder | Agenda visual | Demonstração própria por etapa. |
| Notificações | Contador sem prioridade. | Resolver | Marcar lida | Alertas equivalentes | Ajuda contextual | Contador somente quando necessário e acesso direto. |
| Meu plano | Benefícios pouco tangíveis. | Comparar planos | Regularizar | Pressão comercial | Demos por benefício | Badge semântico e acesso ao centro de recursos. |
| Ajuda | Conteúdo genérico. | Buscar resposta | Abrir tópico | Interrupção automática | Drawer contextual | Ajuda aberta sob demanda, com propósito e passos. |
| Discover | Nove cards textuais e um modal idêntico. | Testar demo | Comparar planos | Modal genérico | Nove mockups | Cards visuais, fluxo, benefício, plano e modal próprio. |
| Perfil | Impacto no documento pouco claro. | Completar dados | Voltar | Campos sem contexto | Prévia do emitente | Priorizado no próximo passo e nas pendências. |

## Auditoria SuperAdministrador

| Tela / área | Problema | Ação principal | Ajuda necessária | Solução implementada |
|---|---|---|---|---|
| Dashboard | Parede de indicadores iguais. | Investigar atenção | Impacto operacional | Quatro sinais, área de atenção, receita e saúde. |
| Contas / Conta 360 | Navegação técnica e pouco contexto. | Abrir conta | Impacto e preservação | Shell operacional e mockup Conta 360. |
| Usuários | Hierarquia fraca. | Abrir usuário | Permissões | Grupo operacional e tabela leve. |
| Planos / versões / recursos | Catálogo pouco visual. | Abrir plano | Impacto da versão | Mockup de gestão e acesso agrupado. |
| Assinaturas / pagamentos / inadimplência | Situação financeira fragmentada. | Revisar ocorrência | Carência e preservação | Indicadores e seção financeira priorizada. |
| Liberações / e-mails / fila | Estados técnicos sem impacto. | Investigar | Efeito na conta | Grupo operacional e saúde semântica. |
| Auditoria / banco / saúde | Diagnóstico sem hierarquia. | Ver readiness | Correlação | Pill de saúde, breadcrumb e painel visual. |
| Suporte | Contexto da conta ausente. | Abrir solicitação | Conta 360 | Navegação orientada à operação. |

## Responsividade

- **Desktop:** sidebar de 272 px, conteúdo máximo de 1360 px e topbar de 72 px.
- **Tablet:** sidebar vira painel oculto e as grades passam a duas ou uma coluna.
- **Celular:** cinco ações persistentes, botão Novo elevado, folha de ações e conteúdo sem tabela obrigatória.
- **Zoom/reduced motion:** unidades flexíveis, conteúdo rolável e transições desativadas pela preferência do sistema.

## Pendências verificáveis

A busca visual e a command palette estão implementadas no shell; a consulta remota agrupada depende do endpoint autenticado de busca por conta. Pipeline, aprovações, ordens e agenda permanecem demonstrações enquanto não houver rotas de domínio publicadas. Isso é apresentado honestamente e não cria botões que aleguem alterar dados reais.
