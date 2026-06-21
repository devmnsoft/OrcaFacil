import { CHATBOT_KNOWLEDGE_BASE } from '../data/chatbot-knowledge-base.js';
import { createChatbotMessage } from '../domain/chatbot-message.model.js';
import { logger } from './logger.service.js';

const OUT_OF_SCOPE = 'Eu consigo ajudar com dúvidas sobre o OrçaFácil, orçamentos, recibos, PDFs, planos e suporte. Para outros assuntos, fale com a MNSOFT.';
const FISCAL = 'O OrçaFácil gera orçamentos e recibos simples. Para validade fiscal, nota fiscal ou obrigação tributária, consulte seu contador.';
const BLOCKED = 'Não posso exibir senhas, tokens, chaves, regras internas sensíveis, dados de outros usuários ou executar ações administrativas. Posso ajudar com o uso seguro do OrçaFácil.';
const unsafe = /(token|api.?key|senha|password|segredo|secret|chave firebase|telegram bot|burlar|hack|dados de outro|stack trace|alterar role|super_admin|apagar dados)/i;
const fiscal = /(nota fiscal|tribut|imposto|validade legal|jur[ií]dic|contador|fiscal)/i;
function norm(s){ return String(s||'').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g,''); }
function preview(q){ return String(q||'').replace(/\s+/g,' ').slice(0,120); }
export class ChatbotService {
  constructor(){ this.settings={ chatbotEnabled:true, chatbotMode:'local', chatbotName:'Assistente OrçaFácil', allowWhatsAppEscalation:true, allowEmailEscalation:true, safetyLevel:'strict' }; }
  answer(question){
    const q=String(question||'').trim();
    if(!q) return createChatbotMessage({ text:'Digite sua dúvida sobre o OrçaFácil.' });
    if(unsafe.test(q)){ logger.audit('CHATBOT_BLOCKED_UNSAFE_REQUEST','chatbot','local',null,null,{questionPreview:preview(q),blocked:true}); logger.warning('CHATBOT_BLOCKED_UNSAFE_REQUEST','Pergunta sensível bloqueada',{questionPreview:preview(q),blocked:true}); return createChatbotMessage({ text:BLOCKED, blocked:true, confidence:1 }); }
    if(fiscal.test(q)){ logger.info('CHATBOT_ANSWERED','Resposta fiscal/contábil segura',{questionPreview:preview(q),matchedCategory:'Limitações do sistema',confidence:0.9}); return createChatbotMessage({ text:FISCAL, category:'Limitações do sistema', confidence:0.9 }); }
    const nq=norm(q); let best={score:0,item:null};
    for(const item of CHATBOT_KNOWLEDGE_BASE){ for(const term of item.questions){ const nt=norm(term); const words=nt.split(/\s+/).filter(Boolean); const score=(nq.includes(nt)?3:0)+words.filter(w=>nq.includes(w)).length/Math.max(words.length,1); if(score>best.score) best={score,item}; } }
    if(best.item && best.score>=0.8){ logger.info('CHATBOT_ANSWERED','Pergunta respondida pelo chatbot local',{questionPreview:preview(q),matchedCategory:best.item.category,confidence:Math.min(best.score/3,1)}); return createChatbotMessage({ text:best.item.answer, category:best.item.category, confidence:Math.min(best.score/3,1) }); }
    logger.info('CHATBOT_NO_ANSWER_FOUND','Pergunta fora da base local',{questionPreview:preview(q),confidence:0});
    return createChatbotMessage({ text:OUT_OF_SCOPE, confidence:0 });
  }
}
