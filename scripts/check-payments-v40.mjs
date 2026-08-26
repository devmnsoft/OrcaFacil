import fs from 'node:fs';

const files = {
  services: fs.readFileSync('src/OrcaFacil.Application/Payments/PaymentServices.cs', 'utf8'),
  schema: fs.readFileSync('database/patch_sprint39_payments_v40.sql', 'utf8'),
  permissions: fs.readFileSync('src/OrcaFacil.Application/Security/PermissionCodes.cs', 'utf8')
};
const required = [
  'IPaymentProvider', 'IPaymentCheckoutProvider', 'IPaymentWebhookVerifier', 'IPaymentReconciliationProvider',
  'IPixPaymentProvider', 'IBankSlipPaymentProvider', 'ICardPaymentProvider', 'provider_not_configured',
  'FixedTimeEquals', 'TryBeginWebhookAsync', 'ManualPaymentConfirmationService', 'PaymentReconciliationService'
];
for (const token of required) if (!files.services.includes(token)) throw new Error(`Payments V4.0 contract missing: ${token}`);
for (const table of ['payment_providers','payment_checkout_sessions','payment_invoices','payment_transactions','payment_webhook_events','payment_reconciliation_batches','payment_refunds','payment_disputes','payment_audit_events'])
  if (!files.schema.includes(`orcafacil.${table}`)) throw new Error(`Payments V4.0 table missing: ${table}`);
for (const permission of ['Payments.ConfigureProvider','Payments.ManualConfirm','Payments.Reconcile','Payments.Refund','Payments.WebhooksManage','Billing.DunningManage'])
  if (!files.permissions.includes(permission)) throw new Error(`Payments V4.0 permission missing: ${permission}`);
const forbiddenColumns = /\b(card_number|cardnumber|pan|cvv|cvc|security_code)\b/i;
const sqlWithoutComment = files.schema.replace(/--.*$/gm, '').replace(/COMMENT ON[\s\S]*?;/gi, '');
if (forbiddenColumns.test(sqlWithoutComment)) throw new Error('Forbidden card-data column found in payment schema.');
console.log('Payments V4.0 safety contracts: OK');
