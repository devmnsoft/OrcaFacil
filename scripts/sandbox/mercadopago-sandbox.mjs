#!/usr/bin/env node
// mercadopago-sandbox.mjs
// FECHÔ 2.0 — Instrumento de homologação R5: sandbox do Mercado Pago.
//
// Propósito: executar as etapas do runbook docs/SANDBOX-MERCADOPAGO-R5.md contra
// o gateway real (endpoint /api/webhooks/mercadopago + API do Mercado Pago).
// Nenhuma credencial vive no repositório: sem variáveis configuradas o script
// sai com código 2 — nenhum sucesso fictício.
//
// Variáveis de ambiente (definir na sessão, nunca commitar):
//   MERCADOPAGO_ACCESS_TOKEN    token de teste (começa com "TEST-") — exigido por pix/boleto/status
//   MERCADOPAGO_WEBHOOK_SECRET  segredo do webhook           — exigido por webhook
//   MERCADOPAGO_WEBHOOK_URL     endereço da app autenticada  — exigido por webhook
//                               ex.: https://host/api/webhooks/mercadopago
//
// Uso:
//   node scripts/sandbox/mercadopago-sandbox.mjs pix  --amount 24.90 --reference "inv:<uuid>"
//   node scripts/sandbox/mercadopago-sandbox.mjs boleto --amount 49.90 --reference "inv:<uuid>"
//   node scripts/sandbox/mercadopago-sandbox.mjs status --id <payment_id>
//   node scripts/sandbox/mercadopago-sandbox.mjs webhook --id <payment_id> [--type payment] [--action updated]
//   node scripts/sandbox/mercadopago-sandbox.mjs webhook --id <payment_id> --tamper
//
// "webhook" monta e assina a notificação exatamente no formato oficial
// (ts=...,v1=..., manifest id:[data.id];request-id:[x-request-id];ts:[ts];) e a
// envia para a aplicação, que responde com o resultado do processamento
// (recebido, duplicado, status do juízo de liquidação).
//
// --tamper assina com um segredo errado: espera-se HTTP 401 (assinatura
// inválida recusada; nenhum efeito financeiro aplicado).

import { createHmac, randomBytes } from 'node:crypto';

const args = process.argv.slice(2);
const sub = args[0];

const opt = (name) => {
  const i = args.indexOf(`--${name}`);
  return i >= 0 && i + 1 < args.length ? args[i + 1] : undefined;
};

// Flags sem valor (--echo, --tamper).
const has = (name) => args.includes(`--${name}`);

const die = (code, message, hint) => {
  console.error(`[sandbox] ${message}`);
  if (hint) console.error(`[sandbox] ${hint}`);
  process.exit(code);
};

const accessToken = process.env.MERCADOPAGO_ACCESS_TOKEN;
const webhookSecret = process.env.MERCADOPAGO_WEBHOOK_SECRET;
const webhookUrl = process.env.MERCADOPAGO_WEBHOOK_URL;

const API = 'https://api.mercadopago.com';

const requestOptions = (method, token, payload, idempotencyKey) => {
  const headers = { 'Accept': 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  const init = { method, headers };
  if (payload !== undefined) {
    headers['Content-Type'] = 'application/json';
    init.body = JSON.stringify(payload);
  }
  if (idempotencyKey) {
    headers['X-Idempotency-Key'] = idempotencyKey;
    headers['X-Request-Id'] = idempotencyKey;
  }
  return init;
};

const callApi = async (path, init) => {
  const response = await fetch(`${API}${path}`, init);
  const body = await response.text();
  console.log(`[sandbox] ${init?.method ?? 'GET'} ${path} -> HTTP ${response.status}`);
  console.log(body.length > 2000 ? `${body.slice(0, 2000)}... (truncado)` : body);
  if (!response.ok) die(1, 'Chamada ao Mercado Pago falhou.');
  return body;
};

const payer = () => {
  const email = opt('email') ?? 'comprador.sandbox@example.invalid';
  const docType = opt('dtype') ?? 'CPF';
  const docNumber = opt('doc') ?? '19119119100'; // valor sintético aceito pelo sandbox
  return { payment_method: email, document: { type: docType, number: docNumber } };
};

const createPayment = async (kind) => {
  if (!accessToken) die(2, 'MERCADOPAGO_ACCESS_TOKEN ausente (token TEST-).', 'Defina a variável na sessão e tente de novo.');
  if (!accessToken.startsWith('TEST-')) die(2, 'Token não parece ser de teste (TEST-). Abort: sandbox exige token de teste.');
  const amount = Number(opt('amount') ?? '24.90');
  const reference = opt('reference') ?? '';
  const idempotencyKey = opt('idempotency') ?? randomBytes(16).toString('hex');
  if (!(amount > 0) || !reference) die(2, 'Informe --amount (>0) e --reference (inv:).');

  const payload = {
    transaction_amount: amount,
    description: opt('description') ?? 'Fechô cobrança (homologação sandbox)',
    payment_method_id: kind === 'pix' ? 'pix' : 'bolbradesco',
    external_reference: reference,
    payer: { email: payer().email, identification: payer().document },
    idempotency_key: idempotencyKey
  };

  const body = await callApi('/v1/payments', requestOptions('POST', accessToken, payload, idempotencyKey));
  const json = JSON.parse(body);
  if (json?.id) console.log(`\n[sandbox] payment_id = ${json.id}  (use em "status" e "webhook")`);
  if (json?.point_of_interaction?.transaction_data?.qr_code) console.log('[sandbox] cópia e cola PIX: presente e não interpretada');
};

const showStatus = async () => {
  if (!accessToken) die(2, 'MERCADOPAGO_ACCESS_TOKEN ausente.');
  const id = opt('id');
  if (!id) die(2, 'Informe --id <payment_id>.');
  await callApi(`/v1/payments/${id}`, requestOptions('GET', accessToken));
};

const sendWebhook = async () => {
  if (!webhookSecret) die(2, 'MERCADOPAGO_WEBHOOK_SECRET ausente.');
  if (!webhookUrl) die(2, 'MERCADOPAGO_WEBHOOK_URL ausente (ex.: https://host/api/webhooks/mercadopago).');
  const id = opt('id');
  if (!id) die(2, 'Informe --id <payment_id>.');
  const eventType = opt('type') ?? 'payment';
  const action = opt('action') ?? 'updated';
  const keepSecret = !has('tamper');
  const secret = keepSecret ? webhookSecret : 'segredo-errado-propositado';

  const payload = JSON.stringify({ type: eventType, action, data: { id } });
  const ts = Math.floor(Date.now() / 1000).toString();
  const requestId = randomBytes(8).toString('hex');
  // Manifest exatamente como o gateway espera: id:[data.id];request-id:[x-request-id];ts:[ts];
  const manifest = `id:${id};request-id:${requestId};ts:${ts};`;
  const v1 = createHmac('sha256', secret).update(manifest).digest('hex');

  // --echo imprime as partes da assinatura e NÃO envia: permite conferência
  // offline do HMAC (ex.: validador independente recomputando v1).
  if (has('echo')) {
    console.log(JSON.stringify({ payload: JSON.parse(payload), ts, requestId, manifest, v1, webHookSecretUsedIsReal: keepSecret }, null, 2));
    return;
  }

  const response = await fetch(webhookUrl, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'x-signature': `ts=${ts},v1=${v1}`,
      'x-request-id': requestId
    },
    body: payload
  });
  const body = await response.text();
  console.log(`[sandbox] webhook ${eventType} id=${id} tamper=${!keepSecret} -> HTTP ${response.status}`);
  console.log(body);

  if (opt('tamper') && response.status !== 401) {
    die(1, 'Esperava HTTP 401 para assinatura inválida: aplicação aceitou payload não autorizado.');
  }
  if (!response.ok) die(1, 'Webhook recusado pela aplicação.');
};

switch (sub) {
  case 'pix': await createPayment('pix'); break;
  case 'boleto': await createPayment('boleto'); break;
  case 'status': await showStatus(); break;
  case 'webhook': await sendWebhook(); break;
  default:
    die(2, `Subcomando desconhecido: ${sub}`, 'Use pix | boleto | status | webhook.');
}
