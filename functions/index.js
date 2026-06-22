const admin = require('firebase-admin');
const { onDocumentCreated } = require('firebase-functions/v2/firestore');
const { onCall, onRequest, HttpsError } = require('firebase-functions/v2/https');
const { onSchedule } = require('firebase-functions/v2/scheduler');
const { logger } = require('firebase-functions');

admin.initializeApp();
const db = admin.firestore();
const fv = admin.firestore.FieldValue;

function env(name) { return process.env[name] || process.env[`FIREBASE_CONFIG_${name}`] || ''; }
function cents(n) { return Math.round(Number(n || 0) * 100) / 100; }
function addDays(date, days) { const d = new Date(date); d.setUTCDate(d.getUTCDate() + days); return d; }
function sanitizePayload(payload = {}) { const copy = JSON.parse(JSON.stringify(payload || {})); delete copy.card; delete copy.token; delete copy.access_token; delete copy.authorization; delete copy.payer?.identification; return copy; }
async function getSettings() { const snap = await db.doc('adminSettings/global').get(); return snap.exists ? snap.data() : {}; }
async function getPlans() {
  const snap = await db.doc('adminSettings/plans').get();
  return snap.exists ? snap.data() : {
    free: { name: 'Free', priceMonthly: 0, maxDocumentsPerMonth: 20, maxPdfPerMonth: 20, watermark: true, historyLimit: 20, allowLogo: true, allowPublicApproval: false, allowBackupExport: true },
    pro: { name: 'Pro', priceMonthly: 19.90, priceYearly: 199.00, maxDocumentsPerMonth: null, maxPdfPerMonth: null, watermark: false, historyLimit: null, allowLogo: true, allowPublicApproval: true, allowBackupExport: true }
  };
}
async function addEvent(type, payload = {}) { await db.collection('systemEvents').add({ type, severity: payload.severity || 'info', title: payload.title || type, message: payload.message || '', uid: payload.uid || '', userEmail: payload.userEmail || '', metadata: payload.metadata || {}, createdAt: fv.serverTimestamp(), source: 'cloud_function' }); }
async function addAudit(action, uid, before = null, after = null) { await db.collection('auditLogs').add({ uid: 'cloud_function', userEmail: 'cloud-function', action, entityType: 'billing', entityId: uid, before, after, url: 'cloud_functions', createdAt: fv.serverTimestamp() }); }
async function addError(code, error, context = {}) { await db.collection('systemErrors').add({ message: error.message || String(error), stack: error.stack || '', code, severity: 'critical', context, createdAt: fv.serverTimestamp(), resolved: false, source: 'cloud_function' }); }
async function queueFinanceTelegram(type, title, message, payload = {}, severity = 'info') { await db.collection('telegramQueue').add({ id: '', type, title, message, severity, payload, status: 'pending', createdAt: fv.serverTimestamp() }).catch(() => null); }

async function notifyTelegram(message) {
  const token = env('TELEGRAM_BOT_TOKEN');
  if (!token) throw new Error('TELEGRAM_BOT_TOKEN não configurado.');
  const settings = await getSettings();
  const chatId = message.metadata?.telegramChatId || message.payload?.telegramChatId || settings.telegramChatId || env('TELEGRAM_DEFAULT_CHAT_ID') || '7535235489';
  if (!chatId) throw new Error('Chat ID do Telegram não configurado.');
  const text = `${message.title || 'OrçaFácil'}\n\n${message.message || ''}`.slice(0, 3900);
  const res = await fetch(`https://api.telegram.org/bot${token}/sendMessage`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ chat_id: chatId, text, disable_web_page_preview: true }) });
  const data = await res.json().catch(() => ({}));
  if (!res.ok || data.ok === false) throw new Error(data.description || `Telegram HTTP ${res.status}`);
  return data;
}

exports.sendTelegramNotification = onDocumentCreated('telegramQueue/{messageId}', async (event) => {
  const ref = event.data.ref; const message = event.data.data(); if (message.status && message.status !== 'pending') return;
  try { await notifyTelegram(message); await ref.set({ status: 'sent', sentAt: fv.serverTimestamp(), error: '' }, { merge: true }); await addEvent('TELEGRAM_NOTIFICATION_SENT', { severity: 'success', title: 'Telegram enviado', metadata: { queueId: ref.id } }); }
  catch (error) { logger.error('Telegram notification failed', error); await ref.set({ status: 'failed', error: error.message || String(error) }, { merge: true }); await addError('telegram/send-failed', error, { queueId: ref.id, type: message.type }); }
});

exports.createCheckoutPreference = onCall(async (request) => {
  const uid = request.auth?.uid;
  if (!uid) throw new HttpsError('unauthenticated', 'Faça login para assinar o Pro.');
  const { plan = 'pro', billingCycle = 'monthly' } = request.data || {};
  if (plan !== 'pro' || !['monthly', 'yearly'].includes(billingCycle)) throw new HttpsError('invalid-argument', 'Plano ou ciclo inválido.');
  await addEvent('CHECKOUT_CREATE_START', { uid, metadata: { plan, billingCycle } });
  try {
    const userRef = db.doc(`users/${uid}`); const userSnap = await userRef.get();
    if (!userSnap.exists) throw new HttpsError('not-found', 'Usuário não encontrado.');
    const user = userSnap.data(); if (user.isBlocked || user.isActive === false) throw new HttpsError('permission-denied', 'Usuário bloqueado ou inativo.');
    const plans = await getPlans(); const amount = cents(billingCycle === 'yearly' ? plans.pro.priceYearly : plans.pro.priceMonthly);
    const accessToken = env('MERCADO_PAGO_ACCESS_TOKEN'); if (!accessToken) throw new Error('MERCADO_PAGO_ACCESS_TOKEN não configurado.');
    const baseUrl = env('APP_BASE_URL') || 'https://orcafacil-b771c.web.app';
    const externalReference = `${uid}:pro:${billingCycle}:${Date.now()}`;
    const preference = { items: [{ id: `orcafacil-pro-${billingCycle}`, title: `OrçaFácil Pro ${billingCycle === 'yearly' ? 'anual' : 'mensal'}`, quantity: 1, currency_id: 'BRL', unit_price: amount }], payer: { email: user.email || request.auth.token.email || '' }, external_reference: externalReference, metadata: { uid, plan: 'pro', billing_cycle: billingCycle }, back_urls: { success: `${baseUrl}/pagamento-sucesso.html`, pending: `${baseUrl}/pagamento-pendente.html`, failure: `${baseUrl}/pagamento-falha.html` }, auto_return: 'approved', notification_url: `${baseUrl}/mercadoPagoWebhook` };
    const res = await fetch('https://api.mercadopago.com/checkout/preferences', { method: 'POST', headers: { Authorization: `Bearer ${accessToken}`, 'Content-Type': 'application/json' }, body: JSON.stringify(preference) });
    const mp = await res.json(); if (!res.ok) throw new Error(mp.message || `Mercado Pago HTTP ${res.status}`);
    await db.doc(`users/${uid}/billing/subscription`).set({ provider: 'mercadopago', status: 'pending', plan: 'pro', billingCycle, amount, currency: 'BRL', externalPreferenceId: mp.id || '', updatedAt: fv.serverTimestamp(), updatedBy: 'createCheckoutPreference' }, { merge: true });
    await db.doc(`users/${uid}/billing/payments/${mp.id}`).set({ id: mp.id, provider: 'mercadopago', status: 'pending', plan: 'pro', billingCycle, amount, currency: 'BRL', externalPreferenceId: mp.id || '', createdAt: fv.serverTimestamp(), updatedAt: fv.serverTimestamp() }, { merge: true });
    await addEvent('CHECKOUT_CREATE_SUCCESS', { uid, severity: 'success', metadata: { preferenceId: mp.id, billingCycle, amount } });
    return { init_point: mp.init_point, sandbox_init_point: mp.sandbox_init_point, preferenceId: mp.id };
  } catch (error) { await addEvent('CHECKOUT_CREATE_ERROR', { uid, severity: 'error', message: error.message }); await addError('billing/checkout-create-error', error, { uid, plan, billingCycle }); throw error instanceof HttpsError ? error : new HttpsError('internal', 'Não foi possível criar o checkout.'); }
});

async function fetchPayment(paymentId) { const token = env('MERCADO_PAGO_ACCESS_TOKEN'); if (!token) throw new Error('MERCADO_PAGO_ACCESS_TOKEN não configurado.'); const res = await fetch(`https://api.mercadopago.com/v1/payments/${encodeURIComponent(paymentId)}`, { headers: { Authorization: `Bearer ${token}` } }); const data = await res.json(); if (!res.ok) throw new Error(data.message || `Mercado Pago payment HTTP ${res.status}`); return data; }
function uidFromPayment(payment) { return payment.metadata?.uid || String(payment.external_reference || '').split(':')[0] || ''; }
function cycleFromPayment(payment) { return payment.metadata?.billing_cycle || String(payment.external_reference || '').split(':')[2] || 'monthly'; }

exports.mercadoPagoWebhook = onRequest(async (req, res) => {
  const body = req.body || {}; const paymentId = body.data?.id || body.id || req.query['data.id'] || req.query.id || '';
  const webhookRef = db.collection('paymentWebhooks').doc(String(body.id || `${Date.now()}-${paymentId || 'unknown'}`));
  await webhookRef.set({ id: webhookRef.id, provider: 'mercadopago', eventType: body.type || body.action || req.query.type || '', externalPaymentId: String(paymentId || ''), processed: false, status: 'received', receivedAt: fv.serverTimestamp(), rawPayloadSanitized: sanitizePayload(body) }, { merge: true });
  await addEvent('PAYMENT_WEBHOOK_RECEIVED', { metadata: { paymentId } });
  try {
    if (!paymentId) throw new Error('Webhook sem paymentId.');
    const payment = await fetchPayment(paymentId); const uid = uidFromPayment(payment); if (!uid) throw new Error('Pagamento sem uid em metadata/external_reference.');
    const billingCycle = cycleFromPayment(payment); const preferenceId = payment.preference_id || payment.order?.id || ''; const paymentDocId = String(payment.id);
    const payRef = db.doc(`users/${uid}/billing/payments/${paymentDocId}`); const paySnap = await payRef.get();
    if (paySnap.exists && paySnap.data().status === 'approved') { await webhookRef.set({ uid, processed: true, status: 'duplicate_approved', processedAt: fv.serverTimestamp() }, { merge: true }); return res.status(200).send('ok'); }
    const status = payment.status || 'pending'; const amount = cents(payment.transaction_amount || 0);
    await payRef.set({ id: paymentDocId, provider: 'mercadopago', status, plan: 'pro', billingCycle, amount, currency: payment.currency_id || 'BRL', externalPaymentId: String(payment.id), externalPreferenceId: preferenceId, externalMerchantOrderId: String(payment.order?.id || ''), paymentMethod: payment.payment_method_id || payment.payment_type_id || '', payerEmail: payment.payer?.email || '', rawStatus: payment.status || '', rawStatusDetail: payment.status_detail || '', createdAt: payment.date_created ? new Date(payment.date_created) : fv.serverTimestamp(), approvedAt: payment.date_approved ? new Date(payment.date_approved) : null, updatedAt: fv.serverTimestamp() }, { merge: true });
    if (status === 'approved') {
      const now = new Date(); const expiresAt = addDays(now, billingCycle === 'yearly' ? 365 : 30);
      await db.doc(`users/${uid}/billing/subscription`).set({ provider: 'mercadopago', status: 'active', plan: 'pro', billingCycle, amount, currency: payment.currency_id || 'BRL', startedAt: now, expiresAt, lastPaymentAt: payment.date_approved ? new Date(payment.date_approved) : now, nextBillingAt: expiresAt, externalPaymentId: String(payment.id), externalPreferenceId: preferenceId, updatedAt: fv.serverTimestamp(), updatedBy: 'mercadoPagoWebhook' }, { merge: true });
      await db.doc(`users/${uid}`).set({ plan: 'pro', updatedAt: fv.serverTimestamp() }, { merge: true });
      await addAudit('PLAN_UPGRADED_TO_PRO', uid, null, { billingCycle, paymentId: payment.id, expiresAt }); await addEvent('PAYMENT_APPROVED', { uid, severity: 'success', metadata: { paymentId: payment.id, amount } }); await addEvent('SUBSCRIPTION_ACTIVATED', { uid, severity: 'success', metadata: { billingCycle, expiresAt } });
      await queueFinanceTelegram('PAYMENT_APPROVED', '💰 OrçaFácil - Pagamento aprovado', `Usuário: ${payment.payer?.email || uid}\nPlano: Pro ${billingCycle}\nValor: R$ ${amount.toFixed(2).replace('.', ',')}\nStatus: approved`, { uid, paymentId }, 'success');
    } else { await addEvent(status === 'pending' ? 'PAYMENT_PENDING' : 'PAYMENT_REJECTED', { uid, severity: status === 'pending' ? 'info' : 'warning', metadata: { paymentId: payment.id, status } }); }
    await webhookRef.set({ uid, externalPreferenceId: preferenceId, processed: true, status, processedAt: fv.serverTimestamp() }, { merge: true });
    await addEvent('PAYMENT_WEBHOOK_PROCESSED', { uid, severity: 'success', metadata: { paymentId, status } }); res.status(200).send('ok');
  } catch (error) { logger.error('Mercado Pago webhook failed', error); await webhookRef.set({ processed: false, status: 'error', error: error.message || String(error), processedAt: fv.serverTimestamp() }, { merge: true }); await addError('billing/webhook-error', error, { paymentId }); await queueFinanceTelegram('PAYMENT_WEBHOOK_ERROR', '⚠️ OrçaFácil - Erro no webhook', `Pagamento: ${paymentId || '-'}\nErro: ${error.message || error}`, { paymentId }, 'critical'); res.status(200).send('received'); }
});

exports.checkExpiredSubscriptions = onSchedule('every day 03:00', async () => {
  const now = new Date(); const groups = await db.collectionGroup('billing').where('status', '==', 'active').where('expiresAt', '<', now).get();
  for (const snap of groups.docs) {
    if (snap.id !== 'subscription') continue; const uid = snap.ref.parent.parent.id;
    await snap.ref.set({ status: 'expired', plan: 'free', updatedAt: fv.serverTimestamp(), updatedBy: 'checkExpiredSubscriptions' }, { merge: true });
    await db.doc(`users/${uid}`).set({ plan: 'free', updatedAt: fv.serverTimestamp() }, { merge: true });
    await addAudit('SUBSCRIPTION_EXPIRED', uid, { status: 'active' }, { status: 'expired', plan: 'free' }); await addEvent('SUBSCRIPTION_EXPIRED', { uid, severity: 'warning' });
  }
});
