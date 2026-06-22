export const CHATBOT_KNOWLEDGE_BASE = [
  { category:'Introdução ao OrçaFácil', questions:['o que é orçafácil','para que serve','como funciona'], answer:'O OrçaFácil é um sistema simples para autônomos, MEIs e pequenos prestadores criarem orçamentos e recibos profissionais em PDF pelo navegador ou celular.' },
  { category:'Orçamentos', questions:['como criar orçamento','novo orçamento','como gerar orçamento'], answer:'Acesse Novo Documento, escolha Orçamento, informe cliente, itens, valores e validade. Depois clique em Salvar ou Gerar PDF.' },
  { category:'Recibos', questions:['como criar recibo','como gerar recibo','novo recibo'], answer:'Em Novo Documento, escolha Recibo, preencha cliente, descrição/itens e valores. O PDF do recibo inclui os dados e o valor total.' },
  { category:'PDF', questions:['como gerar pdf','baixar pdf','pdf não gera'], answer:'Preencha os campos obrigatórios e clique em Gerar PDF. Se falhar, confira cliente e itens. O erro técnico fica em Admin Geral > Bugs e Erros para super_admin.' },
  { category:'Perfil do emitente', questions:['dados do emitente','configurar empresa','usar logo','minha logo'], answer:'Abra Dados do emitente, informe nome/razão social, CPF/CNPJ, telefone, e-mail, cidade, Pix e opcionalmente sua logo. Salve antes de gerar o PDF.' },
  { category:'Histórico', questions:['histórico','editar documento','duplicar documento','excluir documento'], answer:'Em Meus documentos você pode abrir, editar, gerar PDF, duplicar, excluir e alterar status dos orçamentos e recibos salvos.' },
  { category:'Planos', questions:['plano free','plano pro','diferença free pro','marca dágua','remover marca','remover marca do pdf'], answer:'No plano Free o PDF pode sair com marca OrçaFácil. No Pro, o PDF fica sem marca e com aparência mais profissional. Para ativar, use Minha assinatura ou fale com a MNSOFT.' },
  { category:'Exportação', questions:['exportar backup','exportar csv','exportar json','backup'], answer:'No Histórico, use Exportar JSON para backup completo ou Exportar CSV para planilha com os principais dados dos documentos.' },
  { category:'Privacidade', questions:['privacidade','meus pdfs são salvos','lgpd','segurança'], answer:'Seus documentos ficam associados ao seu usuário no Firebase ou no localStorage no modo demonstração. Evite inserir dados desnecessários e exporte/remova dados quando precisar.' },
  { category:'WhatsApp MNSOFT', questions:['falar com mnsoft','whatsapp','suporte','atendimento','email'], answer:'Você pode falar com a MNSOFT pelo botão de WhatsApp do sistema ou enviar e-mail para comercial@mnsoft.com.br. Empresa responsável: MNSOFT, CNPJ 18.160.057/0001-13.' },
  { category:'Aprovação pública', questions:['como enviar orçamento para aprovação','link de aprovação','reenviar link','desativar link público'], answer:'No Histórico ou no documento, clique em Compartilhar / Aprovação. Gere o link seguro, copie, abra, envie por WhatsApp/e-mail, desative ou gere novamente quando necessário.' },
  { category:'Aceite do cliente', questions:['como o cliente aprova','cliente aprovar orçamento','como cliente recusa','recusar orçamento'], answer:'O cliente abre o link sem login, confere itens, total e condições. Para aprovar, informa nome e marca a declaração de aceite. Para recusar, informa nome e motivo.' },
  { category:'Evidência do aceite', questions:['aceite tem validade jurídica','validade do aceite','código de evidência','cliente visualizou'], answer:'O OrçaFácil registra um aceite eletrônico simples com data, nome informado, navegador e código de evidência. Isso ajuda no controle comercial, mas não substitui assinatura digital certificada ou orientação jurídica. Para contratos formais, consulte um profissional jurídico.' },
  { category:'Conversão em recibo', questions:['como converter orçamento em recibo','converter aprovado em recibo'], answer:'Quando o orçamento estiver aprovado, use Converter em recibo no Histórico. O sistema cria um recibo REC, copia cliente, itens e total, e vincula o recibo ao orçamento original.' },
  { category:'Publicação IIS/Firebase', questions:['publicar','firebase hosting','iis','servidor local'], answer:'O OrçaFácil funciona como aplicação estática no Firebase Hosting/IIS e também com servidor Node local na porta 8095 para testes.' },
  { category:'Limitações do sistema', questions:['nota fiscal','validade fiscal','contador','imposto'], answer:'O OrçaFácil gera orçamentos e recibos simples. Para validade fiscal, nota fiscal ou obrigação tributária, consulte seu contador.' }
];

export const billingKnowledge = [
  { q: 'Como assinar o Pro?', a: 'Abra Minha assinatura e escolha Assinar mensal ou Assinar anual. O checkout é feito pelo Mercado Pago com confirmação segura.' },
  { q: 'Como renovar?', a: 'Abra Minha assinatura e clique novamente no ciclo desejado. Se precisar de ajuda, fale com a MNSOFT pelo WhatsApp.' },
  { q: 'O que muda no Pro?', a: 'O Pro remove a marca do PDF, libera documentos e PDFs ilimitados, histórico completo, aprovação pública quando disponível e suporte prioritário.' },
  { q: 'Meu pagamento foi aprovado?', a: 'Consulte Minha assinatura. O assistente não consulta dados sensíveis de pagamento; a ativação depende da confirmação do Mercado Pago.' },
  { q: 'Quanto custa?', a: 'O Pro custa R$ 19,90 por mês ou R$ 199,00 por ano.' },
  { q: 'O sistema emite nota fiscal?', a: 'Não. O OrçaFácil gera orçamentos e recibos, mas não emite nota fiscal.' },
  { q: 'Posso cancelar?', a: 'Sim. Fale com a MNSOFT para suporte de cancelamento ou ajuste manual da assinatura.' }
];
