import { db } from './firebase.service.js';
import { collection, doc, serverTimestamp, setDoc } from 'https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js';
import { uid } from '../utils.js';
import { queueTelegramNotification } from './telegram-notification.service.js';
import { isPermissionDenied } from '../utils/firebase-errors.js';

const localKey = name => `orcafacil:demo:${name}`;
const bufferKey = name => `orcafacil:monitoring-buffer:${name}`;
let userContext = null;
let dev = location.hostname === 'localhost' || location.hostname === '127.0.0.1';
let permissionWarned = false;

function safeJson(value){ try { return JSON.parse(JSON.stringify(value ?? null)); } catch { return null; } }
function base(){ return { uid:userContext?.uid||'', userEmail:userContext?.email||'', userName:userContext?.displayName||userContext?.name||'', userAgent:navigator.userAgent, url:location.href }; }
function isDemo(){ return Boolean(userContext?.demo) || localStorage.getItem('orcafacil:demo-enabled') === '1'; }
function canWriteRemote(){ return Boolean(userContext?.uid) && !isDemo(); }
function localWrite(name, data){ const list=JSON.parse(localStorage.getItem(localKey(name))||'[]'); list.unshift({...data, createdAt:new Date().toISOString()}); localStorage.setItem(localKey(name), JSON.stringify(list.slice(0,500))); return data.id; }
function bufferWrite(name, data){ const list=JSON.parse(localStorage.getItem(bufferKey(name))||'[]'); list.unshift({...data, createdAt:new Date().toISOString(), bufferedAt:new Date().toISOString()}); localStorage.setItem(bufferKey(name), JSON.stringify(list.slice(0,200))); return data.id; }
function classify(error, context={}){ const msg=String(error?.message||error||'Erro não tratado'); const code=String(error?.code||context?.code||''); if (code.includes('permission-denied')||msg.match(/permission|permiss/i)) return 'critical'; if (msg.match(/pdf|jspdf/i)) return 'critical'; if (msg.match(/network|unavailable|offline/i)) return 'warning'; return context?.severity || 'error'; }
async function write(collectionName, data){
  try {
    if (isDemo()) return localWrite(collectionName, data);
    if (!canWriteRemote()) return bufferWrite(collectionName, data);
    await setDoc(doc(collection(db, collectionName), data.id), data);
    return data.id;
  } catch (error) {
    if (dev && !permissionWarned) {
      permissionWarned = true;
      const label = isPermissionDenied(error) ? 'permissão insuficiente' : (error?.code || error?.message || error);
      console.warn('[monitoring] Não foi possível gravar monitoramento remoto. O sistema continuará normalmente.', label);
    }
    return null;
  }
}
function canNotifyTelegram(severity, context={}){ return severity === 'critical' && canWriteRemote() && !isDemo() && context.telegram !== false; }

export const MonitoringService = {
  setUserContext(user){ userContext = user || null; },
  async trackEvent(type, payload={}){
    const id=uid(); const data={ id, type, severity:payload.severity||'info', title:payload.title||type, message:payload.message||'', ...base(), documentId:payload.documentId||'', documentNumber:payload.documentNumber||'', metadata:safeJson(payload.metadata||{}), createdAt:serverTimestamp(), source:'frontend' };
    await write('systemEvents', data);
    return id;
  },
  async trackError(error, context={}){
    const id=uid(); const severity=classify(error, context); const data={ id, message:String(error?.message||error||'Erro não tratado'), stack:String(error?.stack||''), code:String(error?.code||context?.code||''), severity, ...base(), context:safeJson(context), file:context.file||'', line:context.line||null, column:context.column||null, createdAt:serverTimestamp(), resolved:false, resolvedAt:null, resolvedBy:null, adminNote:'' };
    await write('systemErrors', data);
    if(canNotifyTelegram(severity, context)) {
      try { await queueTelegramNotification(context.type||'CRITICAL_ERROR','🚨 OrçaFácil - Erro crítico',`${data.message}\nUsuário: ${data.userEmail||'-'}\nPágina: ${location.pathname}`, { ...data, demo:isDemo() }, 'critical'); } catch { /* Telegram nunca deve quebrar o fluxo principal. */ }
    }
    return id;
  },
  async audit(action, entityType, entityId, before=null, after=null, metadata={}){
    const id=uid(); const data={ id, action, entityType, entityId:entityId||'', ...base(), before:safeJson(before), after:safeJson(after), metadata:safeJson(metadata), createdAt:serverTimestamp(), ipInfo:null };
    await write('auditLogs', data);
    return id;
  },
  installGlobalHandlers(toast){
    window.addEventListener('error', e=>{ this.trackError(e.error||e.message,{ file:e.filename, line:e.lineno, column:e.colno, type:'GLOBAL_ERROR' }).catch(()=>{}); if(toast) toast('Ocorreu um erro inesperado. Nossa equipe técnica foi notificada.','error'); });
    window.addEventListener('unhandledrejection', e=>{ this.trackError(e.reason||'Promise rejeitada',{ type:'UNHANDLED_REJECTION', telegram:false }).catch(()=>{}); if(toast) toast('Não foi possível concluir a operação. Tente novamente.','error'); });
  }
};
