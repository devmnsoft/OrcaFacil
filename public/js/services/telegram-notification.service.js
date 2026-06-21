import { db } from './firebase.service.js';
import { collection, doc, getDoc, serverTimestamp, setDoc } from 'https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js';
import { uid } from '../utils.js';

const localKey = 'orcafacil:demo:telegramQueue';
const defaults = { telegramEnabled:false, notifyOnUserRegister:true, notifyOnDocumentCreated:true, notifyOnPdfGenerated:false, notifyOnQuoteApproved:true, notifyOnCriticalError:true, notifyOnLogin:false };

function localPush(message){ const list=JSON.parse(localStorage.getItem(localKey)||'[]'); list.unshift({...message, createdAt:new Date().toISOString()}); localStorage.setItem(localKey, JSON.stringify(list.slice(0,200))); }
function settingFlag(type){ return { USER_REGISTERED:'notifyOnUserRegister', DOCUMENT_CREATED:'notifyOnDocumentCreated', PDF_GENERATED:'notifyOnPdfGenerated', QUOTE_APPROVED:'notifyOnQuoteApproved', QUOTE_REJECTED:'notifyOnQuoteApproved', CRITICAL_ERROR:'notifyOnCriticalError', FIRESTORE_PERMISSION_DENIED:'notifyOnCriticalError', PDF_GENERATION_FAILED:'notifyOnCriticalError', USER_LOGIN:'notifyOnLogin', BACKUP_EXPORTED_JSON:'notifyOnExport', BACKUP_EXPORTED_CSV:'notifyOnExport' }[type]; }

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
