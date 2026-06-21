export class UserRepository {
  constructor(adapter) { this.adapter = adapter; }
  getCurrentUserDocument() { return this.adapter.user || null; }
  ensureUserDocument() { return this.adapter.ensureUserDocument?.(); }
}
