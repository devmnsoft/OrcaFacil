export class PublicQuoteModel {
  constructor(data = {}) {
    this.token = data.token || '';
    this.ownerUid = data.ownerUid || '';
    this.documentId = data.documentId || '';
    this.type = data.type || 'orcamento';
    this.publicEnabled = Boolean(data.publicEnabled);
    this.issuer = data.issuer || {};
    this.client = data.client || {};
    this.document = data.document || {};
    this.decision = data.decision || { status: 'pendente', note: '', decidedAt: null, acceptedTerms: false, evidenceHash: '' };
    this.timeline = Array.isArray(data.timeline) ? data.timeline : [];
    this.createdAt = data.createdAt || '';
    this.updatedAt = data.updatedAt || '';
    this.expiresAt = data.expiresAt || '';
    this.disabledAt = data.disabledAt || '';
    this.lastAccessAt = data.lastAccessAt || '';
    this.viewCount = Number(data.viewCount || 0);
  }

  isAvailable() {
    const expired = this.expiresAt && new Date(this.expiresAt) < new Date(new Date().toISOString().slice(0, 10));
    return Boolean(this.token && this.ownerUid && this.documentId && this.publicEnabled && !this.disabledAt && !expired);
  }
}
