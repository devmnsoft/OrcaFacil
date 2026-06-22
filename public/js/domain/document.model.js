import { DocumentItem } from './document-item.model.js';

export class DocumentModel {
  constructor(data = {}) {
    this.id = data.id || '';
    this.type = data.type || 'orcamento';
    this.number = data.number || '';
    this.clientName = data.clientName || '';
    this.clientDocument = data.clientDocument || data.clientDoc || '';
    this.clientPhone = data.clientPhone || data.clientContact || '';
    this.clientEmail = data.clientEmail || '';
    this.clientCity = data.clientCity || '';
    this.issueDate = data.issueDate || data.date || '';
    this.dueDate = data.dueDate || data.validUntil || '';
    this.date = data.date || data.issueDate || '';
    this.validUntil = data.validUntil || data.dueDate || '';
    this.items = (data.items || []).map((item) => item instanceof DocumentItem ? item : new DocumentItem(item));
    this.subtotal = Number(data.subtotal) || 0;
    this.discount = Number(data.discount) || 0;
    this.total = Number(data.total) || 0;
    this.notes = data.notes || '';
    this.status = data.status || 'rascunho';
    this.timeline = Array.isArray(data.timeline) ? data.timeline : [];
    this.convertedReceiptId = data.convertedReceiptId || '';
    this.convertedReceiptNumber = data.convertedReceiptNumber || '';
    this.originBudgetId = data.originBudgetId || '';
    this.originBudgetNumber = data.originBudgetNumber || '';
    this.publicToken = data.publicToken || '';
    this.publicEnabled = Boolean(data.publicEnabled);
    this.clientDecision = data.clientDecision || 'pendente';
    this.clientDecisionAt = data.clientDecisionAt || null;
    this.clientDecisionNote = data.clientDecisionNote || '';
    this.createdAt = data.createdAt || '';
    this.updatedAt = data.updatedAt || '';
    this.calculateTotals();
  }

  calculateTotals() {
    this.subtotal = this.items.reduce((sum, item) => sum + (item.qty * item.unit), 0);
    this.discount = this.items.reduce((sum, item) => sum + item.discount, 0);
    this.total = Math.max(0, this.subtotal - this.discount);
    return { subtotal: this.subtotal, discount: this.discount, total: this.total };
  }

  isBudget() { return this.type === 'orcamento'; }
  isReceipt() { return this.type === 'recibo'; }
  canGeneratePublicLink() { return this.isBudget() && Boolean(this.id || this.number); }

  toFirestore() {
    this.calculateTotals();
    return { ...this, items: this.items.map((item) => ({ ...item })) };
  }

  static fromFirestore(data = {}) {
    return new DocumentModel(data);
  }
}
