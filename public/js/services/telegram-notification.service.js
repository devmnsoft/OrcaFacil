import { auth, db } from './firebase.service.js';
import { collection, doc, getDoc, serverTimestamp, setDoc } from 'https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js';
import { uid } from '../utils.js';
import { logger } from './logger.service.js';

const defaults = { telegramEnabled:false, telegramChatId:'', notifyOnUserRegister:true, notifyOnDocumentCreated:true, notifyOnPdfGenerated:false, notifyOnQuoteApproved:true, notifyOnQuoteRejected:true, notifyOnCriticalError:true, notifyOnLogin:false, notifyOnBackupExport:false };
const permissionWarned = new Set();
const isPermissionDenied = err => String(err?.code || '').includes('permission-denied') || /permission|permiss/i.test(String(err?.message || err || ''));

function isDemoMode(payload={}){ return Boolean(payload?.demo) || localStorage.getItem('orcafacil:demo-enabled') === '1' || Boolean(auth.currentUser?.demo); }
function settingFlag(type){ return { USER_REGISTERED:'notifyOnUserRegister', DOCUMENT_CREATED:'notifyOnDocumentCreated', PDF_GENERATED:'notifyOnPdfGenerated', QUOTE_APPROVED:'notifyOnQuoteApproved', QUOTE_REJECTED:'notifyOnQuoteRejected', CRITICAL_ERROR:'notifyOnCriticalError', FIRESTORE_PERMISSION_DENIED:'notifyOnCriticalError', PDF_GENERATION_FAILED:'notifyOnCriticalError', USER_LOGIN:'notifyOnLogin', BACKUP_EXPORTED_JSON:'notifyOnBackupExport', BACKUP_EXPORTED_CSV:'notifyOnBackupExport' }[type]; }
function safeMetadata(payload={}){ try { const copy=JSON.parse(JSON.stringify(payload || {})); delete copy.token; delete copy.access_token; delete copy.authorization; delete copy.password; delete copy.senha; return copy; } catch { return {}; } }
function logOnce(key, level, type, message, err=null, metadata={}){
  if(permissionWarned.has(key)) return;
  permissionWarned.add(key);
  if (['error','critical'].includes(level)) logger[level]?.(type, message, err, metadata);
  else logger[level]?.(type, message, {...metadata, errorMessage:err ? String(err.message || err) : ''});
}

export async function getTelegramSettings(){
  try { const snap=await getDoc(doc(db,'adminSettings','global')); return {...defaults, ...(snap.exists()?snap.data():{})}; } catch { return defaults; }
}

export async function queueTelegramNotification(type, title, message, payload={}, severity='info'){
  const user = auth.currentUser;
  const id = uid();
  const metadata = safeMetadata(payload);
  const item={ id, uid:user?.uid || '', type:String(type || ''), title:String(title || ''), message:String(message || ''), severity, metadata, status:'pending', createdAt:serverTimestamp() };
  try {
    if (isDemoMode(payload)) return null;
    if (!user) { logOnce(`auth:${type}`,'debug','TELEGRAM_QUEUE_SKIPPED','Telegram não enfileirado: usuário ainda não autenticado.',null,{type}); return null; }
    const settings = await getTelegramSettings(); const flag=settingFlag(type);
    if (!settings.telegramEnabled || (flag && settings[flag] === false)) return null;
    await setDoc(doc(collection(db,'telegramQueue'), id), item);
    return id;
  } catch (err) {
    if (isPermissionDenied(err)) {
      logOnce('permission-denied','warning','TELEGRAM_QUEUE_PERMISSION_DENIED','Telegram não enfileirado por permissão insuficiente. Verifique as Firestore Rules e mantenha a fila protegida.',err,{type});
      return null;
    }
    logOnce(`failed:${type}`,'warning','TELEGRAM_QUEUE_FAILED','Telegram não enfileirado. A aplicação seguirá funcionando.',err,{type});
    return null;
  }
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
