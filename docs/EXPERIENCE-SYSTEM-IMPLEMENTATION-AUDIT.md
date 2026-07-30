# OrçaFácil Experience System — auditoria de implementação

## Princípios operacionais

- Planos controlam acesso e capacidade; nunca apagam dados.
- A interface explica **plano escolhido** e **plano disponível agora**, sem expor códigos técnicos.
- Toda mutação continua protegida no servidor; bloqueios visuais não concedem acesso.
- Cliente e SuperAdministrador têm arquitetura de informação, linguagem e shell próprios.

## Área do cliente

| Tela | Objetivo e ação principal | Diagnóstico anterior | Estados e plano | Melhoria implementada |
|---|---|---|---|---|
| Dashboard | Decidir o próximo passo; criar orçamento | Saudação genérica, sete métricas equivalentes e menu plano | Vazio sem sequência; plano apenas como texto | Saudação pessoal, próxima melhor ação, quatro ações, atenção, métricas hierárquicas e uso do plano |
| Onboarding | Preparar a conta; continuar guia | Checklist isolado | Progresso pouco visível | Progresso contextual no início e retorno pelo menu de ajuda |
| Clientes | Organizar contatos; cadastrar cliente | Tabela desktop e vazio genérico | Limite não explicado | Entrada no menu Cadastros, ação contextual e mensagem de preservação |
| Serviços | Manter catálogo; cadastrar serviço | Página desconectada | Preço ausente pouco orientado | Ação rápida, contexto comercial e orientação no vazio |
| Orçamentos | Criar e acompanhar propostas | Misturado ao histórico | Rascunho sem explicação | Grupo Vendas, ação principal persistente e explicação contextual |
| Recibos | Registrar recebimentos; criar recibo | Confundido com documento fiscal | Limites pouco visíveis | Linguagem que esclarece que não substitui nota fiscal |
| Histórico | Consultar documentos; abrir item | Nome de módulo técnico | Erro/vazio genérico | Renomeado para Documentos e adaptado à navegação móvel |
| Modelos | Reutilizar conteúdo; conhecer modelos | Descoberta baixa | Premium parecia indisponibilidade | Oferta contextual e acesso à demonstração |
| Aprovações | Receber respostas; ver demonstração | Sem rota dedicada | Benefício pago invisível | Descoberta no centro de demonstrações sem botão falso |
| Pipeline | Priorizar propostas; ver demonstração | Sem rota funcional | Não aplicável | Não exibido como rota; demonstrado separadamente |
| Ordens | Organizar execução; conhecer recurso | Sem rota funcional | Não aplicável | Não exibido como rota; demonstrado separadamente |
| Agenda | Planejar atendimento; conhecer recurso | Sem rota funcional | Não aplicável | Não exibido como rota; demonstrado separadamente |
| Emitente | Completar identidade; salvar dados | Chamado de perfil em alguns pontos | Incompleto sem prioridade | Próxima melhor ação e nome consistente “Dados do emitente” |
| Usuários | Compartilhar rotina | Sem rota funcional | Limite de equipe não explicado | Benefício demonstrado sem criar navegação quebrada |
| Notificações | Agir sobre avisos; abrir notificação | Contagem consultada no layout | Vazio técnico | Contagem fornecida pelo factory do shell e hierarquia no topo |
| Meu plano | Compreender acesso; regularizar | “Assinatura”, foco em bloqueio | Escolhido/efetivo confundidos | Linguagem humana, uso, garantia de preservação e demonstrações |
| Cobrança | Atualizar dados; salvar perfil | Descoberta baixa | Erros sem contexto | Acesso pelo Meu plano e orientação de regularização |
| Ajuda | Resolver dúvida; abrir tópico | Link isolado | Sem ajuda contextual | Central de ajuda no shell, drawer e conteúdo por contexto |
| Perfil | Gerenciar conta; salvar | Emitente e usuário confundidos | Sem estado guiado | Conta e identidade separadas na arquitetura de informação |
| Discover | Conhecer benefícios; abrir demonstração | Inexistente | Dados de demo poderiam se misturar | Centro explícito, itens marcados “Demonstração” e nenhum dado real |

## SuperAdministrador

| Tela | Objetivo e ação principal | Diagnóstico anterior | Estados e risco | Melhoria implementada |
|---|---|---|---|---|
| Dashboard | Entender operação; tratar alerta | Cards técnicos sem sequência operacional | Saúde sem ação | Shell próprio, prioridade operacional, indicadores e atalhos |
| Contas | Localizar conta; abrir visão | Navegação dispersa | PII sem contexto | Grupo Clientes e busca apresentada com responsabilidade |
| Usuários | Investigar acesso; abrir usuário | Misturado ao cliente | PII | Menu operacional próprio e política de mascaramento |
| Planos | Governar catálogo; criar versão | Fluxo técnico | Publicação arriscada | Grupo Planos e receita; versão publicada permanece imutável |
| Versões | Preparar alteração; comparar | Baixa descoberta | Impacto oculto | Sequência rascunho → impacto → publicação documentada |
| Recursos | Configurar benefício; revisar | Códigos expostos | Falta de configuração | Nome humano “Benefícios” e alerta operacional |
| Assinaturas | Entender acesso; abrir conta | Plano escolhido confundido com efetivo | Pausa | Dois estados apresentados separadamente |
| Pagamentos | Conciliar; investigar pagamento | Fluxo isolado | Inadimplência | Agrupado com receita e sem simular estado do provedor |
| Inadimplência | Priorizar regularização; abrir conta | Sem hierarquia | Ação crítica | Indicador de atenção e acesso à conta |
| Liberações | Conceder prazo; registrar motivo | Risco de falsificar pagamento | Auditoria obrigatória | Explicação explícita de benefício temporário |
| E-mails | Diagnosticar envio; reprocessar | Estado técnico bruto | Fila parada | Grupo Operação e saúde resumida no shell |
| Fila | Tratar mensagens; abrir falha | Dead letters pouco visíveis | Reprocessamento | Contagem crítica no topo administrativo |
| Banco | Ver prontidão; executar diagnóstico | Informação dispersa | Migração pendente | Grupo Sistema e alerta acionável |
| Saúde | Avaliar dependências; abrir detalhe | Sem síntese | Indisponibilidade | Estado geral antes dos detalhes |
| Auditoria | Investigar ação; filtrar correlação | CorrelationId isolado | PII | Correlação disponível no shell e mascaramento preservado |
| Erros | Diagnosticar repetição; abrir ocorrência | Lista técnica | Exposição indevida | Navegação de operação e prioridade por recorrência |
| Suporte | Atender conta; abrir chamado | Sem indicador global | Urgência | Contagem pendente no shell administrativo |
| Configurações | Governar sistema; editar | Misturada à operação | Ação crítica | Grupo Sistema, autorização e confirmação no backend |

## Critérios transversais verificados

- **Mobile:** navegação inferior, action sheet, áreas de toque, safe area e conteúdo sem sobreposição.
- **Acessibilidade:** skip link, foco visível, Escape, retorno de foco, `aria-live`, headings e movimento reduzido.
- **Performance:** layouts não consultam banco; factories agregam o shell uma vez por request; assets locais substituem ícones remotos.
- **Segurança:** antiforgery, POST, autorização por conta, `SessionVersion`, PII mascarada e auditoria permanecem requisitos invariáveis.
- **Política de pausa:** PDFs, branding, modelos, links, membros, histórico e configurações permanecem armazenados; somente novos usos e benefícios incompatíveis ficam pausados.
