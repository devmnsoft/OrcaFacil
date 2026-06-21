import { db } from './firebase.service.js';
import { collection, doc, getDoc, serverTimestamp, setDoc } from 'https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js';
import { uid } from '../utils.js';

const localKey = 'orcafacil:demo:telegramQueue';
const defaults = { telegramEnabled:true, telegramChatId:'7535235489', notifyOnUserRegister:true, notifyOnDocumentCreated:true, notifyOnPdfGenerated:false, notifyOnQuoteApproved:true, notifyOnQuoteRejected:true, notifyOnCriticalError:true, notifyOnLogin:false, notifyOnBackupExport:false };

function localPush(message){ const list=JSON.parse(localStorage.getItem(localKey)||'[]'); list.unshift({...message, createdAt:new Date().toISOString()}); localStorage.setItem(localKey, JSON.stringify(list.slice(0,200))); }
function settingFlag(type){ return { USER_REGISTERED:'notifyOnUserRegister', DOCUMENT_CREATED:'notifyOnDocumentCreated', PDF_GENERATED:'notifyOnPdfGenerated', QUOTE_APPROVED:'notifyOnQuoteApproved', QUOTE_REJECTED:'notifyOnQuoteRejected', CRITICAL_ERROR:'notifyOnCriticalError', FIRESTORE_PERMISSION_DENIED:'notifyOnCriticalError', PDF_GENERATION_FAILED:'notifyOnCriticalError', USER_LOGIN:'notifyOnLogin', BACKUP_EXPORTED_JSON:'notifyOnBackupExport', BACKUP_EXPORTED_CSV:'notifyOnBackupExport' }[type]; }

export async function getTelegramSettings(){
  try { const snap=await getDoc(doc(db,'adminSettings','global')); return {...defaults, ...(snap.exists()?snap.data():{})}; } catch { return defaults; }
}

export async function queueTelegramNotification(type, title, message, payload={}, severity='info'){
  const id = uid(); const item={ id, type, title, message, severity, payload, status:'pending', createdAt:serverTimestamp(), sentAt:null, error:'' };
  try {
    if (payload?.demo) { localPush({...item, createdAt:new Date().toISOString()}); return id; }
    const settings = await getTelegramSettings(); const flag=settingFlag(type);
    if (!settings.telegramEnabled || (flag && settings[flag] === false)) return null;
    await setDoc(doc(collection(db,'telegramQueue'), id), item);
    return id;
  } catch (err) { console.warn?.('Telegram queue failed', err); return null; }
}


export const TelegramNotificationService = {
  queueTelegramMessage: (type, title, message, severity='info', payload={}) => queueTelegramNotification(type, title, message, payload, severity),
  notifyUserRegistered: user => queueTelegramNotification('USER_REGISTERED', '🆕 Novo usuário no OrçaFácil', `Nome: ${user?.displayName || user?.name || '-'}
E-mail: ${user?.email || '-'}
Plano: ${user?.plan || 'Free'}
Data: ${new Date().toLocaleString('pt-BR')}`, { uid:user?.uid, email:user?.email }, 'success'),
  notifyDocumentCreated: (document, user) => queueTelegramNotification('DOCUMENT_CREATED', document?.type === 'recibo' ? '🧾 Novo recibo criado' : '📄 Novo orçamento criado', `Usuário: ${user?.email || '-'}
Cliente: ${document?.clientName || '-'}
Documento: ${document?.number || '-'}
Valor: ${new Intl.NumberFormat('pt-BR',{style:'currency',currency:'BRL'}).format(document?.total || 0)}
Status: ${document?.status || 'Rascunho'}`, { documentId:document?.id, documentNumber:document?.number, uid:user?.uid }, 'info'),
  notifyPdfGenerated: (document, user) => queueTelegramNotification('PDF_GENERATED', '🧾 PDF gerado', `Usuário: ${user?.email || '-'}
Documento: ${document?.number || '-'}
Cliente: ${document?.clientName || '-'}`, { documentId:document?.id, documentNumber:document?.number, uid:user?.uid }, 'info'),
  notifyQuoteApproved: (document, user) => queueTelegramNotification('QUOTE_APPROVED', '✅ Orçamento aprovado', `Usuário: ${user?.email || '-'}
Cliente: ${document?.clientName || '-'}
Documento: ${document?.number || '-'}
Mensagem: ${document?.clientDecisionNote || '-'}`, { documentId:document?.id, documentNumber:document?.number, uid:user?.uid }, 'success'),
  notifyQuoteRejected: (document, user) => queueTelegramNotification('QUOTE_REJECTED', '❌ Orçamento recusado', `Usuário: ${user?.email || '-'}
Cliente: ${document?.clientName || '-'}
Documento: ${document?.number || '-'}
Mensagem: ${document?.clientDecisionNote || '-'}`, { documentId:document?.id, documentNumber:document?.number, uid:user?.uid }, 'warning'),
  notifyCriticalError: (error, context={}) => queueTelegramNotification('CRITICAL_ERROR', '🚨 OrçaFácil - Erro crítico', `Usuário: ${context.userEmail || '-'}
Erro: ${error?.message || error}
Contexto: ${context.type || '-'}
Página: ${location.pathname}
Data: ${new Date().toLocaleString('pt-BR')}`, { context }, 'critical')
};
