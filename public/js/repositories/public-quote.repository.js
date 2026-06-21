export class PublicQuoteRepository {
  constructor(adapter) { this.adapter = adapter; }
  createOrUpdate(publicQuote) { return this.adapter.createOrUpdatePublicQuote?.(publicQuote); }
  getByToken(token) { return this.adapter.getPublicQuoteByToken?.(token); }
  disable(token) { return this.adapter.disablePublicQuote?.(token); }
}
