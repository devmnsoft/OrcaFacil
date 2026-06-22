export class PublicQuoteRepository {
  constructor(adapter) { this.adapter = adapter; }
  createOrUpdate(publicQuote) { return this.adapter.createOrUpdatePublicQuote?.(publicQuote); }
  getByToken(token) { return this.adapter.getPublicQuoteByToken?.(token); }
  disable(token) { return this.adapter.disablePublicQuote?.(token); }
  recordView(token, payload) { return this.adapter.recordPublicQuoteView?.(token, payload); }
  decide(token, decision, payload) { return this.adapter.decidePublicQuote?.(token, decision, payload); }
}

