import { APP_CONFIG } from '../core/config.js';
export function getWhatsAppSupportUrl(message = 'Olá, preciso de suporte no OrçaFácil.') { return `https://wa.me/${APP_CONFIG.support.whatsappNumber}?text=${encodeURIComponent(message)}`; }
export function getSupportEmailUrl(subject = 'Suporte OrçaFácil') { return `mailto:${APP_CONFIG.support.email}?subject=${encodeURIComponent(subject)}`; }
