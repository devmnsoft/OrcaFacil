const CONTROL = /[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/g;

export function escapeHtml(value = '') {
  return String(value).replace(/[&<>'"]/g, (char) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[char]));
}

export function sanitizeText(value = '', maxLength = 500) {
  return String(value ?? '').replace(CONTROL, '').replace(/[<>]/g, '').trim().slice(0, maxLength);
}

export function sanitizeEmail(value = '') {
  return sanitizeText(value, 254).toLowerCase().replace(/[^a-z0-9.!#$%&'*+/=?^_`{|}~@-]/gi, '');
}

export function sanitizePhone(value = '') {
  return sanitizeText(value, 32).replace(/[^\d()+\-\s.]/g, '');
}

export function sanitizeCurrency(value = 0) {
  const number = Number(String(value).replace(',', '.'));
  return Number.isFinite(number) ? Math.max(0, Math.round(number * 100) / 100) : 0;
}
