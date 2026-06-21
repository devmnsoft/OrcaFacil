export const isValidEmail = (email) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(email || '').trim());
export const hasMinDigits = (value, min) => String(value || '').replace(/\D/g, '').length >= min;
