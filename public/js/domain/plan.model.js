export class PlanModel {
  constructor({ code = 'free', name = 'Free', priceMonthly = 0, priceYearly = 0, features = [] } = {}) {
    this.code = code;
    this.name = name;
    this.priceMonthly = Number(priceMonthly) || 0;
    this.priceYearly = Number(priceYearly) || 0;
    this.features = features;
  }

  isPro() { return this.code === 'pro'; }
  isFree() { return !this.isPro(); }
}
