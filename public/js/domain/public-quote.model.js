export class PublicQuoteModel {
  constructor({ token = '', ownerUid = '', documentId = '', publicEnabled = false, createdAt = '', disabledAt = '', lastAccessAt = '' } = {}) {
    this.token = token;
    this.ownerUid = ownerUid;
    this.documentId = documentId;
    this.publicEnabled = Boolean(publicEnabled);
    this.createdAt = createdAt;
    this.disabledAt = disabledAt;
    this.lastAccessAt = lastAccessAt;
  }

  isAvailable() {
    return Boolean(this.token && this.ownerUid && this.documentId && this.publicEnabled && !this.disabledAt);
  }
}
