export class DocumentItem {
  constructor({ id = '', description = '', qty = 0, unit = 0, discount = 0 } = {}) {
    this.id = id;
    this.description = description;
    this.qty = Number(qty) || 0;
    this.unit = Number(unit) || 0;
    this.discount = Number(discount) || 0;
  }

  getTotal() {
    return Math.max(0, (this.qty * this.unit) - this.discount);
  }
}
