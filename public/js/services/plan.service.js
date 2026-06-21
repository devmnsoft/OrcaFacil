import { WHATSAPP_MNSOFT } from '../core/config.js';
export class PlanService {
  constructor(source = {}) { this.source = source; }
  getCurrentPlan(userOrProfile = this.source) { return userOrProfile?.plan || 'free'; }
  isFree(userOrProfile = this.source) { return this.getCurrentPlan(userOrProfile) !== 'pro'; }
  isPro(userOrProfile = this.source) { return this.getCurrentPlan(userOrProfile) === 'pro'; }
  getUpgradeWhatsAppLink() { return `https://wa.me/${WHATSAPP_MNSOFT}?text=${encodeURIComponent('Olá, quero ativar o plano Pro do OrçaFácil.')}`; }
}
