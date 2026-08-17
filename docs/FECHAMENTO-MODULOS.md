# Matriz de fechamento dos módulos

Atualizada em 17/08/2026. A classificação abaixo considera as Razor Pages existentes, os contratos de navegação e os checks estáticos da release. **“Form salva” significa que a tela possui handler persistente; não substitui a validação integrada contra uma instância PostgreSQL.** JS/Razor/menu foram validados pelos scripts do repositório; a coluna mobile registra a existência do tratamento responsivo, não uma homologação visual em dispositivo físico.

| Módulo | Status real | Tela | Rota/menu | Form salva | JS/Razor | Botão fake | Design/mobile | Ação recomendada |
|---|---|---:|---|---|---|---:|---|---|
| Home pública | Completo | Sim | OK | Lead | OK | Não | Aceitável/Sim | Homologar conversão com dados reais |
| Preços | Completo | Sim | OK | N/A | OK | Não | Aceitável/Sim | Homologar catálogo publicado |
| Suporte público | Completo | Sim | OK | Busca real | OK | Não | Aceitável/Sim | Revisar conteúdo periodicamente |
| Login | Completo | Sim | OK | Autentica | OK | Não | Premium/Sim | Homologar cookie e PostgreSQL no ambiente alvo |
| Cadastro | Completo | Sim | OK | Persiste | OK | Não | Premium/Sim | Homologar e-mail e plano no ambiente alvo |
| Onboarding | Completo | Sim | OK | Persiste | OK | Não | Premium/Sim | Acompanhar métricas de ativação |
| Dashboard | Completo | Sim | OK | N/A | OK | Não | Premium/Sim | Homologar métricas com volume real |
| Clientes | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar CRUD e busca |
| Cliente 360 | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar histórico e contatos |
| Serviços | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar custo por permissão |
| Pacotes | Funciona, mas precisa UI | Sim | OK | Sim | OK | Não | Básico/Sim | Refinar comparação e composição |
| Templates | Completo | Sim | OK | N/A | OK | Não | Aceitável/Sim | Homologar aplicação em documento |
| Novo orçamento | Completo | Sim | OK | Sim | OK | Não | Premium/Sim | Homologar itens e descontos |
| Detalhe do orçamento | Completo | Sim | OK | Sim | OK | Não | Premium/Sim | Homologar revisões e follow-up |
| Proposta pública | Completo | Sim | Rota por token | Decisão | OK | Não | Premium/Sim | Homologar token, impressão e decisões |
| Rotina Comercial | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar follow-ups vencidos |
| Templates de Mensagem | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar variáveis permitidas |
| Contratos | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar recorrência e vencimentos |
| OS | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar idempotência da geração |
| Agenda | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar fusos e sobreposição |
| Pagamentos | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar reversão e origem |
| Recibos | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar impressão e origem |
| Relatórios | Completo | Sim | OK | Filtros | OK | Não | Aceitável/Sim | Homologar totais com massa real |
| Alertas | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar regras e leitura |
| Leads | Completo | Sim | Admin OK | Sim | OK | Não | Aceitável/Sim | Homologar captação pública |
| Importação | Funciona, mas falta integração | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar arquivos limite e rollback |
| Arquivos | Funciona, mas precisa UI | Sim | OK | Sim | OK | Não | Básico/Sim | Refinar biblioteca e estados de erro |
| Configurações | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar permissões por seção |
| Meu Plano | Completo | Sim | OK | Sim | OK | Não | Aceitável/Sim | Homologar provedor de cobrança |
| Admin | Funciona, mas falta integração | Sim | Protegido | Parcial | OK | Não | Aceitável/Sim | Manter fora de operação sem integrações configuradas |
| Diagnóstico | Completo | Sim | Protegido | N/A | OK | Não | Aceitável/Sim | Restringir a operadores autorizados |
| Logs | Completo | Sim | Admin | Filtros | OK | Não | Aceitável/Sim | Definir retenção em produção |
| Auditoria | Completo | Sim | Protegido | Filtros | OK | Não | Aceitável/Sim | Homologar retenção e exportação |

## Fechamentos realizados nesta sprint

- O checklist de ativação continua sendo derivado de registros reais da conta e agora expõe a data real da primeira conclusão de cada etapa.
- A etapa de compartilhamento usa a linguagem do fluxo vendável (“Gerar link público”), mantendo CTA para uma rota existente.
- Os oito checks de release permanecem como gates: schema, JavaScript, navegação pública/autenticada, Razor, pendências abertas e consistência visual.

## Pendências que exigem ambiente integrado

1. Executar os três builds .NET com o SDK definido pelo projeto.
2. Aplicar os patches apenas em cópia/instância PostgreSQL preservada e executar o diagnóstico de schema.
3. Homologar login real, criação de conta, fluxo completo e perfis Admin com credenciais próprias do ambiente.
4. Fazer inspeção visual e de console nas larguras 320, 360, 390, 430, 768, 1024, 1366 e 1440 px.
5. Validar integrações externas (e-mail, cobrança e armazenamento) somente quando configuradas; nenhuma delas deve ser simulada.
