const admin = require('firebase-admin');
const { onDocumentCreated } = require('firebase-functions/v2/firestore');
const { logger } = require('firebase-functions');

admin.initializeApp();
const db = admin.firestore();

function env(name) {
  return process.env[name] || process.env[`FIREBASE_CONFIG_${name}`] || '';
}

async function getSettings() {
  const snap = await db.doc('adminSettings/global').get();
  return snap.exists ? snap.data() : {};
}

async function notifyTelegram(message) {
  const token = env('TELEGRAM_BOT_TOKEN');
  if (!token) throw new Error('TELEGRAM_BOT_TOKEN não configurado.');
  const settings = await getSettings();
  const chatId = message.payload?.telegramChatId || settings.telegramChatId || env('TELEGRAM_DEFAULT_CHAT_ID') || '7535235489';
  if (!chatId) throw new Error('Chat ID do Telegram não configurado.');
  const text = `${message.title || 'OrçaFácil'}\n\n${message.message || ''}`.slice(0, 3900);
  const res = await fetch(`https://api.telegram.org/bot${token}/sendMessage`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ chat_id: chatId, text, disable_web_page_preview: true })
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok || data.ok === false) throw new Error(data.description || `Telegram HTTP ${res.status}`);
  return data;
}

exports.sendTelegramNotification = onDocumentCreated('telegramQueue/{messageId}', async (event) => {
  const ref = event.data.ref;
  const message = event.data.data();
  if (message.status && message.status !== 'pending') return;
  try {
    await notifyTelegram(message);
    await ref.set({ status: 'sent', sentAt: admin.firestore.FieldValue.serverTimestamp(), error: '' }, { merge: true });
    await db.collection('systemEvents').add({ type: 'TELEGRAM_NOTIFICATION_SENT', severity: 'success', title: 'Telegram enviado', message: message.title || '', metadata: { queueId: ref.id }, createdAt: admin.firestore.FieldValue.serverTimestamp(), source: 'cloud_function' });
  } catch (error) {
    logger.error('Telegram notification failed', error);
    await ref.set({ status: 'failed', error: error.message || String(error) }, { merge: true });
    await db.collection('systemErrors').add({ message: error.message || String(error), stack: error.stack || '', code: 'telegram/send-failed', severity: 'error', context: { queueId: ref.id, type: message.type }, createdAt: admin.firestore.FieldValue.serverTimestamp(), resolved: false, resolvedAt: null, resolvedBy: null, adminNote: '', source: 'cloud_function' });
  }
});
