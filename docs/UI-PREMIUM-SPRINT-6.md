# Inventário visual — Sprint 6

## Critério

O inventário cobre somente as Razor Pages do ASP.NET atual. **Premium** indica que a tela já usa a linguagem `of-*`, ações reais, estados vazios e adaptação mobile; **Aceitável** indica fluxo funcional e coerente ainda sujeito a validação visual final; **Precisa refinamento** identifica dívida real, sem encobrir o recurso; **Ocultar do menu até finalizar** é reservado a uma rota sem fluxo implementado. A classificação foi feita por inspeção estática e pelos verificadores desta sprint; a homologação visual com dados representativos continua necessária em ambiente com .NET e banco configurados.

## Telas revisadas

| Área | Tela | Estado | Evidência / decisão |
|---|---|---|---|
| Pública | Home | Premium | Hero, navegação e CTAs reais |
| Pública | Preços | Premium | Planos reais e acesso ao cadastro |
| Pública | Ajuda pública | Aceitável | Conteúdo e navegação existentes |
| Pública | Suporte público | Premium | Formulário real e microcopy humana |
| Acesso | Login | Premium | Card editorial, recuperação, senha visível e loading seguro |
| Acesso | Cadastro | Premium | Escolha de plano/conta, consentimentos e validação real |
| Ativação | Onboarding | Premium | Nove marcos derivados dos dados da conta e CTAs reais |
| Principal | Dashboard | Premium | Indicadores consultados por conta e próximas ações |
| Comercial | Clientes | Premium | Busca, ações e estado vazio |
| Comercial | Cliente 360 | Premium | Central de relacionamento e histórico real |
| Comercial | Serviços | Premium | Catálogo, filtros e ações reais |
| Comercial | Orçamentos | Premium | Resumo, filtros e criação real |
| Comercial | Detalhe do orçamento | Premium | Documento, histórico e ações por estado |
| Comercial | Proposta pública | Premium | Documento responsivo e decisão do cliente |
| Comercial | Pipeline comercial | Premium | Colunas de leitura rápida e negócios reais |
| Comercial | Ações comerciais | Aceitável | Automação e filtros funcionais |
| Comercial | Templates de mensagem | Premium | Edição e preview; WhatsApp sem envio automático |
| Operação | OS | Premium | Status, filtros, ações e cards mobile |
| Operação | Detalhe da OS | Premium | Timeline e checklist reais |
| Operação | Agenda | Premium | Visões e compromissos reais |
| Financeiro | Contas a receber | Premium | Valores, vencimentos e filtros |
| Financeiro | Pagamentos | Premium | Registro real e estados financeiros |
| Financeiro | Recibos | Premium | Emissão real e aviso fiscal correto |
| Financeiro | Contratos | Premium | Vigência, recorrência e status |
| Financeiro | Fluxo de caixa | Premium | Agregações reais por período |
| Inteligência | Relatórios | Aceitável | Dados reais; gráficos ainda pedem homologação visual |
| Inteligência | Alertas | Premium | Priorização e destinos reais |
| Conta | Configurações | Premium | Seções claras, permissões e validação |
| Conta | Meu Plano | Premium | Uso e limites reais |
| Admin | Admin Dashboard | Premium | Resumo operacional SaaS |
| Admin | Contas Admin | Premium | Filtros, status e detalhe protegido |
| Admin | Usuários | Premium | Convites, papéis e ações protegidas |
| Admin | Planos | Premium | Catálogo administrável |
| Admin | Assinaturas | Premium | Situação e operação reais |
| Admin | Auditoria | Aceitável | Filtros e tabela responsiva |
| Admin | Logs | Aceitável | Consulta protegida; revisar volume em produção |
| Admin | Diagnóstico | Premium | Dados sanitizados e checks operacionais |
| Admin | EmailOutbox | Premium | Fila real, status e reprocessamento |
| Admin | Suporte | Premium | Chamados e ações reais |

Nenhuma tela revisada foi classificada como **Ruim**, **Quebrada** ou **Ocultar do menu até finalizar** na inspeção estática. Isso não equivale a afirmar que a execução foi homologada: nesta entrega, o contêiner não disponibiliza o SDK .NET, por isso build, autenticação com banco e captura no navegador devem ser repetidos no ambiente ASP.NET preparado.

## Consolidação aplicada

- O vocabulário semântico inclui cabeçalhos, cards de dados/métricas/formulários, action e command bars, controles, modal e lista mobile.
- Os checks automatizados cobrem fundamentos de acessibilidade, viewport, tabelas responsivas e microcopy proibida, além dos checks de navegação, segurança JavaScript e recursos abertos já existentes.
- A próxima rodada visual deve registrar screenshots nos oito viewports definidos pela sprint, com contas comum e administrativa e dados reais controlados.
